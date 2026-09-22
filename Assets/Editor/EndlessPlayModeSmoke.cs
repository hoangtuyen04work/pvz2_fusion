using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Smoke test dòng chính của Endless, chạy được từ command line.</summary>
[InitializeOnLoad]
public static class EndlessPlayModeSmoke
{
    private const string RunningKey = "PvZ.EndlessSmoke.Running";
    private const string StageKey = "PvZ.EndlessSmoke.Stage";
    private const string ErrorKey = "PvZ.EndlessSmoke.Error";

    static EndlessPlayModeSmoke()
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
        SessionState.SetString(ErrorKey, SessionState.GetString(ErrorKey, string.Empty)
            + "\n" + message + "\n" + stack);
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(RunningKey, false)) return;
        int stage = SessionState.GetInt(StageKey, 0);
        if (!EditorApplication.isPlaying)
        {
            if (stage >= 2) Finish();
            return;
        }

        try
        {
            if (stage == 0) EnterEndless();
            else if (stage == 1) ValidateEndless();
        }
        catch (Exception exception)
        {
            SessionState.SetString(ErrorKey, SessionState.GetString(ErrorKey, string.Empty) + "\n" + exception);
            SessionState.SetInt(StageKey, 2);
            EditorApplication.isPlaying = false;
        }
    }

    private static void EnterEndless()
    {
        if (SceneManager.GetActiveScene().name != "MainMenu" ||
            UnityEngine.Object.FindAnyObjectByType<MainMenuController>() == null) return;

        EndlessMenuOverlay.Show();
        EndlessMenuOverlay menu = UnityEngine.Object.FindAnyObjectByType<EndlessMenuOverlay>();
        if (menu == null) throw new InvalidOperationException("Endless menu was not created.");
        Invoke(menu, "Play");

        PlantSelectionOverlay selection = UnityEngine.Object.FindAnyObjectByType<PlantSelectionOverlay>();
        if (selection == null) throw new InvalidOperationException("Plant selection was not opened for Endless.");
        foreach (string key in new[] { "SunFlower", "PeaShooter", "WallNut" })
            Invoke(selection, "Toggle", key);
        Invoke(selection, "Confirm");
        SessionState.SetInt(StageKey, 1);
    }

    private static void ValidateEndless()
    {
        if (SceneManager.GetActiveScene().name != "GameScene") return;
        EndlessGameController controller = UnityEngine.Object.FindAnyObjectByType<EndlessGameController>();
        if (controller == null || !controller.Running || GameManagement.levelData == null) return;

        if (!EndlessRun.Active) throw new InvalidOperationException("Endless session is not active.");
        if (GameManagement.levelData.levelName != "Sinh Tồn Vô Hạn" || GameManagement.levelData.initialSun != 150)
            throw new InvalidOperationException("Endless level profile was not applied.");
        if (GameObject.Find("Endless Canvas") == null)
            throw new InvalidOperationException("Endless HUD was not created.");
        ZombieManagement manager = UnityEngine.Object.FindAnyObjectByType<ZombieManagement>();
        if (manager == null || manager.NowNode_index != 0)
            throw new InvalidOperationException("Legacy zombie timeline advanced during Endless.");

        SessionState.SetInt(StageKey, 2);
        EditorApplication.isPlaying = false;
    }

    private static object Invoke(object target, string method, params object[] arguments)
    {
        MethodInfo info = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
        if (info == null) throw new MissingMethodException(target.GetType().Name, method);
        return info.Invoke(target, arguments);
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
            Debug.LogError("Endless PlayMode smoke test failed:" + errors);
            EditorApplication.Exit(1);
        }
        Debug.Log("Endless PlayMode smoke test passed.");
        EditorApplication.Exit(0);
    }
}
