using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public enum ImportedPlantKind { Shooter, Sun, Bomb, Mine, Chomper, Spikeweed, Hypno }

public sealed class ImportedPlantDefinition
{
    public string key, framePath, attackPath, cardPath;
    public ImportedPlantKind kind;
    public int cost, health, damage, shots;
    public float cooldown, interval, range;
    public int[] rows;
}

public static class ImportedPlantRuntime
{
    private const string Root = "Sprites/Imported/MarbleXu/";
    private static readonly Dictionary<string, ImportedPlantDefinition> Definitions = BuildDefinitions();

    public static bool Supports(string key) => Definitions.ContainsKey(key);

    public static Sprite Preview(string key)
    {
        if (!Definitions.TryGetValue(key, out var d)) return null;
        var sprites = Resources.LoadAll<Sprite>(d.framePath);
        return sprites.OrderBy(s => NaturalIndex(s.name)).FirstOrDefault();
    }

    public static Sprite CardPreview(string key)
    {
        return Definitions.TryGetValue(key, out var definition)
            ? Resources.Load<Sprite>(definition.cardPath)
            : null;
    }

    public static float VisualScale(string key)
    {
        Sprite sprite = Preview(key);
        if (sprite == null || sprite.bounds.size.y <= 0f) return 1f;
        // Legacy plants use roughly 200px canvases at 250 PPU (~0.8 world units).
        return Mathf.Clamp(0.78f / sprite.bounds.size.y, 1f, 3.2f);
    }

    public static GameObject CreatePlant(string key, Vector3 position, Transform parent)
    {
        if (!Definitions.TryGetValue(key, out var d)) return null;
        var go = new GameObject(key);
        go.tag = "Plant";
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = Preview(key);
        float visualScale = VisualScale(key);
        go.transform.localScale = new Vector3(visualScale, visualScale, 1f);
        var collider = go.AddComponent<BoxCollider2D>();
        // Compensate for the visual scale so planting/collision footprints stay unchanged.
        collider.size = new Vector2(0.62f / visualScale, 0.76f / visualScale);
        go.AddComponent<AudioSource>();
        go.AddComponent<Animator>();
        var halo = new GameObject("Halo");
        halo.transform.SetParent(go.transform, false);
        halo.SetActive(false);
        var animator = go.AddComponent<RuntimeFrameAnimator>();
        animator.Configure(renderer, d.framePath, 12f);
        var plant = go.AddComponent<ImportedPlant>();
        plant.Configure(d, animator);
        return go;
    }

    public static Card CreateCard(string key, Transform parent)
    {
        return Definitions.ContainsKey(key) && PlantLoadoutCatalog.TryGet(key, out var entry)
            ? SeedPacketFactory.CreateGameplayCard(entry, parent)
            : null;
    }

    public static int NaturalIndex(string name)
    {
        int end = name.Length - 1;
        while (end >= 0 && char.IsDigit(name[end])) end--;
        return int.TryParse(name.Substring(end + 1), out int value) ? value : 0;
    }

