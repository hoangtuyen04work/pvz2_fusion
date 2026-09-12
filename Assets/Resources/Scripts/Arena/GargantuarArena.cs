using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GargantuarArenaBootstrap
{
    public const string SceneName = "GargantuarArena";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == SceneName && Object.FindAnyObjectByType<GargantuarArenaGame>() == null)
        {
            var root = new GameObject("Gargantuar Arena", typeof(AudioSource));
            root.AddComponent<GargantuarArenaGame>();
        }
    }
}

public sealed class GargantuarArenaGame : MonoBehaviour
{
    public const float Left = -6.15f;
    public const float Right = 6.15f;
    public const float Bottom = -3.95f;
    public const float Top = 3.55f;

    private ArenaPeashooter player;
    private ArenaGargantuar gargantuar;
    private ArenaJoystick joystick;
    private Text scoreText;
    private Text bestText;
    private Text livesText;
    private Text bossText;
    private Image bossFill;
    private GameObject pauseOverlay;
    private GameObject gameOverOverlay;
    private AudioSource audioSource;
    private Font font;
    private int score;
    private int kills;
    private int lives = 3;
    private bool paused;
    private bool ended;

    private void Awake()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        font = Resources.Load<Font>("Fonts/Baloo2");

        BuildCameraAndWorld();
        BuildPlayer();
        BuildInterface();
        BuildGargantuar();
        UpdateHud();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !ended)
            SetPaused(!paused);
    }

    private void BuildCameraAndWorld()
    {
        var cameraObject = new GameObject("Arena Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        var camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.16f, 0.06f);

        var background = new GameObject("Lawn Background", typeof(SpriteRenderer));
        var renderer = background.GetComponent<SpriteRenderer>();
        renderer.sprite = Resources.Load<Sprite>("Sprites/BackGround/Background_Day");
        renderer.sortingOrder = -100;
        renderer.color = new Color(0.78f, 0.92f, 0.72f);
        if (renderer.sprite != null)
        {
            Vector2 size = renderer.sprite.bounds.size;
            background.transform.localScale = new Vector3(18f / size.x, 10f / size.y, 1f);
        }

        CreateWorldPanel("Top Shade", new Vector2(0f, 4.45f), new Vector2(18f, 1.1f), new Color(0.025f, 0.08f, 0.025f, 0.78f), -20);
        CreateWorldPanel("Bottom Shade", new Vector2(0f, -4.65f), new Vector2(18f, 0.7f), new Color(0.025f, 0.08f, 0.025f, 0.58f), -20);
    }

    private void BuildPlayer()
    {
        var playerObject = new GameObject("Player Peashooter", typeof(ArenaPeashooter), typeof(AudioSource));
        playerObject.transform.position = new Vector3(-4.7f, -0.2f, 0f);
        player = playerObject.GetComponent<ArenaPeashooter>();
        player.Initialize(this);
    }

    private void BuildGargantuar()
    {
        var gargantuarObject = new GameObject("Gargantuar", typeof(ArenaGargantuar), typeof(AudioSource));
        gargantuarObject.transform.position = new Vector3(5.1f, 0f, 0f);
        gargantuar = gargantuarObject.GetComponent<ArenaGargantuar>();
        gargantuar.Initialize(this, player.transform);
    }

    private void BuildInterface()
    {
        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        var canvasObject = new GameObject("Arena Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.matchWidthOrHeight = 0.5f;

        var title = CreateText("Title", canvasObject.transform, "ĐẤU TRƯỜNG GARGANTUAR", 34, TextAnchor.MiddleCenter, new Color(0.76f, 1f, 0.3f));
        SetAnchors(title.rectTransform, 0.30f, 0.925f, 0.70f, 0.995f);

        scoreText = CreateText("Score", canvasObject.transform, string.Empty, 30, TextAnchor.MiddleLeft, Color.white);
        SetAnchors(scoreText.rectTransform, 0.025f, 0.93f, 0.23f, 0.99f);
        bestText = CreateText("Best", canvasObject.transform, string.Empty, 22, TextAnchor.MiddleLeft, new Color(1f, 0.86f, 0.3f));
        SetAnchors(bestText.rectTransform, 0.025f, 0.885f, 0.23f, 0.94f);
        livesText = CreateText("Lives", canvasObject.transform, string.Empty, 28, TextAnchor.MiddleRight, Color.white);
        SetAnchors(livesText.rectTransform, 0.78f, 0.93f, 0.91f, 0.99f);

        var pause = CreateButton("Pause", canvasObject.transform, "Ⅱ", new Color(0.16f, 0.32f, 0.10f, 0.94f), TogglePause);
        SetAnchors(pause.GetComponent<RectTransform>(), 0.925f, 0.925f, 0.98f, 0.985f);

        var bossTrack = CreateImage("Boss Health Track", canvasObject.transform, new Color(0.08f, 0.06f, 0.035f, 0.92f));
        SetAnchors(bossTrack.rectTransform, 0.34f, 0.865f, 0.66f, 0.907f);
        bossFill = CreateImage("Boss Health", bossTrack.transform, new Color(0.72f, 0.12f, 0.08f, 1f));
        Stretch(bossFill.rectTransform);
        bossFill.type = Image.Type.Filled;
        bossFill.fillMethod = Image.FillMethod.Horizontal;
        bossText = CreateText("Boss Health Text", bossTrack.transform, string.Empty, 21, TextAnchor.MiddleCenter, Color.white);
        Stretch(bossText.rectTransform);

        var stickObject = new GameObject("Joystick", typeof(RectTransform), typeof(Image), typeof(ArenaJoystick));
        stickObject.transform.SetParent(canvasObject.transform, false);
        SetAnchors(stickObject.GetComponent<RectTransform>(), 0.035f, 0.045f, 0.19f, 0.32f);
        stickObject.GetComponent<Image>().color = new Color(0.05f, 0.12f, 0.035f, 0.48f);
        joystick = stickObject.GetComponent<ArenaJoystick>();
        var knob = CreateImage("Knob", stickObject.transform, new Color(0.56f, 0.9f, 0.22f, 0.78f));
        knob.rectTransform.anchorMin = knob.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        knob.rectTransform.sizeDelta = new Vector2(82f, 82f);
        joystick.SetKnob(knob.rectTransform);
        player.SetJoystick(joystick);

        var fire = CreateButton("Fire", canvasObject.transform, "BẮN", new Color(0.82f, 0.23f, 0.07f, 0.94f), player.Fire);
        SetAnchors(fire.GetComponent<RectTransform>(), 0.815f, 0.07f, 0.955f, 0.255f);
        fire.AddComponent<ArenaPulseButton>();

        var hint = CreateText("Hint", canvasObject.transform, "WASD / PHÍM MŨI TÊN để di chuyển  •  SPACE / CTRL để bắn", 21, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.86f));
        SetAnchors(hint.rectTransform, 0.23f, 0.012f, 0.77f, 0.065f);

        pauseOverlay = BuildOverlay(canvasObject.transform, "TẠM DỪNG", false);
        gameOverOverlay = BuildOverlay(canvasObject.transform, "HẾT LƯỢT!", true);
    }

    private GameObject BuildOverlay(Transform parent, string heading, bool gameOver)
    {
        var overlay = new GameObject(heading, typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(parent, false);
        Stretch(overlay.GetComponent<RectTransform>());
        overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);

        var panel = CreateImage("Panel", overlay.transform, new Color(0.08f, 0.16f, 0.055f, 0.98f));
        SetAnchors(panel.rectTransform, 0.31f, 0.24f, 0.69f, 0.76f);
        var title = CreateText("Heading", panel.transform, heading, 54, TextAnchor.MiddleCenter, new Color(0.68f, 1f, 0.28f));
        SetAnchors(title.rectTransform, 0.07f, 0.68f, 0.93f, 0.92f);

        if (gameOver)
        {
            var result = CreateText("Result", panel.transform, "Gargantuar đã bắt được bạn.\nHãy thử phá kỷ lục một lần nữa!", 26, TextAnchor.MiddleCenter, Color.white);
            SetAnchors(result.rectTransform, 0.08f, 0.44f, 0.92f, 0.7f);
            var retry = CreateButton("Retry", panel.transform, "CHƠI LẠI", new Color(0.46f, 0.72f, 0.12f, 1f), Restart);
            SetAnchors(retry.GetComponent<RectTransform>(), 0.18f, 0.24f, 0.82f, 0.40f);
            var menu = CreateButton("Menu", panel.transform, "VỀ MENU", new Color(0.25f, 0.38f, 0.18f, 1f), ReturnToMenu);
            SetAnchors(menu.GetComponent<RectTransform>(), 0.18f, 0.07f, 0.82f, 0.21f);
        }
        else
        {
            var resume = CreateButton("Resume", panel.transform, "TIẾP TỤC", new Color(0.46f, 0.72f, 0.12f, 1f), TogglePause);
            SetAnchors(resume.GetComponent<RectTransform>(), 0.18f, 0.43f, 0.82f, 0.59f);
            var retry = CreateButton("Retry", panel.transform, "CHƠI LẠI", new Color(0.34f, 0.52f, 0.16f, 1f), Restart);
            SetAnchors(retry.GetComponent<RectTransform>(), 0.18f, 0.25f, 0.82f, 0.41f);
            var menu = CreateButton("Menu", panel.transform, "VỀ MENU", new Color(0.25f, 0.38f, 0.18f, 1f), ReturnToMenu);
            SetAnchors(menu.GetComponent<RectTransform>(), 0.18f, 0.07f, 0.82f, 0.23f);
        }

        overlay.SetActive(false);
        return overlay;
    }

    public void PlayerHit(Vector2 gargantuarPosition)
    {
        if (ended || player.IsInvulnerable) return;
        lives--;
        player.TakeHit(gargantuarPosition);
        UpdateHud();
        if (lives <= 0)
        {
            ended = true;
            int best = Mathf.Max(PlayerPrefs.GetInt("GargantuarArenaBest", 0), score);
            PlayerPrefs.SetInt("GargantuarArenaBest", best);
            PlayerPrefs.Save();
            gameOverOverlay.SetActive(true);
            Time.timeScale = 0f;
        }
    }

    public void GargantuarHit(int health, int maximum)
    {
        if (bossFill == null || bossText == null) return;
        bossFill.fillAmount = Mathf.Clamp01(health / (float)maximum);
        bossText.text = "GARGANTUAR  " + Mathf.Max(0, health) + " / " + maximum;
    }

    public void GargantuarKilled()
    {
        score += 100;
        kills++;
        int best = Mathf.Max(PlayerPrefs.GetInt("GargantuarArenaBest", 0), score);
        PlayerPrefs.SetInt("GargantuarArenaBest", best);
        UpdateHud();
    }

    public void PlayHitSound()
    {
        var clip = Resources.Load<AudioClip>("Sounds/Zombies/bodyhit1");
        if (clip != null) audioSource.PlayOneShot(clip, 0.75f);
    }

    private void UpdateHud()
    {
        scoreText.text = "ĐIỂM  " + score + "   •   HẠ  " + kills;
        bestText.text = "KỶ LỤC  " + Mathf.Max(PlayerPrefs.GetInt("GargantuarArenaBest", 0), score);
        livesText.text = "MẠNG  " + new string('♥', Mathf.Max(0, lives));
    }

    private void TogglePause() => SetPaused(!paused);

    private void SetPaused(bool value)
    {
        if (ended) return;
        paused = value;
        pauseOverlay.SetActive(value);
        Time.timeScale = value ? 0f : 1f;
        AudioListener.pause = value;
    }

    private void Restart()
    {
        RestoreTime();
        SceneManager.LoadScene(GargantuarArenaBootstrap.SceneName);
    }

    private void ReturnToMenu()
    {
        RestoreTime();
        SceneManager.LoadScene("MainMenu");
    }

    private void RestoreTime()
    {
        paused = false;
        AudioListener.pause = false;
        Time.timeScale = 1f;
    }

    private void OnDestroy() => RestoreTime();

    private static void CreateWorldPanel(string name, Vector2 position, Vector2 size, Color color, int order)
    {
        var texture = Texture2D.whiteTexture;
        var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 1f);
        var go = new GameObject(name, typeof(SpriteRenderer));
        go.transform.position = position;
        go.transform.localScale = size;
        var renderer = go.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = order;
    }

    private GameObject CreateButton(string name, Transform parent, string label, Color color, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        go.GetComponent<Button>().onClick.AddListener(action);
        var outline = go.GetComponent<Outline>();
        outline.effectColor = new Color(0.02f, 0.04f, 0.01f, 0.9f);
        outline.effectDistance = new Vector2(3f, -3f);
        var text = CreateText("Label", go.transform, label, 31, TextAnchor.MiddleCenter, Color.white);
        Stretch(text.rectTransform);
        text.raycastTarget = false;
        return go;
    }

    private Text CreateText(string name, Transform parent, string value, int size, TextAnchor alignment, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>();
        text.font = font;
        text.fontStyle = FontStyle.Bold;
        text.text = value;
        text.fontSize = size;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 13;
        text.resizeTextMaxSize = size;
        text.alignment = alignment;
        text.color = color;
        go.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.85f);
        return text;
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static void SetAnchors(RectTransform rect, float xMin, float yMin, float xMax, float yMax)
    {
        rect.anchorMin = new Vector2(xMin, yMin);
        rect.anchorMax = new Vector2(xMax, yMax);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}

