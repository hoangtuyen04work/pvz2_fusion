using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ImportedPvZPlayModeSmoke
{
    private const string RunningKey = "PvZ.ImportedSmoke.Running";
    private const string StageKey = "PvZ.ImportedSmoke.Stage";
    private const string ErrorKey = "PvZ.ImportedSmoke.Error";

    static ImportedPvZPlayModeSmoke()
    {
        if (SessionState.GetBool(RunningKey, false)) Attach();
    }

    public static void Run()
    {
        GameSession.SelectedPlants.Clear();
        SessionState.SetBool(RunningKey, true);
        SessionState.SetInt(StageKey, 0);
        SessionState.SetString(ErrorKey, string.Empty);
        Attach();
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
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
        if (type != LogType.Exception && type != LogType.Error && type != LogType.Assert) return;
        if (message.Contains("Shader") || message.Contains("Licensing")) return;
        SessionState.SetString(ErrorKey, SessionState.GetString(ErrorKey, string.Empty) + "\n" + message + "\n" + stack);
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(RunningKey, false)) return;
        int stage = SessionState.GetInt(StageKey, 0);

        if (!EditorApplication.isPlaying)
        {
            if (stage < 2) return;
            Finish();
            return;
        }

        try
        {
            if (stage == 0) DriveMainMenu();
            else if (stage == 1) ValidateGameplay();
        }
        catch (Exception exception)
        {
            SessionState.SetString(ErrorKey, SessionState.GetString(ErrorKey, string.Empty) + "\n" + exception);
            SessionState.SetInt(StageKey, 2);
            EditorApplication.isPlaying = false;
        }
    }

    private static void DriveMainMenu()
    {
        MainMenuController menu = UnityEngine.Object.FindAnyObjectByType<MainMenuController>();
        if (menu == null || GameObject.Find("MainMenuCanvas") == null) return;
        Invoke(menu, "SelectLevel", 5);
        Invoke(menu, "PlaySelectedLevel");
        PlantSelectionOverlay overlay = UnityEngine.Object.FindAnyObjectByType<PlantSelectionOverlay>();
        if (overlay == null) throw new InvalidOperationException("Plant selection overlay was not created.");
        if (overlay.GetComponentInChildren<ScrollRect>() == null)
            throw new InvalidOperationException("Plant selection overlay is missing its vertical scroll view.");
        foreach (SeedPacketView packet in overlay.GetComponentsInChildren<SeedPacketView>(true))
            if (packet.name.StartsWith("SunNut", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("SunNut must not appear in the plant selection list.");
        foreach (string key in new[] { "SunFlower", "RepeaterPea", "SnowPea", "CherryBomb", "SunShroom", "Spikeweed" })
            Invoke(overlay, "Toggle", key);
        Invoke(overlay, "Confirm");
        SessionState.SetInt(StageKey, 1);
    }

    private static void ValidateGameplay()
    {
        if (SceneManager.GetActiveScene().name != "GameScene" || GameManagement.levelData == null) return;
        var expected = new List<string> { "SunFlower", "RepeaterPea", "SnowPea", "CherryBomb", "SunShroom", "Spikeweed" };
        if (GameManagement.levelData.plantCards.Count != expected.Count)
            throw new InvalidOperationException("Selected plant count was not transferred to gameplay.");
        for (int i = 0; i < expected.Count; i++)
            if (GameManagement.levelData.plantCards[i] != expected[i])
                throw new InvalidOperationException("Selected plant order was not preserved at index " + i + ".");

        Card[] cards = UnityEngine.Object.FindObjectsByType<Card>(FindObjectsInactive.Include);
        var gameplayCards = new List<Card>();
        foreach (Card card in cards)
            if (card.transform.parent != null && card.transform.parent.name == "Card Group") gameplayCards.Add(card);
        if (gameplayCards.Count != expected.Count)
            throw new InvalidOperationException("Gameplay seed bank does not match selected plant count.");
        foreach (Card card in gameplayCards)
            if (card.myButton == null || card.lowerImage == null || card.GetComponent<LayoutElement>() == null ||
                card.transform.Find("Cost Background/Sun Cost")?.GetComponent<Text>()?.text != card.sunNeeded.ToString())
                throw new InvalidOperationException("A gameplay seed packet is missing unified UI components: " + card.name);

        Text sunText = GameObject.Find("Sun Text")?.GetComponent<Text>();
        if (sunText == null || !sunText.enabled || string.IsNullOrWhiteSpace(sunText.text) ||
            !sunText.gameObject.activeInHierarchy || sunText.maskable ||
            sunText.transform.parent == null || sunText.transform.GetSiblingIndex() != sunText.transform.parent.childCount - 1)
            throw new InvalidOperationException("The total sun counter is not visible in gameplay.");

        GameObject torchA = new GameObject("Smoke Torch A");
        GameObject torchB = new GameObject("Smoke Torch B");
        ImportedProjectile snowProjectile = ImportedProjectile.Create("SnowPea", Vector3.zero, 0, 20, 10f);
        GameObject firstSnowResult = snowProjectile.PassThroughTorchwood(0, 30, torchA);
        if (firstSnowResult != null || snowProjectile.AppliesSlow || snowProjectile.IsFire)
            throw new InvalidOperationException("Snow pea did not thaw correctly at the first Torchwood.");
        GameObject snowFire = snowProjectile.PassThroughTorchwood(0, 30, torchB);
        if (snowFire == null || snowFire.GetComponent<FirePea>() == null ||
            snowFire.GetComponent<StraightBullet>()?.hurt != 30)
            throw new InvalidOperationException("Thawed pea did not become the canonical FirePea at the second Torchwood.");
        if (snowFire != null) UnityEngine.Object.Destroy(snowFire);
        UnityEngine.Object.Destroy(torchA);
        UnityEngine.Object.Destroy(torchB);

        GameObject plant = ImportedPlantRuntime.CreatePlant("RepeaterPea", Vector3.zero, null);
        if (plant?.GetComponent<ImportedPlant>() == null) throw new InvalidOperationException("Could not create imported plant during PlayMode.");
        UnityEngine.Object.Destroy(plant);

        ZombieManagement manager = UnityEngine.Object.FindAnyObjectByType<ZombieManagement>(FindObjectsInactive.Include);
        if (manager == null) throw new InvalidOperationException("Zombie Management was not found in GameScene.");
        if (!manager.SpawnImportedTestZombie("FlagZombie", 0) || !manager.SpawnImportedTestZombie("NewspaperZombie", 1))
            throw new InvalidOperationException("Could not spawn imported zombies during PlayMode.");

        SessionState.SetInt(StageKey, 2);
        EditorApplication.isPlaying = false;
    }

    private static object Invoke(object target, string method, params object[] args)
    {
        MethodInfo info = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
        if (info == null) throw new MissingMethodException(target.GetType().Name, method);
        return info.Invoke(target, args);
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
            Debug.LogError("Imported PvZ PlayMode smoke test failed:" + errors);
            EditorApplication.Exit(1);
        }
        Debug.Log("Imported PvZ PlayMode smoke test passed.");
        EditorApplication.Exit(0);
    }
}