    private static Dictionary<string, ImportedPlantDefinition> BuildDefinitions()
    {
        var map = new Dictionary<string, ImportedPlantDefinition>(StringComparer.OrdinalIgnoreCase);
        Add(map, "RepeaterPea", ImportedPlantKind.Shooter, 200, 300, 20, 2, 7.5f, 1.45f, 99f, "RepeaterPea", "RepeaterPea");
        Add(map, "SnowPea", ImportedPlantKind.Shooter, 175, 300, 20, 1, 7.5f, 1.45f, 99f, "SnowPea", "SnowPea");
        Add(map, "Threepeater", ImportedPlantKind.Shooter, 325, 300, 20, 1, 7.5f, 1.45f, 99f, "Threepeater", "Threepeater", new[]{-1,0,1});
        Add(map, "CherryBomb", ImportedPlantKind.Bomb, 150, 300, 1800, 1, 50f, 0.75f, 1.65f, "CherryBomb", "CherryBomb");
        Add(map, "PotatoMine", ImportedPlantKind.Mine, 25, 300, 1800, 1, 30f, 14f, 0.55f, "PotatoMine/PotatoMineInit", "PotatoMine/PotatoMineExplode");
        Add(map, "Chomper", ImportedPlantKind.Chomper, 150, 300, 1800, 1, 7.5f, 42f, 1.15f, "Chomper/Chomper", "Chomper/ChomperAttack");
        Add(map, "PuffShroom", ImportedPlantKind.Shooter, 0, 300, 20, 1, 7.5f, 1.45f, 3.2f, "PuffShroom/PuffShroom", "PuffShroom/PuffShroom");
        Add(map, "SunShroom", ImportedPlantKind.Sun, 25, 300, 0, 1, 7.5f, 24f, 0f, "SunShroom/SunShroom", "SunShroom/SunShroom");
        Add(map, "ScaredyShroom", ImportedPlantKind.Shooter, 25, 300, 20, 1, 7.5f, 1.45f, 99f, "ScaredyShroom/ScaredyShroom", "ScaredyShroom/ScaredyShroom");
        Add(map, "HypnoShroom", ImportedPlantKind.Hypno, 75, 300, 1800, 1, 30f, 0f, 0f, "HypnoShroom/HypnoShroom", "HypnoShroom/HypnoShroom");
        Add(map, "IceShroom", ImportedPlantKind.Bomb, 75, 300, 20, 1, 50f, 1f, 99f, "IceShroom/IceShroom", "IceShroom/IceShroomSnow");
        Add(map, "Jalapeno", ImportedPlantKind.Bomb, 125, 300, 1800, 1, 50f, 0.8f, -1f, "Jalapeno/Jalapeno", "Jalapeno/JalapenoExplode");
        Add(map, "Spikeweed", ImportedPlantKind.Spikeweed, 100, 300, 20, 1, 7.5f, 1f, 0.8f, "Spikeweed/Spikeweed", "Spikeweed/Spikeweed");
        return map;
    }

    private static void Add(Dictionary<string, ImportedPlantDefinition> map, string key, ImportedPlantKind kind, int cost, int hp, int damage, int shots, float cooldown, float interval, float range, string frames, string attack, int[] rows = null)
    {
        map[key] = new ImportedPlantDefinition
        {
            key=key, kind=kind, cost=cost, health=hp, damage=damage, shots=shots,
            cooldown=cooldown, interval=interval, range=range,
            framePath=Root+"Plants/"+frames, attackPath=Root+"Plants/"+attack,
            cardPath=Root+"Cards/card_"+CardFile(key), rows=rows ?? new[]{0}
        };
    }

    private static string CardFile(string key)
    {
        switch (key) {
            case "RepeaterPea": return "repeaterpea";
            case "Threepeater": return "threepeashooter";
            default: return key.ToLowerInvariant();
        }
    }
}

public sealed class RuntimeFrameAnimator : MonoBehaviour
{
    private SpriteRenderer renderer;
    private Sprite[] frames = Array.Empty<Sprite>();
    private float fps = 12f, elapsed;
    public void Configure(SpriteRenderer target, string path, float rate) { renderer=target; fps=rate; SetFrames(path); }
    public void SetFrames(string path) { frames=Resources.LoadAll<Sprite>(path).OrderBy(s=>ImportedPlantRuntime.NaturalIndex(s.name)).ToArray(); elapsed=0f; if(frames.Length>0) renderer.sprite=frames[0]; }
    private void Update() { if(frames.Length<2) return; elapsed += Time.deltaTime; renderer.sprite=frames[Mathf.FloorToInt(elapsed*fps)%frames.Length]; }
}

public sealed class ImportedPlant : Plant
{
    private const float LawnRightEdge = 5.3f;
    private ImportedPlantDefinition definition;
    private RuntimeFrameAnimator frameAnimator;
    private float nextAction;
    private bool armed;

    public void Configure(ImportedPlantDefinition value, RuntimeFrameAnimator animator)
    {
        definition=value; frameAnimator=animator; bloodVolume=value.health;
    }

    protected override void Start()
    {
        base.Start();
        nextAction = Time.time + definition.interval;
        if(definition.kind==ImportedPlantKind.Bomb) Invoke(nameof(TriggerBomb), definition.interval);
        if(definition.kind==ImportedPlantKind.Mine) Invoke(nameof(ArmMine), definition.interval);
    }