public sealed class ArenaPeashooter : MonoBehaviour
{
    private const float MoveSpeed = 4.2f;
    private const float FireDelay = 0.18f;
    private GargantuarArenaGame game;
    private ArenaJoystick joystick;
    private Transform visual;
    private Vector2 facing = Vector2.right;
    private float nextFireTime;
    private float invulnerableUntil;
    private AudioSource audioSource;

    public bool IsInvulnerable => Time.time < invulnerableUntil;

    public void Initialize(GargantuarArenaGame owner)
    {
        game = owner;
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.clip = Resources.Load<AudioClip>("Sounds/Plants/firepea");

        visual = new GameObject("Peashooter Visual", typeof(SpriteRenderer)).transform;
        visual.SetParent(transform, false);
        var renderer = visual.GetComponent<SpriteRenderer>();
        renderer.sprite = Resources.Load<Sprite>("Sprites/Plants/PeaShooterSingle");
        renderer.sortingOrder = 30;
        visual.localScale = Vector3.one * 1.35f;
        CreateShadow(transform, new Vector2(0f, -0.47f), new Vector3(0.55f, 0.25f, 1f), 20);
    }

    public void SetJoystick(ArenaJoystick value) => joystick = value;

    private void Update()
    {
        Vector2 keyboard = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        Vector2 input = keyboard.sqrMagnitude > 0.01f ? keyboard : (joystick != null ? joystick.Value : Vector2.zero);
        if (input.sqrMagnitude > 1f) input.Normalize();

        if (input.sqrMagnitude > 0.04f)
        {
            facing = input.normalized;
            transform.position += (Vector3)(input * MoveSpeed * Time.deltaTime);
            UpdateFacing();
        }

        Vector3 position = transform.position;
        position.x = Mathf.Clamp(position.x, GargantuarArenaGame.Left + 0.45f, GargantuarArenaGame.Right - 0.45f);
        float upperEdge = GargantuarArenaGame.Top - 0.45f;
        float lowerEdge = GargantuarArenaGame.Bottom + 0.45f;
        if (position.y > upperEdge)
            position.y = lowerEdge;
        else if (position.y < lowerEdge)
            position.y = upperEdge;
        transform.position = position;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.JoystickButton0))
            Fire();

        if (IsInvulnerable)
        {
            float alpha = Mathf.PingPong(Time.time * 9f, 0.75f) + 0.25f;
            visual.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, alpha);
        }
        else
        {
            visual.GetComponent<SpriteRenderer>().color = Color.white;
        }
    }

    private void UpdateFacing()
    {
        float angle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
        visual.localRotation = Quaternion.Euler(0f, 0f, angle);
        visual.GetComponent<SpriteRenderer>().flipY = facing.x < -0.01f;
    }

    public void Fire()
    {
        if (Time.time < nextFireTime || Time.timeScale == 0f) return;
        nextFireTime = Time.time + FireDelay;
        var bullet = new GameObject("Pea", typeof(SpriteRenderer), typeof(ArenaPeaBullet));
        bullet.transform.position = transform.position + (Vector3)(facing * 0.62f) + new Vector3(0f, 0.12f, 0f);
        var renderer = bullet.GetComponent<SpriteRenderer>();
        renderer.sprite = Resources.Load<Sprite>("Sprites/PlantBullet/PeaBullet/PeaBullet");
        renderer.sortingOrder = 50;
        bullet.transform.localScale = Vector3.one * 1.05f;
        bullet.GetComponent<ArenaPeaBullet>().Initialize(game, facing);
        if (audioSource.clip != null) audioSource.PlayOneShot(audioSource.clip, 0.7f);
    }

    public void TakeHit(Vector2 source)
    {
        invulnerableUntil = Time.time + 1.5f;
        Vector2 push = ((Vector2)transform.position - source).normalized;
        transform.position += (Vector3)(push * 0.8f);
    }

    internal static void CreateShadow(Transform parent, Vector2 position, Vector3 scale, int order)
    {
        var shadow = new GameObject("Shadow", typeof(SpriteRenderer));
        shadow.transform.SetParent(parent, false);
        shadow.transform.localPosition = position;
        shadow.transform.localScale = scale;
        var renderer = shadow.GetComponent<SpriteRenderer>();
        renderer.sprite = Resources.Load<Sprite>("Sprites/Items/Shadow");
        renderer.color = new Color(1f, 1f, 1f, 0.55f);
        renderer.sortingOrder = order;
    }
}

