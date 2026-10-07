using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Coordinates one Endless stage at a time. Progression rules live in EndlessProgression,
/// runtime stat mutation lives in EndlessModifierSystem, and this class owns spawning and transitions.
/// </summary>
public sealed class EndlessGameController : MonoBehaviour
{
    private const int MaximumLiveZombies = 40;

    private sealed class ZombieSpec
    {
        public readonly string prefabName;
        public readonly int threat;
        public readonly int points;
        public readonly int unlockStage;

        public ZombieSpec(string prefabName, int threat, int points, int unlockStage)
        {
            this.prefabName = prefabName;
            this.threat = threat;
            this.points = points;
            this.unlockStage = unlockStage;
        }
    }

    private static readonly ZombieSpec[] ZombieRoster =
    {
        new ZombieSpec("ZombieNormal", 1, 100, 1),
        new ZombieSpec("ConeZombie", 2, 180, 3),
        new ZombieSpec("SnowZombie", 3, 260, 5),
        new ZombieSpec("BoneZombie", 3, 280, 7),
        new ZombieSpec("BucketZombie", 4, 380, 8),
        new ZombieSpec("YetiZombie", 7, 700, 12),
        new ZombieSpec("IceBlockZombie", 8, 850, 15)
    };

    private readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>();
    private readonly List<int> recentRows = new List<int>();
    private readonly List<Zombie> spawnedZombies = new List<Zombie>();
    private ZombieManagement legacyZombieManager;
    private Transform zombieParent;
    private Text stageText;
    private Text scoreText;
    private Text statusText;
    private Text modifierText;
    private Text bossHealthText;
    private Text announcementText;
    private Text speedButtonText;
    private GameObject resultOverlay;
    private Text resultText;
    private Text resultBoardText;
    private int currentStage;
    private int currentCycle;
    private int stageScore;
    private int stageKills;
    private int liveZombieCount;
    private float stageElapsedSeconds;
    private bool running;
    private bool transitioning;
    private bool doubleSpeed;

    public bool Running => running;
    public float RequestedTimeScale => doubleSpeed ? 2f : 1f;

    private void Awake()
    {
        EndlessRun.Controller = this;
        UnityEngine.Random.InitState(EndlessRun.Seed);
        LoadPrefabs();
        BuildInterface();
    }

    private IEnumerator Start()
    {
        yield return null;
        yield return null;

        GameObject managerObject = GameObject.Find("Zombie Management");
        if (managerObject == null || GameManagement.levelData == null || EndlessRun.Session == null)
        {
            Debug.LogError("Endless không tìm thấy sân chơi hoặc trạng thái lượt chơi.", this);
            statusText.text = "Không thể khởi tạo Sinh tồn";
            yield break;
        }

        legacyZombieManager = managerObject.GetComponent<ZombieManagement>();
        zombieParent = managerObject.transform;
        if (legacyZombieManager == null)
        {
            Debug.LogError("Endless thiếu ZombieManagement.", this);
            statusText.text = "Không thể khởi tạo đội hình zombie";
            yield break;
        }

        running = true;
        EndlessModifierSystem.ApplyToExistingCards();
        EndlessModifierSystem.ApplyToExistingPlants();
        UpdateHud();
        StartCoroutine(RunStages());
    }

    private void Update()
    {
        if (running && !transitioning && Time.timeScale > 0f)
            stageElapsedSeconds += Time.unscaledDeltaTime;
    }

    private void LoadPrefabs()
    {
        for (int index = 0; index < ZombieRoster.Length; index++)
        {
            ZombieSpec spec = ZombieRoster[index];
            GameObject prefab = Resources.Load<GameObject>("Prefabs/Zombies/" + spec.prefabName);
            if (prefab != null) prefabs[spec.prefabName] = prefab;
            else Debug.LogWarning("Thiếu prefab Endless: " + spec.prefabName, this);
        }
    }