    private void Update()
    {
        if(definition==null || Time.time<nextAction) return;
        switch(definition.kind)
        {
            case ImportedPlantKind.Shooter: ShootIfPossible(); break;
            case ImportedPlantKind.Sun: CreateSun(); break;
            case ImportedPlantKind.Chomper: ChompIfPossible(); break;
            case ImportedPlantKind.Spikeweed: DamageNearby(); break;
            case ImportedPlantKind.Mine: if(armed) TriggerMine(); break;
        }
    }

    private List<Zombie> Targets(int targetRow, float range)
    {
        return FindObjectsByType<Zombie>()
            .Where(z=>z.gameObject.activeInHierarchy && z.enabled && z.bloodVolume>0 &&
                z.pos_row==targetRow && z.transform.position.x>=transform.position.x-0.25f &&
                z.transform.position.x<=LawnRightEdge && z.transform.position.x-transform.position.x<=range)
            .OrderBy(z=>z.transform.position.x).ToList();
    }

    public bool HasTargetInLane(int targetRow, float range)
    {
        return Targets(targetRow, range).Count > 0;
    }

    private void ShootIfPossible()
    {
        if(definition.key=="ScaredyShroom" && Targets(row,1.5f).Count>0) { nextAction=Time.time+0.25f; return; }
        bool fired=false;
        int rowCount=GameManagement.levelData!=null ? GameManagement.levelData.rowCount : 5;
        foreach(int offset in definition.rows)
        {
            int targetRow=row+offset;
            if(targetRow<0 || targetRow>=rowCount || Targets(targetRow,definition.range).Count==0) continue;
            for(int shot=0;shot<definition.shots;shot++) SpawnProjectile(targetRow, shot*0.16f);
            fired=true;
        }
        nextAction=Time.time+(fired?definition.interval:0.25f);
    }

    private void SpawnProjectile(int targetRow, float delay)
    {
        ImportedProjectile.Create(
            definition.key,
            transform.position+new Vector3(0.42f,0.15f,0f),
            targetRow,
            definition.damage,
            delay);
    }

    private void CreateSun()
    {
        var prefab=Resources.Load<GameObject>("Prefabs/Sun/FlowerSun");
        var manager=GameObject.Find("Sun Management");
        if(prefab!=null && manager!=null) Instantiate(prefab,transform.position,Quaternion.identity,manager.transform);
        nextAction=Time.time+definition.interval;
    }

    private void ChompIfPossible()
    {
        var target=Targets(row,definition.range).FirstOrDefault();
        if(target==null) { nextAction=Time.time+0.2f; return; }
        target.beAttacked(definition.damage); frameAnimator.SetFrames(definition.attackPath); Invoke(nameof(ResetIdleFrames), 1f); nextAction=Time.time+definition.interval;
    }

    private void DamageNearby()
    {
        foreach(var zombie in Targets(row,definition.range)) zombie.beAttacked(definition.damage);
        nextAction=Time.time+definition.interval;
    }

    private void ArmMine() { armed=true; frameAnimator.SetFrames("Sprites/Imported/MarbleXu/Plants/PotatoMine/PotatoMine"); nextAction=Time.time; }
    private void TriggerMine()
    {
        List<Zombie> targets=Targets(row,definition.range);
        if(targets.Count==0) { nextAction=Time.time+0.1f; return; }
        foreach(Zombie target in targets) target.beAttacked(definition.damage);
        die("");
    }

    private void TriggerBomb()
    {
        var all=FindObjectsByType<Zombie>();
        foreach(var zombie in all)
        {
            if(!zombie.gameObject.activeInHierarchy || !zombie.enabled || zombie.bloodVolume<=0) continue;
            bool hit=definition.key=="IceShroom" || (definition.key=="Jalapeno" ? zombie.pos_row==row : Vector2.Distance(zombie.transform.position,transform.position)<=definition.range);
            if(!hit) continue;
            zombie.beAttacked(definition.damage);
            if(definition.key=="IceShroom") zombie.ApplySlow(0.5f,10f);
        }
        frameAnimator.SetFrames(definition.attackPath);
        Invoke(nameof(FinishBomb),0.35f);
    }
    private void FinishBomb(){die("");}
    private void ResetIdleFrames(){ if(definition!=null) frameAnimator.SetFrames(definition.framePath); }

    public bool OnBitten(Zombie attacker)
    {
        if(definition==null || definition.kind!=ImportedPlantKind.Hypno) return false;
        attacker.beAttacked(definition.damage); die(""); return true;
    }
}

