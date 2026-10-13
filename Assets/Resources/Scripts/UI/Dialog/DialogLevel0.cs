using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogLevel0 : MonoBehaviour, IStartupDialogSkippable
{
    public GameObject zombieIntroduce;   //Bảng giới thiệu zombie ban đầu
    public GameObject plantIntroduce;    //Bảng giới thiệu cây ban đầu

    private GameObject fusionTutorialPanel;
    private int currentTutorialPage = 0;
    private const int TotalTutorialPages = 2;

    private Text pageIndicatorText;
    private Text nextPageButtonText;
    private Image pageDot1;
    private Image pageDot2;
    private GameObject page1Container;
    private GameObject page2Container;
    private ScrollRect tutorialScrollRect;
    private RectTransform tutorialFrameRect;
    private float tutorialFrameFitScale = 1f;
    private bool tutorialEntranceAnimating;
    private int lastLayoutScreenWidth;
    private int lastLayoutScreenHeight;
    private Rect lastLayoutSafeArea;
    private static Sprite roundedUiSprite;
    private static Sprite tutorialDialogMain;
    private static Sprite tutorialDialogChild;
    private static Sprite tutorialButtonNormal;
    private static Sprite tutorialButtonHighlighted;

    public const string FirstTimeTutorialKey = "PvZ_FirstTime_FusionTutorial_Done";

    private static Sprite GetRoundedUiSprite()
    {
        if (roundedUiSprite != null) return roundedUiSprite;

        const int size = 16;
        const float radius = 6.5f;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Runtime Rounded UI Texture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float px = x + .5f;
                float py = y + .5f;
                float nearestX = Mathf.Clamp(px, radius, size - radius);
                float nearestY = Mathf.Clamp(py, radius, size - radius);
                float distance = Vector2.Distance(new Vector2(px, py), new Vector2(nearestX, nearestY));
                byte alpha = (byte)Mathf.RoundToInt(Mathf.Clamp01(radius + .5f - distance) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        roundedUiSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(radius, radius, radius, radius)
        );

        return roundedUiSprite;
    }

    void Start()
    {
        // Ẩn các bảng prefab cũ để trình diễn bảng hướng dẫn Fusion chuyên biệt và đầy đủ
        if (plantIntroduce != null) plantIntroduce.SetActive(false);
        if (zombieIntroduce != null) zombieIntroduce.SetActive(false);

        BuildFusionTutorialUI();
        AdventureDialogUI.AddCloseButton(zombieIntroduce, FinishTutorialFromInformation);
        AdventureDialogUI.AddCloseButton(plantIntroduce, FinishTutorialFromInformation);
    }

    private void OnDestroy()
    {
        // Tutorial dùng Canvas độc lập nên cần dọn riêng khi prefab hội thoại bị huỷ.
        if (fusionTutorialPanel != null) Destroy(fusionTutorialPanel);
    }

    private void Update()
    {
        if (tutorialFrameRect == null || tutorialEntranceAnimating) return;
        if (lastLayoutScreenWidth == Screen.width && lastLayoutScreenHeight == Screen.height &&
            lastLayoutSafeArea == Screen.safeArea) return;

        FitTutorialToSafeArea();
    }

    public void SkipConversationToInformation()
    {
        CancelInvoke();
        PlayButtonClickAudio();
        if (fusionTutorialPanel != null) fusionTutorialPanel.SetActive(false);
        if (plantIntroduce != null) plantIntroduce.SetActive(false);
        if (zombieIntroduce != null)
        {
            zombieIntroduce.SetActive(true);
            AdventureDialogUI.AddCloseButton(zombieIntroduce, FinishTutorialFromInformation);
        }
        else
        {
            FinishTutorialFromInformation();
        }
    }

    private void FinishTutorialFromInformation()
    {
        PlayerPrefs.SetInt(FirstTimeTutorialKey, 1);
        PlayerPrefs.Save();
        FinishAndStartGame();
    }

    private void BuildFusionTutorialUI()
    {
        Font font = Resources.Load<Font>("Fonts/Baloo2");
        tutorialDialogMain = tutorialDialogMain ?? CreateSlicedSprite(Resources.Load<Sprite>("GameUI/dialog_main"), .12f, .14f);
        tutorialDialogChild = tutorialDialogChild ?? CreateSlicedSprite(Resources.Load<Sprite>("GameUI/dialog_child"), .12f, .20f);
        tutorialButtonNormal = tutorialButtonNormal ?? CreateSlicedSprite(Resources.Load<Sprite>("GameUI/button1"), .14f, .30f);
        tutorialButtonHighlighted = tutorialButtonHighlighted ?? CreateSlicedSprite(Resources.Load<Sprite>("GameUI/button2"), .18f, .28f);

        fusionTutorialPanel = new GameObject("Bảng Hướng Dẫn Fusion",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster),
            typeof(CanvasRenderer), typeof(CanvasGroup));

        // Canvas riêng giúp tutorial không còn phụ thuộc TopCanvas 800x600 của gameplay.
        Canvas tutorialCanvas = fusionTutorialPanel.GetComponent<Canvas>();
        tutorialCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        tutorialCanvas.sortingOrder = 2200;

        CanvasScaler tutorialScaler = fusionTutorialPanel.GetComponent<CanvasScaler>();
        tutorialScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        tutorialScaler.referenceResolution = new Vector2(760f, 520f);
        tutorialScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        RectTransform panelRect = fusionTutorialPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        // Lớp nền đen mờ bao phủ
        var darkShade = fusionTutorialPanel.AddComponent<Image>();
        darkShade.color = new Color(0f, 0f, 0f, 0.78f);
        darkShade.raycastTarget = true;

        // Khung gỗ chính chuẩn PvZ (dialog_main)
        GameObject woodFrame = new GameObject("Khung Gỗ", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        woodFrame.transform.SetParent(fusionTutorialPanel.transform, false);
        RectTransform frameRect = woodFrame.GetComponent<RectTransform>();
        tutorialFrameRect = frameRect;
        frameRect.anchorMin = new Vector2(0.5f, 0.5f);
        frameRect.anchorMax = new Vector2(0.5f, 0.5f);
        frameRect.pivot = new Vector2(0.5f, 0.5f);
        frameRect.sizeDelta = new Vector2(760f, 520f);
        FitTutorialToSafeArea();

        Image frameImg = woodFrame.GetComponent<Image>();
        frameImg.sprite = tutorialDialogMain;
        frameImg.type = Image.Type.Sliced;
        Shadow frameShadow = woodFrame.AddComponent<Shadow>();
        frameShadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
        frameShadow.effectDistance = new Vector2(10f, -12f);

        // Lớp giấy sáng giúp nội dung tách khỏi nền gỗ và dễ đọc trên mọi bản đồ.
        GameObject contentBackdrop = new GameObject("Nền Giấy Nội Dung", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        contentBackdrop.transform.SetParent(woodFrame.transform, false);
        RectTransform backdropRect = contentBackdrop.GetComponent<RectTransform>();
        backdropRect.anchorMin = new Vector2(0.045f, 0.18f);
        backdropRect.anchorMax = new Vector2(0.955f, 0.80f);
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;
        Image backdropImage = contentBackdrop.GetComponent<Image>();
        backdropImage.sprite = tutorialDialogChild;
        backdropImage.type = Image.Type.Sliced;
        backdropImage.color = new Color(1f, 0.96f, 0.82f, 0.92f);
        backdropImage.raycastTarget = false;

        // Tiêu đề bảng
        GameObject titleObj = new GameObject("Tiêu Đề", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
        titleObj.transform.SetParent(woodFrame.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -34f);
        titleRect.sizeDelta = new Vector2(520f, 40f);

        Text titleText = titleObj.GetComponent<Text>();
        titleText.font = font;
        titleText.fontSize = 26;
        titleText.resizeTextForBestFit = true;
        titleText.resizeTextMinSize = 20;
        titleText.resizeTextMaxSize = 26;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(0.24f, 0.09f, 0.02f, 1f);
        titleText.text = "HƯỚNG DẪN GHÉP CÂY";
        Outline titleOutline = titleObj.GetComponent<Outline>();
        titleOutline.effectColor = new Color(1f, 0.88f, 0.55f, 0.9f);
        titleOutline.effectDistance = new Vector2(1.2f, -1.2f);

        // Chỉ số trang (Trang 1/2)
        GameObject pageIndObj = new GameObject("Chỉ Số Trang", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        pageIndObj.transform.SetParent(woodFrame.transform, false);
        RectTransform pageIndRect = pageIndObj.GetComponent<RectTransform>();
        pageIndRect.anchorMin = new Vector2(0.5f, 1f);
        pageIndRect.anchorMax = new Vector2(0.5f, 1f);
        pageIndRect.pivot = new Vector2(0.5f, 1f);
        pageIndRect.anchoredPosition = new Vector2(0f, -72f);
        pageIndRect.sizeDelta = new Vector2(320f, 22f);

        pageIndicatorText = pageIndObj.GetComponent<Text>();
        pageIndicatorText.font = font;
        pageIndicatorText.fontSize = 14;
        pageIndicatorText.resizeTextForBestFit = true;
        pageIndicatorText.resizeTextMinSize = 12;
        pageIndicatorText.resizeTextMaxSize = 14;
        pageIndicatorText.fontStyle = FontStyle.Bold;
        pageIndicatorText.alignment = TextAnchor.MiddleCenter;
        pageIndicatorText.color = new Color(0.38f, 0.18f, 0.05f, 1f);
        pageIndicatorText.text = "TRANG 1 / 2  •  CÁCH GHÉP";

        pageDot1 = CreatePageDot("Trang 1", woodFrame.transform, new Vector2(-195f, -82f));
        pageDot2 = CreatePageDot("Trang 2", woodFrame.transform, new Vector2(195f, -82f));

        // Vùng cuộn nội dung (Scroll View)
        GameObject scrollObj = new GameObject("Scroll View", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
        scrollObj.transform.SetParent(woodFrame.transform, false);
        RectTransform scrollRectTransform = scrollObj.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0.065f, 0.19f);
        scrollRectTransform.anchorMax = new Vector2(0.935f, 0.785f);
        scrollRectTransform.offsetMin = Vector2.zero;
        scrollRectTransform.offsetMax = Vector2.zero;
        scrollObj.GetComponent<Image>().color = Color.clear;

        tutorialScrollRect = scrollObj.GetComponent<ScrollRect>();

        // Viewport
        GameObject viewportObj = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
        viewportObj.transform.SetParent(scrollObj.transform, false);
        RectTransform vpRect = viewportObj.GetComponent<RectTransform>();
        vpRect.anchorMin = new Vector2(0f, 0f);
        vpRect.anchorMax = new Vector2(0.965f, 1f);
        vpRect.offsetMin = Vector2.zero;
        vpRect.offsetMax = Vector2.zero;
        viewportObj.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.001f);

        // Vùng chứa nội dung (Content)
        GameObject contentArea = new GameObject("Vùng Nội Dung", typeof(RectTransform));
        contentArea.transform.SetParent(viewportObj.transform, false);
        RectTransform contentRect = contentArea.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 300f);

        // Thanh cuộn (Scrollbar) chuẩn phong cách gỗ PvZ
        GameObject scrollbarObj = new GameObject("Scrollbar Dọc", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Scrollbar));
        scrollbarObj.transform.SetParent(scrollObj.transform, false);
        RectTransform barRect = scrollbarObj.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0.975f, 0.04f);
        barRect.anchorMax = new Vector2(0.995f, 0.96f);
        barRect.offsetMin = Vector2.zero;
        barRect.offsetMax = Vector2.zero;
        scrollbarObj.GetComponent<Image>().color = Color.clear;

        GameObject slidingArea = new GameObject("Sliding Area", typeof(RectTransform));
        slidingArea.transform.SetParent(scrollbarObj.transform, false);
        RectTransform saRect = slidingArea.GetComponent<RectTransform>();
        saRect.anchorMin = Vector2.zero;
        saRect.anchorMax = Vector2.one;
        saRect.offsetMin = Vector2.zero;
        saRect.offsetMax = Vector2.zero;

        Sprite roundedSprite = GetRoundedUiSprite();

        // Track rãnh gỗ sẫm
        GameObject trackObj = new GameObject("Track", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        trackObj.transform.SetParent(slidingArea.transform, false);
        RectTransform trackRect = trackObj.GetComponent<RectTransform>();
        trackRect.anchorMin = Vector2.zero;
        trackRect.anchorMax = Vector2.one;
        trackRect.offsetMin = Vector2.zero;
        trackRect.offsetMax = Vector2.zero;
        Image trackImg = trackObj.GetComponent<Image>();
        trackImg.sprite = roundedSprite;
        trackImg.type = Image.Type.Sliced;
        trackImg.color = new Color(0.18f, 0.08f, 0.03f, 0.60f);
        trackImg.raycastTarget = false;

        // Con trượt (Handle) gỗ vàng sáng
        GameObject handleObj = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        handleObj.transform.SetParent(slidingArea.transform, false);
        RectTransform handleRect = handleObj.GetComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.one;
        handleRect.offsetMin = Vector2.zero;
        handleRect.offsetMax = Vector2.zero;
        Image handleImg = handleObj.GetComponent<Image>();
        handleImg.sprite = roundedSprite;
        handleImg.type = Image.Type.Sliced;
        handleImg.color = new Color(0.96f, 0.74f, 0.22f, 0.95f);

        Scrollbar scrollbar = scrollbarObj.GetComponent<Scrollbar>();
        scrollbar.handleRect = handleRect;
        scrollbar.targetGraphic = handleImg;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        // Cài đặt ScrollRect
        tutorialScrollRect.viewport = vpRect;
        tutorialScrollRect.content = contentRect;
        tutorialScrollRect.horizontal = false;
        tutorialScrollRect.vertical = true;
        tutorialScrollRect.movementType = ScrollRect.MovementType.Clamped;
        tutorialScrollRect.inertia = true;
        tutorialScrollRect.decelerationRate = 0.15f;
        tutorialScrollRect.scrollSensitivity = 35f;
        tutorialScrollRect.verticalScrollbar = scrollbar;
        tutorialScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        tutorialScrollRect.verticalNormalizedPosition = 1f;

        BuildPage1_CoreLogic(contentArea.transform, font);
        BuildPage2_FormulasAndEvolution(contentArea.transform, font);

        // Footer độc lập với vùng cuộn: nội dung không thể tràn xuống che nút.
        GameObject footer = new GameObject("Thanh Điều Hướng", typeof(RectTransform));
        footer.transform.SetParent(woodFrame.transform, false);
        RectTransform footerRect = footer.GetComponent<RectTransform>();
        footerRect.anchorMin = new Vector2(0.05f, 0.02f);
        footerRect.anchorMax = new Vector2(0.95f, 0.17f);
        footerRect.offsetMin = Vector2.zero;
        footerRect.offsetMax = Vector2.zero;

        // Nút CHUYỂN TRANG / TIẾP THEO
        GameObject nextPageButton = CreateActionButton("Nút Chuyển Trang", footer.transform, new Vector2(-180f, 8f), new Vector2(160f, 50f),
            "TRANG SAU", font, 17, OnNextPageClicked);
        nextPageButtonText = nextPageButton.GetComponentInChildren<Text>();

        // Nút VÀO TRẬN / BẮT ĐẦU CHƠI
        CreateActionButton("Nút Bắt Đầu", footer.transform, new Vector2(0f, 8f), new Vector2(160f, 50f),
            "VÀO TRẬN", font, 17, OnStartGameClicked);

        // NÚT BỎ QUA / SKIP (To rõ, nổi bật để người dùng có thể skip bất cứ lúc nào)
        CreateSkipButton("Nút Bỏ Qua", footer.transform, new Vector2(180f, 8f), new Vector2(160f, 50f),
            "BỎ QUA", font, 17, OnSkipClicked);

        UpdatePageVisibility();
        StartCoroutine(AnimateTutorialEntrance(woodFrame.transform, fusionTutorialPanel.GetComponent<CanvasGroup>()));
    }

    private Image CreatePageDot(string name, Transform parent, Vector2 position)
    {
        GameObject dot = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dot.transform.SetParent(parent, false);
        RectTransform rect = dot.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(14f, 14f);
        Image image = dot.GetComponent<Image>();
        image.sprite = GetRoundedUiSprite();
        image.type = Image.Type.Sliced;
        image.raycastTarget = false;
        return image;
    }

    private IEnumerator AnimateTutorialEntrance(Transform frame, CanvasGroup canvasGroup)
    {
        tutorialEntranceAnimating = true;
        yield return null;
        FitTutorialToSafeArea();

        canvasGroup.alpha = 0f;
        Vector3 fittedScale = Vector3.one * tutorialFrameFitScale;
        frame.localScale = fittedScale * 0.92f;
        float elapsed = 0f;
        const float duration = 0.28f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - progress, 3f);
            canvasGroup.alpha = eased;
            frame.localScale = Vector3.LerpUnclamped(fittedScale * 0.92f, fittedScale, eased);
            yield return null;
        }
        canvasGroup.alpha = 1f;
        frame.localScale = fittedScale;
        tutorialEntranceAnimating = false;
    }

    private void FitTutorialToSafeArea()
    {
        if (tutorialFrameRect == null || fusionTutorialPanel == null || Screen.width <= 0 || Screen.height <= 0)
            return;

        RectTransform panelRect = fusionTutorialPanel.GetComponent<RectTransform>();
        Rect safeArea = Screen.safeArea;
        float safeWidth = panelRect.rect.width * safeArea.width / Screen.width;
        float safeHeight = panelRect.rect.height * safeArea.height / Screen.height;
        const float margin = 18f;

        tutorialFrameFitScale = Mathf.Clamp(
            Mathf.Min((safeWidth - margin * 2f) / 760f, (safeHeight - margin * 2f) / 520f),
            0.25f, 1f);

        // Đặt giữa vùng an toàn để tránh tai thỏ, camera khoét lỗ và thanh điều hướng.
        tutorialFrameRect.anchoredPosition = new Vector2(
            (safeArea.center.x / Screen.width - 0.5f) * panelRect.rect.width,
            (safeArea.center.y / Screen.height - 0.5f) * panelRect.rect.height);
        tutorialFrameRect.localScale = Vector3.one * tutorialFrameFitScale;

        lastLayoutScreenWidth = Screen.width;
        lastLayoutScreenHeight = Screen.height;
        lastLayoutSafeArea = safeArea;
    }

    private void BuildPage1_CoreLogic(Transform parent, Font font)
    {
        page1Container = new GameObject("Page1_CoreLogic", typeof(RectTransform));
        page1Container.transform.SetParent(parent, false);
        RectTransform rect = page1Container.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0f, 300f);

        // Banner mô tả logic
        GameObject descCard = new GameObject("Banner Mô Tả", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        descCard.transform.SetParent(page1Container.transform, false);
        RectTransform descCardRect = descCard.GetComponent<RectTransform>();
        descCardRect.anchorMin = new Vector2(0.5f, 1f);
        descCardRect.anchorMax = new Vector2(0.5f, 1f);
        descCardRect.pivot = new Vector2(0.5f, 1f);
        descCardRect.anchoredPosition = new Vector2(0f, -2f);
        descCardRect.sizeDelta = new Vector2(610f, 54f);

        Image descBg = descCard.GetComponent<Image>();
        descBg.sprite = GetRoundedUiSprite();
        descBg.type = Image.Type.Sliced;
        descBg.color = new Color(0.88f, 0.70f, 0.40f, 0.96f);

        GameObject descObj = new GameObject("Mô Tả Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        descObj.transform.SetParent(descCard.transform, false);
        RectTransform descRect = descObj.GetComponent<RectTransform>();
        descRect.anchorMin = Vector2.zero;
        descRect.anchorMax = Vector2.one;
        descRect.offsetMin = new Vector2(28f, 7f);
        descRect.offsetMax = new Vector2(-28f, -7f);

        Text descText = descObj.GetComponent<Text>();
        descText.font = font;
        descText.fontSize = 14;
        descText.resizeTextForBestFit = true;
        descText.resizeTextMinSize = 12;
        descText.resizeTextMaxSize = 14;
        descText.alignment = TextAnchor.MiddleCenter;
        descText.color = new Color(0.24f, 0.08f, 0.02f, 1f);
        descText.lineSpacing = 1.15f;
        descText.horizontalOverflow = HorizontalWrapMode.Wrap;
        descText.verticalOverflow = VerticalWrapMode.Truncate;
        descText.text = "TRỒNG CÂY NỀN  →  CHỌN CÂY GHÉP  →  NHẤN CÂY NỀN  →  TẠO CÂY ĐỘT BIẾN";

        // Khung chứa 3 thẻ bước thao tác
        GameObject cardsHolder = new GameObject("Cards_Holder", typeof(RectTransform));
        cardsHolder.transform.SetParent(page1Container.transform, false);
        RectTransform chRect = cardsHolder.GetComponent<RectTransform>();
        chRect.anchorMin = new Vector2(0.5f, 1f);
        chRect.anchorMax = new Vector2(0.5f, 1f);
        chRect.pivot = new Vector2(0.5f, 1f);
        chRect.anchoredPosition = new Vector2(0f, -64f);
        chRect.sizeDelta = new Vector2(660f, 228f);

        CreateLogicStepCard(cardsHolder.transform, font, new Vector2(-220f, 0f),
            "BƯỚC 1: CÂY NỀN",
            "Sprites/Plants/PeaShooterSingle",
            "ĐẬU BẮN / HƯỚNG DƯƠNG",
            "Trồng một cây nền\nlên ô đất trống.");

        CreateLogicStepCard(cardsHolder.transform, font, new Vector2(0f, 0f),
            "BƯỚC 2: CÂY GHÉP",
            "Sprites/Plants/Torchwood",
            "ĐUỐC / ÓC CHÓ / NẤM",
            "Chọn cây tương thích,\nnhấn lên cây nền.");

        CreateLogicStepCard(cardsHolder.transform, font, new Vector2(220f, 0f),
            "BƯỚC 3: ĐỘT BIẾN",
            "Sprites/Plants/Hybrids/PeaTorch/Preview",
            "PEATORCH BẮN ĐẠN LỬA",
            "Hai cây hợp nhất thành\ncây đột biến mạnh hơn.");
    }

    private void BuildPage2_FormulasAndEvolution(Transform parent, Font font)
    {
        page2Container = new GameObject("Page2_FormulasAndEvolution", typeof(RectTransform));
        page2Container.transform.SetParent(parent, false);
        RectTransform rect = page2Container.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0f, 300f);

        // Banner mô tả công thức
        GameObject descCard = new GameObject("Banner Mô Tả", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        descCard.transform.SetParent(page2Container.transform, false);
        RectTransform descCardRect = descCard.GetComponent<RectTransform>();
        descCardRect.anchorMin = new Vector2(0.5f, 1f);
        descCardRect.anchorMax = new Vector2(0.5f, 1f);
        descCardRect.pivot = new Vector2(0.5f, 1f);
        descCardRect.anchoredPosition = new Vector2(0f, -2f);
        descCardRect.sizeDelta = new Vector2(610f, 54f);

        Image descBg = descCard.GetComponent<Image>();
        descBg.sprite = GetRoundedUiSprite();
        descBg.type = Image.Type.Sliced;
        descBg.color = new Color(0.88f, 0.70f, 0.40f, 0.96f);

        GameObject descObj = new GameObject("Mô Tả Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        descObj.transform.SetParent(descCard.transform, false);
        RectTransform descRect = descObj.GetComponent<RectTransform>();
        descRect.anchorMin = Vector2.zero;
        descRect.anchorMax = Vector2.one;
        descRect.offsetMin = new Vector2(28f, 7f);
        descRect.offsetMax = new Vector2(-28f, -7f);

        Text descText = descObj.GetComponent<Text>();
        descText.font = font;
        descText.fontSize = 14;
        descText.resizeTextForBestFit = true;
        descText.resizeTextMinSize = 12;
        descText.resizeTextMaxSize = 14;
        descText.alignment = TextAnchor.MiddleCenter;
        descText.color = new Color(0.24f, 0.08f, 0.02f, 1f);
        descText.lineSpacing = 1.15f;
        descText.horizontalOverflow = HorizontalWrapMode.Wrap;
        descText.verticalOverflow = VerticalWrapMode.Truncate;
        descText.text = "GHÉP ĐÚNG CÔNG THỨC  →  MỞ KHÓA TIẾN HÓA  →  XÂY ĐỘI HÌNH MẠNH HƠN";

        // Khung chứa 3 thẻ công thức
        GameObject cardsHolder = new GameObject("Cards_Holder", typeof(RectTransform));
        cardsHolder.transform.SetParent(page2Container.transform, false);
        RectTransform chRect = cardsHolder.GetComponent<RectTransform>();
        chRect.anchorMin = new Vector2(0.5f, 1f);
        chRect.anchorMax = new Vector2(0.5f, 1f);
        chRect.pivot = new Vector2(0.5f, 1f);
        chRect.anchoredPosition = new Vector2(0f, -64f);
        chRect.sizeDelta = new Vector2(660f, 228f);

        CreateFormulaCard(cardsHolder.transform, font, new Vector2(-220f, 0f),
            "SUNPEA",
            "Sprites/Plants/Hybrids/SunPea/Preview",
            "ĐẬU BẮN + HƯỚNG DƯƠNG",
            "Vừa bắn đậu, vừa tạo nắng\nđể mở rộng đội hình.");

        CreateFormulaCard(cardsHolder.transform, font, new Vector2(0f, 0f),
            "FIRE WALL-NUT",
            "Sprites/Plants/FireWallNut/FireWallNutV2",
            "ÓC CHÓ + CÂY ĐUỐC",
            "Chặn đường và gây bỏng\ncho zombie tấn công.");

        CreateFormulaCard(cardsHolder.transform, font, new Vector2(220f, 0f),
            "NỮ HOÀNG HOA NẮNG",
            "Sprites/Plants/Hybrids/SunflowerQueen/Preview",
            "TIẾN HÓA CẤP 3",
            "Phóng cầu lửa mạnh\ntrên toàn chiến tuyến.");
    }

    private void CreateLogicStepCard(Transform parent, Font font, Vector2 position, string stepTitle, string iconPath, string subTitle, string detail)
    {
        GameObject card = new GameObject("Card_" + stepTitle, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        card.transform.SetParent(parent, false);
        RectTransform r = card.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0.5f, 1f);
        r.anchorMax = new Vector2(0.5f, 1f);
        r.pivot = new Vector2(0.5f, 1f);
        r.anchoredPosition = position;
        r.sizeDelta = new Vector2(206f, 220f);

        Image bg = card.GetComponent<Image>();
        bg.sprite = tutorialDialogChild;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(1f, 0.96f, 0.82f, 1f);
        Shadow cardShadow = card.AddComponent<Shadow>();
        cardShadow.effectColor = new Color(0.18f, 0.07f, 0.02f, 0.38f);
        cardShadow.effectDistance = new Vector2(4f, -5f);

        // Tiêu đề bước
        GameObject stepObj = new GameObject("Bước", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        stepObj.transform.SetParent(card.transform, false);
        RectTransform stepRect = stepObj.GetComponent<RectTransform>();
        stepRect.anchorMin = new Vector2(0.5f, 1f);
        stepRect.anchorMax = new Vector2(0.5f, 1f);
        stepRect.pivot = new Vector2(0.5f, 1f);
        stepRect.anchoredPosition = new Vector2(0f, -20f);
        stepRect.sizeDelta = new Vector2(138f, 22f);
        Text stepText = stepObj.GetComponent<Text>();
        stepText.font = font;
        stepText.fontSize = 11;
        stepText.resizeTextForBestFit = true;
        stepText.resizeTextMinSize = 9;
        stepText.resizeTextMaxSize = 11;
        stepText.fontStyle = FontStyle.Bold;
        stepText.alignment = TextAnchor.MiddleCenter;
        stepText.color = new Color(0.24f, 0.09f, 0.02f, 1f);
        stepText.horizontalOverflow = HorizontalWrapMode.Wrap;
        int separatorIndex = stepTitle.IndexOf(':');
        stepText.text = separatorIndex >= 0 ? stepTitle.Substring(0, separatorIndex) : stepTitle;

        // Icon
        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObj.transform.SetParent(card.transform, false);
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 1f);
        iconRect.anchorMax = new Vector2(0.5f, 1f);
        iconRect.pivot = new Vector2(0.5f, 1f);
        iconRect.anchoredPosition = new Vector2(0f, -50f);
        iconRect.sizeDelta = new Vector2(62f, 62f);

        Image iconImg = iconObj.GetComponent<Image>();
        Sprite sp = Resources.Load<Sprite>(iconPath);
        if (sp == null) sp = Resources.Load<Sprite>("Sprites/Plants/PeaShooterSingle");
        iconImg.sprite = sp;
        iconImg.preserveAspect = true;
        Shadow iconShadow = iconObj.AddComponent<Shadow>();
        iconShadow.effectColor = new Color(0f, 0f, 0f, 0.32f);
        iconShadow.effectDistance = new Vector2(2f, -3f);

        // Tên phụ
        GameObject subObj = new GameObject("Tên Phụ", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        subObj.transform.SetParent(card.transform, false);
        RectTransform subRect = subObj.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0.5f, 1f);
        subRect.anchorMax = new Vector2(0.5f, 1f);
        subRect.pivot = new Vector2(0.5f, 1f);
        subRect.anchoredPosition = new Vector2(0f, -114f);
        subRect.sizeDelta = new Vector2(142f, 25f);
        Text subText = subObj.GetComponent<Text>();
        subText.font = font;
        subText.fontSize = 11;
        subText.resizeTextForBestFit = true;
        subText.resizeTextMinSize = 9;
        subText.resizeTextMaxSize = 11;
        subText.fontStyle = FontStyle.Bold;
        subText.alignment = TextAnchor.MiddleCenter;
        subText.color = new Color(0.85f, 0.35f, 0.05f, 1f);
        subText.horizontalOverflow = HorizontalWrapMode.Wrap;
        subText.text = subTitle;

        // Nội dung đặt trực tiếp trên thẻ; không phủ thêm lớp bán trong suốt
        // khiến phần thông tin cây bị bạc/mờ.
        GameObject textBgObj = new GameObject("TextArea", typeof(RectTransform));
        textBgObj.transform.SetParent(card.transform, false);
        RectTransform textBgRect = textBgObj.GetComponent<RectTransform>();
        textBgRect.anchorMin = new Vector2(0.16f, 0.16f);
        textBgRect.anchorMax = new Vector2(0.84f, 0.36f);
        textBgRect.offsetMin = Vector2.zero;
        textBgRect.offsetMax = Vector2.zero;

        GameObject descObj = new GameObject("Nội Dung", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        descObj.transform.SetParent(textBgObj.transform, false);
        RectTransform descRect = descObj.GetComponent<RectTransform>();
        descRect.anchorMin = Vector2.zero;
        descRect.anchorMax = Vector2.one;
        descRect.offsetMin = new Vector2(4f, 1f);
        descRect.offsetMax = new Vector2(-4f, -1f);

        Text dText = descObj.GetComponent<Text>();
        dText.font = font;
        dText.fontSize = 10;
        dText.resizeTextForBestFit = true;
        dText.resizeTextMinSize = 8;
        dText.resizeTextMaxSize = 10;
        dText.alignment = TextAnchor.MiddleCenter;
        dText.color = new Color(0.24f, 0.09f, 0.03f, 1f);
        dText.lineSpacing = 1.15f;
        dText.horizontalOverflow = HorizontalWrapMode.Wrap;
        dText.verticalOverflow = VerticalWrapMode.Truncate;
        dText.text = detail;
    }

    private void CreateFormulaCard(Transform parent, Font font, Vector2 position, string title, string iconPath, string recipe, string detail)
    {
        GameObject card = new GameObject("Card_" + title, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        card.transform.SetParent(parent, false);
        RectTransform r = card.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0.5f, 1f);
        r.anchorMax = new Vector2(0.5f, 1f);
        r.pivot = new Vector2(0.5f, 1f);
        r.anchoredPosition = position;
        r.sizeDelta = new Vector2(206f, 220f);

        Image bg = card.GetComponent<Image>();
        bg.sprite = tutorialDialogChild;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(1f, 0.96f, 0.82f, 1f);
        Shadow cardShadow = card.AddComponent<Shadow>();
        cardShadow.effectColor = new Color(0.18f, 0.07f, 0.02f, 0.38f);
        cardShadow.effectDistance = new Vector2(4f, -5f);

        // Tên cây
        GameObject nameObj = new GameObject("Tên", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        nameObj.transform.SetParent(card.transform, false);
        RectTransform nameRect = nameObj.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.5f, 1f);
        nameRect.anchorMax = new Vector2(0.5f, 1f);
        nameRect.pivot = new Vector2(0.5f, 1f);
        nameRect.anchoredPosition = new Vector2(0f, -20f);
        nameRect.sizeDelta = new Vector2(138f, 22f);
        Text nameText = nameObj.GetComponent<Text>();
        nameText.font = font;
        nameText.fontSize = 11;
        nameText.resizeTextForBestFit = true;
        nameText.resizeTextMinSize = 9;
        nameText.resizeTextMaxSize = 11;
        nameText.fontStyle = FontStyle.Bold;
        nameText.alignment = TextAnchor.MiddleCenter;
        nameText.color = new Color(0.24f, 0.09f, 0.02f, 1f);
        nameText.horizontalOverflow = HorizontalWrapMode.Wrap;
        nameText.text = title;

        // Icon
        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObj.transform.SetParent(card.transform, false);
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 1f);
        iconRect.anchorMax = new Vector2(0.5f, 1f);
        iconRect.pivot = new Vector2(0.5f, 1f);
        iconRect.anchoredPosition = new Vector2(0f, -50f);
        iconRect.sizeDelta = new Vector2(62f, 62f);

        Image iconImg = iconObj.GetComponent<Image>();
        Sprite sp = Resources.Load<Sprite>(iconPath);
        if (sp == null) sp = Resources.Load<Sprite>("Sprites/Plants/SunFlower");
        iconImg.sprite = sp;
        iconImg.preserveAspect = true;
        Shadow iconShadow = iconObj.AddComponent<Shadow>();
        iconShadow.effectColor = new Color(0f, 0f, 0f, 0.32f);
        iconShadow.effectDistance = new Vector2(2f, -3f);

        // Công thức ghép
        GameObject recipeObj = new GameObject("Công Thức", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        recipeObj.transform.SetParent(card.transform, false);
        RectTransform recipeRect = recipeObj.GetComponent<RectTransform>();
        recipeRect.anchorMin = new Vector2(0.5f, 1f);
        recipeRect.anchorMax = new Vector2(0.5f, 1f);
        recipeRect.pivot = new Vector2(0.5f, 1f);
        recipeRect.anchoredPosition = new Vector2(0f, -114f);
        recipeRect.sizeDelta = new Vector2(142f, 25f);
        Text recipeText = recipeObj.GetComponent<Text>();
        recipeText.font = font;
        recipeText.fontSize = 11;
        recipeText.resizeTextForBestFit = true;
        recipeText.resizeTextMinSize = 9;
        recipeText.resizeTextMaxSize = 11;
        recipeText.fontStyle = FontStyle.Bold;
        recipeText.alignment = TextAnchor.MiddleCenter;
        recipeText.color = new Color(0.85f, 0.35f, 0.05f, 1f);
        recipeText.horizontalOverflow = HorizontalWrapMode.Wrap;
        recipeText.text = recipe;

        // Nội dung đặt trực tiếp trên thẻ, không có lớp nền mờ riêng.
        GameObject textBgObj = new GameObject("TextArea", typeof(RectTransform));
        textBgObj.transform.SetParent(card.transform, false);
        RectTransform textBgRect = textBgObj.GetComponent<RectTransform>();
        textBgRect.anchorMin = new Vector2(0.16f, 0.16f);
        textBgRect.anchorMax = new Vector2(0.84f, 0.36f);
        textBgRect.offsetMin = Vector2.zero;
        textBgRect.offsetMax = Vector2.zero;

        GameObject descObj = new GameObject("Nội Dung", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        descObj.transform.SetParent(textBgObj.transform, false);
        RectTransform descRect = descObj.GetComponent<RectTransform>();
        descRect.anchorMin = Vector2.zero;
        descRect.anchorMax = Vector2.one;
        descRect.offsetMin = new Vector2(4f, 1f);
        descRect.offsetMax = new Vector2(-4f, -1f);

        Text dText = descObj.GetComponent<Text>();
        dText.font = font;
        dText.fontSize = 10;
        dText.resizeTextForBestFit = true;
        dText.resizeTextMinSize = 8;
        dText.resizeTextMaxSize = 10;
        dText.alignment = TextAnchor.MiddleCenter;
        dText.color = new Color(0.24f, 0.09f, 0.03f, 1f);
        dText.lineSpacing = 1.15f;
        dText.horizontalOverflow = HorizontalWrapMode.Wrap;
        dText.verticalOverflow = VerticalWrapMode.Truncate;
        dText.text = detail;
    }

    private GameObject CreateActionButton(string name, Transform parent, Vector2 anchoredPos, Vector2 size,
        string label, Font font, int fontSize, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform r = btnObj.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0.5f, 0f);
        r.anchorMax = new Vector2(0.5f, 0f);
        r.pivot = new Vector2(0.5f, 0f);
        r.anchoredPosition = anchoredPos;
        r.sizeDelta = size;

        Image img = btnObj.GetComponent<Image>();
        img.sprite = tutorialButtonNormal;
        img.type = Image.Type.Sliced;

        Button btn = btnObj.GetComponent<Button>();
        btn.transition = Selectable.Transition.SpriteSwap;
        Sprite highlighted = tutorialButtonHighlighted;
        SpriteState state = btn.spriteState;
        state.highlightedSprite = highlighted;
        state.pressedSprite = highlighted;
        state.selectedSprite = highlighted;
        btn.spriteState = state;
        btn.onClick.AddListener(onClick);

        GameObject textObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text t = textObj.GetComponent<Text>();
        t.font = font;
        t.fontSize = fontSize;
        t.resizeTextForBestFit = true;
        t.resizeTextMinSize = 14;
        t.resizeTextMaxSize = fontSize;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = new Color(1f, 0.95f, 0.74f, 1f);
        t.text = label;

        Outline outline = textObj.GetComponent<Outline>();
        outline.effectColor = new Color(0.12f, 0.04f, 0.01f, 0.9f);
        outline.effectDistance = new Vector2(1.4f, -1.4f);

        return btnObj;
    }

    private GameObject CreateSkipButton(string name, Transform parent, Vector2 anchoredPos, Vector2 size,
        string label, Font font, int fontSize, UnityEngine.Events.UnityAction onClick)
    {
        // Nút Skip phong cách riêng biệt để dễ nhận biết, viền sắc nét
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform r = btnObj.GetComponent<RectTransform>();
        r.anchorMin = new Vector2(0.5f, 0f);
        r.anchorMax = new Vector2(0.5f, 0f);
        r.pivot = new Vector2(0.5f, 0f);
        r.anchoredPosition = anchoredPos;
        r.sizeDelta = size;

        Image img = btnObj.GetComponent<Image>();
        img.sprite = tutorialButtonNormal;
        img.color = new Color(1f, 0.88f, 0.88f, 1f); // Hơi ửng đỏ nhẹ để phân biệt nút Bỏ qua
        img.type = Image.Type.Sliced;

        Button btn = btnObj.GetComponent<Button>();
        btn.transition = Selectable.Transition.SpriteSwap;
        Sprite highlighted = tutorialButtonHighlighted;
        SpriteState state = btn.spriteState;
        state.highlightedSprite = highlighted;
        state.pressedSprite = highlighted;
        state.selectedSprite = highlighted;
        btn.spriteState = state;
        btn.onClick.AddListener(onClick);

        GameObject textObj = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text t = textObj.GetComponent<Text>();
        t.font = font;
        t.fontSize = fontSize;
        t.resizeTextForBestFit = true;
        t.resizeTextMinSize = 14;
        t.resizeTextMaxSize = fontSize;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = new Color(1f, 0.90f, 0.40f, 1f); // Vàng sáng
        t.text = label;

        Outline outline = textObj.GetComponent<Outline>();
        outline.effectColor = new Color(0.40f, 0.08f, 0.05f, 0.95f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        return btnObj;
    }

    private static Sprite CreateSlicedSprite(Sprite source, float horizontalBorderRatio, float verticalBorderRatio)
    {
        if (source == null) return null;

        Rect rect = source.rect;
        Vector2 normalizedPivot = new Vector2(source.pivot.x / rect.width, source.pivot.y / rect.height);
        Vector4 border = new Vector4(
            rect.width * horizontalBorderRatio,
            rect.height * verticalBorderRatio,
            rect.width * horizontalBorderRatio,
            rect.height * verticalBorderRatio);

        Sprite sliced = Sprite.Create(source.texture, rect, normalizedPivot,
            source.pixelsPerUnit, 0, SpriteMeshType.FullRect, border);
        sliced.name = source.name + "_TutorialSliced";
        return sliced;
    }

    private void OnNextPageClicked()
    {
        PlayButtonClickAudio();
        currentTutorialPage = (currentTutorialPage + 1) % TotalTutorialPages;
        UpdatePageVisibility();
    }

    private void UpdatePageVisibility()
    {
        if (page1Container != null) page1Container.SetActive(currentTutorialPage == 0);
        if (page2Container != null) page2Container.SetActive(currentTutorialPage == 1);

        Color activeDot = new Color(0.96f, 0.67f, 0.14f, 1f);
        Color inactiveDot = new Color(0.34f, 0.16f, 0.06f, 0.45f);
        if (pageDot1 != null) pageDot1.color = currentTutorialPage == 0 ? activeDot : inactiveDot;
        if (pageDot2 != null) pageDot2.color = currentTutorialPage == 1 ? activeDot : inactiveDot;
        if (nextPageButtonText != null)
            nextPageButtonText.text = currentTutorialPage == 0 ? "TRANG SAU" : "TRANG TRƯỚC";

        if (tutorialScrollRect != null)
        {
            tutorialScrollRect.verticalNormalizedPosition = 1f;
        }

        if (pageIndicatorText != null)
        {
            if (currentTutorialPage == 0)
                pageIndicatorText.text = "TRANG 1 / 2  •  CÁCH GHÉP";
            else
                pageIndicatorText.text = "TRANG 2 / 2  •  CÔNG THỨC MẪU";
        }
    }

    private void OnStartGameClicked()
    {
        PlayButtonClickAudio();
        PlayerPrefs.SetInt(FirstTimeTutorialKey, 1);
        PlayerPrefs.Save();
        FinishAndStartGame();
    }

    private void OnSkipClicked()
    {
        // Người dùng chọn Skip / Bỏ qua hướng dẫn
        PlayButtonClickAudio();
        PlayerPrefs.SetInt(FirstTimeTutorialKey, 1);
        PlayerPrefs.Save();
        FinishAndStartGame();
    }

    private void FinishAndStartGame()
    {
        if (fusionTutorialPanel != null) fusionTutorialPanel.SetActive(false);
        if (plantIntroduce != null) plantIntroduce.SetActive(false);
        if (zombieIntroduce != null) zombieIntroduce.SetActive(false);

        GameObject gameMgmt = GameObject.Find("Game Management");
        if (gameMgmt != null)
        {
            gameMgmt.GetComponent<GameManagement>()?.awakeAll();
        }

        gameObject.SetActive(false);
    }

    private void PlayButtonClickAudio()
    {
        AudioClip clip = Resources.Load<AudioClip>("Sounds/UI/buttonClick");
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, new Vector3(0, 0, -10));
        }
    }

    public void clickStartOfZombie()
    {
        OnStartGameClicked();
    }

    public void clickStartOfPlant()
    {
        OnStartGameClicked();
    }
}