    private IEnumerator RunStages()
    {
        statusText.text = "Chuẩn bị phòng tuyến...";
        yield return new WaitForSeconds(2f);

        while (running)
        {
            if (EndlessRun.Session.pendingReward != 0)
            {
                currentStage = Mathf.Max(1, EndlessRun.Session.completedStages);
                currentCycle = EndlessRules.CycleForStage(currentStage);
            }
            if (EndlessRun.Session.pendingReward == 1 || EndlessRun.Session.pendingReward == 2)
            {
                yield return HandleNormalReward();
                yield return new WaitForSeconds(1.2f);
                continue;
            }
            if (EndlessRun.Session.pendingReward == 3 || EndlessRun.Session.pendingReward == 4)
            {
                yield return HandleBossReward();
                yield return new WaitForSeconds(1.2f);
                continue;
            }

            currentStage = EndlessRun.Session.CurrentStage;
            currentCycle = EndlessRules.CycleForStage(currentStage);
            bool bossStage = EndlessRules.IsBossStage(currentStage);
            EnsureCycleDebuff();
            SelectStageDebuff();
            EndlessModifierSystem.ApplyToExistingPlants();
            EndlessModifierSystem.ApplyToExistingCards();
            EndlessModifierSystem.ApplyToExistingSuns();

            stageScore = 0;
            stageKills = 0;
            stageElapsedSeconds = 0f;
            UpdateHud();

            yield return ShowAnnouncement(bossStage
                ? "BOSS ĐANG TIẾN VÀO KHU VƯỜN!"
                : "MÀN " + currentStage + "  •  " + EndlessRules.PositionInCycle(currentStage) + "/5",
                bossStage ? 2.2f : 1.35f);

            List<ZombieSpec> queue = BuildStageQueue(currentStage, bossStage);
            statusText.text = bossStage ? "Đánh bại boss để hoàn thành chu kỳ" : "Giữ vững phòng tuyến";
            for (int index = 0; index < queue.Count; index++)
            {
                while (running && liveZombieCount >= MaximumLiveZombies)
                    yield return new WaitForSeconds(0.25f);
                if (!running) yield break;

                SpawnZombie(queue[index], -1, true, false, null);
                float interval = Mathf.Max(0.34f, 1.20f - currentStage * 0.018f);
                yield return new WaitForSeconds(UnityEngine.Random.Range(interval * 0.76f, interval * 1.18f));
            }

            if (bossStage)
            {
                yield return new WaitForSeconds(1f);
                SpawnBoss();
            }

            while (running && liveZombieCount > 0)
                yield return new WaitForSeconds(0.25f);
            if (!running) yield break;

            int completionBonus = bossStage ? 1000 * currentCycle : 250 * currentStage;
            stageScore += completionBonus;
            float elapsed = Mathf.Max(0f, stageElapsedSeconds);
            EndlessRun.CompleteStage(stageScore, stageKills, elapsed, bossStage);
            UpdateHud();
            yield return ShowAnnouncement(bossStage
                ? "BOSS ĐÃ BỊ ĐÁNH BẠI!"
                : "HOÀN THÀNH MÀN " + currentStage, 1.5f);

            if (bossStage)
            {
                yield return HandleBossReward();
                yield return new WaitForSeconds(1.2f);
                continue;
            }

            yield return HandleNormalReward();
            yield return new WaitForSeconds(1.2f);
        }
    }

    private void EnsureCycleDebuff()
    {
        if (!string.IsNullOrEmpty(EndlessRun.Session.activeCycleDebuffId)) return;
        List<EndlessChoice> offer = EndlessContentCatalog.CreateOffer(
            EndlessChoiceKind.CycleDebuff, EndlessRun.Session, currentStage, 1, 71 + currentCycle);
        if (offer.Count == 0) return;
        EndlessRun.SetCycleDebuff(offer[0].Id, EndlessRules.CycleDebuffLevel(currentCycle));
    }

    private void SelectStageDebuff()
    {
        List<EndlessChoice> offer = EndlessContentCatalog.CreateOffer(
            EndlessChoiceKind.StageDebuff, EndlessRun.Session, currentStage, 1, 113);
        EndlessRun.SetStageDebuff(offer.Count > 0 ? offer[0].Id : string.Empty);
    }

    private List<ZombieSpec> BuildStageQueue(int stage, bool boss)
    {
        int budget = EndlessRules.StageBudget(stage, boss);
        var result = new List<ZombieSpec>();
        while (budget > 0)
        {
            var affordable = new List<ZombieSpec>();
            for (int index = 0; index < ZombieRoster.Length; index++)
            {
                ZombieSpec item = ZombieRoster[index];
                if (item.unlockStage <= stage && item.threat <= budget && prefabs.ContainsKey(item.prefabName))
                    affordable.Add(item);
            }
            if (affordable.Count == 0) break;
            ZombieSpec selected = affordable[UnityEngine.Random.Range(0, affordable.Count)];
            result.Add(selected);
            budget -= selected.threat;
        }

        for (int index = result.Count - 1; index > 0; index--)
        {
            int other = UnityEngine.Random.Range(0, index + 1);
            ZombieSpec temporary = result[index];
            result[index] = result[other];
            result[other] = temporary;
        }
        return result;
    }

