using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Menu tạm dừng dựng ở runtime để dùng thống nhất cho mọi màn chơi.</summary>
public class GameplayPauseMenu : MonoBehaviour
{
    private static GameplayPauseMenu instance;

    private GameObject pauseButton;
    private GameObject overlay;
    private CanvasGroup overlayGroup;
    private AudioSource uiAudio;
    private Font font;
    private Image soundStateImage;
    private bool isPaused;
    private bool animating;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        UiAudioSettings.ApplySavedState();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
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
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1200;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800f, 600f);
        scaler.matchWidthOrHeight = .5f;

        EnsureEventSystem();

        pauseButton = CreateTextureButton("Nút Pause", canvasObject.transform, LoadUiSprite("pause"), TogglePause);
        RectTransform pauseRect = pauseButton.GetComponent<RectTransform>();
        pauseRect.anchorMin = pauseRect.anchorMax = new Vector2(1f, 1f);
        pauseRect.pivot = new Vector2(1f, 1f);
        pauseRect.anchoredPosition = new Vector2(-16f, -14f);
        pauseRect.sizeDelta = new Vector2(62f, 68f);
        AddSoftShadow(pauseButton, new Vector2(2f, -3f));

        overlay = new GameObject("Lớp tạm dừng", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        overlay.transform.SetParent(canvasObject.transform, false);
        Stretch(overlay.GetComponent<RectTransform>());
        overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, .67f);
        overlayGroup = overlay.GetComponent<CanvasGroup>();

        Image settingIcon = CreateImage("Biểu tượng cài đặt", overlay.transform, LoadUiSprite("setting"));
        Center(settingIcon.rectTransform, new Vector2(49f, 54f), new Vector2(-92f, 242f));
        settingIcon.raycastTarget = false;
        Text title = CreateText("Tiêu đề", overlay.transform, "TẠM DỪNG", 31, new Color(1f, .93f, .48f));
        Center(title.rectTransform, new Vector2(210f, 50f), new Vector2(27f, 241f));

        float y = 154f;
        CreateMenuRow(overlay.transform, "TIẾP TỤC", new Vector2(-20f, y), true, "play", Resume);
        CreateSoundRow(overlay.transform, new Vector2(20f, y - 77f), false);

        if (NetSession.IsOnline)
            CreateMenuRow(overlay.transform, "RỜI TRẬN", new Vector2(-20f, y - 154f), true, "return", ReturnToMainMenu);
        else
            CreateMenuRow(overlay.transform, "CHƠI LẠI", new Vector2(-20f, y - 154f), true, "confirm", RestartLevel);

        CreateMenuRow(overlay.transform, "VỀ MENU", new Vector2(20f, y - 231f), false, "return", ReturnToMainMenu);
        CreateMenuRow(overlay.transform, "THOÁT GAME", new Vector2(-20f, y - 308f), true, "cancel", QuitGame);

        overlay.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!NetSession.IsOnline && !hasFocus && !isPaused && Time.timeScale > 0f)
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

        if (!NetSession.IsOnline)
        {
            Time.timeScale = 0f;
            AudioListener.pause = true;
        }

        PlayClick();
        StartCoroutine(FadeOverlay(0f, 1f, .18f, false));
    }

    private void Resume()
    {
        if (!isPaused || animating) return;
        PlayClick();
        StartCoroutine(FadeOverlay(overlayGroup.alpha, 0f, .16f, true));
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

        if (!resumeAfter) yield break;
        overlay.SetActive(false);
        pauseButton.SetActive(true);
        isPaused = false;
        AudioListener.pause = false;
        Time.timeScale = 1f;
    }

    private void ToggleSound()
    {
        bool enabled = UiAudioSettings.Toggle();
        UpdateSoundState(enabled);
        if (enabled) PlayClick();
    }

    private void UpdateSoundState(bool enabled)
    {
        if (soundStateImage != null)
            soundStateImage.sprite = LoadUiSprite(enabled ? "on" : "off");
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

    private void CreateMenuRow(Transform parent, string label, Vector2 position, bool firstStyle,
        string iconName, UnityEngine.Events.UnityAction action)
    {
        GameObject button = CreateTextureButton(label, parent,
            LoadUiSprite(firstStyle ? "menu_child1" : "menu_child2"), action);
        Center(button.GetComponent<RectTransform>(), firstStyle
            ? new Vector2(286f, 114f)
            : new Vector2(286f, 110f), position);
        AddSoftShadow(button, new Vector2(2f, -3f));

        Text text = CreateText("Chữ " + label, button.transform, label, 22, Color.white);
        SetAnchors(text.rectTransform, .29f, .22f, .82f, .78f);
        text.raycastTarget = false;

        Image icon = CreateImage("Icon " + iconName, button.transform, LoadUiSprite(iconName));
        icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(.86f, .5f);
        icon.rectTransform.pivot = new Vector2(.5f, .5f);
        icon.rectTransform.sizeDelta = new Vector2(43f, 47f);
        icon.rectTransform.anchoredPosition = Vector2.zero;
        icon.raycastTarget = false;
    }

    private void CreateSoundRow(Transform parent, Vector2 position, bool firstStyle)
    {
        GameObject button = CreateTextureButton("Âm thanh", parent,
            LoadUiSprite(firstStyle ? "menu_child1" : "menu_child2"), ToggleSound);
        Center(button.GetComponent<RectTransform>(), new Vector2(286f, 110f), position);
        AddSoftShadow(button, new Vector2(2f, -3f));

        Text text = CreateText("Chữ Âm thanh", button.transform, "ÂM THANH", 21, Color.white);
        SetAnchors(text.rectTransform, .28f, .22f, .67f, .78f);
        text.raycastTarget = false;

        soundStateImage = CreateImage("Trạng thái âm thanh", button.transform,
            LoadUiSprite(UiAudioSettings.Enabled ? "on" : "off"));
        soundStateImage.rectTransform.anchorMin = soundStateImage.rectTransform.anchorMax = new Vector2(.81f, .5f);
        soundStateImage.rectTransform.pivot = new Vector2(.5f, .5f);
        soundStateImage.rectTransform.sizeDelta = new Vector2(53f, 33f);
        soundStateImage.rectTransform.anchoredPosition = Vector2.zero;
        soundStateImage.raycastTarget = false;
    }

    private static GameObject CreateTextureButton(string name, Transform parent, Sprite sprite,
        UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
            typeof(Button), typeof(PauseTextureButton));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        Button button = go.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        PauseTextureButton motion = go.GetComponent<PauseTextureButton>();
        motion.target = image;
        return go;
    }

    private Text CreateText(string name, Transform parent, string value, int fontSize, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
        go.transform.SetParent(parent, false);
        Text text = go.GetComponent<Text>();
        text.font = font;
        text.fontStyle = FontStyle.Bold;
        text.text = value;
        text.fontSize = fontSize;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 13;
        text.resizeTextMaxSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        Outline outline = go.GetComponent<Outline>();
        outline.effectColor = new Color(.13f, .055f, .012f, .92f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        return text;
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        return image;
    }

    private void PlayClick()
    {
        AudioClip clip = Resources.Load<AudioClip>("Sounds/UI/buttonClick");
        if (clip != null) uiAudio.PlayOneShot(clip);
    }

    private static Sprite LoadUiSprite(string name) => Resources.Load<Sprite>("GameUI/" + name);

    private static void AddSoftShadow(GameObject target, Vector2 distance)
    {
        Shadow shadow = target.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, .48f);
        shadow.effectDistance = distance;
    }

    private static void Center(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.pivot = new Vector2(.5f, .5f);
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

    private static void SetAnchors(RectTransform rect, float x0, float y0, float x1, float y1)
    {
        rect.anchorMin = new Vector2(x0, y0);
        rect.anchorMax = new Vector2(x1, y1);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }
}

/// <summary>Lưu lựa chọn âm thanh dùng chung qua các scene.</summary>
public static class UiAudioSettings
{
    private const string SoundPrefKey = "game_audio_enabled";
    public static bool Enabled => PlayerPrefs.GetInt(SoundPrefKey, 1) == 1;

    public static void ApplySavedState()
    {
        AudioListener.volume = Enabled ? 1f : 0f;
    }

    public static bool Toggle()
    {
        bool enabled = !Enabled;
        PlayerPrefs.SetInt(SoundPrefKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
        AudioListener.volume = enabled ? 1f : 0f;
        return enabled;
    }
}

public class PauseTextureButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    public Image target;
    private Vector3 targetScale = Vector3.one;

    private void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, 16f * Time.unscaledDeltaTime);
    }

    public void OnPointerEnter(PointerEventData eventData) => targetScale = Vector3.one * 1.035f;
    public void OnPointerExit(PointerEventData eventData) => targetScale = Vector3.one;
    public void OnPointerDown(PointerEventData eventData) => targetScale = Vector3.one * .95f;
    public void OnPointerUp(PointerEventData eventData) => targetScale = Vector3.one * 1.035f;
}
