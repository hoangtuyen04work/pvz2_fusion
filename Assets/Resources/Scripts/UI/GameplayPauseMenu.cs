using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameplayPauseMenu : MonoBehaviour
{
    private static GameplayPauseMenu instance;

    private GameObject pauseButton;
    private GameObject overlay;
    private CanvasGroup overlayGroup;
    private AudioSource uiAudio;
    private Font font;
    private bool isPaused;
    private bool animating;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Tự phủ menu pause lên mọi scene gameplay, không phụ thuộc tên scene.
        // Scene chỉ cần có GameManagement như cấu trúc level chuẩn của project.
        if (instance != null || Object.FindAnyObjectByType<GameManagement>() == null) return;

        var root = new GameObject("Menu tạm dừng", typeof(GameplayPauseMenu), typeof(AudioSource));
        instance = root.GetComponent<GameplayPauseMenu>();
        instance.Build();
    }

    private void Build()
    {
        font = Resources.Load<Font>("Fonts/Baloo2");
        uiAudio = GetComponent<AudioSource>();
        uiAudio.playOnAwake = false;
        uiAudio.ignoreListenerPause = true;

        var canvasObject = new GameObject("PauseCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1200;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800f, 600f);
        scaler.matchWidthOrHeight = 0.5f;

        EnsureEventSystem();

        pauseButton = CreateTextureButton(
            "Nút Tùy chọn",
            canvasObject.transform,
            Resources.Load<Sprite>("Sprites/UI/PauseMenu/button_normal"),
            Resources.Load<Sprite>("Sprites/UI/PauseMenu/button_highlight"),
            TogglePause
        );
        var pauseRect = pauseButton.GetComponent<RectTransform>();
        pauseRect.anchorMin = pauseRect.anchorMax = new Vector2(1f, 1f);
        pauseRect.pivot = new Vector2(1f, 1f);
        pauseRect.anchoredPosition = new Vector2(-18f, -16f);
        pauseRect.sizeDelta = new Vector2(176f, 48f);

        var pauseOutline = pauseButton.AddComponent<Outline>();
        pauseOutline.effectColor = new Color(0.02f, 0.025f, 0.015f, 0.85f);
        pauseOutline.effectDistance = new Vector2(2.5f, -2.5f);

        var pauseLabel = CreateText("Chữ Tùy chọn", pauseButton.transform, "TÙY CHỌN", 24, new Color(0.64f, 1f, 0.24f));
        Stretch(pauseLabel.rectTransform);
        pauseLabel.rectTransform.offsetMin = new Vector2(12f, 4f);
        pauseLabel.rectTransform.offsetMax = new Vector2(-12f, -3f);
        pauseLabel.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -1.4f);
        pauseLabel.raycastTarget = false;

        overlay = new GameObject("Lớp tạm dừng", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        overlay.transform.SetParent(canvasObject.transform, false);
        Stretch(overlay.GetComponent<RectTransform>());
        overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);
        overlayGroup = overlay.GetComponent<CanvasGroup>();

        var backdrop = new GameObject("Bia đá", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        backdrop.transform.SetParent(overlay.transform, false);
        var backdropRect = backdrop.GetComponent<RectTransform>();
        Center(backdropRect, new Vector2(382f, 448f), new Vector2(0f, -4f));
        backdrop.GetComponent<Image>().sprite = Resources.Load<Sprite>("Sprites/UI/PauseMenu/options_menuback");
        backdrop.GetComponent<Image>().raycastTarget = false;

        var title = CreateText("Tiêu đề", overlay.transform, "TẠM DỪNG", 39, new Color(0.64f, 1f, 0.25f));
        Center(title.rectTransform, new Vector2(290f, 58f), new Vector2(0f, 154f));

        CreatePauseActionButton(overlay.transform, "TIẾP TỤC", new Vector2(0f, 82f), Resume);
        CreatePauseActionButton(overlay.transform, "CHƠI LẠI", new Vector2(0f, 17f), RestartLevel);
        CreatePauseActionButton(overlay.transform, "VỀ MENU CHÍNH", new Vector2(0f, -48f), ReturnToMainMenu);
        CreatePauseActionButton(overlay.transform, "THOÁT GAME", new Vector2(0f, -113f), QuitGame);

        var hint = CreateText("Gợi ý", overlay.transform, "Nhấn ESC để tiếp tục", 18, new Color(0.78f, 0.80f, 0.72f));
        Center(hint.rectTransform, new Vector2(280f, 34f), new Vector2(0f, -172f));

        overlay.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && !isPaused && Time.timeScale > 0f)
            Pause();
    }

    private void TogglePause()
    {
        if (animating) return;
        if (isPaused) Resume();
        else if (Time.timeScale > 0f) Pause();
    }

    private void Pause()
    {
        if (isPaused) return;
        isPaused = true;
        pauseButton.SetActive(false);
        overlay.SetActive(true);
        overlayGroup.alpha = 0f;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        PlayClick();
        StartCoroutine(FadeOverlay(0f, 1f, 0.18f, false));
    }

    private void Resume()
    {
        if (!isPaused || animating) return;
        PlayClick();
        StartCoroutine(FadeOverlay(overlayGroup.alpha, 0f, 0.16f, true));
    }

    private IEnumerator FadeOverlay(float from, float to, float duration, bool resumeAfter)
    {
        animating = true;
        float elapsed = 0f;
        overlayGroup.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            overlayGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        overlayGroup.alpha = to;
        animating = false;

        if (resumeAfter)
        {
            overlay.SetActive(false);
            pauseButton.SetActive(true);
            isPaused = false;
            AudioListener.pause = false;
            Time.timeScale = 1f;
        }
    }

    private void RestartLevel()
    {
        if (animating) return;
        PlayClick();
        RestoreGameState();
        SceneManager.LoadScene("GameScene");
    }

    private void ReturnToMainMenu()
    {
        if (animating) return;
        PlayClick();
        RestoreGameState();
        SceneManager.LoadScene("MainMenu");
    }

    private void QuitGame()
    {
        PlayClick();
        RestoreGameState();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void RestoreGameState()
    {
        isPaused = false;
        AudioListener.pause = false;
        Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        RestoreGameState();
        if (instance == this) instance = null;
    }

    private void CreatePauseActionButton(Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        var button = CreateTextureButton(
            label,
            parent,
            Resources.Load<Sprite>("Sprites/UI/PauseMenu/button_normal"),
            Resources.Load<Sprite>("Sprites/UI/PauseMenu/button_highlight"),
            action
        );
        Center(button.GetComponent<RectTransform>(), new Vector2(292f, 56f), position);
        var text = CreateText("Chữ", button.transform, label, 25, Color.white);
        Stretch(text.rectTransform);
        text.raycastTarget = false;
    }

    private static GameObject CreateTextureButton(string name, Transform parent, Sprite normal, Sprite highlighted, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(PauseTextureButton));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.sprite = normal;
        var button = go.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(action);
        var motion = go.GetComponent<PauseTextureButton>();
        motion.target = image;
        motion.normal = normal;
        motion.highlighted = highlighted;
        return go;
    }

    private Text CreateText(string name, Transform parent, string value, int fontSize, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline), typeof(Shadow));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>();
        text.font = font;
        text.fontStyle = FontStyle.Bold;
        text.text = value;
        text.fontSize = fontSize;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 14;
        text.resizeTextMaxSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        var outline = go.GetComponent<Outline>();
        outline.effectColor = new Color(0.035f, 0.025f, 0.02f, 0.96f);
        outline.effectDistance = new Vector2(1.8f, -1.8f);
        var shadows = go.GetComponents<Shadow>();
        var shadow = shadows[shadows.Length - 1];
        shadow.effectColor = new Color(0f, 0f, 0f, 0.68f);
        shadow.effectDistance = new Vector2(2.8f, -3.2f);
        return text;
    }

    private void PlayClick()
    {
        var clip = Resources.Load<AudioClip>("Sounds/UI/buttonClick");
        if (clip != null) uiAudio.PlayOneShot(clip);
    }

    private static void Center(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }
}

public class PauseTextureButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public Image target;
    public Sprite normal;
    public Sprite highlighted;
    private Vector3 targetScale = Vector3.one;

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, 16f * Time.unscaledDeltaTime);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (target != null && highlighted != null) target.sprite = highlighted;
        targetScale = Vector3.one * 1.035f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (target != null) target.sprite = normal;
        targetScale = Vector3.one;
    }

    public void OnPointerDown(PointerEventData eventData) => targetScale = Vector3.one * 0.95f;
    public void OnPointerUp(PointerEventData eventData) => targetScale = Vector3.one * 1.035f;
}