    private void SpawnBoss()
    {
        ZombieSpec selected = null;
        for (int index = ZombieRoster.Length - 1; index >= 0; index--)
        {
            if (ZombieRoster[index].unlockStage <= currentStage && prefabs.ContainsKey(ZombieRoster[index].prefabName))
            {
                selected = ZombieRoster[index];
                break;
            }
        }
        if (selected != null) SpawnZombie(selected, -1, false, true, null);
    }

    private void SpawnZombie(ZombieSpec spec, int forcedRow, bool allowElite, bool boss, Vector3? forcedPosition)
    {
        if (spec == null || !prefabs.TryGetValue(spec.prefabName, out GameObject prefab)) return;
        int row = forcedRow >= 0 ? forcedRow : ChooseRow();
        float y = GameManagement.levelData.zombieInitPosY[Mathf.Clamp(row, 0, GameManagement.levelData.zombieInitPosY.Count - 1)];
        Vector3 position = forcedPosition ?? new Vector3(6f, y, 0f);
        position.y = y;
        GameObject created = Instantiate(prefab, position, Quaternion.identity, zombieParent);
        Zombie zombie = created.GetComponent<Zombie>();
        if (zombie == null)
        {
            Destroy(created);
            return;
        }

        float stageSpeed = 1f + Mathf.Min(0.45f, currentStage * 0.008f) + UnityEngine.Random.Range(0f, 0.10f);
        EndlessModifierSystem.ApplyToZombie(zombie, stageSpeed, boss);
        zombie.netSleepTime = boss || forcedPosition.HasValue ? 0f : UnityEngine.Random.Range(0f, 0.65f);
        zombie.setPosRow(row);
        legacyZombieManager.addZombieNumAll();

        EndlessEliteRuntime eliteRuntime = null;
        EndlessEliteType eliteType = allowElite ? RollEliteType() : EndlessEliteType.None;
        if (boss || eliteType != EndlessEliteType.None)
        {
            eliteRuntime = created.AddComponent<EndlessEliteRuntime>();
            eliteRuntime.Initialize(this, zombie, eliteType);
            if (boss) eliteRuntime.ConfigureAsBoss(currentCycle);
        }

        EndlessZombieTracker tracker = created.AddComponent<EndlessZombieTracker>();
        tracker.Initialize(this, zombie, spec.points, eliteRuntime, boss, currentCycle);
        spawnedZombies.Add(zombie);
        liveZombieCount++;
        UpdateHud();
    }

    private EndlessEliteType RollEliteType()
    {
        int cycle = currentCycle;
        if (cycle < 2) return EndlessEliteType.None;
        float chance = EndlessRules.BaseEliteChance(cycle) + EndlessModifierSystem.Current.EliteChanceBonus;
        if (UnityEngine.Random.value > chance) return EndlessEliteType.None;

        var unlocked = new List<EndlessEliteType> { EndlessEliteType.Berserker };
        if (cycle >= 3)
        {
            unlocked.Add(EndlessEliteType.Armored);
            unlocked.Add(EndlessEliteType.SunThief);
        }
        if (cycle >= 4) unlocked.Add(EndlessEliteType.Healer);
        if (cycle >= 5) unlocked.Add(EndlessEliteType.Commander);
        if (cycle >= 6) unlocked.Add(EndlessEliteType.Splitter);
        return unlocked[UnityEngine.Random.Range(0, unlocked.Count)];
    }

    private int ChooseRow()
    {
        int rowCount = Mathf.Max(1, GameManagement.levelData.landRowCount);
        var choices = new List<int>();
        for (int row = 0; row < rowCount; row++)
            if (!recentRows.Contains(row)) choices.Add(row);
        if (choices.Count == 0)
        {
            recentRows.Clear();
            for (int row = 0; row < rowCount; row++) choices.Add(row);
        }
        int selected = choices[UnityEngine.Random.Range(0, choices.Count)];
        recentRows.Add(selected);
        if (recentRows.Count >= rowCount) recentRows.Clear();
        return selected;
    }

