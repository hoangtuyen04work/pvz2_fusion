using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Sinh AnimationClip, AnimatorController, Definition và Prefab từ sprite đã cắt sẵn.
/// Thư mục Assets/Generated/AsetEntities là đầu ra có thể tái tạo bất kỳ lúc nào.
/// </summary>
public static class AsetEntityGenerator
{
    private const string OutputRoot = "Assets/Generated/AsetEntities";

    [MenuItem("Tools/Aset Pipeline/2. Tạo Animation và Entity Prefab")]
    public static void Generate()
    {
        AsetSpritePipeline.EnsureFolder(OutputRoot);

        // Bộ asset chỉ có Goblin nên Female được dùng làm Defender mẫu;
        // có thể đổi sourcePack sau này mà không ảnh hưởng lớp runtime.
        BuildEntity(new EntityRecipe("GoblinDefender", "Female Goblin", AsetFaction.Defender,
            150f, 0f, 22f, 1.55f, 0.9f));
        BuildEntity(new EntityRecipe("GoblinEnemy", "Male Goblin", AsetFaction.Enemy,
            110f, 1.35f, 14f, 0.9f, 1.05f));
        BuildEntity(new EntityRecipe("GoblinChiefEnemy", "Chief Goblin", AsetFaction.Enemy,
            320f, 0.82f, 30f, 1.15f, 1.35f));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Aset Pipeline] Đã tạo 3 prefab tại " + OutputRoot);
    }

    [MenuItem("Tools/Aset Pipeline/0. Chạy toàn bộ Pipeline")]
    public static void RunAll()
    {
        AsetSpritePipeline.AnalyzeAndConfigure();
        Generate();
        ValidateGeneratedAssets();
    }

    [MenuItem("Tools/Aset Pipeline/3. Kiểm tra kết quả")]
    public static void ValidateGeneratedAssets()
    {
        string[] prefabs =
        {
            OutputRoot + "/GoblinDefender/GoblinDefender.prefab",
            OutputRoot + "/GoblinEnemy/GoblinEnemy.prefab",
            OutputRoot + "/GoblinChiefEnemy/GoblinChiefEnemy.prefab"
        };
        foreach (string path in prefabs)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null || prefab.GetComponent<AsetEntity2D>() == null ||
                prefab.GetComponent<Animator>() == null || prefab.GetComponent<Collider2D>() == null)
                throw new InvalidOperationException("Prefab thiếu hoặc chưa đủ component: " + path);
        }

        string[] sheets = AssetDatabase.FindAssets("t:Texture2D", new[] { AsetSpritePostprocessor.Root.TrimEnd('/') })
            .Select(AssetDatabase.GUIDToAssetPath).Where(AsetSpritePostprocessor.IsSpriteSheet).ToArray();
        foreach (string sheet in sheets)
        {
            int expected = ExpectedFrameCount(Path.GetFileNameWithoutExtension(sheet));
            int actual = AssetDatabase.LoadAllAssetRepresentationsAtPath(sheet).OfType<Sprite>().Count();
            if (expected > 0 && actual != expected)
                throw new InvalidOperationException($"Sai số frame: {sheet}; cần {expected}, nhận {actual}.");
        }
        Debug.Log("[Aset Pipeline] Validation passed: " + sheets.Length + " spritesheet và 3 prefab hợp lệ.");
    }

    private static int ExpectedFrameCount(string name)
    {
        if (name.IndexOf("Walking", StringComparison.OrdinalIgnoreCase) >= 0) return 20;
        if (name.IndexOf("Running", StringComparison.OrdinalIgnoreCase) >= 0) return 12;
        if (name.IndexOf("Idle", StringComparison.OrdinalIgnoreCase) >= 0) return 16;
        if (name.IndexOf("Attacking", StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("Hurt", StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.Equals("Dying", StringComparison.OrdinalIgnoreCase)) return 10;
        return 0;
    }

    private static void BuildEntity(EntityRecipe recipe)
    {
        string entityFolder = OutputRoot + "/" + recipe.assetName;
        AsetSpritePipeline.EnsureFolder(entityFolder);
        string sheetRoot = AsetSpritePostprocessor.Root + recipe.sourcePack + "/PNG/Spritesheets/";

        AnimationClip idle = CreateClip(sheetRoot + "Left - Idle.png", entityFolder + "/Idle.anim", 12f, true);
        AnimationClip move = CreateClip(sheetRoot + "Left - Walking.png", entityFolder + "/Move.anim", 16f, true);
        AnimationClip attack = CreateClip(sheetRoot + "Left - Attacking.png", entityFolder + "/Attack.anim", 18f, false);
        AnimationClip hurt = CreateClip(sheetRoot + "Left - Hurt.png", entityFolder + "/Hurt.anim", 18f, false);
        AnimationClip die = CreateClip(sheetRoot + "Dying.png", entityFolder + "/Die.anim", 14f, false);

        if (idle == null || move == null || attack == null || hurt == null || die == null)
            throw new InvalidOperationException("Thiếu sprite đã cắt của " + recipe.sourcePack + ". Hãy chạy bước 1 trước.");

        AnimatorController controller = CreateController(entityFolder + "/Animator.controller", idle, move, attack, hurt, die);
        AsetEntityDefinition definition = CreateDefinition(entityFolder + "/Definition.asset", recipe);
        CreatePrefab(entityFolder + "/" + recipe.assetName + ".prefab", recipe, definition, controller, FirstSprite(idle));
    }

    private static AnimationClip CreateClip(string sheetPath, string outputPath, float frameRate, bool loop)
    {
        Sprite[] frames = AssetDatabase.LoadAllAssetRepresentationsAtPath(sheetPath)
            .OfType<Sprite>().OrderBy(sprite => sprite.name, StringComparer.Ordinal).ToArray();
        if (frames.Length == 0) return null;

        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(outputPath);
        if (clip == null)
        {
            clip = new AnimationClip { name = Path.GetFileNameWithoutExtension(outputPath) };
            AssetDatabase.CreateAsset(clip, outputPath);
        }
        clip.frameRate = frameRate;
        var keyframes = new ObjectReferenceKeyframe[frames.Length + (loop ? 1 : 0)];
        for (int i = 0; i < frames.Length; i++)
            keyframes[i] = new ObjectReferenceKeyframe { time = i / frameRate, value = frames[i] };
        if (loop)
            keyframes[keyframes.Length - 1] = new ObjectReferenceKeyframe { time = frames.Length / frameRate, value = frames[0] };

        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static AnimatorController CreateController(string path, AnimationClip idle, AnimationClip move,
        AnimationClip attack, AnimationClip hurt, AnimationClip die)
    {
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(path) != null) AssetDatabase.DeleteAsset(path);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("Moving", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState idleState = machine.AddState("Idle");
        AnimatorState moveState = machine.AddState("Move");
        AnimatorState attackState = machine.AddState("Attack");
        AnimatorState hurtState = machine.AddState("Hurt");
        AnimatorState dieState = machine.AddState("Die");
        idleState.motion = idle;
        moveState.motion = move;
        attackState.motion = attack;
        hurtState.motion = hurt;
        dieState.motion = die;
        machine.defaultState = idleState;

        AddBoolTransition(idleState, moveState, "Moving", true);
        AddBoolTransition(moveState, idleState, "Moving", false);
        AddTriggerTransition(machine, attackState, "Attack");
        AddTriggerTransition(machine, hurtState, "Hurt");
        AddTriggerTransition(machine, dieState, "Die");
        AddExitTransition(attackState, idleState);
        AddExitTransition(hurtState, idleState);
        return controller;
    }

    private static void AddBoolTransition(AnimatorState from, AnimatorState to, string parameter, bool value)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.08f;
        transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parameter);
    }

    private static void AddTriggerTransition(AnimatorStateMachine machine, AnimatorState to, string parameter)
    {
        AnimatorStateTransition transition = machine.AddAnyStateTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.04f;
        transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
    }

    private static void AddExitTransition(AnimatorState from, AnimatorState to)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = true;
        transition.exitTime = 0.92f;
        transition.duration = 0.06f;
    }

    private static AsetEntityDefinition CreateDefinition(string path, EntityRecipe recipe)
    {
        AsetEntityDefinition definition = AssetDatabase.LoadAssetAtPath<AsetEntityDefinition>(path);
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<AsetEntityDefinition>();
            AssetDatabase.CreateAsset(definition, path);
        }
        definition.displayName = recipe.assetName;
        definition.faction = recipe.faction;
        definition.maximumHealth = recipe.health;
        definition.movementSpeed = recipe.speed;
        definition.attackDamage = recipe.damage;
        definition.attackRange = recipe.range;
        definition.attackInterval = recipe.interval;
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static void CreatePrefab(string path, EntityRecipe recipe, AsetEntityDefinition definition,
        RuntimeAnimatorController controller, Sprite firstSprite)
    {
        var root = new GameObject(recipe.assetName, typeof(SpriteRenderer), typeof(Animator),
            typeof(CapsuleCollider2D), typeof(Rigidbody2D));
        try
        {
            SpriteRenderer renderer = root.GetComponent<SpriteRenderer>();
            renderer.sprite = firstSprite;
            renderer.sortingOrder = recipe.faction == AsetFaction.Defender ? 20 : 15;
            root.GetComponent<Animator>().runtimeAnimatorController = controller;
            CapsuleCollider2D collider = root.GetComponent<CapsuleCollider2D>();
            collider.size = new Vector2(0.48f, 0.82f);
            collider.offset = new Vector2(0f, 0.42f);
            Rigidbody2D rigidbody = root.GetComponent<Rigidbody2D>();
            rigidbody.gravityScale = 0f;
            rigidbody.freezeRotation = true;
            rigidbody.bodyType = RigidbodyType2D.Kinematic;

            AsetEntity2D entity = recipe.faction == AsetFaction.Defender
                ? (AsetEntity2D)root.AddComponent<AsetDefender>()
                : root.AddComponent<AsetEnemy>();
            SerializedObject serialized = new SerializedObject(entity);
            serialized.FindProperty("definition").objectReferenceValue = definition;
            serialized.FindProperty("body").objectReferenceValue = renderer;
            serialized.FindProperty("animator").objectReferenceValue = root.GetComponent<Animator>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            root.transform.localScale = Vector3.one * recipe.visualScale;
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }

    private static Sprite FirstSprite(AnimationClip clip)
    {
        EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
        if (bindings.Length == 0) return null;
        ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(clip, bindings[0]);
        return keys.Length > 0 ? keys[0].value as Sprite : null;
    }

    private readonly struct EntityRecipe
    {
        public readonly string assetName;
        public readonly string sourcePack;
        public readonly AsetFaction faction;
        public readonly float health;
        public readonly float speed;
        public readonly float damage;
        public readonly float range;
        public readonly float interval;
        public readonly float visualScale;

        public EntityRecipe(string assetName, string sourcePack, AsetFaction faction,
            float health, float speed, float damage, float range, float interval, float visualScale = 1f)
        {
            this.assetName = assetName;
            this.sourcePack = sourcePack;
            this.faction = faction;
            this.health = health;
            this.speed = speed;
            this.damage = damage;
            this.range = range;
            this.interval = interval;
            this.visualScale = visualScale;
        }
    }
}

