using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartupSkipController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private static StartupSkipController instance;
    private RectTransform buttonRect;
    private CanvasGroup group;
    private Image fadeImage;
    private bool leaving;
    private float phase;
    private Vector3 targetScale = Vector3.one;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "GameScene" || instance != null)
            return;

        var root = new GameObject("Bỏ qua hội thoại mở đầu", typeof(StartupSkipController));
        instance = root.GetComponent<StartupSkipController>();
        instance.Build();
    }

    public static void HideForGameplay()
    {
        if (instance != null)
            instance.StartCoroutine(instance.HideAndDestroy());
    }

    private void Build()
    {
        var canvasObject = new GameObject("SkipCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1000f, 750f);
        scaler.matchWidthOrHeight = 0.5f;
        group = canvasObject.GetComponent<CanvasGroup>();
        group.alpha = 0f;

        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        var buttonObject = new GameObject("Nút Skip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(canvasObject.transform, false);
        buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(1f, 0f);
        buttonRect.pivot = new Vector2(1f, 0f);
        buttonRect.anchoredPosition = new Vector2(-26f, 24f);
        buttonRect.sizeDelta = new Vector2(180f, 116f);

        var image = buttonObject.GetComponent<Image>();
        image.sprite = Resources.Load<Sprite>("Sprites/UI/MainMenu/skip_next");
        image.preserveAspect = true;
        var chromaShader = Resources.Load<Shader>("Shaders/UIChromaKey");
        if (chromaShader != null) image.material = new Material(chromaShader);

        var button = buttonObject.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(SkipToMenu);

        var trigger = buttonObject.AddComponent<EventTriggerRelay>();
        trigger.receiver = this;

        fadeImage = new GameObject("Mờ chuyển cảnh", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
        fadeImage.transform.SetParent(canvasObject.transform, false);
        var fadeRect = fadeImage.rectTransform;
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.offsetMin = fadeRect.offsetMax = Vector2.zero;
        fadeImage.color = Color.black;
        fadeImage.raycastTarget = false;
        fadeImage.canvasRenderer.SetAlpha(0f);

        StartCoroutine(FadeIn());
    }

    private void Update()
    {
        if (buttonRect == null || leaving) return;
        phase += Time.unscaledDeltaTime;
        float bob = Mathf.Sin(phase * 2.4f) * 4f;
        buttonRect.anchoredPosition = new Vector2(-26f, 24f + bob);
        buttonRect.localScale = Vector3.Lerp(buttonRect.localScale, targetScale, 15f * Time.unscaledDeltaTime);
    }

    private void SkipToMenu()
    {
        if (!leaving) StartCoroutine(LoadMenu());
    }

    private IEnumerator LoadMenu()
    {
        leaving = true;
        var clip = Resources.Load<AudioClip>("Sounds/UI/buttonClick");
        if (clip != null) AudioSource.PlayClipAtPoint(clip, Vector3.zero);
        fadeImage.raycastTarget = true;
        fadeImage.CrossFadeAlpha(1f, 0.35f, true);
        yield return new WaitForSecondsRealtime(0.38f);
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    private IEnumerator FadeIn()
    {
        yield return new WaitForSecondsRealtime(0.35f);
        float elapsed = 0f;
        while (elapsed < 0.35f)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.SmoothStep(0f, 1f, elapsed / 0.35f);
            yield return null;
        }
        group.alpha = 1f;
    }

    private IEnumerator HideAndDestroy()
    {
        leaving = true;
        float start = group.alpha;
        float elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(start, 0f, elapsed / 0.2f);
            yield return null;
        }
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    public void OnPointerEnter(PointerEventData eventData) => targetScale = Vector3.one * 1.08f;
    public void OnPointerExit(PointerEventData eventData) => targetScale = Vector3.one;
    public void OnPointerDown(PointerEventData eventData) => targetScale = Vector3.one * 0.92f;
    public void OnPointerUp(PointerEventData eventData) => targetScale = Vector3.one * 1.08f;
}

public class EventTriggerRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public StartupSkipController receiver;
    public void OnPointerEnter(PointerEventData eventData) => receiver.OnPointerEnter(eventData);
    public void OnPointerExit(PointerEventData eventData) => receiver.OnPointerExit(eventData);
    public void OnPointerDown(PointerEventData eventData) => receiver.OnPointerDown(eventData);
    public void OnPointerUp(PointerEventData eventData) => receiver.OnPointerUp(eventData);
}