    internal void ReportZombieRemoved(Zombie removed, int points, bool killed, bool elite, bool boss)
    {
        if (removed != null) spawnedZombies.Remove(removed);
        liveZombieCount = Mathf.Max(0, liveZombieCount - 1);
        if (running && killed)
        {
            stageKills++;
            float multiplier = EndlessModifierSystem.Current.ScoreMultiplier;
            int earned = Mathf.RoundToInt(points * multiplier);
            if (elite) earned = Mathf.RoundToInt(earned * 1.5f);
            if (boss) earned += 750 * currentCycle;
            stageScore += earned;
        }
        if (boss && bossHealthText != null) bossHealthText.gameObject.SetActive(false);
        UpdateHud();
    }

    internal void UpdateBossHealth(Zombie bossZombie)
    {
        if (bossHealthText == null || bossZombie == null || bossZombie.BloodVolumeMax <= 0) return;
        bossHealthText.gameObject.SetActive(true);
        bossHealthText.text = "BOSS  ♥  " + Mathf.Max(0, bossZombie.bloodVolume).ToString("N0") +
            " / " + bossZombie.BloodVolumeMax.ToString("N0");
    }

    public void CollectLivingZombiesInRow(int row, List<Zombie> results)
    {
        if (results == null) return;
        results.Clear();
        for (int index = spawnedZombies.Count - 1; index >= 0; index--)
        {
            Zombie candidate = spawnedZombies[index];
            if (candidate == null)
            {
                spawnedZombies.RemoveAt(index);
                continue;
            }
            if (candidate.IsAlive && candidate.pos_row == row) results.Add(candidate);
        }
    }

    public void SpawnSplitterChildren(int row, Vector3 position)
    {
        if (!running || !prefabs.ContainsKey("ZombieNormal")) return;
        ZombieSpec normal = ZombieRoster[0];
        int capacityAfterParentRemoval = Mathf.Max(0, MaximumLiveZombies - liveZombieCount + 1);
        if (capacityAfterParentRemoval >= 1)
            SpawnZombie(normal, row, false, false, position + new Vector3(-0.15f, 0f, 0f));
        if (capacityAfterParentRemoval >= 2)
            SpawnZombie(normal, row, false, false, position + new Vector3(0.18f, 0f, 0f));
    }

    public void SpawnBossEscort(int bossRow, int cycle, int amount)
    {
        if (!running) return;
        ZombieSpec escort = cycle >= 3 && prefabs.ContainsKey("ConeZombie") ? ZombieRoster[1] : ZombieRoster[0];
        int rowCount = Mathf.Max(1, GameManagement.levelData.landRowCount);
        int spawnCount = Mathf.Min(Mathf.Clamp(amount, 1, 3), Mathf.Max(0, MaximumLiveZombies - liveZombieCount));
        for (int index = 0; index < spawnCount; index++)
        {
            int row = (bossRow + index + 1) % rowCount;
            SpawnZombie(escort, row, true, false, null);
        }
    }

    private IEnumerator HandleNormalReward()
    {
        PauseForChoice();
        if (EndlessRun.Session.pendingReward == 1)
        {
            List<EndlessChoice> availableDebuffs = EndlessContentCatalog.CreateOffer(
                EndlessChoiceKind.TradeoffDebuff, EndlessRun.Session, currentStage, 1, 307);
            if (availableDebuffs.Count == 0)
            {
                EndlessRun.SetPendingReward(0);
                ResumeAfterChoice();
                yield break;
            }

            bool buffResolved = false;
            EndlessChoice selectedBuff = null;
            List<EndlessChoice> buffs = EndlessContentCatalog.CreateOffer(
                EndlessChoiceKind.Buff, EndlessRun.Session, currentStage, 3, 211);
            EndlessRewardOverlay.Show("PHẦN THƯỞNG KHU VƯỜN",
                "Chọn buff sẽ buộc bạn nhận thêm một debuff, hoặc bỏ qua an toàn.", buffs, true,
                choice => { selectedBuff = choice; buffResolved = true; },
                () => buffResolved = true);
            yield return new WaitUntil(() => buffResolved || !running);
            if (!running) yield break;
            if (selectedBuff == null)
            {
                EndlessRun.SetPendingReward(0);
                ResumeAfterChoice();
                yield break;
            }
            ApplyRewardChoice(selectedBuff, 2);
        }

        bool debuffResolved = false;
        List<EndlessChoice> debuffs = EndlessContentCatalog.CreateOffer(
            EndlessChoiceKind.TradeoffDebuff, EndlessRun.Session, currentStage, 3, 307);
        EndlessRewardOverlay.Show("CÁI GIÁ PHẢI TRẢ",
            "Bạn đã nhận buff. Hãy chọn một debuff để tiếp tục.", debuffs, false,
            choice => { ApplyRewardChoice(choice, 0); debuffResolved = true; },
            () => debuffResolved = true);
        yield return new WaitUntil(() => debuffResolved || !running);
        if (!running) yield break;
        if (EndlessRun.Session.pendingReward != 0) EndlessRun.SetPendingReward(0);
        ResumeAfterChoice();
    }