public sealed class ArenaPeaBullet : MonoBehaviour
{
    private const float Speed = 10.5f;
    private const int Damage = 20;
    private GargantuarArenaGame game;
    private Vector2 direction;

    public void Initialize(GargantuarArenaGame owner, Vector2 value)
    {
        game = owner;
        direction = value.normalized;
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * Speed * Time.deltaTime);
        var gargantuar = Object.FindAnyObjectByType<ArenaGargantuar>();
        if (gargantuar != null && gargantuar.CanBeHit && Vector2.Distance(transform.position, gargantuar.HitCenter) < 0.78f)
        {
            gargantuar.TakeDamage(Damage);
            Destroy(gameObject);
            return;
        }

        if (transform.position.x < GargantuarArenaGame.Left - 1f || transform.position.x > GargantuarArenaGame.Right + 1f ||
            transform.position.y < GargantuarArenaGame.Bottom - 1f || transform.position.y > GargantuarArenaGame.Top + 1f)
            Destroy(gameObject);
    }
}

public sealed class ArenaGargantuar : MonoBehaviour
{
    private const int MaximumHealth = 200;
    private const float MoveSpeed = 1.18f;
    private const float VisualScale = 1.08f;
    private GargantuarArenaGame game;
    private Transform player;
    private Transform visual;
    private SpriteRenderer characterRenderer;
    private Sprite[] animationFrames;
    private SpriteRenderer[] renderers;
    private AudioSource audioSource;
    private int health;
    private float verticalIntent;
    private float nextSteerTime;
    private float nextThumpTime;
    private float nextAttackTime;
    private float flashUntil;
    private float visualAlpha = 1f;
    private bool alive;
    private bool attacking;
    private bool transitioning;

