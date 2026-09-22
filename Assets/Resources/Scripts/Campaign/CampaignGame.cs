using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CampaignBootstrap
{
    public const string GameScene = "CampaignScene";
    public const string SettingsScene = "CampaignSettings";
    public const string ProgressScene = "CampaignProgress";
    public const string AchievementsScene = "CampaignAchievements";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == GameScene && Object.FindAnyObjectByType<CampaignGame>() == null)
            new GameObject("Three Worlds Campaign", typeof(AudioSource), typeof(CampaignGame));
        else if ((scene.name == SettingsScene || scene.name == ProgressScene || scene.name == AchievementsScene)
                 && Object.FindAnyObjectByType<CampaignInfoScreen>() == null)
            new GameObject("Campaign Information Screen", typeof(CampaignInfoScreen));
    }
}

public enum CampaignNpcKind
{
    Scout,
    Guardian,
    Healer
}

public sealed class CampaignGame : MonoBehaviour
{
    public const float Left = -7.2f;
    public const float Right = 7.2f;
    public const float Bottom = -4.1f;
    public const float Top = 3.7f;

    private readonly string[] mapNames = { "NGHĨA ĐỊA HUYẾT NGUYỆT", "HÀNH LANG MA CÀ RỒNG", "ĐIỆN HẮC ÁM" };
    private readonly string[] objectives = { "Hạ 5 Trinh Sát", "Hạ 7 Hộ Vệ", "Hạ đội hình đủ 3 loại NPC" };
    private readonly int[] targets = { 5, 7, 12 };
    private readonly int[] maximumActiveNpcs = { 2, 3, 5 };
    private readonly float[] spawnIntervals = { 1.60f, 1.22f, 0.82f };
    private readonly string[] backgrounds =
    {
        "aset/map/map1", "aset/map/map2", "aset/map/map3"
    };

    private readonly List<CampaignNpc> npcs = new List<CampaignNpc>();
    private CampaignPlayer player;
    private Transform world;
    private SpriteRenderer background;
    private AudioSource audioSource;
    private AudioSource musicSource;
    private Text mapText;
    private Text objectiveText;
    private Text healthText;
    private Text scoreText;
    private Text aiText;
    private Text announcement;
    private Image healthFill;
    private Image flash;
    private GameObject pauseOverlay;
    private GameObject resultOverlay;
    private RectTransform fog;
    private int currentMap;
    private int health;
    private int score;
    private int defeated;
    private int spawned;
    private float nextSpawn;
    private float nextHazard;
    private float nextPlayerHeal;
    private bool hazardActive;
    private bool paused;
    private bool playing;
    private bool transitioning;

    public int CurrentMap => currentMap;
    public bool IsPlaying => playing && !paused && !transitioning;
    public bool HazardActive => hazardActive;