    private IEnumerator HandleBossReward()
    {
        PauseForChoice();
        if (EndlessRun.Session.pendingReward == 3)
        {
            bool resolved = false;
            List<EndlessChoice> buffs = EndlessContentCatalog.CreateOffer(
                EndlessChoiceKind.Buff, EndlessRun.Session, currentStage, 3, 401 + currentCycle);
            EndlessRewardOverlay.Show("CHIẾN LỢI PHẨM TỪ BOSS",
                "Chọn một buff miễn phí. Bạn không phải nhận debuff đánh đổi.", buffs, false,
                choice => { ApplyRewardChoice(choice, 4); resolved = true; },
                () => resolved = true);
            yield return new WaitUntil(() => resolved || !running);
            if (!running) yield break;
            if (EndlessRun.Session.pendingReward != 4) EndlessRun.SetPendingReward(4);
        }

        if (!string.IsNullOrEmpty(EndlessRun.Session.activeCycleDebuffId) &&
            EndlessRun.Session.activeCycleDebuffCycle <= currentCycle)
            EndlessRun.FinishCycle();
        int nextStage = EndlessRun.Session.CurrentStage;
        int nextCycle = EndlessRules.CycleForStage(nextStage);
        if (string.IsNullOrEmpty(EndlessRun.Session.activeCycleDebuffId))
        {
            List<EndlessChoice> nextCycleDebuff = EndlessContentCatalog.CreateOffer(
                EndlessChoiceKind.CycleDebuff, EndlessRun.Session, nextStage, 1, 71 + nextCycle);
            if (nextCycleDebuff.Count > 0)
                EndlessRun.SetCycleDebuff(nextCycleDebuff[0].Id, EndlessRules.CycleDebuffLevel(nextCycle));
        }

        transitioning = true;
        bool deckResolved = false;
        string nextDebuffName = ChoiceName(EndlessRun.Session.activeCycleDebuffId);
        PlantSelectionOverlay.Show(0, () =>
        {
            UIManagement ui = FindAnyObjectByType<UIManagement>();
            if (ui != null) ui.RefreshEndlessDeck();
            else Debug.LogError("Không thể cập nhật bộ thẻ Endless vì thiếu UIManagement.", this);
            EndlessRun.SetPendingReward(0);
            EndlessRun.SaveProgress();
            transitioning = false;
            ResumeAfterChoice();
            UpdateHud();
            deckResolved = true;
        }, false, "CHU KỲ " + nextCycle + " • " + nextDebuffName, "BẮT ĐẦU CHU KỲ");
        yield return new WaitUntil(() => deckResolved || !running);
    }

    private void ApplyRewardChoice(EndlessChoice choice, int pendingRewardAfter = -1)
    {
        if (!EndlessRun.AddChoice(choice, pendingRewardAfter)) return;
        if (choice.Id == "garden_recovery") EndlessModifierSystem.RecoverExistingPlants(0.25f);
        EndlessModifierSystem.ApplyToExistingPlants();
        EndlessModifierSystem.ApplyToExistingCards();
        EndlessModifierSystem.ApplyToExistingSuns();
        UpdateHud();
    }

    private void PauseForChoice()
    {
        Time.timeScale = 0f;
        AudioListener.pause = true;
    }

    private void ResumeAfterChoice()
    {
        AudioListener.pause = false;
        Time.timeScale = RequestedTimeScale;
    }

    private void ToggleSpeed()
    {
        if (!running || transitioning || Time.timeScale <= 0f) return;
        doubleSpeed = !doubleSpeed;
        Time.timeScale = RequestedTimeScale;
        UpdateSpeedButton();
    }

    private void UpdateSpeedButton()
    {
        if (speedButtonText != null)
            speedButtonText.text = doubleSpeed ? "TỐC ĐỘ  x2" : "TỐC ĐỘ  x1";
    }