    public bool CanBeHit => alive && !transitioning;
    public Vector2 HitCenter => (Vector2)transform.position + new Vector2(0f, 0.35f);

    public void Initialize(GargantuarArenaGame owner, Transform target)
    {
        game = owner;
        player = target;
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        BuildVisual();
        Spawn(true);
    }

    private void BuildVisual()
    {
        visual = new GameObject("Armored Gargantuar Visual", typeof(SpriteRenderer)).transform;
        visual.SetParent(transform, false);
        visual.localScale = Vector3.one * VisualScale;
        ArenaPeashooter.CreateShadow(transform, new Vector2(0.1f, -1.22f), new Vector3(1.2f, 0.45f, 1f), 20);

        characterRenderer = visual.GetComponent<SpriteRenderer>();
        characterRenderer.sortingOrder = 40;
        Texture2D sheet = Resources.Load<Texture2D>("Sprites/Zombies/ArmoredGargantuar/ArmoredGargantuarSpritesheet");
        animationFrames = CreateAnimationFrames(sheet);
        if (animationFrames.Length > 0)
            characterRenderer.sprite = animationFrames[0];

        Shader chromaShader = Resources.Load<Shader>("Shaders/ArenaChromaKey");
        if (chromaShader != null)
            characterRenderer.material = new Material(chromaShader);

        renderers = new[] { characterRenderer };
    }

