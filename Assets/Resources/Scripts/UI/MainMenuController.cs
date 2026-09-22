using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    private const int GargantuarArenaLevelIndex = 6;
    private const int ThreeWorldsCampaignLevelIndex = 7;

    public Font menuFont;
    public Sprite stoneButtonNormal;
    public Sprite stoneButtonHighlighted;
    public Sprite stoneButtonPressed;
    public GameObject levelPanel;
    public GameObject helpPanel;
    public AudioSource audioSource;

    private CanvasGroup menuGroup;
    private Image fadeImage;
    private Text noticeText;
    private bool transitioning;
    private GameObject optionsPanel;
    private int selectedLevel = -1;
    private Button playLevelButton;
    private Text selectedLevelText;
    private readonly List<Image> levelCardFrames = new List<Image>();
    private Material menuHoverMaterial;
    private float menuHoverTarget;
    private float menuHoverAmount;

    private void Update()
    {
        if (menuHoverMaterial == null) return;
        menuHoverAmount = Mathf.Lerp(menuHoverAmount, menuHoverTarget, 18f * Time.unscaledDeltaTime);
        menuHoverMaterial.SetFloat("_HoverAmount", menuHoverAmount);
    }

    private void Awake()
    {
        Time.timeScale = 1f;
        if (menuFont == null)
            menuFont = Resources.Load<Font>("Fonts/Baloo2");

        BuildMenu();
        StartCoroutine(AnimateEntrance());
    }

    private void BuildMenu()
    {
        if (GameObject.Find("MainMenuCanvas") != null)
            return;

        EnsureMenuCamera();

        var canvasObject = new GameObject("MainMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1000f, 750f);
        scaler.matchWidthOrHeight = 0.5f;

        menuGroup = canvasObject.GetComponent<CanvasGroup>();
        menuGroup.alpha = 0f;

        EnsureEventSystem();

        var frameObject = new GameObject("Khung Menu 4x3", typeof(RectTransform), typeof(AspectRatioFitter));
        frameObject.transform.SetParent(canvasObject.transform, false);
        Stretch(frameObject.GetComponent<RectTransform>());
        var frameFitter = frameObject.GetComponent<AspectRatioFitter>();
        frameFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        frameFitter.aspectRatio = 4f / 3f;
        Transform menuFrame = frameObject.transform;

        var background = CreateImage("Nền Menu", menuFrame, Resources.Load<Sprite>("Sprites/UI/MainMenu/main_menu_vi"));
        Stretch(background.rectTransform);
        background.raycastTarget = false;
        var hoverShader = Resources.Load<Shader>("Shaders/UIMenuTextHover");
        if (hoverShader != null)
        {
            menuHoverMaterial = new Material(hoverShader);
            background.material = menuHoverMaterial;
        }

        // Các hitbox bám theo đúng vị trí các phiến đá trên ảnh nền 4:3.
        CreateHotspot(menuFrame, "Phiêu lưu", 0.500f, 0.656f, 0.891f, 0.843f, new Vector4(0.520f, 0.710f, 0.865f, 0.825f), StartAdventure);
        CreateHotspot(menuFrame, "Trò chơi nhỏ", 0.503f, 0.497f, 0.880f, 0.690f, new Vector4(0.520f, 0.535f, 0.850f, 0.655f), () => ShowNotice("Chế độ Trò chơi nhỏ sẽ sớm ra mắt!"));
        CreateHotspot(menuFrame, "Giải đố", 0.512f, 0.385f, 0.862f, 0.548f, new Vector4(0.545f, 0.420f, 0.835f, 0.515f), () => ShowNotice("Chế độ Giải đố sẽ sớm ra mắt!"));
        CreateHotspot(menuFrame, "Sinh tồn", 0.514f, 0.287f, 0.839f, 0.435f, new Vector4(0.540f, 0.310f, 0.805f, 0.405f), EndlessMenuOverlay.Show);
        CreateHotspot(menuFrame, "Cửa hàng", 0.341f, 0.059f, 0.445f, 0.144f, new Vector4(0.350f, 0.075f, 0.435f, 0.130f), () => ShowNotice("Cửa hàng hiện đang đóng cửa."));
        CreateHotspot(menuFrame, "Tùy chọn", 0.683f, 0.109f, 0.784f, 0.227f, new Vector4(0.690f, 0.130f, 0.770f, 0.200f), ShowOptions);
        CreateHotspot(menuFrame, "Trợ giúp", 0.775f, 0.069f, 0.871f, 0.218f, new Vector4(0.790f, 0.085f, 0.855f, 0.165f), ShowHelp);
        CreateHotspot(menuFrame, "Thoát", 0.864f, 0.084f, 0.965f, 0.229f, new Vector4(0.880f, 0.110f, 0.950f, 0.190f), QuitGame);

        noticeText = CreateText("Thông báo", menuFrame, string.Empty, 27, TextAnchor.MiddleCenter, Color.white);
        SetAnchors(noticeText.rectTransform, 0.22f, 0.02f, 0.78f, 0.105f);
        noticeText.gameObject.SetActive(false);

        levelPanel = BuildLevelSelection(menuFrame);
        optionsPanel = BuildModal(menuFrame, "TÙY CHỌN", "Âm thanh và thiết lập nâng cao sẽ được bổ sung trong bản cập nhật tiếp theo.");
        helpPanel = BuildModal(menuFrame, "TRỢ GIÚP",
            "Chọn PHIÊU LƯU để chơi một mình: chọn thẻ cây rồi nhấn vào ô đất để trồng cây chống zombie.\n\n"
            + "CHƠI MẠNG: vào PHIÊU LƯU rồi bấm nút CHƠI MẠNG ở góc trái trên bảng chọn màn. Một người bấm TẠO PHÒNG rồi đọc địa chỉ hiện trên màn hình, người kia bấm THAM GIA và gõ địa chỉ đó vào.\n\n"
            + "Chế độ ĐỒNG ĐỘI: hai người cùng trồng cây, dùng chung kho nắng và dãy thẻ.\n"
            + "Chế độ ĐỐI KHÁNG: chủ phòng giữ phe Cây, người tham gia chỉ huy phe Zombie, tích não để thả quân theo từng hàng.\n\n"
            + "Hai máy phải cùng mạng nội bộ. Nếu chơi qua Internet thì cần mở cổng 7777 hoặc dùng phần mềm tạo mạng ảo.");

        fadeImage = CreateImage("Chuyển cảnh", canvasObject.transform, null);
        Stretch(fadeImage.rectTransform);
        fadeImage.color = Color.black;
        fadeImage.raycastTarget = true;
        fadeImage.canvasRenderer.SetAlpha(0f);
        fadeImage.gameObject.SetActive(false);
    }

    private void CreateHotspot(Transform parent, string label, float xMin, float yMin, float xMax, float yMax, Vector4 textRect, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject("Nút " + label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(MenuButtonMotion));
        go.transform.SetParent(parent, false);
        SetAnchors(go.GetComponent<RectTransform>(), xMin, yMin, xMax, yMax);

        var image = go.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.001f);
        var button = go.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(action);

        var motion = go.GetComponent<MenuButtonMotion>();
        motion.targetGraphic = image;
        motion.highlightMenuText = true;
        motion.menuController = this;
        motion.hoverRect = textRect;
    }

    // Mở sảnh chờ chơi mạng, mang theo màn đang chọn để chủ phòng khỏi phải chọn lại.
    private void OpenNetLobby()
    {
        if (transitioning) return;
        PlayClick();
        NetLobbyUI.Open(selectedLevel);
    }

    private GameObject BuildModal(Transform parent, string title, string body)
    {
        var panel = new GameObject(title + " Panel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        panel.transform.SetParent(parent, false);
        SetAnchors(panel.GetComponent<RectTransform>(), 0.22f, 0.24f, 0.78f, 0.76f);
        panel.GetComponent<Image>().color = new Color(0.09f, 0.12f, 0.06f, 0.95f);

        var titleText = CreateText("Tiêu đề", panel.transform, title, 44, TextAnchor.MiddleCenter, new Color(0.55f, 1f, 0.24f));
        SetAnchors(titleText.rectTransform, 0.08f, 0.72f, 0.92f, 0.94f);

        var bodyText = CreateText("Nội dung", panel.transform, body, 25, TextAnchor.MiddleCenter, Color.white);
        SetAnchors(bodyText.rectTransform, 0.09f, 0.28f, 0.91f, 0.72f);

        var close = new GameObject("Đóng", typeof(RectTransform), typeof(Image), typeof(Button), typeof(MenuButtonMotion));
        close.transform.SetParent(panel.transform, false);
        SetAnchors(close.GetComponent<RectTransform>(), 0.34f, 0.07f, 0.66f, 0.24f);
        var closeImage = close.GetComponent<Image>();
        closeImage.sprite = stoneButtonNormal != null ? stoneButtonNormal : Resources.Load<Sprite>("Sprites/UI/Menu/button_normal");
        closeImage.type = Image.Type.Sliced;
        var closeButton = close.GetComponent<Button>();
        closeButton.transition = Selectable.Transition.None;
        closeButton.onClick.AddListener(() => CloseModal(panel));
        close.GetComponent<MenuButtonMotion>().targetGraphic = closeImage;
        var closeText = CreateText("Chữ", close.transform, "ĐÓNG", 27, TextAnchor.MiddleCenter, Color.white);
        Stretch(closeText.rectTransform);
        closeText.raycastTarget = false;

        panel.SetActive(false);
        return panel;
    }

    private GameObject BuildLevelSelection(Transform parent)
    {
        var panel = new GameObject("Chọn màn chơi Panel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        panel.transform.SetParent(parent, false);
        SetAnchors(panel.GetComponent<RectTransform>(), 0.055f, 0.065f, 0.945f, 0.935f);
        panel.GetComponent<Image>().color = new Color(0.075f, 0.10f, 0.055f, 0.975f);

        var title = CreateText("Tiêu đề", panel.transform, "CHỌN MÀN CHƠI", 42, TextAnchor.MiddleCenter, new Color(0.62f, 1f, 0.25f));
        SetAnchors(title.rectTransform, 0.27f, 0.855f, 0.73f, 0.97f);

        // Góc trái trên: lối vào chế độ chơi mạng hai người, dùng chung màn đang chọn bên dưới.
        var netButton = CreateStoneButton("Chơi mạng", panel.transform, "CHƠI MẠNG", 22, OpenNetLobby);
        SetAnchors(netButton.GetComponent<RectTransform>(), 0.032f, 0.878f, 0.235f, 0.968f);

        var netHint = CreateText("Chú thích chơi mạng", panel.transform, "2 người", 17,
            TextAnchor.MiddleCenter, new Color(0.72f, 0.78f, 0.66f));
        SetAnchors(netHint.rectTransform, 0.032f, 0.828f, 0.235f, 0.874f);
        netHint.raycastTarget = false;

        var close = CreateStoneButton("Đóng chọn màn", panel.transform, "X", 26, () => CloseModal(panel));
        SetAnchors(close.GetComponent<RectTransform>(), 0.91f, 0.88f, 0.975f, 0.965f);

        var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewportObject.transform.SetParent(panel.transform, false);
        var viewportRect = viewportObject.GetComponent<RectTransform>();
        SetAnchors(viewportRect, 0.045f, 0.165f, 0.955f, 0.845f);
        viewportObject.GetComponent<Image>().color = new Color(0.18f, 0.24f, 0.12f, 0.28f);
        viewportObject.GetComponent<Mask>().showMaskGraphic = true;

        var contentObject = new GameObject("Danh sách màn", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        contentObject.transform.SetParent(viewportObject.transform, false);
        var contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;

        var grid = contentObject.GetComponent<GridLayoutGroup>();
        grid.padding = new RectOffset(12, 12, 12, 12);
        grid.spacing = new Vector2(12f, 14f);
        grid.cellSize = new Vector2(142f, 130f);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;

        var fitter = contentObject.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = panel.AddComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        scroll.elasticity = 0.12f;
        scroll.inertia = true;
        scroll.scrollSensitivity = 34f;

        string[] levelNames =
        {
            "Mèo Miu Xuất Trận",
            "Hành Trình Mới",
            "Thầy Luyện Xác",
            "Vùng Đất Bất Tử",
            "Sông Băng Địa Cực",
            "Sân Thử Nghiệm",
            "Đấu Trường Gargantuar",
            "Chiến Dịch Ba Cõi"
        };
        string[] thumbnails =
        {
            "Sprites/BackGround/Background_Day",
            "Sprites/BackGround/Background_Day",
            "Sprites/BackGround/Background_Night_Wall",
            "Sprites/BackGround/background_Night_Bone",
            "Sprites/BackGround/Background_Ice",
            "Sprites/BackGround/Background_Day",
            "Sprites/BackGround/Background_Day",
            "Sprites/BackGround/background_Night_Bone"
        };

        for (int i = 0; i < levelNames.Length; i++)
            CreateLevelCard(contentObject.transform, i, levelNames[i], thumbnails[i]);

        selectedLevelText = CreateText("Màn đã chọn", panel.transform, "Hãy chọn một màn chơi", 24, TextAnchor.MiddleCenter, Color.white);
        SetAnchors(selectedLevelText.rectTransform, 0.10f, 0.055f, 0.62f, 0.15f);

        playLevelButton = CreateStoneButton("Chơi", panel.transform, "CHƠI", 29, PlaySelectedLevel).GetComponent<Button>();
        SetAnchors(playLevelButton.GetComponent<RectTransform>(), 0.66f, 0.045f, 0.88f, 0.15f);
        playLevelButton.interactable = false;

        panel.SetActive(false);
        return panel;
    }

    private void CreateLevelCard(Transform parent, int levelIndex, string levelName, string thumbnailPath)
    {
        var card = new GameObject("Màn " + (levelIndex + 1), typeof(RectTransform), typeof(Image), typeof(Button), typeof(MenuButtonMotion));
        card.transform.SetParent(parent, false);
        var frame = card.GetComponent<Image>();
        frame.sprite = stoneButtonNormal != null ? stoneButtonNormal : Resources.Load<Sprite>("Sprites/UI/Menu/button_normal");
        frame.type = Image.Type.Sliced;
        frame.color = new Color(0.74f, 0.76f, 0.70f, 1f);
        levelCardFrames.Add(frame);

        var button = card.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        int capturedLevel = levelIndex;
        button.onClick.AddListener(() => SelectLevel(capturedLevel));
        card.GetComponent<MenuButtonMotion>().targetGraphic = frame;

        var thumbnail = CreateImage("Ảnh màn", card.transform, Resources.Load<Sprite>(thumbnailPath));
        SetAnchors(thumbnail.rectTransform, 0.075f, 0.39f, 0.925f, 0.90f);
        thumbnail.raycastTarget = false;

        var label = CreateText("Tên màn", card.transform, "MÀN " + (levelIndex + 1) + "\n" + levelName, 21, TextAnchor.MiddleCenter, Color.white);
        SetAnchors(label.rectTransform, 0.07f, 0.045f, 0.93f, 0.38f);
        label.raycastTarget = false;
    }

    private GameObject CreateStoneButton(string name, Transform parent, string label, int fontSize, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(MenuButtonMotion));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.sprite = stoneButtonNormal != null ? stoneButtonNormal : Resources.Load<Sprite>("Sprites/UI/Menu/button_normal");
        image.type = Image.Type.Sliced;
        var button = go.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(action);
        go.GetComponent<MenuButtonMotion>().targetGraphic = image;
        var text = CreateText("Chữ", go.transform, label, fontSize, TextAnchor.MiddleCenter, Color.white);
        Stretch(text.rectTransform);
        text.raycastTarget = false;
        return go;
    }

    private void SelectLevel(int levelIndex)
    {
        selectedLevel = levelIndex;
        for (int i = 0; i < levelCardFrames.Count; i++)
            levelCardFrames[i].color = i == levelIndex
                ? new Color(0.64f, 1f, 0.30f, 1f)
                : new Color(0.74f, 0.76f, 0.70f, 1f);

        string[] names = { "Mèo Miu Xuất Trận", "Hành Trình Mới", "Thầy Luyện Xác", "Vùng Đất Bất Tử", "Sông Băng Địa Cực", "Sân Thử Nghiệm", "Đấu Trường Gargantuar", "Chiến Dịch Ba Cõi" };
        selectedLevelText.text = "Đã chọn: Màn " + (levelIndex + 1) + " — " + names[levelIndex];
        playLevelButton.interactable = true;
        PlayClick();
    }

    private void PlaySelectedLevel()
    {
        if (selectedLevel < 0 || transitioning) return;

        if (selectedLevel == GargantuarArenaLevelIndex)
        {
            StartCoroutine(LoadSceneWithFade(GargantuarArenaBootstrap.SceneName));
            return;
        }

        if (selectedLevel == ThreeWorldsCampaignLevelIndex)
        {
            StartCoroutine(LoadSceneWithFade(CampaignBootstrap.GameScene));
            return;
        }

        GameSession.SelectedLevel = selectedLevel;
        PlantSelectionOverlay.Show(selectedLevel, () => StartCoroutine(LoadSceneWithFade("GameScene")));
    }

    private void ShowOptions() => OpenModal(optionsPanel);
    private void ShowHelp() => OpenModal(helpPanel);

    private void OpenModal(GameObject panel)
    {
        if (transitioning || panel == null) return;
        panel.SetActive(true);
        StartCoroutine(FadeCanvasGroup(panel.GetComponent<CanvasGroup>(), 0f, 1f, 0.2f, false));
    }

    private void CloseModal(GameObject panel)
    {
        if (panel != null)
            StartCoroutine(FadeCanvasGroup(panel.GetComponent<CanvasGroup>(), panel.GetComponent<CanvasGroup>().alpha, 0f, 0.16f, true));
    }

    private void ShowNotice(string message)
    {
        if (!transitioning)
            StartCoroutine(ShowNoticeRoutine(message));
    }

    private IEnumerator ShowNoticeRoutine(string message)
    {
        noticeText.text = message;
        noticeText.gameObject.SetActive(true);
        noticeText.canvasRenderer.SetAlpha(0f);
        noticeText.CrossFadeAlpha(1f, 0.15f, true);
        yield return new WaitForSecondsRealtime(1.8f);
        noticeText.CrossFadeAlpha(0f, 0.25f, true);
        yield return new WaitForSecondsRealtime(0.25f);
        noticeText.gameObject.SetActive(false);
    }

    private void StartAdventure()
    {
        if (!transitioning)
            OpenModal(levelPanel);
    }

    private IEnumerator LoadSceneWithFade(string sceneName)
    {
        transitioning = true;
        PlayClick();
        fadeImage.gameObject.SetActive(true);
        fadeImage.canvasRenderer.SetAlpha(0f);
        fadeImage.CrossFadeAlpha(1f, 0.48f, true);
        yield return new WaitForSecondsRealtime(0.5f);
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator AnimateEntrance()
    {
        float elapsed = 0f;
        while (elapsed < 0.7f)
        {
            elapsed += Time.unscaledDeltaTime;
            menuGroup.alpha = Mathf.SmoothStep(0f, 1f, elapsed / 0.7f);
            yield return null;
        }
        menuGroup.alpha = 1f;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration, bool disableAfter)
    {
        float elapsed = 0f;
        group.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        group.alpha = to;
        if (disableAfter) group.gameObject.SetActive(false);
    }

    private void QuitGame()
    {
        PlayClick();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void PlayClick()
    {
        var clip = Resources.Load<AudioClip>("Sounds/UI/graveButtonClick");
        if (clip != null) AudioSource.PlayClipAtPoint(clip, Vector3.zero);
    }

    public void SetMenuTextHighlight(Vector4 hoverRect, bool highlighted)
    {
        if (menuHoverMaterial == null) return;
        if (highlighted)
        {
            menuHoverMaterial.SetVector("_HoverRect", hoverRect);
            menuHoverTarget = 1f;
        }
        else
        {
            menuHoverTarget = 0f;
        }
    }

    private Text CreateText(string name, Transform parent, string value, int size, TextAnchor alignment, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>();
        text.font = menuFont;
        text.text = value;
        text.fontSize = size;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 15;
        text.resizeTextMaxSize = size;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        return image;
    }

    private static void Stretch(RectTransform rect)
    {
        SetAnchors(rect, 0f, 0f, 1f, 1f);
    }

    private static void SetAnchors(RectTransform rect, float xMin, float yMin, float xMax, float yMax)
    {
        rect.anchorMin = new Vector2(xMin, yMin);
        rect.anchorMax = new Vector2(xMax, yMax);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static void EnsureMenuCamera()
    {
        if (Camera.main != null || Object.FindAnyObjectByType<Camera>() != null)
            return;

        var cameraObject = new GameObject("Main Menu Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.cullingMask = 0;
        camera.orthographic = true;
        camera.depth = -100f;
    }
}

public static class GameSession
{
    public static int SelectedLevel = -1;
    public static readonly List<string> SelectedPlants = new List<string>();
}

public class MenuButtonMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public Graphic targetGraphic;
    public bool highlightMenuText;
    public MainMenuController menuController;
    public Vector4 hoverRect;
    private Vector3 targetScale = Vector3.one;

    private void Update()
    {
        if (!highlightMenuText)
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, 14f * Time.unscaledDeltaTime);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (highlightMenuText)
            menuController.SetMenuTextHighlight(hoverRect, true);
        else
            targetScale = Vector3.one * 1.025f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (highlightMenuText)
            menuController.SetMenuTextHighlight(hoverRect, false);
        else
            targetScale = Vector3.one;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (highlightMenuText)
            menuController.SetMenuTextHighlight(hoverRect, true);
        else
            targetScale = Vector3.one * 0.965f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (highlightMenuText)
            menuController.SetMenuTextHighlight(hoverRect, true);
        else
            targetScale = Vector3.one * 1.025f;
    }
}