    public void EndRun()
    {
        if (!running || transitioning) return;
        running = false;
        StopAllCoroutines();

        EndlessSessionState session = EndlessRun.Session;
        float currentElapsed = Mathf.Max(0f, stageElapsedSeconds);
        int finalScore = (session != null ? session.score : 0) + stageScore;
        int finalKills = (session != null ? session.kills : 0) + stageKills;
        int completedStages = session != null ? session.completedStages : 0;
        float duration = (session != null ? session.durationSeconds : 0f) + currentElapsed;
        EndlessLeaderboard.Add(new EndlessScoreRecord
        {
            playerName = string.IsNullOrWhiteSpace(NetSession.LocalName) ? "Người chơi" : NetSession.LocalName,
            score = finalScore,
            wave = completedStages,
            kills = finalKills,
            durationSeconds = duration,
            seed = EndlessRun.Seed,
            playedAtUtc = DateTime.UtcNow.ToString("o"),
            gameVersion = Application.version
        });
        EndlessRun.DeleteProgress();

        int minutes = Mathf.FloorToInt(duration / 60f);
        int seconds = Mathf.FloorToInt(duration % 60f);
        resultText.text = "Bạn đã vượt qua " + completedStages + " màn  •  " +
            (completedStages / EndlessRules.StagesPerCycle) + " boss\n" +
            finalScore.ToString("N0") + " điểm  •  " + finalKills + " zombie\n" +
            "Thời gian " + minutes.ToString("00") + ":" + seconds.ToString("00");
        resultBoardText.text = EndlessLeaderboard.Format(5);
        resultOverlay.SetActive(true);
        PauseForChoice();
    }

    private void Restart()
    {
        RestoreTime();
        EndlessRun.Begin();
        SceneManager.LoadScene(EndlessRun.EntrySceneName);
    }

    private void ReturnToMenu()
    {
        RestoreTime();
        EndlessRun.Clear();
        SceneManager.LoadScene("MainMenu");
    }

    private IEnumerator ShowAnnouncement(string value, float duration)
    {
        announcementText.text = value;
        announcementText.gameObject.SetActive(true);
        yield return new WaitForSeconds(duration);
        announcementText.gameObject.SetActive(false);
    }

    private void UpdateHud()
    {
        if (stageText == null) return;
        EndlessSessionState session = EndlessRun.Session;
        int stage = currentStage > 0 ? currentStage : (session != null ? session.CurrentStage : 1);
        int cycle = EndlessRules.CycleForStage(stage);
        int position = EndlessRules.PositionInCycle(stage);
        stageText.text = "MÀN " + stage + "  •  CHU KỲ " + cycle + "  •  " + position + "/5" +
            (EndlessRules.IsBossStage(stage) ? "  •  BOSS" : string.Empty);
        int totalScore = (session != null ? session.score : 0) + stageScore;
        int totalKills = (session != null ? session.kills : 0) + stageKills;
        scoreText.text = "ĐIỂM " + totalScore.ToString("N0") + "  •  HẠ " + totalKills;

        string cycleName = session != null ? ChoiceName(session.activeCycleDebuffId) : string.Empty;
        string stageName = session != null ? ChoiceName(session.activeStageDebuffId) : string.Empty;
        modifierText.text = "CHU KỲ: " + (string.IsNullOrEmpty(cycleName) ? "—" : cycleName) +
            "\nMÀN: " + (string.IsNullOrEmpty(stageName) ? "—" : stageName) +
            "\nBUILD: " + CountStacks(session != null ? session.buffs : null) + " buff • " +
            CountStacks(session != null ? session.tradeoffDebuffs : null) + " debuff";
    }

    private static int CountStacks(List<EndlessModifierStack> values)
    {
        int total = 0;
        if (values == null) return total;
        for (int index = 0; index < values.Count; index++) total += Mathf.Max(0, values[index].stacks);
        return total;
    }

    private static string ChoiceName(string id)
    {
        EndlessChoice choice = EndlessContentCatalog.Find(id);
        return choice == null ? string.Empty : choice.DisplayName.Replace("Chu kỳ: ", string.Empty).Replace("Màn: ", string.Empty);
    }

