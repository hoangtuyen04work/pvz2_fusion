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
        if (scene.name == "MainMenu" && Object.FindAnyObjectByType<HomeSplashPresentation>() == null)
            new GameObject("Splash & Loading Presentation", typeof(HomeSplashPresentation));

        if (scene.name == "GameScene" && Object.FindAnyObjectByType<GameplayBackgroundMotion>() == null)
            new GameObject("Gameplay Background Motion", typeof(GameplayBackgroundMotion));
    }
}

public sealed class HomeSplashPresentation : MonoBehaviour
{
    private CanvasGroup group;
    private Image progressFill;
    private Text loadingText;
    private RectTransform logo;
    private float phase;

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

        var backdrop = CreateImage("Introductory Image", canvasObject.transform,
            Resources.Load<Sprite>("Sprites/UI/MainMenu/main_menu_vi"));
        Stretch(backdrop.rectTransform);
        backdrop.color = new Color(0.42f, 0.50f, 0.38f, 1f);

        var shade = CreateImage("Cinematic Shade", canvasObject.transform, null);
        Stretch(shade.rectTransform);
        shade.color = new Color(0.015f, 0.035f, 0.018f, 0.50f);

        var logoImage = CreateImage("Game Icon Logo", canvasObject.transform,
            Resources.Load<Sprite>("Sprites/UI/Intro/pvz_fusion_logo"));
        logo = logoImage.rectTransform;
        logo.anchorMin = logo.anchorMax = new Vector2(0.5f, 0.60f);
        logo.pivot = new Vector2(0.5f, 0.5f);
        logo.sizeDelta = new Vector2(850f, 430f);
        logoImage.preserveAspect = true;

        var track = CreateImage("Loading Bar", canvasObject.transform, null);
        SetAnchors(track.rectTransform, 0.25f, 0.19f, 0.75f, 0.225f);
        track.color = new Color(0.06f, 0.09f, 0.035f, 0.92f);

        progressFill = CreateImage("Loading Progress", track.transform, null);
        Stretch(progressFill.rectTransform);
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = 0;
        progressFill.fillAmount = 0f;
        progressFill.color = new Color(0.55f, 0.92f, 0.18f, 1f);

        loadingText = CreateText("Loading Label", canvasObject.transform, "ĐANG TẢI... 0%", 23, Color.white);
        SetAnchors(loadingText.rectTransform, 0.25f, 0.115f, 0.75f, 0.18f);
    }

    private IEnumerator Play()
    {
        float elapsed = 0f;
        // 4.2s loading + 0.35s ready hold + 0.45s fade = ~5s total splash time.
        const float duration = 4.2f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);
            float displayed = Mathf.SmoothStep(0f, 1f, normalized);
            progressFill.fillAmount = displayed;
            loadingText.text = "ĐANG TẢI... " + Mathf.RoundToInt(displayed * 100f) + "%";
            yield return null;
        }

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
        if (logo == null) return;
        phase += Time.unscaledDeltaTime;
        float pulse = 1f + Mathf.Sin(phase * 2.2f) * 0.025f;
        logo.localScale = Vector3.one * pulse;
        logo.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(phase * 1.15f) * 1.2f);
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