/// <summary>
/// Tự chạy đúng một lần khi project đã import xong thư mục aset.
/// Nếu đầu ra đã tồn tại thì không làm lại, tránh làm chậm mỗi lần mở Unity.
/// </summary>
[InitializeOnLoad]
public static class AsetPipelineAutoSetup
{
    private const string ExpectedPrefab = "Assets/Generated/AsetEntities/GoblinDefender/GoblinDefender.prefab";
    private const string NewContentSessionKey = "AsetPipeline.AttackPlayerAndMaps.v2";

    static AsetPipelineAutoSetup()
    {
        if (!Application.isBatchMode) EditorApplication.delayCall += TryGenerate;
    }

    private static void TryGenerate()
    {
        if (!AssetDatabase.IsValidFolder(AsetSpritePostprocessor.Root.TrimEnd('/'))) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += TryGenerate;
            return;
        }

        try
        {
            // Reimport một lần trong phiên làm việc để các asset Player/Map vừa thêm
            // chắc chắn nhận đúng grid trước khi Play Mode bắt đầu.
            if (!SessionState.GetBool(NewContentSessionKey, false))
            {
                SessionState.SetBool(NewContentSessionKey, true);
                AsetSpritePipeline.AnalyzeAndConfigure();
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(ExpectedPrefab) == null)
            {
                Debug.Log("[Aset Pipeline] Bắt đầu thiết lập prefab tự động lần đầu...");
                AsetEntityGenerator.Generate();
                AsetEntityGenerator.ValidateGeneratedAssets();
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }
}
