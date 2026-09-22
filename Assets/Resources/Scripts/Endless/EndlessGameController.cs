using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Toàn bộ luật Sinh tồn. Lớp này chỉ dùng scene/prefab có sẵn và không tham gia màn Phiêu lưu.
/// </summary>
public sealed class EndlessGameController : MonoBehaviour
{
    private const int MaximumLiveZombies = 40;

    private sealed class ZombieSpec
    {
        public readonly string prefabName;
        public readonly int threat;
        public readonly int points;
        public readonly int unlockWave;

        public ZombieSpec(string prefabName, int threat, int points, int unlockWave)
        {
            this.prefabName = prefabName;
            this.threat = threat;
            this.points = points;
            this.unlockWave = unlockWave;
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
    private ZombieManagement legacyZombieManager;
    private Transform zombieParent;
    private Text waveText;
    private Text scoreText;
    private Text statusText;
    private Text announcementText;
    private GameObject resultOverlay;
    private Text resultText;
    private Text resultBoardText;
    private float startedAt;
    private int wave;
    private int completedWaves;
    private int score;
    private int kills;
    private int liveZombieCount;
    private bool running;

    public bool Running => running;

    private void Awake()
    {
        EndlessRun.Controller = this;
        UnityEngine.Random.InitState(EndlessRun.Seed);
        LoadPrefabs();
        BuildInterface();
    }

    private IEnumerator Start()
    {
        // Đợi GameManagement hoàn thành việc dựng sân, thẻ cây và các object trong awakeList.
        yield return null;
        yield return null;

        GameObject managerObject = GameObject.Find("Zombie Management");
        if (managerObject == null || GameManagement.levelData == null)
        {
            Debug.LogError("Endless không tìm thấy sân chơi chuẩn.", this);
            statusText.text = "Không thể khởi tạo Sinh tồn";
            yield break;
        }

        legacyZombieManager = managerObject.GetComponent<ZombieManagement>();
        zombieParent = managerObject.transform;
        startedAt = Time.time;
        running = true;
        UpdateHud();
        StartCoroutine(RunWaves());
    }

    private void LoadPrefabs()
    {
        foreach (ZombieSpec spec in ZombieRoster)
        {
            if (prefabs.ContainsKey(spec.prefabName)) continue;
            GameObject prefab = Resources.Load<GameObject>("Prefabs/Zombies/" + spec.prefabName);
            if (prefab != null) prefabs.Add(spec.prefabName, prefab);
            else Debug.LogWarning("Thiếu prefab Endless: " + spec.prefabName, this);
        }
    }

    private IEnumerator RunWaves()
    {
        statusText.text = "Chuẩn bị phòng tuyến...";
        yield return new WaitForSeconds(3f);

        while (running)
        {
            wave++;
            bool hugeWave = wave % 5 == 0;
            yield return ShowAnnouncement(hugeWave
                ? "ĐỢT ZOMBIE KHỔNG LỒ!"
                : "ĐỢT " + wave, hugeWave ? 2.2f : 1.25f);

            List<ZombieSpec> queue = BuildWave(wave, hugeWave);
            statusText.text = hugeWave ? "Đợt lớn đang tấn công" : "Zombie đang tới";
            UpdateHud();

            foreach (ZombieSpec spec in queue)
            {
                while (running && liveZombieCount >= MaximumLiveZombies)
                    yield return new WaitForSeconds(0.35f);
                if (!running) yield break;

                SpawnZombie(spec);
                float interval = Mathf.Max(0.38f, 1.35f - wave * 0.025f);
                yield return new WaitForSeconds(UnityEngine.Random.Range(interval * 0.72f, interval * 1.18f));
            }

            while (running && liveZombieCount > 0)
                yield return new WaitForSeconds(0.35f);
            if (!running) yield break;

            completedWaves = wave;
            score += 250 * wave;
            statusText.text = "Hoàn thành đợt " + wave + "  +" + (250 * wave) + " điểm";
            UpdateHud();
            yield return ShowAnnouncement("ĐÃ VƯỢT QUA ĐỢT " + wave, 1.4f);
            yield return new WaitForSeconds(hugeWave ? 7f : 4f);
        }
    }

    private List<ZombieSpec> BuildWave(int waveNumber, bool hugeWave)
    {
        int budget = Mathf.Min(100, Mathf.RoundToInt(5f + 1.8f * waveNumber + 0.04f * waveNumber * waveNumber));
        if (hugeWave) budget = Mathf.RoundToInt(budget * 1.35f);

        var queue = new List<ZombieSpec>();
        while (budget > 0)
        {
            var affordable = new List<ZombieSpec>();
            foreach (ZombieSpec spec in ZombieRoster)
                if (spec.unlockWave <= waveNumber && spec.threat <= budget && prefabs.ContainsKey(spec.prefabName))
                    affordable.Add(spec);

            if (affordable.Count == 0) break;
            ZombieSpec selected = affordable[UnityEngine.Random.Range(0, affordable.Count)];
            queue.Add(selected);
            budget -= selected.threat;
        }

        // Xáo trộn để zombie mạnh không luôn xuất hiện cùng một đoạn của wave.
        for (int index = queue.Count - 1; index > 0; index--)
        {
            int other = UnityEngine.Random.Range(0, index + 1);
            ZombieSpec temporary = queue[index];
            queue[index] = queue[other];
            queue[other] = temporary;
        }
        return queue;
    }

    private void SpawnZombie(ZombieSpec spec)
    {
        if (!prefabs.TryGetValue(spec.prefabName, out GameObject prefab)) return;

        int row = ChooseRow();
        float y = GameManagement.levelData.zombieInitPosY[row];
        GameObject created = Instantiate(prefab, new Vector3(6f, y, 0f), Quaternion.identity, zombieParent);
        Zombie zombie = created.GetComponent<Zombie>();
        if (zombie == null)
        {
            Destroy(created);
            return;
        }

        zombie.netSpeedScale = 1f + Mathf.Min(0.60f, wave * 0.012f) + UnityEngine.Random.Range(0f, 0.20f);
        zombie.netSleepTime = UnityEngine.Random.Range(0f, 0.8f);
        zombie.setPosRow(row);
        legacyZombieManager.addZombieNumAll();

        EndlessZombieTracker tracker = created.AddComponent<EndlessZombieTracker>();
        tracker.Initialize(this, zombie, spec.points);
        liveZombieCount++;
        UpdateHud();
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

    internal void ReportZombieRemoved(int points, bool killed)
    {
        liveZombieCount = Mathf.Max(0, liveZombieCount - 1);
        if (running && killed)
        {
            kills++;
            score += points;
        }
        UpdateHud();
    }

    public void EndRun()
    {
        if (!running) return;
        running = false;
        StopAllCoroutines();

        float duration = Mathf.Max(0f, Time.time - startedAt);
        EndlessLeaderboard.Add(new EndlessScoreRecord
        {
            playerName = string.IsNullOrWhiteSpace(NetSession.LocalName) ? "Người chơi" : NetSession.LocalName,
            score = score,
            wave = completedWaves,
            kills = kills,
            durationSeconds = duration,
            seed = EndlessRun.Seed,
            playedAtUtc = DateTime.UtcNow.ToString("o"),
            gameVersion = Application.version
        });

        int minutes = Mathf.FloorToInt(duration / 60f);
        int seconds = Mathf.FloorToInt(duration % 60f);
        resultText.text = "Bạn đã trụ qua " + completedWaves + " đợt\n"
            + score.ToString("N0") + " điểm  •  " + kills + " zombie\n"
            + "Thời gian " + minutes.ToString("00") + ":" + seconds.ToString("00");
        resultBoardText.text = EndlessLeaderboard.Format(5);
        resultOverlay.SetActive(true);
        Time.timeScale = 0f;
        AudioListener.pause = true;
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
        if (waveText == null) return;
        waveText.text = "ĐỢT  " + Mathf.Max(1, wave) + "   •   CỜ  " + (completedWaves / 5);
        scoreText.text = "ĐIỂM  " + score.ToString("N0") + "   •   HẠ  " + kills;
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

        Image hud = EndlessMenuOverlay.ImageObject("HUD", canvasObject.transform, new Color(0.035f, 0.09f, 0.025f, 0.88f));
        hud.raycastTarget = false;
        EndlessMenuOverlay.Anchor(hud.rectTransform, 0.56f, 0.895f, 0.985f, 0.985f);
        waveText = EndlessMenuOverlay.TextObject("Wave", hud.transform, string.Empty, 23,
            TextAnchor.MiddleLeft, new Color(0.70f, 1f, 0.32f));
        EndlessMenuOverlay.Anchor(waveText.rectTransform, 0.04f, 0.50f, 0.96f, 0.95f);
        waveText.raycastTarget = false;
        scoreText = EndlessMenuOverlay.TextObject("Score", hud.transform, string.Empty, 20,
            TextAnchor.MiddleLeft, Color.white);
        EndlessMenuOverlay.Anchor(scoreText.rectTransform, 0.04f, 0.06f, 0.96f, 0.51f);
        scoreText.raycastTarget = false;

        statusText = EndlessMenuOverlay.TextObject("Status", canvasObject.transform, "Đang khởi tạo...", 21,
            TextAnchor.MiddleCenter, new Color(1f, 0.88f, 0.42f));
        EndlessMenuOverlay.Anchor(statusText.rectTransform, 0.33f, 0.84f, 0.77f, 0.89f);
        statusText.raycastTarget = false;
        announcementText = EndlessMenuOverlay.TextObject("Announcement", canvasObject.transform, string.Empty, 44,
            TextAnchor.MiddleCenter, new Color(0.76f, 1f, 0.30f));
        EndlessMenuOverlay.Anchor(announcementText.rectTransform, 0.16f, 0.42f, 0.84f, 0.58f);
        announcementText.raycastTarget = false;
        announcementText.gameObject.SetActive(false);

        resultOverlay = EndlessMenuOverlay.ImageObject("Endless Result", canvasObject.transform,
            new Color(0f, 0f, 0f, 0.84f)).gameObject;
        EndlessMenuOverlay.Stretch(resultOverlay.GetComponent<RectTransform>());
        Canvas resultCanvas = resultOverlay.AddComponent<Canvas>();
        resultCanvas.overrideSorting = true;
        resultCanvas.sortingOrder = 1300;
        resultOverlay.AddComponent<GraphicRaycaster>();
        Image panel = EndlessMenuOverlay.ImageObject("Panel", resultOverlay.transform,
            new Color(0.08f, 0.16f, 0.055f, 0.99f));
        EndlessMenuOverlay.Anchor(panel.rectTransform, 0.22f, 0.10f, 0.78f, 0.90f);
        Text title = EndlessMenuOverlay.TextObject("Title", panel.transform, "KẾT THÚC SINH TỒN", 39,
            TextAnchor.MiddleCenter, new Color(0.68f, 1f, 0.28f));
        EndlessMenuOverlay.Anchor(title.rectTransform, 0.07f, 0.84f, 0.93f, 0.96f);
        resultText = EndlessMenuOverlay.TextObject("Result", panel.transform, string.Empty, 25,
            TextAnchor.MiddleCenter, Color.white);
        EndlessMenuOverlay.Anchor(resultText.rectTransform, 0.08f, 0.64f, 0.92f, 0.84f);
        Text boardTitle = EndlessMenuOverlay.TextObject("Board Title", panel.transform, "THÀNH TÍCH CAO", 22,
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
        root.GetComponent<Button>().onClick.AddListener(action);
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

/// <summary>Theo dõi zombie do Endless sinh mà không cần thay đổi lớp Zombie cũ.</summary>
public sealed class EndlessZombieTracker : MonoBehaviour
{
    private EndlessGameController owner;
    private Zombie zombie;
    private int points;
    private bool reported;

    public void Initialize(EndlessGameController controller, Zombie trackedZombie, int scoreValue)
    {
        owner = controller;
        zombie = trackedZombie;
        points = scoreValue;
    }

    private void LateUpdate()
    {
        if (!reported && zombie != null && zombie.bloodVolume <= 0)
            Report(true);
    }

    private void OnDestroy()
    {
        if (!reported) Report(false);
    }

    private void Report(bool killed)
    {
        reported = true;
        if (owner != null) owner.ReportZombieRemoved(points, killed);
    }
}
