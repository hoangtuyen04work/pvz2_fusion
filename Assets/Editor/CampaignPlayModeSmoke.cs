using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Smoke test scene bootstrap, ba loại AI, lưu map và các màn điều hướng.</summary>
[InitializeOnLoad]
public static class CampaignPlayModeSmoke
{
    private const string Running = "ThreeWorlds.Smoke.Running";
    private const string Stage = "ThreeWorlds.Smoke.Stage";
    private const string Errors = "ThreeWorlds.Smoke.Errors";

    static CampaignPlayModeSmoke()
    {
        if (SessionState.GetBool(Running, false)) Attach();
    }

    public static void Run()
    {
        CampaignSceneBuilder.CreateScenes();
        SessionState.SetBool(Running, true);
        SessionState.SetInt(Stage, 0);
        SessionState.SetString(Errors, string.Empty);
        Attach();
        EditorSceneManager.OpenScene("Assets/Scenes/CampaignScene.unity");
        EditorApplication.isPlaying = true;
    }

    private static void Attach()
    {
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        Application.logMessageReceived -= Capture;
        Application.logMessageReceived += Capture;
    }

    private static void Capture(string message, string stack, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        if (message.Contains("Shader") || message.Contains("Licensing")) return;
        SessionState.SetString(Errors, SessionState.GetString(Errors, string.Empty) + "\n" + message + "\n" + stack);
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(Running, false)) return;
        int stage = SessionState.GetInt(Stage, 0);
        if (!EditorApplication.isPlaying)
        {
            if (stage >= 4) Finish();
            return;
        }