    private static Sprite[] CreateAnimationFrames(Texture2D sheet)
    {
        if (sheet == null) return new Sprite[0];
        var frames = new Sprite[8];
        float width = sheet.width / 4f;
        float height = sheet.height / 2f;
        for (int index = 0; index < frames.Length; index++)
        {
            int column = index % 4;
            int rowFromTop = index / 4;
            float y = rowFromTop == 0 ? height : 0f;
            frames[index] = Sprite.Create(sheet, new Rect(column * width, y, width, height),
                new Vector2(0.5f, 0.5f), 160f, 0, SpriteMeshType.FullRect);
            frames[index].name = "Armored Gargantuar Frame " + index;
        }
        return frames;
    }

    private void Update()
    {
        if (!alive) return;

        bool showingHurtFrame = Time.time < flashUntil;
        if (!showingHurtFrame)
            SetColor(Color.white);

        if (transitioning) return;
        if (attacking) return;

        if (Time.time >= nextSteerTime)
        {
            float chase = Mathf.Clamp((player.position.y - transform.position.y) * 0.7f, -1f, 1f);
            verticalIntent = Mathf.Clamp(chase + Random.Range(-0.45f, 0.45f), -1f, 1f);
            nextSteerTime = Time.time + Random.Range(0.7f, 1.45f);
        }

        transform.position += new Vector3(-MoveSpeed, verticalIntent * MoveSpeed * 0.72f, 0f) * Time.deltaTime;
        Vector3 position = transform.position;
        if (position.y < GargantuarArenaGame.Bottom + 1.25f || position.y > GargantuarArenaGame.Top - 1.25f)
        {
            position.y = Mathf.Clamp(position.y, GargantuarArenaGame.Bottom + 1.25f, GargantuarArenaGame.Top - 1.25f);
            verticalIntent *= -1f;
        }
        transform.position = position;

        if (showingHurtFrame)
        {
            SetFrame(5);
            visual.localPosition = new Vector3(0.08f, 0f, 0f);
            visual.localRotation = Quaternion.Euler(0f, 0f, -3f);
        }
        else
        {
            AnimateWalk();
        }

        if (Time.time >= nextThumpTime)
        {
            nextThumpTime = Time.time + 1.45f;
            var thump = Resources.Load<AudioClip>("Sounds/Zombies/GargantuarArena/thump");
            if (thump != null) audioSource.PlayOneShot(thump, 0.42f);
        }

        if (transform.position.x < GargantuarArenaGame.Left - 1.35f)
        {
            StartCoroutine(WrapToRight());
            return;
        }

        if (Time.time >= nextAttackTime && Vector2.Distance(HitCenter, (Vector2)player.position) < 1.25f)
            StartCoroutine(SmashAttack());
    }