    private void BuildInterface()
    {
        var canvasObject = new GameObject("Endless Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1100;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1000f, 750f);
        scaler.matchWidthOrHeight = 0.5f;

        Image hud = EndlessMenuOverlay.ImageObject("Garden HUD", canvasObject.transform, new Color(0.07f, 0.16f, 0.035f, 0.91f));
        hud.raycastTarget = false;
        EndlessMenuOverlay.Anchor(hud.rectTransform, 0.50f, 0.875f, 0.985f, 0.985f);
        stageText = EndlessMenuOverlay.TextObject("Stage", hud.transform, string.Empty, 22,
            TextAnchor.MiddleLeft, new Color(0.72f, 1f, 0.32f));
        EndlessMenuOverlay.Anchor(stageText.rectTransform, 0.04f, 0.52f, 0.96f, 0.96f);
        stageText.raycastTarget = false;
        scoreText = EndlessMenuOverlay.TextObject("Score", hud.transform, string.Empty, 19,
            TextAnchor.MiddleLeft, Color.white);
        EndlessMenuOverlay.Anchor(scoreText.rectTransform, 0.04f, 0.06f, 0.96f, 0.52f);
        scoreText.raycastTarget = false;

        Image modifierPanel = EndlessMenuOverlay.ImageObject("Modifiers", canvasObject.transform,
            new Color(0.23f, 0.12f, 0.25f, 0.90f));
        modifierPanel.raycastTarget = false;
        EndlessMenuOverlay.Anchor(modifierPanel.rectTransform, 0.015f, 0.785f, 0.34f, 0.91f);
        modifierText = EndlessMenuOverlay.TextObject("Modifier Text", modifierPanel.transform, string.Empty, 17,
            TextAnchor.MiddleLeft, new Color(1f, 0.88f, 0.65f));
        EndlessMenuOverlay.Anchor(modifierText.rectTransform, 0.05f, 0.08f, 0.95f, 0.92f);
        modifierText.raycastTarget = false;

        GameObject speedButton = CreateButton(canvasObject.transform, "TỐC ĐỘ  x1",
            new Color(0.46f, 0.58f, 0.16f), ToggleSpeed);
        EndlessMenuOverlay.Anchor(speedButton.GetComponent<RectTransform>(), 0.35f, 0.91f, 0.48f, 0.975f);
        speedButtonText = speedButton.GetComponentInChildren<Text>();
        if (speedButtonText != null) speedButtonText.fontSize = 18;
        UpdateSpeedButton();

        statusText = EndlessMenuOverlay.TextObject("Status", canvasObject.transform, "Đang khởi tạo...", 20,
            TextAnchor.MiddleCenter, new Color(1f, 0.88f, 0.42f));
        EndlessMenuOverlay.Anchor(statusText.rectTransform, 0.34f, 0.82f, 0.77f, 0.87f);
        statusText.raycastTarget = false;
        announcementText = EndlessMenuOverlay.TextObject("Announcement", canvasObject.transform, string.Empty, 42,
            TextAnchor.MiddleCenter, new Color(0.76f, 1f, 0.30f));
        EndlessMenuOverlay.Anchor(announcementText.rectTransform, 0.13f, 0.42f, 0.87f, 0.58f);
        announcementText.raycastTarget = false;
        announcementText.gameObject.SetActive(false);

        bossHealthText = EndlessMenuOverlay.TextObject("Boss Health", canvasObject.transform, string.Empty, 24,
            TextAnchor.MiddleCenter, new Color(1f, 0.48f, 0.32f));
        EndlessMenuOverlay.Anchor(bossHealthText.rectTransform, 0.34f, 0.755f, 0.72f, 0.81f);
        bossHealthText.fontStyle = FontStyle.Bold;
        bossHealthText.raycastTarget = false;
        bossHealthText.gameObject.SetActive(false);

        resultOverlay = EndlessMenuOverlay.ImageObject("Endless Result", canvasObject.transform,
            new Color(0f, 0f, 0f, 0.84f)).gameObject;
        EndlessMenuOverlay.Stretch(resultOverlay.GetComponent<RectTransform>());
        Canvas resultCanvas = resultOverlay.AddComponent<Canvas>();
        resultCanvas.overrideSorting = true;
        resultCanvas.sortingOrder = 1500;
        resultOverlay.AddComponent<GraphicRaycaster>();
        Image panel = EndlessMenuOverlay.ImageObject("Soil Panel", resultOverlay.transform,
            new Color(0.14f, 0.20f, 0.065f, 0.99f));
        EndlessMenuOverlay.Anchor(panel.rectTransform, 0.22f, 0.10f, 0.78f, 0.90f);
        Text title = EndlessMenuOverlay.TextObject("Title", panel.transform, "KẾT THÚC SINH TỒN", 38,
            TextAnchor.MiddleCenter, new Color(0.68f, 1f, 0.28f));
        EndlessMenuOverlay.Anchor(title.rectTransform, 0.07f, 0.84f, 0.93f, 0.96f);
        resultText = EndlessMenuOverlay.TextObject("Result", panel.transform, string.Empty, 24,
            TextAnchor.MiddleCenter, Color.white);
        EndlessMenuOverlay.Anchor(resultText.rectTransform, 0.08f, 0.64f, 0.92f, 0.84f);
        Text boardTitle = EndlessMenuOverlay.TextObject("Board Title", panel.transform, "THÀNH TÍCH CỤC BỘ", 22,
            TextAnchor.MiddleCenter, new Color(1f, 0.84f, 0.28f));
        EndlessMenuOverlay.Anchor(boardTitle.rectTransform, 0.08f, 0.56f, 0.92f, 0.63f);
        resultBoardText = EndlessMenuOverlay.TextObject("Board", panel.transform, string.Empty, 19,
            TextAnchor.UpperCenter, Color.white);
        EndlessMenuOverlay.Anchor(resultBoardText.rectTransform, 0.07f, 0.27f, 0.93f, 0.56f);
        GameObject retry = CreateButton(panel.transform, "CHƠI LẠI", new Color(0.45f, 0.70f, 0.14f), Restart);
        EndlessMenuOverlay.Anchor(retry.GetComponent<RectTransform>(), 0.09f, 0.08f, 0.47f, 0.20f);
        GameObject menu = CreateButton(panel.transform, "VỀ MENU", new Color(0.28f, 0.40f, 0.18f), ReturnToMenu);
        EndlessMenuOverlay.Anchor(menu.GetComponent<RectTransform>(), 0.53f, 0.08f, 0.91f, 0.20f);
        resultOverlay.SetActive(false);
    }

    private static GameObject CreateButton(Transform parent, string label, Color color,
        UnityEngine.Events.UnityAction action)
    {
        var root = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        root.transform.SetParent(parent, false);
        root.GetComponent<Image>().color = color;
        Button button = root.GetComponent<Button>();
        button.onClick.AddListener(action);
        ColorBlock colors = button.colors;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.20f);
        button.colors = colors;
        Text text = EndlessMenuOverlay.TextObject("Label", root.transform, label, 24, TextAnchor.MiddleCenter, Color.white);
        EndlessMenuOverlay.Stretch(text.rectTransform);
        text.raycastTarget = false;
        return root;
    }

