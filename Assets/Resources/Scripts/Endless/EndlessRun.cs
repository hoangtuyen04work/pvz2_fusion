using System;
using System.IO;
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
    public static EndlessSessionState Session { get; private set; }
    public static int StartingSunBonus => GetBuffStacks("strong_start") * 100;
    public static bool HasSavedProgress => File.Exists(ProgressPath);
    public static float GameplayTimeScale => Active && Controller != null ? Controller.RequestedTimeScale : 1f;

    public static void Begin()
    {
        NetSession.Reset();
        GameSession.SelectedLevel = 0;
        Seed = unchecked((int)System.DateTime.UtcNow.Ticks);
        Session = new EndlessSessionState { seed = Seed };
        Active = true;
        SaveProgress();
    }

    public static int GetBuffStacks(string id)
    {
        return Session == null ? 0 : Session.GetStacks(id, true);
    }

    public static bool TryResume()
    {
        try
        {
            if (!File.Exists(ProgressPath)) return false;
            EndlessSessionState loaded = JsonUtility.FromJson<EndlessSessionState>(File.ReadAllText(ProgressPath));
            if (loaded == null || loaded.seed == 0 || loaded.completedStages < 0) return false;
            loaded.buffs = NormalizeStacks(loaded.buffs, EndlessChoiceKind.Buff);
            loaded.tradeoffDebuffs = NormalizeStacks(loaded.tradeoffDebuffs, EndlessChoiceKind.TradeoffDebuff);
            if (loaded.selectedPlants == null) loaded.selectedPlants = new System.Collections.Generic.List<string>();
            var validPlants = new System.Collections.Generic.List<string>();
            for (int index = 0; index < loaded.selectedPlants.Count && validPlants.Count < PlantLoadoutCatalog.MaxSelected; index++)
            {
                string key = loaded.selectedPlants[index];
                if (PlantLoadoutCatalog.IsSelectionChoice(key) &&
                    PlantLoadoutCatalog.TryGet(key, out _) && !validPlants.Contains(key))
                    validPlants.Add(key);
            }
            loaded.selectedPlants = validPlants;
            loaded.pendingReward = Mathf.Clamp(loaded.pendingReward, 0, 4);
            EndlessChoice cycleDebuff = EndlessContentCatalog.Find(loaded.activeCycleDebuffId);
            if (cycleDebuff == null || cycleDebuff.Kind != EndlessChoiceKind.CycleDebuff)
            {
                loaded.activeCycleDebuffId = string.Empty;
                loaded.activeCycleDebuffCycle = 0;
                loaded.activeCycleDebuffLevel = 0;
            }
            else
            {
                loaded.activeCycleDebuffCycle = Mathf.Max(1, loaded.activeCycleDebuffCycle);
                loaded.activeCycleDebuffLevel = Mathf.Clamp(loaded.activeCycleDebuffLevel, 1, 3);
            }
            EndlessChoice stageDebuff = EndlessContentCatalog.Find(loaded.activeStageDebuffId);
            if (stageDebuff == null || stageDebuff.Kind != EndlessChoiceKind.StageDebuff)
                loaded.activeStageDebuffId = string.Empty;
            NetSession.Reset();
            GameSession.SelectedLevel = 0;
            GameSession.SelectedPlants.Clear();
            GameSession.SelectedPlants.AddRange(loaded.selectedPlants);
            if (GameSession.SelectedPlants.Count == 0) return false;
            Session = loaded;
            Seed = loaded.seed;
            Active = true;
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Không thể tiếp tục lượt Endless: " + exception.Message);
            return false;
        }
    }

    public static bool AddChoice(EndlessChoice choice, int pendingRewardAfter = -1)
    {
        if (!Active || Session == null || choice == null) return false;
        bool added = Session.AddStack(choice);
        if (added)
        {
            if (pendingRewardAfter >= 0) Session.pendingReward = Mathf.Clamp(pendingRewardAfter, 0, 4);
            SaveProgress();
        }
        return added;
    }

    public static void SetStageDebuff(string id)
    {
        if (Session == null) return;
        Session.activeStageDebuffId = id ?? string.Empty;
        SaveProgress();
    }

    public static void SetCycleDebuff(string id, int level)
    {
        if (Session == null) return;
        Session.activeCycleDebuffId = id ?? string.Empty;
        Session.activeCycleDebuffCycle = Session.CurrentCycle;
        Session.activeCycleDebuffLevel = Mathf.Clamp(level, 1, 3);
        SaveProgress();
    }

    public static void CompleteStage(int scoreEarned, int killsEarned, float elapsedSeconds, bool boss)
    {
        if (Session == null) return;
        Session.completedStages++;
        Session.score += Mathf.Max(0, scoreEarned);
        Session.kills += Mathf.Max(0, killsEarned);
        Session.durationSeconds += Mathf.Max(0f, elapsedSeconds);
        if (boss) Session.bossesDefeated++;
        Session.pendingReward = boss ? 3 : 1;
        Session.activeStageDebuffId = string.Empty;
        SaveProgress();
    }

    public static void SetPendingReward(int value)
    {
        if (Session == null) return;
        Session.pendingReward = Mathf.Clamp(value, 0, 4);
        SaveProgress();
    }

    public static void FinishCycle()
    {
        if (Session == null) return;
        Session.lastCycleDebuffId = Session.activeCycleDebuffId;
        Session.activeCycleDebuffId = string.Empty;
        Session.activeCycleDebuffCycle = 0;
        Session.activeCycleDebuffLevel = 0;
        SaveProgress();
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
        Session = null;
    }

    public static void DeleteProgress()
    {
        string path = ProgressPath;
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Không thể xóa tiến trình Endless: " + exception.Message);
        }
    }

    public static void SaveProgress()
    {
        if (!Active || Session == null) return;
        string path = ProgressPath;
        string temporary = path + ".tmp";
        try
        {
            if (Session.selectedPlants == null) Session.selectedPlants = new System.Collections.Generic.List<string>();
            Session.selectedPlants.Clear();
            Session.selectedPlants.AddRange(GameSession.SelectedPlants);
            File.WriteAllText(temporary, JsonUtility.ToJson(Session, true));
            if (File.Exists(path)) File.Replace(temporary, path, null);
            else File.Move(temporary, path);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Không thể lưu tiến trình Endless: " + exception.Message);
        }
    }

    private static string ProgressPath => Path.Combine(Application.persistentDataPath, "endless_run.json");

    private static System.Collections.Generic.List<EndlessModifierStack> NormalizeStacks(
        System.Collections.Generic.List<EndlessModifierStack> source, EndlessChoiceKind expectedKind)
    {
        var result = new System.Collections.Generic.List<EndlessModifierStack>();
        if (source == null) return result;
        for (int index = 0; index < source.Count; index++)
        {
            EndlessModifierStack stack = source[index];
            if (stack == null || string.IsNullOrEmpty(stack.id)) continue;
            EndlessChoice choice = EndlessContentCatalog.Find(stack.id);
            if (choice == null || choice.Kind != expectedKind) continue;
            int validStacks = Mathf.Clamp(stack.stacks, 1, choice.MaxStacks);
            EndlessModifierStack existing = result.Find(item => item.id == stack.id);
            if (existing == null)
                result.Add(new EndlessModifierStack { id = stack.id, stacks = validStacks });
            else
                existing.stacks = Mathf.Clamp(existing.stacks + validStacks, 1, choice.MaxStacks);
        }
        return result;
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
            UnityEngine.Object.FindAnyObjectByType<EndlessGameController>() == null)
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
