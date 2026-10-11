using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogLevel0 : MonoBehaviour
{
    public GameObject zombieIntroduce;   //Bảng giới thiệu zombie ban đầu
    public GameObject plantIntroduce;    //Bảng giới thiệu cây ban đầu

    private GameObject fusionTutorialPanel;
    private int currentTutorialPage = 0;
    private const int TotalTutorialPages = 2;

    private Text pageIndicatorText;
    private GameObject page1Container;
    private GameObject page2Container;
    private ScrollRect tutorialScrollRect;
    private static Sprite roundedUiSprite;

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
    }

    private void BuildFusionTutorialUI()
    {
        Font font = Resources.Load<Font>("Fonts/Baloo2");

        fusionTutorialPanel = new GameObject("Bảng Hướng Dẫn Fusion", typeof(RectTransform), typeof(CanvasRenderer));
        fusionTutorialPanel.transform.SetParent(transform, false);

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
        frameRect.anchorMin = new Vector2(0.5f, 0.5f);
        frameRect.anchorMax = new Vector2(0.5f, 0.5f);
        frameRect.pivot = new Vector2(0.5f, 0.5f);
        frameRect.sizeDelta = new Vector2(880f, 540f);

        Image frameImg = woodFrame.GetComponent<Image>();
        frameImg.sprite = Resources.Load<Sprite>("GameUI/dialog_main");
        frameImg.preserveAspect = true;

        // Tiêu đề bảng
        GameObject titleObj = new GameObject("Tiêu Đề", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
        titleObj.transform.SetParent(woodFrame.transform, false);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -28f);
        titleRect.sizeDelta = new Vector2(620f, 48f);

        Text titleText = titleObj.GetComponent<Text>();
        titleText.font = font;
        titleText.fontSize = 28;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(0.24f, 0.09f, 0.02f, 1f);
        titleText.text = "HƯỚNG DẪN LOGIC GHÉP CÂY (PLANT FUSION)";
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
        pageIndRect.sizeDelta = new Vector2(400f, 28f);

        pageIndicatorText = pageIndObj.GetComponent<Text>();
        pageIndicatorText.font = font;
        pageIndicatorText.fontSize = 17;
        pageIndicatorText.fontStyle = FontStyle.Bold;
        pageIndicatorText.alignment = TextAnchor.MiddleCenter;
        pageIndicatorText.color = new Color(0.38f, 0.18f, 0.05f, 1f);
        pageIndicatorText.text = "Trang 1 / 2: Cách thức Thao Tác & Quy Tắc Ghép";

        // Vùng cuộn nội dung (Scroll View)
        GameObject scrollObj = new GameObject("Scroll View", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
        scrollObj.transform.SetParent(woodFrame.transform, false);
        RectTransform scrollRectTransform = scrollObj.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0.06f, 0.19f);
        scrollRectTransform.anchorMax = new Vector2(0.94f, 0.84f);
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
        contentRect.sizeDelta = new Vector2(0f, 390f);

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

        // Nút CHUYỂN TRANG / TIẾP THEO
        CreateActionButton("Nút Chuyển Trang", woodFrame.transform, new Vector2(-155f, 48f), new Vector2(170f, 52f),
            "TIẾP THEO", font, 20, OnNextPageClicked);

        // Nút VÀO TRẬN / BẮT ĐẦU CHƠI
        CreateActionButton("Nút Bắt Đầu", woodFrame.transform, new Vector2(40f, 48f), new Vector2(170f, 52f),
            "VÀO TRẬN", font, 20, OnStartGameClicked);

        // NÚT BỎ QUA / SKIP (To rõ, nổi bật để người dùng có thể skip bất cứ lúc nào)
        CreateSkipButton("Nút Bỏ Qua", woodFrame.transform, new Vector2(235f, 48f), new Vector2(170f, 52f),
            "BỎ QUA >>", font, 20, OnSkipClicked);

        UpdatePageVisibility();
    }

    private void BuildPage1_CoreLogic(Transform parent, Font font)
    {
        page1Container = new GameObject("Page1_CoreLogic", typeof(RectTransform), typeof(ContentSizeFitter));
        page1Container.transform.SetParent(parent, false);
        RectTransform rect = page1Container.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0f, 390f);

        ContentSizeFitter csf = page1Container.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Banner mô tả logic
        GameObject descCard = new GameObject("Banner Mô Tả", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        descCard.transform.SetParent(page1Container.transform, false);
        RectTransform descCardRect = descCard.GetComponent<RectTransform>();
        descCardRect.anchorMin = new Vector2(0.5f, 1f);
        descCardRect.anchorMax = new Vector2(0.5f, 1f);
        descCardRect.pivot = new Vector2(0.5f, 1f);
        descCardRect.anchoredPosition = new Vector2(0f, -6f);
        descCardRect.sizeDelta = new Vector2(705f, 64f);

        Image descBg = descCard.GetComponent<Image>();
        descBg.sprite = Resources.Load<Sprite>("GameUI/dialog_child");
        descBg.type = Image.Type.Sliced;
        descBg.color = new Color(0.96f, 0.94f, 0.88f, 0.92f);

        GameObject descObj = new GameObject("Mô Tả Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        descObj.transform.SetParent(descCard.transform, false);
        RectTransform descRect = descObj.GetComponent<RectTransform>();
        descRect.anchorMin = Vector2.zero;
        descRect.anchorMax = Vector2.one;
        descRect.offsetMin = new Vector2(14f, 4f);
        descRect.offsetMax = new Vector2(-14f, -4f);

        Text descText = descObj.GetComponent<Text>();
        descText.font = font;
        descText.fontSize = 15;
        descText.alignment = TextAnchor.MiddleCenter;
        descText.color = new Color(0.24f, 0.08f, 0.02f, 1f);
        descText.lineSpacing = 1.15f;
        descText.horizontalOverflow = HorizontalWrapMode.Wrap;
        descText.verticalOverflow = VerticalWrapMode.Truncate;
        descText.text = "★ QUY TẮC CƠ BẢN: Đặt trước 1 cây nền lên ô đất → Chọn thẻ cây thứ 2 → Nhấp đè trực tiếp lên cây vừa trồng để kích hoạt hợp thể đột biến!";

        // Khung chứa 3 thẻ bước thao tác
        GameObject cardsHolder = new GameObject("Cards_Holder", typeof(RectTransform));
        cardsHolder.transform.SetParent(page1Container.transform, false);
        RectTransform chRect = cardsHolder.GetComponent<RectTransform>();
        chRect.anchorMin = new Vector2(0.5f, 1f);
        chRect.anchorMax = new Vector2(0.5f, 1f);
        chRect.pivot = new Vector2(0.5f, 1f);
        chRect.anchoredPosition = new Vector2(0f, -80f);
        chRect.sizeDelta = new Vector2(710f, 290f);

        CreateLogicStepCard(cardsHolder.transform, font, new Vector2(-236f, 0f),
            "Bước 1: Trồng Cây Nền",
            "Sprites/Plants/PeaShooterSingle",
            "ĐẬU BẮN / HƯỚNG DƯƠNG",
            "Trồng 1 cây xuống bãi cỏ như bình thường để làm gốc cây nền.");

        CreateLogicStepCard(cardsHolder.transform, font, new Vector2(0f, 0f),
            "Bước 2: Chọn Cây Ghép",
            "Sprites/Plants/Torchwood",
            "ĐUỐC / ÓC CHÓ / NẤM",
            "Nhấp chọn thẻ cây tương thích và nhấn đè vào đúng ô cây nền.");

        CreateLogicStepCard(cardsHolder.transform, font, new Vector2(236f, 0f),
            "Bước 3: Đột Biến Siêu Cấp",
            "Sprites/Plants/Hybrids/PeaTorch/Preview",
            "PEATORCH BẮN ĐẠN LỬA!",
            "Hai cây hợp nhất thành loài mới với hỏa lực và năng lực vượt trội!");
    }

    private void BuildPage2_FormulasAndEvolution(Transform parent, Font font)
    {
        page2Container = new GameObject("Page2_FormulasAndEvolution", typeof(RectTransform), typeof(ContentSizeFitter));
        page2Container.transform.SetParent(parent, false);
        RectTransform rect = page2Container.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0f, 390f);

        ContentSizeFitter csf = page2Container.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Banner mô tả công thức
        GameObject descCard = new GameObject("Banner Mô Tả", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        descCard.transform.SetParent(page2Container.transform, false);
        RectTransform descCardRect = descCard.GetComponent<RectTransform>();
        descCardRect.anchorMin = new Vector2(0.5f, 1f);
        descCardRect.anchorMax = new Vector2(0.5f, 1f);
        descCardRect.pivot = new Vector2(0.5f, 1f);
        descCardRect.anchoredPosition = new Vector2(0f, -6f);
        descCardRect.sizeDelta = new Vector2(705f, 64f);

        Image descBg = descCard.GetComponent<Image>();
        descBg.sprite = Resources.Load<Sprite>("GameUI/dialog_child");
        descBg.type = Image.Type.Sliced;
        descBg.color = new Color(0.96f, 0.94f, 0.88f, 0.92f);

        GameObject descObj = new GameObject("Mô Tả Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        descObj.transform.SetParent(descCard.transform, false);
        RectTransform descRect = descObj.GetComponent<RectTransform>();
        descRect.anchorMin = Vector2.zero;
        descRect.anchorMax = Vector2.one;
        descRect.offsetMin = new Vector2(14f, 4f);
        descRect.offsetMax = new Vector2(-14f, -4f);

        Text descText = descObj.GetComponent<Text>();
        descText.font = font;
        descText.fontSize = 15;
        descText.alignment = TextAnchor.MiddleCenter;
        descText.color = new Color(0.24f, 0.08f, 0.02f, 1f);
        descText.lineSpacing = 1.15f;
        descText.horizontalOverflow = HorizontalWrapMode.Wrap;
        descText.verticalOverflow = VerticalWrapMode.Truncate;
        descText.text = "★ CÔNG THỨC MẪU NỔI BẬT: Khám phá hàng chục kiểu lai tạo độc đáo để phòng thủ trước những đợt tấn công nguy hiểm nhất!";

        // Khung chứa 3 thẻ công thức
        GameObject cardsHolder = new GameObject("Cards_Holder", typeof(RectTransform));
        cardsHolder.transform.SetParent(page2Container.transform, false);
        RectTransform chRect = cardsHolder.GetComponent<RectTransform>();
        chRect.anchorMin = new Vector2(0.5f, 1f);
        chRect.anchorMax = new Vector2(0.5f, 1f);
        chRect.pivot = new Vector2(0.5f, 1f);
        chRect.anchoredPosition = new Vector2(0f, -80f);
        chRect.sizeDelta = new Vector2(710f, 290f);

        CreateFormulaCard(cardsHolder.transform, font, new Vector2(-236f, 0f),
            "SunPea (Kinh Tế + Bắn)",
            "Sprites/Plants/Hybrids/SunPea/Preview",
            "Đậu Bắn + Hướng Dương",
            "Vừa bắn đậu phòng ngự, vừa liên tục tự sản sinh mặt trời giúp mở rộng đội hình thần tốc!");

        CreateFormulaCard(cardsHolder.transform, font, new Vector2(0f, 0f),
            "FireWallNut (Tường Lửa)",
            "Sprites/Plants/FireWallNut/FireWallNutV2",
            "Óc Chó + Cây Đuốc",
            "Tường thành phòng ngự bọc lửa kiên cố, vừa chặn đường vừa thiêu rụi zombie khi bị cắn!");

        CreateFormulaCard(cardsHolder.transform, font, new Vector2(236f, 0f),
            "Nữ Hoàng Hướng Dương",
            "Sprites/Plants/Hybrids/SunflowerQueen/Preview",
            "Tiến Hóa Cấp 3 Huyền Thoại",
            "Kết hợp cả Đậu + Đuốc + Hướng Dương! Phóng cầu lửa rực cháy hủy diệt toàn bộ chiến tuyến!");
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
        r.sizeDelta = new Vector2(226f, 275f);

        Image bg = card.GetComponent<Image>();
        bg.sprite = Resources.Load<Sprite>("GameUI/dialog_child");
        bg.type = Image.Type.Sliced;
        bg.color = new Color(1f, 1f, 1f, 0.98f);

        // Tiêu đề bước
        GameObject stepObj = new GameObject("Bước", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        stepObj.transform.SetParent(card.transform, false);
        RectTransform stepRect = stepObj.GetComponent<RectTransform>();
        stepRect.anchorMin = new Vector2(0.5f, 1f);
        stepRect.anchorMax = new Vector2(0.5f, 1f);
        stepRect.pivot = new Vector2(0.5f, 1f);
        stepRect.anchoredPosition = new Vector2(0f, -8f);
        stepRect.sizeDelta = new Vector2(212f, 24f);
        Text stepText = stepObj.GetComponent<Text>();
        stepText.font = font;
        stepText.fontSize = 14;
        stepText.fontStyle = FontStyle.Bold;
        stepText.alignment = TextAnchor.MiddleCenter;
        stepText.color = new Color(0.12f, 0.50f, 0.08f, 1f);
        stepText.horizontalOverflow = HorizontalWrapMode.Wrap;
        stepText.text = stepTitle;

        // Icon
        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObj.transform.SetParent(card.transform, false);
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 1f);
        iconRect.anchorMax = new Vector2(0.5f, 1f);
        iconRect.pivot = new Vector2(0.5f, 1f);
        iconRect.anchoredPosition = new Vector2(0f, -36f);
        iconRect.sizeDelta = new Vector2(74f, 74f);

        Image iconImg = iconObj.GetComponent<Image>();
        Sprite sp = Resources.Load<Sprite>(iconPath);
        if (sp == null) sp = Resources.Load<Sprite>("Sprites/Plants/PeaShooterSingle");
        iconImg.sprite = sp;
        iconImg.preserveAspect = true;

        // Tên phụ
        GameObject subObj = new GameObject("Tên Phụ", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        subObj.transform.SetParent(card.transform, false);
        RectTransform subRect = subObj.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0.5f, 1f);
        subRect.anchorMax = new Vector2(0.5f, 1f);
        subRect.pivot = new Vector2(0.5f, 1f);
        subRect.anchoredPosition = new Vector2(0f, -114f);
        subRect.sizeDelta = new Vector2(212f, 26f);
        Text subText = subObj.GetComponent<Text>();
        subText.font = font;
        subText.fontSize = 13;
        subText.fontStyle = FontStyle.Bold;
        subText.alignment = TextAnchor.MiddleCenter;
        subText.color = new Color(0.85f, 0.35f, 0.05f, 1f);
        subText.horizontalOverflow = HorizontalWrapMode.Wrap;
        subText.text = subTitle;

        // Vùng chứa nội dung chi tiết dạng thẻ giấy
        GameObject textBgObj = new GameObject("TextBg", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        textBgObj.transform.SetParent(card.transform, false);
        RectTransform textBgRect = textBgObj.GetComponent<RectTransform>();
        textBgRect.anchorMin = new Vector2(0.05f, 0.04f);
        textBgRect.anchorMax = new Vector2(0.95f, 0.55f);
        textBgRect.offsetMin = Vector2.zero;
        textBgRect.offsetMax = Vector2.zero;

        Image textBgImg = textBgObj.GetComponent<Image>();
        textBgImg.color = new Color(0.92f, 0.88f, 0.78f, 0.65f);

        GameObject descObj = new GameObject("Nội Dung", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        descObj.transform.SetParent(textBgObj.transform, false);
        RectTransform descRect = descObj.GetComponent<RectTransform>();
        descRect.anchorMin = Vector2.zero;
        descRect.anchorMax = Vector2.one;
        descRect.offsetMin = new Vector2(8f, 6f);
        descRect.offsetMax = new Vector2(-8f, -6f);

        Text dText = descObj.GetComponent<Text>();
        dText.font = font;
        dText.fontSize = 12;
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
        r.sizeDelta = new Vector2(226f, 275f);

        Image bg = card.GetComponent<Image>();
        bg.sprite = Resources.Load<Sprite>("GameUI/dialog_child");
        bg.type = Image.Type.Sliced;
        bg.color = new Color(1f, 1f, 1f, 0.98f);

        // Tên cây
        GameObject nameObj = new GameObject("Tên", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        nameObj.transform.SetParent(card.transform, false);
        RectTransform nameRect = nameObj.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0.5f, 1f);
        nameRect.anchorMax = new Vector2(0.5f, 1f);
        nameRect.pivot = new Vector2(0.5f, 1f);
        nameRect.anchoredPosition = new Vector2(0f, -8f);
        nameRect.sizeDelta = new Vector2(212f, 24f);
        Text nameText = nameObj.GetComponent<Text>();
        nameText.font = font;
        nameText.fontSize = 14;
        nameText.fontStyle = FontStyle.Bold;
        nameText.alignment = TextAnchor.MiddleCenter;
        nameText.color = new Color(0.12f, 0.52f, 0.08f, 1f);
        nameText.horizontalOverflow = HorizontalWrapMode.Wrap;
        nameText.text = title;

        // Icon
        GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObj.transform.SetParent(card.transform, false);
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 1f);
        iconRect.anchorMax = new Vector2(0.5f, 1f);
        iconRect.pivot = new Vector2(0.5f, 1f);
        iconRect.anchoredPosition = new Vector2(0f, -36f);
        iconRect.sizeDelta = new Vector2(74f, 74f);

        Image iconImg = iconObj.GetComponent<Image>();
        Sprite sp = Resources.Load<Sprite>(iconPath);
        if (sp == null) sp = Resources.Load<Sprite>("Sprites/Plants/SunFlower");
        iconImg.sprite = sp;
        iconImg.preserveAspect = true;

        // Công thức ghép
        GameObject recipeObj = new GameObject("Công Thức", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        recipeObj.transform.SetParent(card.transform, false);
        RectTransform recipeRect = recipeObj.GetComponent<RectTransform>();
        recipeRect.anchorMin = new Vector2(0.5f, 1f);
        recipeRect.anchorMax = new Vector2(0.5f, 1f);
        recipeRect.pivot = new Vector2(0.5f, 1f);
        recipeRect.anchoredPosition = new Vector2(0f, -114f);
        recipeRect.sizeDelta = new Vector2(212f, 26f);
        Text recipeText = recipeObj.GetComponent<Text>();
        recipeText.font = font;
        recipeText.fontSize = 13;
        recipeText.fontStyle = FontStyle.Bold;
        recipeText.alignment = TextAnchor.MiddleCenter;
        recipeText.color = new Color(0.85f, 0.35f, 0.05f, 1f);
        recipeText.horizontalOverflow = HorizontalWrapMode.Wrap;
        recipeText.text = recipe;

        // Vùng chứa nội dung chi tiết dạng thẻ giấy
        GameObject textBgObj = new GameObject("TextBg", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        textBgObj.transform.SetParent(card.transform, false);
        RectTransform textBgRect = textBgObj.GetComponent<RectTransform>();
        textBgRect.anchorMin = new Vector2(0.05f, 0.04f);
        textBgRect.anchorMax = new Vector2(0.95f, 0.55f);
        textBgRect.offsetMin = Vector2.zero;
        textBgRect.offsetMax = Vector2.zero;

        Image textBgImg = textBgObj.GetComponent<Image>();
        textBgImg.color = new Color(0.92f, 0.88f, 0.78f, 0.65f);

        GameObject descObj = new GameObject("Nội Dung", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        descObj.transform.SetParent(textBgObj.transform, false);
        RectTransform descRect = descObj.GetComponent<RectTransform>();
        descRect.anchorMin = Vector2.zero;
        descRect.anchorMax = Vector2.one;
        descRect.offsetMin = new Vector2(8f, 6f);
        descRect.offsetMax = new Vector2(-8f, -6f);

        Text dText = descObj.GetComponent<Text>();
        dText.font = font;
        dText.fontSize = 12;
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
        img.sprite = Resources.Load<Sprite>("GameUI/button1");
        img.preserveAspect = true;

        Button btn = btnObj.GetComponent<Button>();
        btn.transition = Selectable.Transition.SpriteSwap;
        Sprite highlighted = Resources.Load<Sprite>("GameUI/button2");
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
        img.sprite = Resources.Load<Sprite>("GameUI/button1");
        img.color = new Color(1f, 0.88f, 0.88f, 1f); // Hơi ửng đỏ nhẹ để phân biệt nút Bỏ qua
        img.preserveAspect = true;

        Button btn = btnObj.GetComponent<Button>();
        btn.transition = Selectable.Transition.SpriteSwap;
        Sprite highlighted = Resources.Load<Sprite>("GameUI/button2");
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
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = new Color(1f, 0.90f, 0.40f, 1f); // Vàng sáng
        t.text = label;

        Outline outline = textObj.GetComponent<Outline>();
        outline.effectColor = new Color(0.40f, 0.08f, 0.05f, 0.95f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        return btnObj;
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

        if (tutorialScrollRect != null)
        {
            tutorialScrollRect.verticalNormalizedPosition = 1f;
        }

        if (pageIndicatorText != null)
        {
            if (currentTutorialPage == 0)
                pageIndicatorText.text = "Trang 1 / 2: Cách thức Thao Tác & Quy Tắc Ghép";
            else
                pageIndicatorText.text = "Trang 2 / 2: Công Thức Ghép Mẫu & Tiến Hóa Cấp Cao";
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
