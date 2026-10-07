using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Adds a short, self-contained splash/loading presentation to the main menu and
/// a gentle living-background motion to gameplay. No scene references are needed.
/// </summary>
public static class PresentationBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Object.FindAnyObjectByType<GameTypographyStyle>() == null)
            new GameObject("Game Typography Style", typeof(GameTypographyStyle));

        if (scene.name == "MainMenu" && Object.FindAnyObjectByType<HomeSplashPresentation>() == null)
            new GameObject("Splash & Loading Presentation", typeof(HomeSplashPresentation));

        if (scene.name == "GameScene" && Object.FindAnyObjectByType<GameplayBackgroundMotion>() == null)
            new GameObject("Gameplay Background Motion", typeof(GameplayBackgroundMotion));
    }
}

/// <summary>
/// Đồng bộ kiểu chữ hoạt hình có hỗ trợ đầy đủ tiếng Việt cho UI tạo sẵn và UI runtime.
/// </summary>
public sealed class GameTypographyStyle : MonoBehaviour
{
    private IEnumerator Start()
    {
        yield return null;
        Apply();
        yield return new WaitForSecondsRealtime(0.35f);
        Apply();
    }

    private static void Apply()
    {
        Font gameFont = Resources.Load<Font>("Fonts/Baloo2");
        if (gameFont == null) return;

        foreach (Text text in Object.FindObjectsByType<Text>(FindObjectsInactive.Include))
        {
            text.font = gameFont;
            text.fontStyle = FontStyle.Bold;
        }
    }
}