    private void AnimateWalk()
    {
        float stride = Mathf.Sin(Time.time * 5.7f);
        SetFrame(stride >= 0f ? 1 : 2);
        visual.localPosition = new Vector3(0f, Mathf.Abs(stride) * 0.055f, 0f);
        visual.localRotation = Quaternion.Euler(0f, 0f, stride * 1.2f);
    }

    private IEnumerator SmashAttack()
    {
        attacking = true;
        SetFrame(3);
        float elapsed = 0f;

        // Giơ cột điện lên: khoảng báo trước cho người chơi né đòn.
        while (elapsed < 0.28f)
        {
            if (!alive) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / 0.28f);
            visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, -5f, t));
            yield return null;
        }

        yield return new WaitForSeconds(0.09f);
        SetFrame(4);
        elapsed = 0f;
        while (elapsed < 0.15f)
        {
            if (!alive) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / 0.15f);
            visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-5f, 7f, t));
            yield return null;
        }

        var thump = Resources.Load<AudioClip>("Sounds/Zombies/GargantuarArena/thump");
        if (thump != null) audioSource.PlayOneShot(thump, 0.9f);
        if (Vector2.Distance(HitCenter, (Vector2)player.position) < 1.48f)
            game.PlayerHit(transform.position);

        elapsed = 0f;
        while (elapsed < 0.38f)
        {
            if (!alive) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / 0.38f);
            visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(7f, 0f, t));
            yield return null;
        }

        ResetPose();
        attacking = false;
        nextAttackTime = Time.time + 0.55f;
    }

    public void TakeDamage(int damage)
    {
        if (!alive) return;
        health -= damage;
        flashUntil = Time.time + 0.09f;
        if (!attacking) SetFrame(5);
        SetColor(new Color(1f, 0.36f, 0.26f));
        game.PlayHitSound();
        game.GargantuarHit(health, MaximumHealth);
        if (health <= 0)
            StartCoroutine(DieAndRespawn());
    }

    private IEnumerator DieAndRespawn()
    {
        alive = false;
        attacking = false;
        transitioning = true;
        SetFrame(6);
        game.GargantuarKilled();
        var death = Resources.Load<AudioClip>("Sounds/Zombies/GargantuarArena/death");
        if (death != null) audioSource.PlayOneShot(death, 0.85f);
        float elapsed = 0f;
        while (elapsed < 0.55f)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= 0.26f) SetFrame(7);
            visual.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, 88f, elapsed / 0.55f));
            visual.localScale = Vector3.one * Mathf.Lerp(VisualScale, 0.82f, elapsed / 0.55f);
            SetAlpha(1f - elapsed / 0.55f);
            yield return null;
        }
        visual.gameObject.SetActive(false);
        yield return new WaitForSeconds(0.55f);
        Spawn(false);
    }

    private void Spawn(bool center)
    {
        health = MaximumHealth;
        alive = true;
        attacking = false;
        transitioning = true;
        visual.gameObject.SetActive(true);
        visual.localScale = Vector3.one * 0.78f;
        visual.localRotation = Quaternion.identity;
        transform.position = new Vector3(GargantuarArenaGame.Right - 1.1f,
            center ? 0f : Random.Range(GargantuarArenaGame.Bottom + 1.3f, GargantuarArenaGame.Top - 1.3f), 0f);
        nextSteerTime = 0f;
        nextThumpTime = Time.time + 0.4f;
        nextAttackTime = Time.time + 0.8f;
        ResetPose();
        SetFrame(0);
        SetColor(Color.white);
        SetAlpha(0f);
        game.GargantuarHit(health, MaximumHealth);
        StartCoroutine(SpawnAnimation());
    }

    private IEnumerator SpawnAnimation()
    {
        float elapsed = 0f;
        while (elapsed < 0.32f)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / 0.32f);
            visual.localScale = Vector3.one * Mathf.Lerp(0.78f, VisualScale, t);
            SetAlpha(t);
            yield return null;
        }
        visual.localScale = Vector3.one * VisualScale;
        SetAlpha(1f);
        transitioning = false;
    }

    private IEnumerator WrapToRight()
    {
        transitioning = true;
        float elapsed = 0f;
        while (elapsed < 0.16f)
        {
            elapsed += Time.deltaTime;
            SetAlpha(1f - elapsed / 0.16f);
            yield return null;
        }

        transform.position = new Vector3(GargantuarArenaGame.Right + 1.15f,
            Random.Range(GargantuarArenaGame.Bottom + 1.3f, GargantuarArenaGame.Top - 1.3f), 0f);
        nextSteerTime = 0f;
        elapsed = 0f;
        while (elapsed < 0.22f)
        {
            elapsed += Time.deltaTime;
            SetAlpha(elapsed / 0.22f);
            yield return null;
        }
        SetAlpha(1f);
        transitioning = false;
    }

    private void ResetPose()
    {
        visual.localPosition = Vector3.zero;
        visual.localRotation = Quaternion.identity;
        SetFrame(0);
    }

    private void SetFrame(int index)
    {
        if (characterRenderer != null && animationFrames != null && index >= 0 && index < animationFrames.Length)
            characterRenderer.sprite = animationFrames[index];
    }

    private void SetColor(Color color)
    {
        if (renderers == null) return;
        foreach (var renderer in renderers)
            renderer.color = new Color(color.r, color.g, color.b, visualAlpha);
    }

    private void SetAlpha(float alpha)
    {
        visualAlpha = Mathf.Clamp01(alpha);
        if (renderers == null) return;
        foreach (var renderer in renderers)
        {
            Color color = renderer.color;
            color.a = visualAlpha;
            renderer.color = color;
        }
    }
}

public sealed class ArenaJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private RectTransform rect;
    private RectTransform knob;
    public Vector2 Value { get; private set; }

    private void Awake() => rect = GetComponent<RectTransform>();
    public void SetKnob(RectTransform value) => knob = value;
    public void OnPointerDown(PointerEventData eventData) => UpdateValue(eventData);
    public void OnDrag(PointerEventData eventData) => UpdateValue(eventData);

    public void OnPointerUp(PointerEventData eventData)
    {
        Value = Vector2.zero;
        if (knob != null) knob.anchoredPosition = Vector2.zero;
    }

    private void UpdateValue(PointerEventData eventData)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out Vector2 local))
            return;
        Vector2 radius = rect.rect.size * 0.5f;
        Value = new Vector2(local.x / Mathf.Max(1f, radius.x), local.y / Mathf.Max(1f, radius.y));
        Value = Vector2.ClampMagnitude(Value, 1f);
        if (knob != null) knob.anchoredPosition = Vector2.Scale(Value, radius * 0.56f);
    }
}

public sealed class ArenaPulseButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public void OnPointerDown(PointerEventData eventData) => transform.localScale = Vector3.one * 0.92f;
    public void OnPointerUp(PointerEventData eventData) => transform.localScale = Vector3.one;
}
