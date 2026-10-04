using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
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

    // Giữ nguyên id dữ liệu màn, chỉ đổi thứ tự trình bày: campaign 1-2-3-4-5-7,
    // sau đó mới tới sandbox Map Test.
    private static readonly int[] LevelOrder = { 0, 1, 2, 3, 4, 6, 7, 5 };
    private static readonly string[] LevelNames =
    {
        "Mèo Miu Xuất Trận",
        "Hành Trình Mới",
        "Thầy Luyện Xác",
        "Vùng Đất Bất Tử",
        "Sông Băng Địa Cực",
        "Map Test",
        "Rừng Nhật Thực",
        "Đảo Thiên Đường"
    };
    private static readonly string[] LevelThumbnails =
    {
        "Sprites/BackGround/Background_Day",
        "Sprites/BackGround/BG_sanvuon",
        "Sprites/BackGround/Background_Night_Wall",
        "Sprites/BackGround/background_Night_Bone",
        "Sprites/BackGround/Background_Ice",
        "Sprites/BackGround/Background_Forest",
        "Sprites/BackGround/map7",
        "Sprites/BackGround/map8"
    };

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

        var frameObject = new GameObject("Khung Menu 3x2", typeof(RectTransform), typeof(AspectRatioFitter));
        frameObject.transform.SetParent(canvasObject.transform, false);
        Stretch(frameObject.GetComponent<RectTransform>());
        var frameFitter = frameObject.GetComponent<AspectRatioFitter>();
        frameFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        frameFitter.aspectRatio = 3f / 2f;
        Transform menuFrame = frameObject.transform;

        var background = CreateImage("Nền Menu", menuFrame, Resources.Load<Sprite>("Sprites/UI/MainMenu/menu_Main_final"));
        Stretch(background.rectTransform);
        background.raycastTarget = false;
        var hoverShader = Resources.Load<Shader>("Shaders/UIMenuTextHover");
        if (hoverShader != null)
        {
            menuHoverMaterial = new Material(hoverShader);
            background.material = menuHoverMaterial;
        }

        // Các hitbox bám theo đúng vị trí artwork menu_Main_final 1536x1024.
        CreateHotspot(menuFrame, "Phiêu lưu", 0.615f, 0.710f, 0.912f, 0.895f, new Vector4(0.655f, 0.755f, 0.885f, 0.855f), StartAdventure);
        CreateHotspot(menuFrame, "Trò chơi nhỏ", 0.620f, 0.590f, 0.910f, 0.715f, new Vector4(0.650f, 0.615f, 0.880f, 0.690f), () => ShowNotice("Chế độ Trò chơi nhỏ sẽ sớm ra mắt!"));
        CreateHotspot(menuFrame, "Giải đố", 0.632f, 0.475f, 0.902f, 0.595f, new Vector4(0.680f, 0.500f, 0.865f, 0.570f), () => ShowNotice("Chế độ Giải đố sẽ sớm ra mắt!"));
        CreateHotspot(menuFrame, "Sinh tồn", 0.642f, 0.355f, 0.895f, 0.485f, new Vector4(0.685f, 0.385f, 0.860f, 0.455f), () => ShowNotice("Chế độ Sinh tồn sẽ sớm ra mắt!"));
        CreateHotspot(menuFrame, "Cửa hàng", 0.130f, 0.085f, 0.345f, 0.245f, new Vector4(0.205f, 0.115f, 0.335f, 0.195f), () => ShowNotice("Cửa hàng hiện đang đóng cửa."));
        CreateHotspot(menuFrame, "Tùy chọn", 0.555f, 0.105f, 0.685f, 0.235f, new Vector4(0.575f, 0.130f, 0.670f, 0.205f), ShowOptions);
        CreateHotspot(menuFrame, "Trợ giúp", 0.690f, 0.100f, 0.820f, 0.235f, new Vector4(0.715f, 0.125f, 0.805f, 0.205f), ShowHelp);
        CreateHotspot(menuFrame, "Thoát", 0.830f, 0.090f, 0.978f, 0.235f, new Vector4(0.865f, 0.120f, 0.955f, 0.205f), QuitGame);

        noticeText = CreateText("Thông báo", menuFrame, string.Empty, 24, TextAnchor.MiddleCenter, new Color(0.28f, 0.12f, 0.035f));
        SetAnchors(noticeText.rectTransform, 0.085f, 0.660f, 0.335f, 0.755f);
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
        if (selectedLevel == 7)
        {
            ShowNotice("Đảo Thiên Đường đang ở chế độ chơi đơn để giữ đúng chu kỳ thủy triều.");
            return;
        }
        NetLobbyUI.Open(selectedLevel);
    }

    private GameObject BuildModal(Transform parent, string title, string body)
    {
        var panel = new GameObject(title + " Panel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        panel.transform.SetParent(parent, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(.5f, .5f);
        panelRect.pivot = new Vector2(.5f, .5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(640f, 360f);
        Image panelHitArea = panel.GetComponent<Image>();
        panelHitArea.color = new Color(0f, 0f, 0f, .001f);

        Sprite dialogSprite = Resources.Load<Sprite>("GameUI/dialog_main");
        Image dialog = CreateImage("Khung dialog", panel.transform, dialogSprite);
        RectTransform dialogRect = dialog.rectTransform;
        dialogRect.anchorMin = dialogRect.anchorMax = new Vector2(.5f, .5f);
        dialogRect.pivot = new Vector2(.5f, .5f);
        dialogRect.anchoredPosition = new Vector2(0f, 15f);
        dialogRect.sizeDelta = new Vector2(620f, 277f);
        dialog.preserveAspect = true;
        dialog.raycastTarget = false;

        var titleText = CreateText("Tiêu đề", panel.transform, title, 30,
            TextAnchor.MiddleCenter, new Color(.23f, .085f, .025f, 1f));
        SetAnchors(titleText.rectTransform, .30f, .70f, .70f, .88f);
        titleText.fontStyle = FontStyle.Bold;

        int bodySize = body.Length > 180 ? 19 : 24;
        var bodyText = CreateText("Nội dung", panel.transform, body, bodySize,
            TextAnchor.MiddleCenter, new Color(.26f, .11f, .035f, 1f));
        SetAnchors(bodyText.rectTransform, .10f, .28f, .90f, .69f);
        bodyText.resizeTextMinSize = body.Length > 180 ? 13 : 16;

        var close = new GameObject("Đóng", typeof(RectTransform), typeof(Image), typeof(Button), typeof(MenuButtonMotion));
        close.transform.SetParent(panel.transform, false);
        RectTransform closeRect = close.GetComponent<RectTransform>();
        closeRect.anchorMin = closeRect.anchorMax = new Vector2(.5f, .5f);
        closeRect.pivot = new Vector2(.5f, .5f);
        closeRect.anchoredPosition = new Vector2(0f, -145f);
        closeRect.sizeDelta = new Vector2(168f, 51f);
        var closeImage = close.GetComponent<Image>();
        Sprite normalButton = Resources.Load<Sprite>("GameUI/button1");
        Sprite highlightedButton = Resources.Load<Sprite>("GameUI/button2");
        closeImage.sprite = normalButton;
        closeImage.preserveAspect = true;
        var closeButton = close.GetComponent<Button>();
        closeButton.transition = Selectable.Transition.SpriteSwap;
        SpriteState states = closeButton.spriteState;
        states.highlightedSprite = highlightedButton;
        states.pressedSprite = highlightedButton;
        states.selectedSprite = highlightedButton;
        closeButton.spriteState = states;
        closeButton.onClick.AddListener(() => CloseModal(panel));
        close.GetComponent<MenuButtonMotion>().targetGraphic = closeImage;
        var closeText = CreateText("Chữ", close.transform, "ĐÓNG", 23,
            TextAnchor.MiddleCenter, new Color(1f, .96f, .74f, 1f));
        Stretch(closeText.rectTransform);
        closeText.fontStyle = FontStyle.Bold;
        closeText.raycastTarget = false;
        Outline closeOutline = closeText.gameObject.AddComponent<Outline>();
        closeOutline.effectColor = new Color(.12f, .045f, .01f, .9f);
        closeOutline.effectDistance = new Vector2(1.4f, -1.4f);

        panel.SetActive(false);
        return panel;
    }

    private GameObject BuildLevelSelection(Transform parent)
    {
        var panel = new GameObject("Chọn màn chơi Panel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        panel.transform.SetParent(parent, false);
        Stretch(panel.GetComponent<RectTransform>());
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, .72f);

        Image mainFrame = CreateImage("Khung chọn màn", panel.transform,
            Resources.Load<Sprite>("GameUI/dialog_main"));
        RectTransform frameRect = mainFrame.rectTransform;
        frameRect.anchorMin = frameRect.anchorMax = new Vector2(.5f, .5f);
        frameRect.pivot = new Vector2(.5f, .5f);
        frameRect.anchoredPosition = new Vector2(0f, 25f);
        frameRect.sizeDelta = new Vector2(920f, 411f);
        mainFrame.preserveAspect = true;
        mainFrame.raycastTarget = false;

        var title = CreateText("Tiêu đề", panel.transform, "CHỌN MÀN CHƠI", 34,
            TextAnchor.MiddleCenter, new Color(.23f, .085f, .025f, 1f));
        CenterRect(title.rectTransform, new Vector2(320f, 54f), new Vector2(0f, 204f));
        title.fontStyle = FontStyle.Bold;
        title.raycastTarget = false;

        var close = new GameObject("Đóng chọn màn", typeof(RectTransform), typeof(Image),
            typeof(Button), typeof(MenuButtonMotion));
        close.transform.SetParent(panel.transform, false);
        CenterRect(close.GetComponent<RectTransform>(), new Vector2(50f, 55f), new Vector2(438f, 218f));
        Image closeImage = close.GetComponent<Image>();
        closeImage.sprite = Resources.Load<Sprite>("GameUI/cancel");
        closeImage.preserveAspect = true;
        Button closeButton = close.GetComponent<Button>();
        closeButton.transition = Selectable.Transition.None;
        closeButton.onClick.AddListener(() => CloseModal(panel));
        close.GetComponent<MenuButtonMotion>().targetGraphic = closeImage;

        var contentObject = new GameObject("Danh sách màn", typeof(RectTransform), typeof(GridLayoutGroup));
        contentObject.transform.SetParent(panel.transform, false);
        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        CenterRect(contentRect, new Vector2(512f, 226f), new Vector2(0f, 24f));

        var grid = contentObject.GetComponent<GridLayoutGroup>();
        grid.padding = new RectOffset(0, 0, 0, 0);
        grid.spacing = new Vector2(8f, 12f);
        // Khung Intro có tỉ lệ 270:236; giữ đúng tỉ lệ để artwork không bị ép ngang.
        grid.cellSize = new Vector2(122f, 107f);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;

        for (int cardIndex = 0; cardIndex < LevelOrder.Length; cardIndex++)
        {
            int levelId = LevelOrder[cardIndex];
            CreateLevelCard(contentObject.transform, levelId, LevelNames[levelId],
                LevelThumbnails[levelId], DisplayLevelLabel(levelId));
        }

        selectedLevelText = CreateText("Màn đã chọn", panel.transform, "HÃY CHỌN MỘT MÀN CHƠI", 21,
            TextAnchor.MiddleCenter, new Color(.29f, .12f, .035f, 1f));
        CenterRect(selectedLevelText.rectTransform, new Vector2(520f, 42f), new Vector2(0f, -128f));
        selectedLevelText.fontStyle = FontStyle.Bold;
        selectedLevelText.raycastTarget = false;

        GameObject netButton = CreateGameUiButton("Chơi mạng", panel.transform, "CHƠI MẠNG", 20, OpenNetLobby);
        CenterRect(netButton.GetComponent<RectTransform>(), new Vector2(176f, 54f), new Vector2(-330f, -224f));

        GameObject playObject = CreateGameUiButton("Chơi", panel.transform, "CHƠI", 23, PlaySelectedLevel);
        CenterRect(playObject.GetComponent<RectTransform>(), new Vector2(176f, 54f), new Vector2(330f, -224f));
        playLevelButton = playObject.GetComponent<Button>();
        playLevelButton.interactable = false;
        playObject.GetComponent<Image>().color = new Color(.55f, .55f, .55f, .85f);

        panel.SetActive(false);
        return panel;
    }

    private static string DisplayLevelLabel(int levelId)
    {
        return levelId == 5 ? "MAP TEST" : "MÀN " + (levelId + 1);
    }

    private void CreateLevelCard(Transform parent, int levelIndex, string levelName,
        string thumbnailPath, string displayLabel)
    {
        var card = new GameObject(displayLabel, typeof(RectTransform), typeof(Image), typeof(Button), typeof(MenuButtonMotion));
        card.transform.SetParent(parent, false);
        var frame = card.GetComponent<Image>();
        frame.sprite = Resources.Load<Sprite>("Sprites/UI/Intro/khung");
        frame.preserveAspect = true;
        frame.color = Color.white;
        levelCardFrames.Add(frame);

        var button = card.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        int capturedLevel = levelIndex;
        button.onClick.AddListener(() => SelectLevel(capturedLevel));
        card.GetComponent<MenuButtonMotion>().targetGraphic = frame;

        var thumbnail = CreateImage("Ảnh preview màn", card.transform, Resources.Load<Sprite>(thumbnailPath));
        // Cửa sổ trong suốt của khung: x 27..244, y 44..154 trên ảnh 270x236.
        SetAnchors(thumbnail.rectTransform, .105f, .350f, .895f, .805f);
        thumbnail.preserveAspect = true;
        thumbnail.raycastTarget = false;

        var label = CreateText("Tên màn", card.transform, displayLabel + " • " + levelName, 12,
            TextAnchor.MiddleCenter, new Color(.92f, .94f, 1f, 1f));
        // Bảng kim loại phía dưới khung: vùng an toàn x 34..236, y 173..220.
        SetAnchors(label.rectTransform, .13f, .065f, .87f, .275f);
        label.fontStyle = FontStyle.Bold;
        label.resizeTextMinSize = 8;
        label.resizeTextMaxSize = 12;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.raycastTarget = false;
    }

    private GameObject CreateGameUiButton(string name, Transform parent, string label, int fontSize,
        UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(MenuButtonMotion));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.sprite = Resources.Load<Sprite>("GameUI/button1");
        image.preserveAspect = true;
        Button button = go.GetComponent<Button>();
        button.transition = Selectable.Transition.SpriteSwap;
        Sprite highlighted = Resources.Load<Sprite>("GameUI/button2");
        SpriteState states = button.spriteState;
        states.highlightedSprite = highlighted;
        states.pressedSprite = highlighted;
        states.selectedSprite = highlighted;
        button.spriteState = states;
        button.onClick.AddListener(action);
        go.GetComponent<MenuButtonMotion>().targetGraphic = image;

        Text text = CreateText("Chữ", go.transform, label, fontSize,
            TextAnchor.MiddleCenter, new Color(1f, .96f, .74f, 1f));
        Stretch(text.rectTransform);
        text.fontStyle = FontStyle.Bold;
        text.raycastTarget = false;
        Outline outline = text.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(.12f, .045f, .01f, .9f);
        outline.effectDistance = new Vector2(1.3f, -1.3f);
        return go;
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
        int selectedCardIndex = System.Array.IndexOf(LevelOrder, levelIndex);
        for (int i = 0; i < levelCardFrames.Count; i++)
            levelCardFrames[i].color = i == selectedCardIndex
                ? new Color(.79f, 1f, .62f, 1f)
                : Color.white;

        selectedLevelText.text = "Đã chọn: " + DisplayLevelLabel(levelIndex) + " — " + LevelNames[levelIndex];
        playLevelButton.interactable = true;
        if (playLevelButton.targetGraphic != null) playLevelButton.targetGraphic.color = Color.white;
        PlayClick();
    }

    private void PlaySelectedLevel()
    {
        if (selectedLevel < 0 || transitioning) return;

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

    private static void CenterRect(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
        rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
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