public sealed class HomeSplashPresentation : MonoBehaviour
{
    private const float ZombieStartX = -300f;
    private const float ZombieEndX = 296f;
    private const float LoadingDuration = 7.5f;
    private const float ZombieFramesPerSecond = 6.5f;
    private CanvasGroup group;
    private Image progressFill;
    private Text loadingText;
    private RectTransform logo;
    private Image zombieRunner;
    private RectTransform zombieRect;
    private Sprite[] zombieFrames;
    private Sprite runtimeProgressSprite;
    private float phase;
    private float zombieAnimationTime;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Build();
        StartCoroutine(Play());
    }

    private void Build()
    {
        var canvasObject = new GameObject("SplashCanvas", typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster), typeof(CanvasGroup));
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1000f, 750f);
        scaler.matchWidthOrHeight = 0.5f;
        group = canvasObject.GetComponent<CanvasGroup>();

        var backdrop = CreateImage("Introductory Image", canvasObject.transform, null);
        Stretch(backdrop.rectTransform);
        backdrop.color = new Color(0.025f, 0.022f, 0.028f, 1f);

        var shade = CreateImage("Cinematic Shade", canvasObject.transform, null);
        Stretch(shade.rectTransform);
        shade.color = new Color(0.015f, 0.035f, 0.018f, 0.50f);

        var logoImage = CreateImage("Game Icon Logo", canvasObject.transform,
            Resources.Load<Sprite>("Sprites/UI/MainMenu/logo"));
        logo = logoImage.rectTransform;
        logo.anchorMin = logo.anchorMax = new Vector2(0.5f, 0.55f);
        logo.pivot = new Vector2(0.5f, 0.5f);
        logo.sizeDelta = new Vector2(850f, 638f);
        logoImage.preserveAspect = true;

        Sprite trackSprite = Resources.Load<Sprite>("Sprites/UI/Intro/process");
        var track = CreateImage("Khung thanh tải", canvasObject.transform, trackSprite);
        track.rectTransform.anchorMin = track.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        track.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        track.rectTransform.anchoredPosition = new Vector2(0f, -220f);
        track.rectTransform.sizeDelta = new Vector2(700f, 90f);
        track.color = trackSprite != null ? Color.white : new Color(0.06f, 0.09f, 0.035f, 0.92f);
        track.preserveAspect = true;
        track.raycastTarget = false;

        Sprite fillSprite = CreateProgressSprite();
        progressFill = CreateImage("Tiến trình tải", track.transform, fillSprite);
        if (fillSprite != null)
        {
            progressFill.rectTransform.anchorMin = progressFill.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            progressFill.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            progressFill.rectTransform.anchoredPosition = new Vector2(-1.5f, -4.5f);
            progressFill.rectTransform.sizeDelta = new Vector2(597f, 43f);
        }
        else
        {
            Stretch(progressFill.rectTransform);
        }
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = 0;
        progressFill.fillAmount = 0f;
        progressFill.color = fillSprite != null ? Color.white : new Color(0.55f, 0.92f, 0.18f, 1f);
        progressFill.raycastTarget = false;

        zombieFrames = CreateZombieFrames();
        zombieRunner = CreateImage("Zombie chạy theo tiến trình", track.transform,
            zombieFrames != null && zombieFrames.Length > 0 ? zombieFrames[0] : null);
        zombieRect = zombieRunner.rectTransform;
        zombieRect.anchorMin = zombieRect.anchorMax = new Vector2(0.5f, 0.5f);
        zombieRect.pivot = new Vector2(0.5f, 0f);
        zombieRect.sizeDelta = new Vector2(65f, 80f);
        zombieRect.anchoredPosition = new Vector2(ZombieStartX, 15f);
        zombieRunner.preserveAspect = true;
        zombieRunner.raycastTarget = false;

        loadingText = CreateText("Loading Label", canvasObject.transform, "ĐANG TẢI... 0%", 23, Color.white);
        SetAnchors(loadingText.rectTransform, 0.25f, 0.075f, 0.75f, 0.135f);
    }

    private IEnumerator Play()
    {
        float elapsed = 0f;
        // Chạy đủ chậm để người chơi nhìn rõ thanh tiến trình và trọn chu kỳ 8 frame zombie.
        while (elapsed < LoadingDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / LoadingDuration);
            float displayed = Mathf.SmoothStep(0f, 1f, normalized);
            SetLoadingProgress(displayed);
            loadingText.text = "ĐANG TẢI... " + Mathf.RoundToInt(displayed * 100f) + "%";
            yield return null;
        }

        SetLoadingProgress(1f);
        loadingText.text = "SẴN SÀNG!";
        yield return new WaitForSecondsRealtime(0.35f);

        elapsed = 0f;
        while (elapsed < 0.45f)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = 1f - Mathf.SmoothStep(0f, 1f, elapsed / 0.45f);
            yield return null;
        }
        Destroy(gameObject);
    }

    private void Update()
    {
        phase += Time.unscaledDeltaTime;
        if (logo != null)
        {
            float pulse = 1f + Mathf.Sin(phase * 2.2f) * 0.025f;
            logo.localScale = Vector3.one * pulse;
            logo.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(phase * 1.15f) * 1.2f);
        }

        if (zombieRunner != null && zombieFrames != null && zombieFrames.Length == 8)
        {
            zombieAnimationTime += Time.unscaledDeltaTime;
            int frame = Mathf.FloorToInt(zombieAnimationTime * ZombieFramesPerSecond) % zombieFrames.Length;
            zombieRunner.sprite = zombieFrames[frame];
        }
    }

    private void SetLoadingProgress(float value)
    {
        value = Mathf.Clamp01(value);
        if (progressFill != null) progressFill.fillAmount = value;
        if (zombieRect != null)
            zombieRect.anchoredPosition = new Vector2(Mathf.Lerp(ZombieStartX, ZombieEndX, value), 15f);
    }

    private static Sprite[] CreateZombieFrames()
    {
        Texture2D sheet = Resources.Load<Texture2D>("Sprites/UI/Intro/zoombie_spritesheet");
        if (sheet == null) return null;

        // Sheet gốc 390x60: 7 ô đầu rộng 49 px, ô cuối rộng 47 px.
        int[] boundaries = { 0, 49, 98, 147, 196, 245, 294, 343, 390 };
        var frames = new Sprite[8];
        float scaleX = sheet.width / 390f;
        for (int index = 0; index < frames.Length; index++)
        {
            float x = boundaries[index] * scaleX;
            float width = (boundaries[index + 1] - boundaries[index]) * scaleX;
            frames[index] = Sprite.Create(sheet, new Rect(x, 0f, width, sheet.height),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            frames[index].name = "loading-zombie-" + (index + 1);
        }
        return frames;
    }

    private Sprite CreateProgressSprite()
    {
        Texture2D texture = Resources.Load<Texture2D>("Sprites/UI/Intro/process_timeline");
        if (texture == null) return null;

        // Bỏ vùng trong suốt xung quanh lớp xanh để fillAmount và zombie cùng một trục 0..100%.
        float scaleX = texture.width / 700f;
        float scaleY = texture.height / 90f;
        runtimeProgressSprite = Sprite.Create(texture,
            new Rect(50f * scaleX, 19f * scaleY, 597f * scaleX, 43f * scaleY),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        runtimeProgressSprite.name = "loading-progress-cropped";
        return runtimeProgressSprite;
    }

    private void OnDestroy()
    {
        if (zombieFrames != null)
            foreach (Sprite frame in zombieFrames)
                if (frame != null) Destroy(frame);
        if (runtimeProgressSprite != null) Destroy(runtimeProgressSprite);
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        return image;
    }

    private static Text CreateText(string name, Transform parent, string value, int size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>();
        text.font = Resources.Load<Font>("Fonts/Baloo2");
        text.fontStyle = FontStyle.Bold;
        text.text = value;
        text.fontSize = size;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 15;
        text.resizeTextMaxSize = size;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        return text;
    }

    private static void Stretch(RectTransform rect) => SetAnchors(rect, 0f, 0f, 1f, 1f);

    private static void SetAnchors(RectTransform rect, float xMin, float yMin, float xMax, float yMax)
    {
        rect.anchorMin = new Vector2(xMin, yMin);
        rect.anchorMax = new Vector2(xMax, yMax);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}

public sealed class GameplayBackgroundMotion : MonoBehaviour
{
    private Transform background;
    private Vector3 origin;
    private Vector3 originalScale;
    private float phase;

    private IEnumerator Start()
    {
        // Level setup can replace the background during its first frames.
        yield return null;
        yield return null;
        if (GameManagement.levelData != null && !GameManagement.levelData.animateBackground)
        {
            Destroy(gameObject);
            yield break;
        }
        var found = GameObject.Find("Background");
        if (found == null)
        {
            Destroy(gameObject);
            yield break;
        }

        background = found.transform;
        origin = background.localPosition;
        originalScale = background.localScale;
        background.localScale = Vector3.Scale(originalScale, new Vector3(1.025f, 1.025f, 1f));
    }

    private void LateUpdate()
    {
        if (background == null) return;
        phase += Time.deltaTime;

        // Slow layered-feeling drift: enough to keep the lawn alive without moving the grid.
        float x = Mathf.Sin(phase * 0.16f) * 0.075f + Mathf.Sin(phase * 0.047f) * 0.035f;
        float y = Mathf.Cos(phase * 0.12f) * 0.025f;
        background.localPosition = origin + new Vector3(x, y, 0f);
    }

    private void OnDestroy()
    {
        if (background != null)
        {
            background.localPosition = origin;
            background.localScale = originalScale;
        }
    }
}
