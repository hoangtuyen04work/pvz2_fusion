using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Trạng thái nhỏ, độc lập của một lượt Endless. Không thay đổi GameSession cũ.
/// </summary>
public static class EndlessRun
{
    public const string EntrySceneName = "EndlessScene";
    public const string GameplaySceneName = "GameScene";

    public static bool Active { get; private set; }
    public static int Seed { get; private set; }
    public static EndlessGameController Controller { get; internal set; }

    public static void Begin()
    {
        NetSession.Reset();
        GameSession.SelectedLevel = 0;
        Seed = unchecked((int)System.DateTime.UtcNow.Ticks);
        Active = true;
    }

    public static bool HandleGameOver()
    {
        if (!Active) return false;
        if (Controller != null) Controller.EndRun();
        return true;
    }

    public static void Clear()
    {
        Active = false;
        Controller = null;
    }
}

/// <summary>Tự nối hệ thống mới vào hai scene mà không cần sửa scene gameplay cũ.</summary>
public static class EndlessBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenu")
        {
            EndlessRun.Clear();
            return;
        }

        if (!EndlessRun.Active) return;

        if (scene.name == EndlessRun.EntrySceneName)
        {
            new GameObject("Endless Scene Loader").AddComponent<EndlessEntryLoader>();
            return;
        }

        if (scene.name == EndlessRun.GameplaySceneName &&
            Object.FindAnyObjectByType<EndlessGameController>() == null)
        {
            new GameObject("Endless Mode").AddComponent<EndlessGameController>();
        }
    }
}

/// <summary>Chờ scene vào ổn định một frame rồi mới mở sân chơi dùng chung.</summary>
public sealed class EndlessEntryLoader : MonoBehaviour
{
    private System.Collections.IEnumerator Start()
    {
        yield return null;
        SceneManager.LoadScene(EndlessRun.GameplaySceneName);
    }
}