    private void RestoreTime()
    {
        AudioListener.pause = false;
        Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        running = false;
        if (EndlessRun.Controller == this) EndlessRun.Controller = null;
        RestoreTime();
    }
}

/// <summary>Reports the lifecycle of a zombie owned by Endless exactly once.</summary>
public sealed class EndlessZombieTracker : MonoBehaviour
{
    private EndlessGameController owner;
    private Zombie zombie;
    private EndlessEliteRuntime eliteRuntime;
    private int points;
    private int bossCycle;
    private bool boss;
    private bool reported;

    public void Initialize(EndlessGameController controller, Zombie trackedZombie, int scoreValue,
        EndlessEliteRuntime elite, bool isBoss, int cycle)
    {
        owner = controller;
        zombie = trackedZombie;
        points = scoreValue;
        eliteRuntime = elite;
        boss = isBoss;
        bossCycle = cycle;
    }

    private void LateUpdate()
    {
        if (reported || zombie == null) return;
        if (boss && eliteRuntime != null)
        {
            eliteRuntime.UpdateBossPhases(bossCycle);
            if (owner != null) owner.UpdateBossHealth(zombie);
        }
        if (zombie.IsDefeatedForEndless) Report(true);
    }

    private void OnDestroy()
    {
        if (!reported) Report(false);
    }

    private void Report(bool killed)
    {
        if (reported) return;
        reported = true;
        bool elite = eliteRuntime != null && eliteRuntime.Type != EndlessEliteType.None;
        if (killed && eliteRuntime != null) eliteRuntime.NotifyKilled();
        if (owner != null) owner.ReportZombieRemoved(zombie, points, killed, elite, boss);
    }
}