        try
        {
            if (stage == 0) ValidateGame();
            else if (stage == 1) ValidateSettings();
            else if (stage == 2) ValidateProgress();
            else if (stage == 3) ValidateAchievements();
        }
        catch (Exception exception)
        {
            SessionState.SetString(Errors, SessionState.GetString(Errors, string.Empty) + "\n" + exception);
            SessionState.SetInt(Stage, 4);
            EditorApplication.isPlaying = false;
        }
    }

    private static void ValidateGame()
    {
        if (SceneManager.GetActiveScene().name != CampaignBootstrap.GameScene) return;
        CampaignGame game = UnityEngine.Object.FindAnyObjectByType<CampaignGame>();
        if (game == null || !game.IsPlaying) return;
        if (GameObject.Find("Three Worlds Canvas") == null) throw new InvalidOperationException("Campaign HUD is missing.");
        CampaignPlayer player = UnityEngine.Object.FindAnyObjectByType<CampaignPlayer>();
        if (player == null) throw new InvalidOperationException("Campaign player is missing.");
        ValidateCharacterSprite(player.gameObject, "Campaign player");
        if (CampaignGame.SpawnKindFor(0, 20) != CampaignNpcKind.Scout ||
            CampaignGame.SpawnKindFor(1, 20) != CampaignNpcKind.Guardian ||
            CampaignGame.SpawnKindFor(2, 0) != CampaignNpcKind.Scout ||
            CampaignGame.SpawnKindFor(2, 1) != CampaignNpcKind.Guardian ||
            CampaignGame.SpawnKindFor(2, 2) != CampaignNpcKind.Healer)
            throw new InvalidOperationException("Map-specific NPC spawn rules are incorrect.");
        for (int i = 1; i <= 3; i++)
            if (Resources.Load<Sprite>("aset/map/map" + i) == null)
                throw new InvalidOperationException("Map background is missing: map" + i);
        if (Resources.Load<Sprite>("aset/Attack_Player/Arrow") == null)
            throw new InvalidOperationException("Player projectile sprite is missing.");

        MethodInfo spawn = typeof(CampaignGame).GetMethod("SpawnNpc", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo map = typeof(CampaignGame).GetField("currentMap", BindingFlags.Instance | BindingFlags.NonPublic);
        map.SetValue(game, 2);
        spawn.Invoke(game, new object[] { 0 });
        spawn.Invoke(game, new object[] { 1 });
        spawn.Invoke(game, new object[] { 2 });
        ScoutNpc scout = UnityEngine.Object.FindAnyObjectByType<ScoutNpc>();
        GuardianNpc guardian = UnityEngine.Object.FindAnyObjectByType<GuardianNpc>();
        HealerNpc healer = UnityEngine.Object.FindAnyObjectByType<HealerNpc>();
        if (scout == null || guardian == null || healer == null)
            throw new InvalidOperationException("One or more smart NPC types were not created.");
        ValidateCharacterSprite(scout.gameObject, "Scout NPC");
        ValidateCharacterSprite(guardian.gameObject, "Guardian NPC");
        ValidateCharacterSprite(healer.gameObject, "Healer NPC");

        MethodInfo result = typeof(CampaignGame).GetMethod("BuildResultContents", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo overlayField = typeof(CampaignGame).GetField("resultOverlay", BindingFlags.Instance | BindingFlags.NonPublic);
        result.Invoke(game, new object[] { false });
        GameObject overlay = (GameObject)overlayField.GetValue(game);
        if (overlay.GetComponentsInChildren<UnityEngine.UI.Button>(true).Length != 3)
            throw new InvalidOperationException("Game Over does not expose exactly three navigation buttons.");

        SessionState.SetInt(Stage, 1);
        SceneManager.LoadScene(CampaignBootstrap.SettingsScene);
    }

    private static void ValidateCharacterSprite(GameObject character, string label)
    {
        SpriteRenderer renderer = character.GetComponent<SpriteRenderer>();
        if (renderer == null || renderer.sprite == null)
            throw new InvalidOperationException(label + " did not load a sprite from Resources/aset.");
        bool isPlayer = character.GetComponent<CampaignPlayer>() != null;
        if (isPlayer && character.GetComponent<CampaignPlayerAnimator>() == null)
            throw new InvalidOperationException(label + " is missing CampaignPlayerAnimator.");
        if (!isPlayer && character.GetComponent<CampaignCharacterAnimator>() == null)
            throw new InvalidOperationException(label + " is missing CampaignCharacterAnimator.");
    }

    private static void ValidateSettings()
    {
        if (SceneManager.GetActiveScene().name != CampaignBootstrap.SettingsScene) return;
        if (UnityEngine.Object.FindAnyObjectByType<CampaignInfoScreen>() == null)
            throw new InvalidOperationException("Settings scene did not bootstrap its independent screen.");
        if (GameObject.Find("Campaign Screen Canvas") == null)
            throw new InvalidOperationException("Settings canvas is missing.");
        SessionState.SetInt(Stage, 2);
        SceneManager.LoadScene(CampaignBootstrap.ProgressScene);
    }

    private static void ValidateProgress()
    {
        if (SceneManager.GetActiveScene().name != CampaignBootstrap.ProgressScene) return;
        if (UnityEngine.Object.FindAnyObjectByType<CampaignInfoScreen>() == null || GameObject.Find("Map 1") == null)
            throw new InvalidOperationException("Progress scene did not render map history.");
        SessionState.SetInt(Stage, 3);
        SceneManager.LoadScene(CampaignBootstrap.AchievementsScene);
    }

    private static void ValidateAchievements()
    {
        if (SceneManager.GetActiveScene().name != CampaignBootstrap.AchievementsScene) return;
        if (UnityEngine.Object.FindAnyObjectByType<CampaignInfoScreen>() == null || GameObject.Find("VỆ BINH BA CÕI") == null)
            throw new InvalidOperationException("Achievements scene did not render campaign badges.");
        SessionState.SetInt(Stage, 4);
        EditorApplication.isPlaying = false;
    }

    private static void Finish()
    {
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= Capture;
        string errors = SessionState.GetString(Errors, string.Empty);
        SessionState.EraseBool(Running);
        SessionState.EraseInt(Stage);
        SessionState.EraseString(Errors);
        if (!string.IsNullOrWhiteSpace(errors))
        {
            Debug.LogError("Three Worlds campaign smoke test failed:" + errors);
            EditorApplication.Exit(1);
            return;
        }
        Debug.Log("Three Worlds campaign smoke test passed.");
        EditorApplication.Exit(0);
    }
}