    private void Awake()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        BuildCameraAndWorld();
        BuildPlayer();
        BuildInterface();
        currentMap = CampaignProgress.Checkpoint;
        health = CampaignProgress.CheckpointHealth();
        score = CampaignProgress.CheckpointScore;
        BeginMap(currentMap);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && playing && !transitioning) TogglePause();
        if (!IsPlaying) return;

        if (spawned < targets[currentMap] && Time.time >= nextSpawn && ActiveNpcCount() < maximumActiveNpcs[currentMap])
        {
            SpawnNpc(spawned++);
            nextSpawn = Time.time + spawnIntervals[currentMap];
        }

        if (currentMap == 1 && Time.time >= nextHazard)
            StartCoroutine(ShadowPulse());
        if (currentMap == 2 && fog != null)
        {
            float pulse = 0.10f + Mathf.PingPong(Time.time * 0.035f, 0.16f);
            fog.GetComponent<Image>().color = new Color(0.04f, 0.02f, 0.09f, pulse);
        }
    }

    private void BuildCameraAndWorld()
    {
        var cameraObject = new GameObject("Campaign Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.04f, 0.08f, 0.035f);

        world = new GameObject("Map World").transform;
        var bg = new GameObject("Map Background", typeof(SpriteRenderer));
        bg.transform.SetParent(world, false);
        background = bg.GetComponent<SpriteRenderer>();
        background.sortingOrder = -100;

        CreateWorldStrip("Top Boundary", new Vector2(0f, 4.35f), new Vector2(17f, 0.9f), new Color(0.02f, 0.04f, 0.02f, 0.72f), -30);
        CreateWorldStrip("Bottom Boundary", new Vector2(0f, -4.55f), new Vector2(17f, 0.7f), new Color(0.02f, 0.04f, 0.02f, 0.58f), -30);

        musicSource = new GameObject("Campaign Music", typeof(AudioSource)).GetComponent<AudioSource>();
        musicSource.transform.SetParent(transform, false);
        musicSource.loop = true;
        musicSource.volume = 0.28f;
    }

    private void BuildPlayer()
    {
        var go = new GameObject("Player Samurai Archer");
        go.transform.position = new Vector3(-4.8f, 0f, 0f);
        player = go.AddComponent<CampaignPlayer>();
        player.Initialize(this);
    }

    private void BuildInterface()
    {
        CampaignUI.EnsureEventSystem();
        Canvas canvas = CampaignUI.Canvas("Three Worlds Canvas", 1000);

        Image hud = CampaignUI.Image("Top HUD", canvas.transform, new Color(0.025f, 0.065f, 0.025f, 0.9f));
        CampaignUI.Anchor(hud.rectTransform, 0f, 0.88f, 1f, 1f);
        mapText = CampaignUI.Text("Map", hud.transform, string.Empty, 28, TextAnchor.MiddleLeft, new Color(0.68f, 1f, 0.28f));
        CampaignUI.Anchor(mapText.rectTransform, 0.025f, 0.50f, 0.34f, 0.96f);
        objectiveText = CampaignUI.Text("Objective", hud.transform, string.Empty, 20, TextAnchor.MiddleLeft, Color.white);
        CampaignUI.Anchor(objectiveText.rectTransform, 0.025f, 0.05f, 0.46f, 0.52f);
        scoreText = CampaignUI.Text("Score", hud.transform, string.Empty, 25, TextAnchor.MiddleRight, new Color(1f, 0.85f, 0.25f));
        CampaignUI.Anchor(scoreText.rectTransform, 0.69f, 0.52f, 0.91f, 0.96f);
        healthText = CampaignUI.Text("Health", hud.transform, string.Empty, 20, TextAnchor.MiddleCenter, Color.white);
        CampaignUI.Anchor(healthText.rectTransform, 0.48f, 0.54f, 0.68f, 0.94f);
        Image healthTrack = CampaignUI.Image("Health Track", hud.transform, new Color(0.16f, 0.07f, 0.04f));
        CampaignUI.Anchor(healthTrack.rectTransform, 0.48f, 0.18f, 0.68f, 0.48f);
        healthFill = CampaignUI.Image("Health Fill", healthTrack.transform, new Color(0.3f, 0.9f, 0.2f));
        CampaignUI.Stretch(healthFill.rectTransform);
        healthFill.type = Image.Type.Filled;
        healthFill.fillMethod = Image.FillMethod.Horizontal;

        Button pause = CampaignUI.Button("Pause", hud.transform, "Ⅱ", new Color(0.23f, 0.4f, 0.13f), TogglePause);
        CampaignUI.Anchor(pause.GetComponent<RectTransform>(), 0.93f, 0.19f, 0.985f, 0.86f);

        aiText = CampaignUI.Text("AI Guide", canvas.transform,
            "AI: Trinh Sát dự đoán • Hộ Vệ che chắn • Hồi Phục cứu đồng minh", 20,
            TextAnchor.MiddleCenter, new Color(0.9f, 1f, 0.74f));
        CampaignUI.Anchor(aiText.rectTransform, 0.20f, 0.815f, 0.80f, 0.87f);

        announcement = CampaignUI.Text("Announcement", canvas.transform, string.Empty, 48,
            TextAnchor.MiddleCenter, new Color(0.75f, 1f, 0.3f));
        CampaignUI.Anchor(announcement.rectTransform, 0.16f, 0.40f, 0.84f, 0.62f);
        announcement.gameObject.SetActive(false);

        var stick = new GameObject("Mobile Joystick", typeof(RectTransform), typeof(Image), typeof(CampaignJoystick));
        stick.transform.SetParent(canvas.transform, false);
        CampaignUI.Anchor(stick.GetComponent<RectTransform>(), 0.035f, 0.045f, 0.18f, 0.30f);
        Image stickImage = stick.GetComponent<Image>();
        stickImage.sprite = CampaignUI.CircleSprite();
        stickImage.preserveAspect = true;
        stickImage.color = new Color(0.03f, 0.08f, 0.03f, 0.68f);
        Image knob = CampaignUI.Image("Knob", stick.transform, new Color(0.58f, 0.95f, 0.25f, 0.9f));
        knob.sprite = CampaignUI.CircleSprite();
        knob.preserveAspect = true;
        knob.rectTransform.anchorMin = knob.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        knob.rectTransform.sizeDelta = new Vector2(78f, 78f);
        stick.GetComponent<CampaignJoystick>().SetKnob(knob.rectTransform);
        player.SetJoystick(stick.GetComponent<CampaignJoystick>());

        Button fire = CampaignUI.Button("Fire", canvas.transform, "BẮN", new Color(0.82f, 0.28f, 0.06f), player.Fire);
        CampaignUI.Anchor(fire.GetComponent<RectTransform>(), 0.84f, 0.065f, 0.965f, 0.265f);

        Button melee = CampaignUI.Button("Melee", canvas.transform, "CHÉM", new Color(0.58f, 0.12f, 0.14f), player.Melee);
        CampaignUI.Anchor(melee.GetComponent<RectTransform>(), 0.70f, 0.075f, 0.82f, 0.245f);

        Text controls = CampaignUI.Text("Controls", canvas.transform, "WASD di chuyển  •  SPACE bắn  •  J chém  •  Q hồi máu (<50 HP)", 18,
            TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.75f));
        CampaignUI.Anchor(controls.rectTransform, 0.29f, 0.015f, 0.71f, 0.06f);

        fog = CampaignUI.Image("Graveyard Fog", canvas.transform, Color.clear).rectTransform;
        CampaignUI.Stretch(fog);
        fog.SetAsFirstSibling();
        fog.GetComponent<Image>().raycastTarget = false;

        flash = CampaignUI.Image("Result Flash", canvas.transform, Color.clear);
        CampaignUI.Stretch(flash.rectTransform);
        flash.raycastTarget = false;
        pauseOverlay = BuildPauseOverlay(canvas.transform);
        resultOverlay = BuildResultOverlay(canvas.transform);
        UpdateHud();
    }

    private GameObject BuildPauseOverlay(Transform parent)
    {
        Image shade = CampaignUI.Image("Pause Overlay", parent, new Color(0f, 0f, 0f, 0.82f));
        CampaignUI.Stretch(shade.rectTransform);
        Image panel = CampaignUI.Image("Panel", shade.transform, new Color(0.08f, 0.16f, 0.055f, 0.98f));
        CampaignUI.Anchor(panel.rectTransform, 0.32f, 0.20f, 0.68f, 0.80f);
        Text title = CampaignUI.Text("Title", panel.transform, "TẠM DỪNG", 48, TextAnchor.MiddleCenter, new Color(0.7f, 1f, 0.3f));
        CampaignUI.Anchor(title.rectTransform, 0.08f, 0.75f, 0.92f, 0.94f);
        Button resume = CampaignUI.Button("Resume", panel.transform, "TIẾP TỤC", new Color(0.45f, 0.7f, 0.14f), TogglePause);
        CampaignUI.Anchor(resume.GetComponent<RectTransform>(), 0.16f, 0.52f, 0.84f, 0.69f);
        Button progress = CampaignUI.Button("Progress", panel.transform, "TIẾN TRÌNH", new Color(0.25f, 0.52f, 0.65f), () => Load(CampaignBootstrap.ProgressScene));
        CampaignUI.Anchor(progress.GetComponent<RectTransform>(), 0.16f, 0.31f, 0.84f, 0.48f);
        Button home = CampaignUI.Button("Home", panel.transform, "TRANG CHỦ", new Color(0.28f, 0.4f, 0.18f), () => Load("MainMenu"));
        CampaignUI.Anchor(home.GetComponent<RectTransform>(), 0.16f, 0.10f, 0.84f, 0.27f);
        shade.gameObject.SetActive(false);
        return shade.gameObject;
    }

    private GameObject BuildResultOverlay(Transform parent)
    {
        Image shade = CampaignUI.Image("Result Overlay", parent, new Color(0f, 0f, 0f, 0.86f));
        CampaignUI.Stretch(shade.rectTransform);
        Canvas nested = shade.gameObject.AddComponent<Canvas>();
        nested.overrideSorting = true;
        nested.sortingOrder = 1400;
        shade.gameObject.AddComponent<GraphicRaycaster>();
        shade.gameObject.SetActive(false);
        return shade.gameObject;
    }

    private void BeginMap(int map)
    {
        currentMap = Mathf.Clamp(map, 0, 2);
        defeated = 0;
        spawned = 0;
        nextSpawn = Time.time + 1.1f;
        nextHazard = Time.time + 4f;
        hazardActive = false;
        transitioning = false;
        playing = true;
        ClearNpcs();
        player.transform.position = new Vector3(-4.8f, 0f, 0f);

        background.sprite = Resources.Load<Sprite>(backgrounds[currentMap]);
        background.color = Color.white;
        if (background.sprite != null)
        {
            Vector2 size = background.sprite.bounds.size;
            Camera camera = Camera.main;
            float viewHeight = camera != null ? camera.orthographicSize * 2f : 10f;
            float viewWidth = camera != null ? viewHeight * camera.aspect : 17.8f;
            float coverScale = Mathf.Max(viewWidth / size.x, viewHeight / size.y);
            background.transform.localScale = new Vector3(coverScale, coverScale, 1f);
        }

        string music = currentMap == 0 ? "Sounds/Background/Music_Day"
            : currentMap == 1 ? "Sounds/Background/Music_Ice" : "Sounds/Background/Music_Night_Bone";
        musicSource.clip = Resources.Load<AudioClip>(music);
        if (CampaignProgress.MusicEnabled && musicSource.clip != null) musicSource.Play();
        fog.gameObject.SetActive(currentMap == 2);
        UpdateHud();
        StartCoroutine(Announce("MAP " + (currentMap + 1) + "\n" + mapNames[currentMap], 1.7f));
    }

    private void SpawnNpc(int sequence)
    {
        Vector3 at = RandomEdgePosition();
        GameObject go = new GameObject("NPC " + sequence);
        go.transform.position = at;
        CampaignNpc npc;
        CampaignNpcKind kind = SpawnKindFor(currentMap, sequence);
        if (kind == CampaignNpcKind.Scout)
        {
            npc = go.AddComponent<ScoutNpc>();
            npc.Initialize(this, player, 55f + currentMap * 10f, 2.15f + currentMap * 0.12f,
                Color.white, CampaignArt.ScoutPack);
        }
        else if (kind == CampaignNpcKind.Guardian)
        {
            npc = go.AddComponent<GuardianNpc>();
            npc.Initialize(this, player, 100f + currentMap * 10f, 1.48f + currentMap * 0.08f,
                Color.white, CampaignArt.GuardianPack);
        }
        else
        {
            npc = go.AddComponent<HealerNpc>();
            npc.Initialize(this, player, 62f, 1.75f, new Color(1f, 0.72f, 1f), CampaignArt.HealerPack);
        }
        npcs.Add(npc);
    }

    /// <summary>Quy tắc spawn có thể kiểm thử độc lập: Map 1=A, Map 2=B, Map 3=A/B/C.</summary>
    public static CampaignNpcKind SpawnKindFor(int map, int sequence)
    {
        if (map <= 0) return CampaignNpcKind.Scout;
        if (map == 1) return CampaignNpcKind.Guardian;
        int index = Mathf.Abs(sequence) % 3;
        return index == 0 ? CampaignNpcKind.Scout
            : index == 1 ? CampaignNpcKind.Guardian : CampaignNpcKind.Healer;
    }

    public CampaignNpc FindNpcNear(Vector3 position, float range)
    {
        CampaignNpc nearest = null;
        float best = range;
        for (int i = npcs.Count - 1; i >= 0; i--)
        {
            if (npcs[i] == null) { npcs.RemoveAt(i); continue; }
            float distance = Vector2.Distance(position, npcs[i].transform.position);
            if (distance <= best) { best = distance; nearest = npcs[i]; }
        }
        return nearest;
    }

    public CampaignNpc FindWeakestNpc(CampaignNpc except)
    {
        CampaignNpc weak = null;
        float ratio = 1.01f;
        foreach (CampaignNpc npc in npcs)
        {
            if (npc == null || npc == except || !npc.Alive || npc.HealthRatio >= ratio) continue;
            weak = npc;
            ratio = npc.HealthRatio;
        }
        return weak;
    }

    public void RouteProjectileHit(CampaignNpc target, int damage)
    {
        GuardianNpc blocker = null;
        float best = 1.75f;
        foreach (CampaignNpc npc in npcs)
        {
            GuardianNpc guard = npc as GuardianNpc;
            if (guard == null || guard == target || !guard.ShieldReady) continue;
            float distance = Vector2.Distance(guard.transform.position, target.transform.position);
            if (distance < best) { best = distance; blocker = guard; }
        }
        if (blocker != null) blocker.BlockDamage(damage);
        else target.TakeDamage(damage);
        PlaySfx("Sounds/Zombies/bodyhit1", 0.55f);
    }

    public void RouteSkillHit(CampaignNpc target, int damage)
    {
        if (target == null || !target.Alive) return;
        target.TakeDamage(damage);
        PlaySfx("Sounds/Zombies/bodyhit2", 0.58f);
    }

    public void NpcDefeated(CampaignNpc npc)
    {
        defeated++;
        score += 100 + currentMap * 50;
        CampaignFloatingText.Show(npc.transform.position, "+" + (100 + currentMap * 50), new Color(1f, 0.85f, 0.25f));
        UpdateHud();
        if (defeated >= targets[currentMap]) StartCoroutine(CompleteMap());
    }

    public void DamagePlayer(int amount)
    {
        health = Mathf.Max(0, health - amount);
        PlaySfx("Sounds/Zombies/chomp1", 0.65f);
        StartCoroutine(DamageFlash());
        UpdateHud();
        if (health <= 0)
        {
            player.Die();
            ShowGameOver();
        }
    }

    /// <summary>
    /// Q chỉ có hiệu lực khi HP dưới 50. Hồi 30 HP và khóa 8 giây để người chơi
    /// không thể liên tục hồi máu trong lúc đang nhận sát thương.
    /// </summary>
    public bool TryHealPlayer(Vector3 playerPosition)
    {
        if (!IsPlaying) return false;
        if (health >= CampaignCombatTuning.HealThreshold)
        {
            CampaignFloatingText.Show(playerPosition, "HP PHẢI DƯỚI 50", new Color(1f, 0.82f, 0.25f));
            return false;
        }
        if (Time.time < nextPlayerHeal)
        {
            int seconds = Mathf.CeilToInt(nextPlayerHeal - Time.time);
            CampaignFloatingText.Show(playerPosition, "HỒI MÁU: " + seconds + "s", new Color(0.55f, 0.85f, 1f));
            return false;
        }

        int restored = Mathf.Min(CampaignCombatTuning.HealAmount, 100 - health);
        health += restored;
        nextPlayerHeal = Time.time + CampaignCombatTuning.HealCooldown;
        CampaignFloatingText.Show(playerPosition, "+" + restored + " HP", new Color(0.3f, 1f, 0.42f));
        PlaySfx("Sounds/UI/buttonClick", 0.55f);
        UpdateHud();
        return true;
    }

    private IEnumerator CompleteMap()
    {
        if (transitioning) yield break;
        transitioning = true;
        playing = false;
        score += health * 5;
        int nextHealth = Mathf.Min(100, health + 25);
        CampaignProgress.CompleteMap(currentMap, score, nextHealth);
        PlaySfx("Sounds/UI/winMusic", 0.75f);
        yield return Announce("HOÀN THÀNH " + mapNames[currentMap] + "\nĐÃ LƯU CHECKPOINT", 2.2f);
        if (currentMap >= 2) ShowWin();
        else
        {
            health = nextHealth;
            BeginMap(currentMap + 1);
        }
    }

    private void ShowGameOver()
    {
        if (!playing) return;
        playing = false;
        transitioning = true;
        CampaignProgress.RecordLoss(score);
        musicSource.Stop();
        PlaySfx("Sounds/UI/loseMusic", 0.95f);
        StartCoroutine(ResultEffect(new Color(0.8f, 0.05f, 0.03f), true));
        BuildResultContents(false);
    }

    private void ShowWin()
    {
        playing = false;
        CampaignProgress.RecordWin(score);
        musicSource.Stop();
        PlaySfx("Sounds/UI/winMusic", 1f);
        StartCoroutine(ResultEffect(new Color(0.55f, 1f, 0.18f), false));
        BuildResultContents(true);
        for (int i = 0; i < 36; i++) StartCoroutine(SpawnConfetti(i * 0.035f));
    }

    private void BuildResultContents(bool won)
    {
        foreach (Transform child in resultOverlay.transform) Destroy(child.gameObject);
        Image panel = CampaignUI.Image("Result Panel", resultOverlay.transform,
            won ? new Color(0.07f, 0.22f, 0.08f, 0.98f) : new Color(0.24f, 0.055f, 0.035f, 0.98f));
        CampaignUI.Anchor(panel.rectTransform, 0.25f, 0.13f, 0.75f, 0.87f);
        Text title = CampaignUI.Text("Title", panel.transform, won ? "CHIẾN THẮNG!" : "GAME OVER", 58,
            TextAnchor.MiddleCenter, won ? new Color(0.7f, 1f, 0.25f) : new Color(1f, 0.3f, 0.18f));
        CampaignUI.Anchor(title.rectTransform, 0.06f, 0.76f, 0.94f, 0.94f);
        string message = won
            ? "Bạn đã bảo vệ cả Ba Cõi!\nĐiểm: " + score + "  •  Kỷ lục: " + CampaignProgress.BestScore
            : "Chiến binh đã kiệt sức tại Map " + (currentMap + 1) + ".\nCheckpoint gần nhất vẫn được giữ. Điểm: " + score;
        Text body = CampaignUI.Text("Message", panel.transform, message, 27, TextAnchor.MiddleCenter, Color.white);
        CampaignUI.Anchor(body.rectTransform, 0.08f, 0.55f, 0.92f, 0.77f);

        Button replay = CampaignUI.Button("Replay", panel.transform, "CHƠI LẠI", new Color(0.45f, 0.72f, 0.14f), Restart);
        CampaignUI.Anchor(replay.GetComponent<RectTransform>(), 0.14f, 0.36f, 0.86f, 0.51f);
        Button home = CampaignUI.Button("Home", panel.transform, "TRANG CHỦ", new Color(0.29f, 0.42f, 0.18f), () => Load("MainMenu"));
        CampaignUI.Anchor(home.GetComponent<RectTransform>(), 0.14f, 0.19f, 0.86f, 0.34f);
        string thirdLabel = won ? "THÀNH TÍCH" : "CÀI ĐẶT";
        string thirdScene = won ? CampaignBootstrap.AchievementsScene : CampaignBootstrap.SettingsScene;
        Button third = CampaignUI.Button("Third Navigation", panel.transform, thirdLabel, new Color(0.25f, 0.48f, 0.63f), () => Load(thirdScene));
        CampaignUI.Anchor(third.GetComponent<RectTransform>(), 0.14f, 0.02f, 0.86f, 0.17f);
        resultOverlay.SetActive(true);
    }

    private IEnumerator ResultEffect(Color color, bool shake)
    {
        Vector3 original = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
        for (int i = 0; i < 12; i++)
        {
            float alpha = (1f - i / 12f) * 0.55f;
            flash.color = new Color(color.r, color.g, color.b, alpha);
            if (shake && CampaignProgress.ShakeEnabled && Camera.main != null)
                Camera.main.transform.position = original + (Vector3)Random.insideUnitCircle * 0.12f;
            yield return new WaitForSecondsRealtime(0.045f);
        }
        flash.color = Color.clear;
        if (Camera.main != null) Camera.main.transform.position = original;
    }

    private IEnumerator SpawnConfetti(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        Image bit = CampaignUI.Image("Confetti", resultOverlay.transform, Color.HSVToRGB(Random.value, 0.8f, 1f));
        RectTransform rect = bit.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(Random.value, 1.05f);
        rect.sizeDelta = new Vector2(12f, 24f);
        float x = rect.anchorMin.x;
        float elapsed = 0f;
        while (elapsed < 2.2f && bit != null)
        {
            elapsed += Time.unscaledDeltaTime;
            rect.anchorMin = rect.anchorMax = new Vector2(x + Mathf.Sin(elapsed * 5f) * 0.025f, 1.05f - elapsed * 0.52f);
            rect.localRotation = Quaternion.Euler(0f, 0f, elapsed * 250f);
            yield return null;
        }
        if (bit != null) Destroy(bit.gameObject);
    }

    private IEnumerator ShadowPulse()
    {
        nextHazard = Time.time + 8f;
        hazardActive = true;
        StartCoroutine(Announce("LỜI NGUYỀN BÓNG TỐI — DI CHUYỂN BỊ CHẬM", 1.2f));
        flash.color = new Color(0.55f, 0.05f, 0.18f, 0.24f);
        yield return new WaitForSeconds(2.6f);
        flash.color = Color.clear;
        hazardActive = false;
    }

    private IEnumerator DamageFlash()
    {
        flash.color = new Color(1f, 0.06f, 0.02f, 0.32f);
        yield return new WaitForSeconds(0.12f);
        flash.color = Color.clear;
    }

    private IEnumerator Announce(string message, float duration)
    {
        announcement.text = message;
        announcement.gameObject.SetActive(true);
        yield return new WaitForSeconds(duration);
        announcement.gameObject.SetActive(false);
    }

    private void TogglePause()
    {
        if (!playing || transitioning) return;
        paused = !paused;
        pauseOverlay.SetActive(paused);
        Time.timeScale = paused ? 0f : 1f;
        AudioListener.pause = paused;
    }

    private void Restart()
    {
        RestoreTime();
        SceneManager.LoadScene(CampaignBootstrap.GameScene);
    }

    private void Load(string scene)
    {
        RestoreTime();
        SceneManager.LoadScene(scene);
    }

    private void UpdateHud()
    {
        if (mapText == null) return;
        mapText.text = "MAP " + (currentMap + 1) + "/3  •  " + mapNames[currentMap];
        objectiveText.text = objectives[currentMap] + "  [" + defeated + "/" + targets[currentMap] + "]";
        scoreText.text = "ĐIỂM  " + score.ToString("N0");
        healthText.text = "SINH LỰC  " + health + "/100";
        healthFill.fillAmount = health / 100f;
        healthFill.color = health > 55 ? new Color(0.3f, 0.9f, 0.2f) : health > 25 ? new Color(1f, 0.7f, 0.12f) : new Color(1f, 0.16f, 0.08f);
    }

    public Vector3 ClampToArena(Vector3 value)
    {
        value.x = Mathf.Clamp(value.x, Left, Right);
        value.y = Mathf.Clamp(value.y, Bottom, Top);
        value.z = 0f;
        return value;
    }

    public void PlaySfx(string resource, float volume)
    {
        if (!CampaignProgress.SfxEnabled) return;
        AudioClip clip = Resources.Load<AudioClip>(resource);
        if (clip != null) audioSource.PlayOneShot(clip, volume);
    }

    private int ActiveNpcCount()
    {
        int count = 0;
        foreach (CampaignNpc npc in npcs) if (npc != null && npc.Alive) count++;
        return count;
    }

    private Vector3 RandomEdgePosition()
    {
        int edge = Random.Range(0, 3);
        if (edge == 0) return new Vector3(Right - 0.3f, Random.Range(Bottom + 0.5f, Top - 0.5f));
        if (edge == 1) return new Vector3(Random.Range(-1f, Right - 0.5f), Top - 0.25f);
        return new Vector3(Random.Range(-1f, Right - 0.5f), Bottom + 0.25f);
    }

    private void ClearNpcs()
    {
        foreach (CampaignNpc npc in npcs) if (npc != null) Destroy(npc.gameObject);
        npcs.Clear();
        foreach (CampaignProjectile projectile in Object.FindObjectsByType<CampaignProjectile>())
            Destroy(projectile.gameObject);
        foreach (CampaignMeleeHitbox hitbox in Object.FindObjectsByType<CampaignMeleeHitbox>())
            Destroy(hitbox.gameObject);
    }

    private void CreateWorldStrip(string name, Vector2 position, Vector2 scale, Color color, int order)
    {
        var go = new GameObject(name, typeof(SpriteRenderer));
        go.transform.SetParent(world, false);
        go.transform.position = position;
        go.transform.localScale = scale;
        SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
        renderer.sprite = CampaignUI.SolidSprite();
        renderer.color = color;
        renderer.sortingOrder = order;
    }

    private void RestoreTime()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    private void OnDestroy() => RestoreTime();
}
