using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class Map9PlayModeSmoke
{
    private const string RunningKey = "PvZ.Map9Smoke.Running";
    private const string StageKey = "PvZ.Map9Smoke.Stage";
    private const string ErrorKey = "PvZ.Map9Smoke.Error";

    static Map9PlayModeSmoke()
    {
        if (SessionState.GetBool(RunningKey, false)) Attach();
    }

    [MenuItem("Tools/Validation/Run Map 9 PlayMode Smoke")]
    public static void Run()
    {
        GameSession.SelectedLevel = 8;
        GameSession.SelectedPlants.Clear();
        SessionState.SetBool(RunningKey, true);
        SessionState.SetInt(StageKey, 0);
        SessionState.SetString(ErrorKey, string.Empty);
        Attach();
        EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity");
        EditorApplication.isPlaying = true;
    }

    private static void Attach()
    {
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        Application.logMessageReceived -= CaptureLog;
        Application.logMessageReceived += CaptureLog;
    }

    private static void CaptureLog(string message, string stack, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        if (message.Contains("Shader") || message.Contains("Licensing")) return;
        SessionState.SetString(ErrorKey, SessionState.GetString(ErrorKey, string.Empty)
            + "\n" + message + "\n" + stack);
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(RunningKey, false)) return;
        int stage = SessionState.GetInt(StageKey, 0);
        if (!EditorApplication.isPlaying)
        {
            if (stage < 4) return;
            Finish();
            return;
        }

        try
        {
            if (stage == 0) StartAndValidateCircuit();
            else if (stage == 1) ValidateActiveCircuit();
            else if (stage == 2) ValidateFailureEffects();
            else if (stage == 3) ValidateSuccessEffects();
        }
        catch (Exception exception)
        {
            SessionState.SetString(ErrorKey, SessionState.GetString(ErrorKey, string.Empty)
                + "\n" + exception);
            SessionState.SetInt(StageKey, 4);
            EditorApplication.isPlaying = false;
        }
    }

    private static void StartAndValidateCircuit()
    {
        if (SceneManager.GetActiveScene().name != "GameScene" || GameManagement.levelData == null
            || GameManagement.levelData.level != 8) return;
        EnergyCircuitTemple temple = UnityEngine.Object.FindAnyObjectByType<EnergyCircuitTemple>();
        GameObject hud = GameObject.Find("Energy Circuit HUD");
        if (temple == null || hud == null) return;

        if (GameManagement.levelData.isTestMode || GameManagement.levelData.initialSun != 200)
            throw new InvalidOperationException("Map 9 is not using the production economy configuration.");
        ValidateZombieSchedule();

        SpriteRenderer background = GameObject.Find("Background")?.GetComponent<SpriteRenderer>();
        if (background == null || background.sprite == null || background.sprite.name != "map9")
            throw new InvalidOperationException("Map 9 background was not loaded from the art bundle.");
        GameObject planting = GameObject.Find("Planting Management");
        if (planting == null || planting.GetComponentsInChildren<PlantGrid>(true).Length != 45)
            throw new InvalidOperationException("Map 9 does not contain exactly 45 planting grids.");
        Image panel = hud.transform.Find("Panel")?.GetComponent<Image>();
        if (panel == null || panel.sprite == null || panel.sprite.name != "map9_circuit_hud")
            throw new InvalidOperationException("Map 9 HUD panel art is not active.");
        RectTransform panelRect = panel.rectTransform;
        if (Vector2.Distance(panelRect.anchoredPosition, new Vector2(-150f, -8f)) > .01f
            || Vector2.Distance(panelRect.sizeDelta, new Vector2(214f, 61f)) > .01f)
            throw new InvalidOperationException("Map 9 HUD is not aligned between the plant bank and information button.");
        Image timeline = panel.transform.Find("Timeline")?.GetComponent<Image>();
        if (timeline == null || timeline.sprite == null
            || timeline.sprite.name != "map9_circuit_hud_timeline"
            || timeline.type != Image.Type.Filled)
            throw new InvalidOperationException("Map 9 HUD timeline art is not active.");

        Transform hudGuide = FindTransform("Minh họa HUD Màn 9");
        if (hudGuide == null || hudGuide.GetComponent<Image>()?.sprite?.name != "map9_circuit_hud")
            throw new InvalidOperationException("Map 9 information overlay is missing its HUD guide.");

        Invoke(temple, "StartCircuit");
        SessionState.SetInt(StageKey, 1);
    }

    private static void ValidateActiveCircuit()
    {
        EnergyCircuitTemple temple = UnityEngine.Object.FindAnyObjectByType<EnergyCircuitTemple>();
        if (temple == null) return;
        int nodeCount = 0;
        int linkCount = 0;
        foreach (Transform child in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (child.name.StartsWith("Energy Node "))
            {
                nodeCount++;
                if (Vector2.Distance(child.localPosition, new Vector2(-.05f, -.31f)) > .001f)
                    throw new InvalidOperationException("Map 9 node is not anchored below the plant's feet.");
            }
            else if (child.name == "Energy Link") linkCount++;
        }
        if (nodeCount != 4 || linkCount != 3)
            throw new InvalidOperationException("Map 9 active circuit expected 4 nodes and 3 links, found "
                + nodeCount + " nodes and " + linkCount + " links.");
        Invoke(temple, "FailCircuit");
        SessionState.SetInt(StageKey, 2);
    }

    private static void ValidateFailureEffects()
    {
        int animatedEffects = 0;
        foreach (Transform child in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            if (child.name == "Map 9 Animated Effect") animatedEffects++;
        if (animatedEffects < 4)
            throw new InvalidOperationException("Map 9 overload animation did not spawn on all four nodes.");
        Image icon = GameObject.Find("Status Icon")?.GetComponent<Image>();
        if (icon == null || icon.sprite == null || icon.sprite.name != "map9_status_failure")
            throw new InvalidOperationException("Map 9 failure HUD icon was not applied.");
        EnergyCircuitTemple temple = UnityEngine.Object.FindAnyObjectByType<EnergyCircuitTemple>();
        if (temple == null) return;
        Invoke(temple, "StartCircuit");
        Invoke(temple, "CompleteCircuit");
        SessionState.SetInt(StageKey, 3);
    }

    private static void ValidateSuccessEffects()
    {
        Image icon = GameObject.Find("Status Icon")?.GetComponent<Image>();
        if (icon == null || icon.sprite == null || icon.sprite.name != "map9_status_success")
            throw new InvalidOperationException("Map 9 success HUD icon was not applied.");
        SessionState.SetInt(StageKey, 4);
        EditorApplication.isPlaying = false;
    }

    private static void ValidateZombieSchedule()
    {
        TextAsset asset = Resources.Load<TextAsset>("Json/ZombieData/Level8");
        TimeNodes schedule = asset != null ? JsonUtility.FromJson<TimeNodes>(asset.text) : null;
        if (schedule?.info == null || schedule.info.Count != 19)
            throw new InvalidOperationException("Map 9 zombie schedule must contain 19 timed entries.");

        int totalZombies = 0;
        int finalWaves = 0;
        foreach (TimeNode node in schedule.info)
        {
            totalZombies += node.number;
            if (node.isFinalWave) finalWaves++;
        }
        if (totalZombies != 38 || finalWaves != 1)
            throw new InvalidOperationException("Map 9 zombie schedule must contain 38 zombies and one final wave.");
    }

    private static object Invoke(object target, string method)
    {
        MethodInfo info = target.GetType().GetMethod(method,
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (info == null) throw new MissingMethodException(target.GetType().Name, method);
        return info.Invoke(target, null);
    }

    private static Transform FindTransform(string name)
    {
        foreach (Transform item in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            if (item.name == name) return item;
        return null;
    }

    private static void Finish()
    {
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= CaptureLog;
        string errors = SessionState.GetString(ErrorKey, string.Empty);
        SessionState.EraseBool(RunningKey);
        SessionState.EraseInt(StageKey);
        SessionState.EraseString(ErrorKey);
        if (!string.IsNullOrWhiteSpace(errors))
        {
            Debug.LogError("Map 9 PlayMode smoke test failed:" + errors);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return;
        }
        Debug.Log("Map 9 PlayMode smoke test passed: production balance, schedule, HUD/info, nodes, links, overload and success VFX.");
        if (Application.isBatchMode) EditorApplication.Exit(0);
    }
}