public sealed class ImportedProjectile : MonoBehaviour
{
    private const float Speed = 4f;
    private const float VisualScale = 1.55f;
    private readonly HashSet<UnityEngine.Object> processedIgniters = new HashSet<UnityEngine.Object>();
    private int row, damage;
    private bool slow, fire, canIgnite;
    private float startAt;
    private SpriteRenderer spriteRenderer;

    public int Row => row;
    public int Damage => damage;
    public bool AppliesSlow => slow;
    public bool IsFire => fire;
    public bool CanIgnite => canIgnite;

    public static ImportedProjectile Create(string sourceKey, Vector3 position, int targetRow, int hurt, float delay)
    {
        GameObject bullet=new GameObject(sourceKey+" Projectile");
        bullet.transform.position=position;
        bullet.transform.localScale=new Vector3(VisualScale,VisualScale,1f);

        SpriteRenderer renderer=bullet.AddComponent<SpriteRenderer>();
        bool mushroom=sourceKey=="PuffShroom" || sourceKey=="ScaredyShroom";
        string spritePath=sourceKey=="SnowPea"
            ? "Sprites/Imported/MarbleXu/Bullets/PeaIce/PeaIce_0"
            : mushroom
                ? "Sprites/Imported/MarbleXu/Bullets/BulletMushRoom/BulletMushRoom_0"
                : "Sprites/Imported/MarbleXu/Bullets/PeaNormal/PeaNormal_0";
        renderer.sprite=Resources.Load<Sprite>(spritePath);
        renderer.sortingLayerName="PlantBullet";

        CircleCollider2D collider=bullet.AddComponent<CircleCollider2D>();
        collider.isTrigger=true;
        collider.radius=0.10f;
        Rigidbody2D body=bullet.AddComponent<Rigidbody2D>();
        body.bodyType=RigidbodyType2D.Kinematic;
        body.gravityScale=0f;
        body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;

        ImportedProjectile projectile=bullet.AddComponent<ImportedProjectile>();
        projectile.Configure(targetRow,hurt,sourceKey=="SnowPea",!mushroom,delay,renderer);
        if(projectile.canIgnite) bullet.tag="Pea";
        return projectile;
    }

    private void Configure(int targetRow,int hurt,bool appliesSlow,bool isPea,float delay,SpriteRenderer renderer)
    {
        row=targetRow; damage=hurt; slow=appliesSlow; canIgnite=isPea;
        startAt=Time.time+delay; spriteRenderer=renderer;
    }

    public void PassThroughTorchwood(int torchwoodRow, int fireDamage, UnityEngine.Object igniter)
    {
        if(!canIgnite || row!=torchwoodRow || igniter==null || !processedIgniters.Add(igniter)) return;
        if(slow)
        {
            slow=false;
            spriteRenderer.sprite=Resources.Load<Sprite>("Sprites/Imported/MarbleXu/Bullets/PeaNormal/PeaNormal_0");
            spriteRenderer.color=Color.white;
            return;
        }
        fire=true;
        damage=Mathf.Max(damage,fireDamage);
        spriteRenderer.sprite=Resources.Load<Sprite>("Sprites/Imported/MarbleXu/Bullets/PeaNormal/PeaNormal_0");
        spriteRenderer.color=new Color(1f,0.34f,0.06f,1f);
        gameObject.tag="Untagged";
    }

    private void Update()
    {
        if(Time.time<startAt) return;
        transform.Translate(Speed*Time.deltaTime,0f,0f);
        Zombie target=FindObjectsByType<Zombie>()
            .Where(z=>z.pos_row==row && z.gameObject.activeInHierarchy && z.enabled && z.bloodVolume>0 &&
                Vector2.Distance(transform.position,z.transform.position)<=0.28f)
            .OrderBy(z=>Mathf.Abs(z.transform.position.x-transform.position.x))
            .FirstOrDefault();
        if(target!=null)
        {
            target.playAudioOfBeingAttacked();
            target.beAttacked(damage);
            if(slow) target.ApplySlow(0.5f,4f);
            if(fire) target.beBurned();
            Destroy(gameObject);
            return;
        }
        if(transform.position.x>7f) Destroy(gameObject);
    }
}
