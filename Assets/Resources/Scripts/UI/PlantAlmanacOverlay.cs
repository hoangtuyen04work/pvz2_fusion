using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Cẩm nang Cây trồng (Plant Almanac / Plant Codex).
/// Trình bày theo phong cách nghệ thuật cổ điển của Plants vs. Zombies:
/// Khung gỗ đá ấm áp, phân tách 2 cột (danh mục thẻ bên trái, bục giới thiệu chi tiết bên phải).
/// </summary>
public sealed class PlantAlmanacOverlay : MonoBehaviour
{
    private static Font cachedFont;
    private static Sprite cachedDialogMain;
    private static Sprite cachedDialogChild;
    private static Sprite cachedButton1;
    private static Sprite cachedButton2;
    private static Sprite cachedCancel;
    private static Sprite cachedCardFrame;
    private static Sprite cachedSunIcon;

    private readonly List<PlantCardView> cardViews = new List<PlantCardView>();
    private PlantLoadoutEntry selectedEntry;

    // Các thành phần thuộc khung chi tiết bên phải
    private Image showcasePortrait;
    private Text showcaseName;
    private Text showcaseSubtitle;
    private Text statFullNameText;
    private Text statCostText;
    private Text statCooldownText;
    private Text statToughnessText;
    private Text statDamageText;
    private Text showcaseDescription;
    private Text showcaseTip;

    private sealed class PlantCardView
    {
        public PlantLoadoutEntry Entry;
        public GameObject Root;
        public Image SelectionGlow;
        public RectTransform Rect;
    }

    public static void Show()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PlantAlmanacOverlay>() != null) return;

        // Nếu đang mở cẩm nang zombie thì đóng lại để chuyển trang
        var existingZombie = UnityEngine.Object.FindAnyObjectByType<ZombieListOverlay>();
        if (existingZombie != null) UnityEngine.Object.Destroy(existingZombie.gameObject);

        var root = new GameObject("Plant Almanac", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(PlantAlmanacOverlay));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1000f, 750f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        root.GetComponent<PlantAlmanacOverlay>().Build();
    }

    private void Awake()
    {
        LoadSharedAssets();
    }

    private static void LoadSharedAssets()
    {
        if (cachedFont == null) cachedFont = Resources.Load<Font>("Fonts/Baloo2") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (cachedDialogMain == null) cachedDialogMain = Resources.Load<Sprite>("GameUI/dialog_main");
        if (cachedDialogChild == null) cachedDialogChild = Resources.Load<Sprite>("GameUI/dialog_child");
        if (cachedButton1 == null) cachedButton1 = Resources.Load<Sprite>("GameUI/button1");
        if (cachedButton2 == null) cachedButton2 = Resources.Load<Sprite>("GameUI/button2");
        if (cachedCancel == null) cachedCancel = Resources.Load<Sprite>("GameUI/cancel");
        if (cachedCardFrame == null) cachedCardFrame = Resources.Load<Sprite>("Sprites/UI/Intro/khung") ?? Resources.Load<Sprite>("Sprites/UI/Menu/button_normal");
        if (cachedSunIcon == null) cachedSunIcon = Resources.Load<Sprite>("Sprites/Plants/SunFlower");
    }

    private void Build()
    {
        EnsureEventSystem();

        // 1. Lớp phủ đen mờ làm dịu nền menu chính
        var shade = CreateImage("Shade", transform, null, new Color(0f, 0f, 0f, 0.78f));
        Stretch(shade.rectTransform);
        var shadeBtn = shade.gameObject.AddComponent<Button>();
        shadeBtn.transition = Selectable.Transition.None;
        shadeBtn.onClick.AddListener(CloseAlmanac);

        // 2. Khung thoại chính bằng gỗ đá PvZ
        var dialogObject = new GameObject("Main Dialog Board", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dialogObject.transform.SetParent(transform, false);
        var dialogRect = dialogObject.GetComponent<RectTransform>();
        CenterRect(dialogRect, new Vector2(930f, 620f), Vector2.zero);

        var dialogImage = dialogObject.GetComponent<Image>();
        dialogImage.sprite = cachedDialogMain;
        dialogImage.type = Image.Type.Sliced;
        dialogImage.color = Color.white;
        dialogImage.raycastTarget = true;

        // 3. Tiêu đề Cẩm Nang Cây Trồng
        var titleText = CreateText("Tiêu đề", dialogObject.transform, "CẨM NANG CÂY TRỒNG", 32, TextAnchor.MiddleCenter, new Color(1f, 0.94f, 0.72f, 1f));
        CenterRect(titleText.rectTransform, new Vector2(400f, 48f), new Vector2(0f, 276f));
        titleText.fontStyle = FontStyle.Bold;
        AddTextShadow(titleText.gameObject, new Color(0.18f, 0.06f, 0.02f, 0.95f), new Vector2(1.8f, -1.8f));

        // 4. Nút chuyển đổi nhanh sang Cẩm nang Zombie (Góc trên trái)
        var tabZombie = CreateStyledButton("Tab Zombie", dialogObject.transform, "ZOMBIE ➔", 16, new Vector2(146f, 40f), new Vector2(-360f, 274f), SwitchToZombieAlmanac);
        // Nút Đóng (X) góc trên phải
        CreateCloseIcon("Nút Đóng X", dialogObject.transform, new Vector2(430f, 274f), CloseAlmanac);

        // 5. Cột trái: Danh sách thẻ cây trồng (Scroll View)
        BuildLeftCatalog(dialogObject.transform);

        // 6. Cột phải: Bục giới thiệu chi tiết (Showcase Podium & Codex)
        BuildRightShowcase(dialogObject.transform);

        // 7. Nút ĐÓNG to phía dưới cùng bên phải
        CreateStyledButton("Nút Đóng", dialogObject.transform, "ĐÓNG", 22, new Vector2(160f, 50f), new Vector2(350f, -270f), CloseAlmanac);

        // 8. Chọn mặc định cây đầu tiên
        if (PlantLoadoutCatalog.All != null && PlantLoadoutCatalog.All.Length > 0)
        {
            SelectPlant(PlantLoadoutCatalog.All[0]);
        }
    }

    private void BuildLeftCatalog(Transform parent)
    {
        // Khung nền chìm màu gỗ sẫm cho danh sách
        var catalogFrame = new GameObject("Catalog Box", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        catalogFrame.transform.SetParent(parent, false);
        var frameRect = catalogFrame.GetComponent<RectTransform>();
        CenterRect(frameRect, new Vector2(506f, 486f), new Vector2(-192f, -8f));

        var frameImg = catalogFrame.GetComponent<Image>();
        frameImg.sprite = cachedDialogChild;
        frameImg.type = Image.Type.Sliced;
        frameImg.color = new Color(0.12f, 0.08f, 0.06f, 0.92f);

        // Scroll View
        var scrollGO = new GameObject("Catalog Scroll", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
        scrollGO.transform.SetParent(catalogFrame.transform, false);
        var scrollRect = scrollGO.GetComponent<RectTransform>();
        SetAnchors(scrollRect, 0.02f, 0.02f, 0.98f, 0.98f);
        scrollGO.GetComponent<Image>().color = Color.clear;

        // Viewport
        var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
        viewportGO.transform.SetParent(scrollGO.transform, false);
        var viewport = viewportGO.GetComponent<RectTransform>();
        SetAnchors(viewport, 0f, 0f, 0.96f, 1f);
        viewportGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.001f);

        // Content Grid
        var contentGO = new GameObject("Grid Content", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        contentGO.transform.SetParent(viewportGO.transform, false);
        var content = contentGO.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;

        var grid = contentGO.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(88f, 114f);
        grid.spacing = new Vector2(8f, 10f);
        grid.padding = new RectOffset(8, 8, 10, 10);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 5;

        var fitter = contentGO.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Scrollbar
        var scrollbar = BuildStyledScrollbar(scrollGO.transform);

        var sr = scrollGO.GetComponent<ScrollRect>();
        sr.viewport = viewport;
        sr.content = content;
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.inertia = true;
        sr.decelerationRate = 0.12f;
        sr.scrollSensitivity = 38f;
        sr.verticalScrollbar = scrollbar;
        sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        sr.verticalNormalizedPosition = 1f;

        // Sinh thẻ cây trồng
        foreach (var entry in PlantLoadoutCatalog.All)
        {
            CreatePlantCard(entry, contentGO.transform);
        }
    }

    private void CreatePlantCard(PlantLoadoutEntry entry, Transform parent)
    {
        var cardGO = new GameObject("Card_" + entry.Key, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(MenuButtonMotion));
        cardGO.transform.SetParent(parent, false);
        var cardRect = cardGO.GetComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(88f, 114f);

        var cardImg = cardGO.GetComponent<Image>();
        cardImg.sprite = cachedCardFrame;
        cardImg.type = Image.Type.Sliced;
        cardImg.color = new Color(0.88f, 0.84f, 0.74f, 1f);

        var btn = cardGO.GetComponent<Button>();
        btn.transition = Selectable.Transition.None;
        var captured = entry;
        btn.onClick.AddListener(() => {
            PlayClickSound();
            SelectPlant(captured);
        });

        var motion = cardGO.GetComponent<MenuButtonMotion>();
        motion.targetGraphic = cardImg;

        // Viền sáng vàng khi được chọn
        var glowGO = new GameObject("Glow", typeof(RectTransform), typeof(Image), typeof(Outline));
        glowGO.transform.SetParent(cardGO.transform, false);
        Stretch(glowGO.GetComponent<RectTransform>());
        var glowImg = glowGO.GetComponent<Image>();
        glowImg.color = new Color(1f, 0.88f, 0.25f, 0.22f);
        glowImg.raycastTarget = false;
        var glowOutline = glowGO.GetComponent<Outline>();
        glowOutline.effectColor = new Color(1f, 0.92f, 0.25f, 0.95f);
        glowOutline.effectDistance = new Vector2(2.5f, -2.5f);
        glowGO.SetActive(false);

        // Nền icon cây trồng
        var iconBackGO = new GameObject("IconBack", typeof(RectTransform), typeof(Image));
        iconBackGO.transform.SetParent(cardGO.transform, false);
        SetAnchors(iconBackGO.GetComponent<RectTransform>(), 0.08f, 0.28f, 0.92f, 0.92f);
        var iconBackImg = iconBackGO.GetComponent<Image>();
        iconBackImg.color = new Color(0.42f, 0.51f, 0.24f, 0.45f);
        iconBackImg.raycastTarget = false;

        // Icon cây trồng
        var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(cardGO.transform, false);
        SetAnchors(iconGO.GetComponent<RectTransform>(), 0.08f, 0.28f, 0.92f, 0.92f);
        var iconImg = iconGO.GetComponent<Image>();
        iconImg.sprite = SeedPacketFactory.LoadIcon(entry);
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;

        // Tên cây rút gọn
        var nameText = CreateText("Name", cardGO.transform, GetPlantDisplayName(entry.Key), 12, TextAnchor.MiddleCenter, new Color(0.22f, 0.10f, 0.04f, 1f));
        nameText.fontStyle = FontStyle.Bold;
        SetAnchors(nameText.rectTransform, 0.04f, 0.18f, 0.96f, 0.32f);
        nameText.raycastTarget = false;

        // Tag giá Nắng ở đáy thẻ
        var costBack = CreateImage("CostTag", cardGO.transform, null, new Color(0.12f, 0.06f, 0.02f, 0.90f));
        SetAnchors(costBack.rectTransform, 0.04f, 0.03f, 0.96f, 0.19f);
        costBack.raycastTarget = false;

        var costText = CreateText("Cost", costBack.transform, entry.Cost + " ☀", 13, TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.22f, 1f));
        Stretch(costText.rectTransform);
        costText.fontStyle = FontStyle.Bold;
        costText.raycastTarget = false;

        var view = new PlantCardView
        {
            Entry = entry,
            Root = cardGO,
            SelectionGlow = glowImg,
            Rect = cardRect
        };
        cardViews.Add(view);
    }

    private void BuildRightShowcase(Transform parent)
    {
        // Khung nền bục trưng bày bên phải
        var showcaseBox = new GameObject("Showcase Box", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        showcaseBox.transform.SetParent(parent, false);
        var boxRect = showcaseBox.GetComponent<RectTransform>();
        CenterRect(boxRect, new Vector2(340f, 486f), new Vector2(262f, -8f));

        var boxImg = showcaseBox.GetComponent<Image>();
        boxImg.sprite = cachedDialogChild;
        boxImg.type = Image.Type.Sliced;
        boxImg.color = new Color(0.14f, 0.09f, 0.07f, 0.92f);

        // 1. Bục đặt chân dung cây (Parchment Pedestal)
        var pedestal = CreateImage("Pedestal", showcaseBox.transform, null, new Color(0.24f, 0.18f, 0.14f, 0.75f));
        CenterRect(pedestal.rectTransform, new Vector2(312f, 114f), new Vector2(0f, 176f));
        pedestal.gameObject.AddComponent<Outline>().effectColor = new Color(0.42f, 0.32f, 0.24f, 0.5f);

        // Chân dung lớn
        var portraitGO = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
        portraitGO.transform.SetParent(pedestal.transform, false);
        CenterRect(portraitGO.GetComponent<RectTransform>(), new Vector2(100f, 100f), Vector2.zero);
        showcasePortrait = portraitGO.GetComponent<Image>();
        showcasePortrait.preserveAspect = true;
        showcasePortrait.raycastTarget = false;

        // 2. Bảng tên gỗ riêng biệt (Name Plate Banner)
        var namePlate = CreateImage("NamePlate", showcaseBox.transform, null, new Color(0.22f, 0.14f, 0.10f, 0.92f));
        CenterRect(namePlate.rectTransform, new Vector2(312f, 44f), new Vector2(0f, 92f));
        namePlate.gameObject.AddComponent<Outline>().effectColor = new Color(0.48f, 0.36f, 0.20f, 0.7f);

        // Tên chính tiếng Việt
        showcaseName = CreateText("ShowcaseName", namePlate.transform, "TÊN CÂY TRỒNG", 18, TextAnchor.MiddleCenter, new Color(1f, 0.94f, 0.65f, 1f));
        SetAnchors(showcaseName.rectTransform, 0.02f, 0.40f, 0.98f, 0.96f);
        showcaseName.fontStyle = FontStyle.Bold;
        AddTextShadow(showcaseName.gameObject, new Color(0.12f, 0.04f, 0.01f, 0.95f), new Vector2(1.2f, -1.2f));

        // Tên phụ tiếng Anh
        showcaseSubtitle = CreateText("ShowcaseSubtitle", namePlate.transform, "Plant Subtitle", 12, TextAnchor.MiddleCenter, new Color(0.85f, 0.80f, 0.70f, 0.90f));
        SetAnchors(showcaseSubtitle.rectTransform, 0.02f, 0.04f, 0.98f, 0.42f);
        showcaseSubtitle.fontStyle = FontStyle.Italic;

        // 3. Khung thông số chiến đấu & Định danh (Stats & Identity Panel)
        var statsPanel = CreateImage("StatsPanel", showcaseBox.transform, null, new Color(0.08f, 0.05f, 0.04f, 0.75f));
        CenterRect(statsPanel.rectTransform, new Vector2(312f, 94f), new Vector2(0f, 18f));

        // Dòng Tên đầy đủ trên thông số
        statFullNameText = CreateText("StatFullName", statsPanel.transform, "🌱 Cây trồng: Hoa Hướng Dương (Sunflower)", 14, TextAnchor.MiddleLeft, new Color(0.85f, 1f, 0.65f, 1f));
        statFullNameText.fontStyle = FontStyle.Bold;
        SetAnchors(statFullNameText.rectTransform, 0.05f, 0.68f, 0.95f, 0.96f);

        statCostText = CreateText("StatCost", statsPanel.transform, "☀ Nắng: 100", 13, TextAnchor.MiddleLeft, new Color(1f, 0.90f, 0.25f, 1f));
        SetAnchors(statCostText.rectTransform, 0.05f, 0.36f, 0.48f, 0.66f);

        statCooldownText = CreateText("StatCooldown", statsPanel.transform, "⏱ Hồi: Nhanh", 13, TextAnchor.MiddleLeft, new Color(0.85f, 0.92f, 1f, 1f));
        SetAnchors(statCooldownText.rectTransform, 0.52f, 0.36f, 0.95f, 0.66f);

        statToughnessText = CreateText("StatToughness", statsPanel.transform, "🛡 Máu: Bình thường", 13, TextAnchor.MiddleLeft, new Color(0.85f, 1f, 0.82f, 1f));
        SetAnchors(statToughnessText.rectTransform, 0.05f, 0.05f, 0.48f, 0.35f);

        statDamageText = CreateText("StatDamage", statsPanel.transform, "⚔ Sát thương: Bình thường", 13, TextAnchor.MiddleLeft, new Color(1f, 0.75f, 0.70f, 1f));
        SetAnchors(statDamageText.rectTransform, 0.52f, 0.05f, 0.95f, 0.35f);

        // 4. Khung mô tả chi tiết & Cốt truyện (Codex Description & Lore)
        var descPanel = CreateImage("DescPanel", showcaseBox.transform, null, new Color(0.18f, 0.12f, 0.09f, 0.88f));
        CenterRect(descPanel.rectTransform, new Vector2(312f, 120f), new Vector2(0f, -94f));

        showcaseDescription = CreateText("ShowcaseDesc", descPanel.transform, "Mô tả công dụng và khả năng đặc biệt của cây trồng trên chiến trường.", 13, TextAnchor.UpperLeft, new Color(0.96f, 0.94f, 0.88f, 1f));
        SetAnchors(showcaseDescription.rectTransform, 0.04f, 0.05f, 0.96f, 0.95f);
        showcaseDescription.horizontalOverflow = HorizontalWrapMode.Wrap;
        showcaseDescription.verticalOverflow = VerticalWrapMode.Truncate;

        // 5. Khung Mẹo chiến thuật
        var tipPanel = CreateImage("TipPanel", showcaseBox.transform, null, new Color(0.10f, 0.16f, 0.08f, 0.90f));
        CenterRect(tipPanel.rectTransform, new Vector2(312f, 50f), new Vector2(0f, -184f));
        tipPanel.gameObject.AddComponent<Outline>().effectColor = new Color(0.28f, 0.48f, 0.20f, 0.6f);

        showcaseTip = CreateText("ShowcaseTip", tipPanel.transform, "💡 Mẹo: Trồng ở các cột thích hợp để phát huy hỏa lực tối đa.", 12, TextAnchor.MiddleLeft, new Color(0.80f, 1f, 0.68f, 1f));
        SetAnchors(showcaseTip.rectTransform, 0.04f, 0.04f, 0.96f, 0.96f);
        showcaseTip.horizontalOverflow = HorizontalWrapMode.Wrap;
    }

    private void SelectPlant(PlantLoadoutEntry entry)
    {
        if (entry == null) return;
        selectedEntry = entry;

        // Cập nhật viền sáng chọn lựa
        foreach (var card in cardViews)
        {
            bool isCurrent = card.Entry.Key == entry.Key;
            if (card.SelectionGlow != null)
                card.SelectionGlow.gameObject.SetActive(isCurrent);
        }

        string vnName = GetPlantDisplayName(entry.Key);
        string engName = entry.DisplayName;
        string fullName = vnName + " (" + engName + ")";

        // Cập nhật chân dung & thông tin bên phải
        if (showcasePortrait != null)
            showcasePortrait.sprite = SeedPacketFactory.LoadIcon(entry);

        if (showcaseName != null)
            showcaseName.text = vnName.ToUpper();

        if (showcaseSubtitle != null)
            showcaseSubtitle.text = engName;

        if (statFullNameText != null)
            statFullNameText.text = "🌱 Cây: " + fullName;

        var info = GetPlantAlmanacInfo(entry.Key);

        if (statCostText != null)
            statCostText.text = "☀ Nắng: " + entry.Cost;

        if (statCooldownText != null)
            statCooldownText.text = "⏱ Hồi: " + (entry.Cooldown <= 7.5f ? "Nhanh (" + entry.Cooldown + "s)" : (entry.Cooldown <= 30f ? "Chậm (" + entry.Cooldown + "s)" : "Rất chậm (" + entry.Cooldown + "s)"));

        if (statToughnessText != null)
            statToughnessText.text = "🛡 Máu: " + info.Toughness;

        if (statDamageText != null)
            statDamageText.text = "⚔ Sát thương: " + info.Damage;

        if (showcaseDescription != null)
            showcaseDescription.text = "<b>" + fullName + "</b>:\n" + info.Description;

        if (showcaseTip != null)
            showcaseTip.text = "💡 Mẹo: " + info.TacticalTip;
    }

    private void SwitchToZombieAlmanac()
    {
        PlayClickSound();
        Destroy(gameObject);
        ZombieListOverlay.Show();
    }

    private void CloseAlmanac()
    {
        PlayClickSound();
        Destroy(gameObject);
    }

    private static void PlayClickSound()
    {
        var clip = Resources.Load<AudioClip>("Sounds/UI/graveButtonClick");
        if (clip != null) AudioSource.PlayClipAtPoint(clip, Vector3.zero);
    }

    #region Tiện ích dựng giao diện

    private Scrollbar BuildStyledScrollbar(Transform parent)
    {
        var scrollbarGO = new GameObject("Scrollbar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Scrollbar));
        scrollbarGO.transform.SetParent(parent, false);
        SetAnchors(scrollbarGO.GetComponent<RectTransform>(), 0.965f, 0.02f, 0.995f, 0.98f);
        scrollbarGO.GetComponent<Image>().color = Color.clear;

        var slidingArea = new GameObject("Sliding Area", typeof(RectTransform));
        slidingArea.transform.SetParent(scrollbarGO.transform, false);
        SetAnchors(slidingArea.GetComponent<RectTransform>(), 0.2f, 0.01f, 0.8f, 0.99f);

        var track = CreateImage("Track", slidingArea.transform, null, new Color(0.12f, 0.06f, 0.03f, 0.75f));
        Stretch(track.rectTransform);

        var handle = CreateImage("Handle", slidingArea.transform, null, new Color(0.96f, 0.72f, 0.20f, 0.95f));
        Stretch(handle.rectTransform);
        handle.gameObject.AddComponent<Outline>().effectColor = new Color(0.35f, 0.20f, 0.05f, 0.9f);

        var scrollbar = scrollbarGO.GetComponent<Scrollbar>();
        scrollbar.handleRect = handle.rectTransform;
        scrollbar.targetGraphic = handle;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        return scrollbar;
    }

    private GameObject CreateStyledButton(string name, Transform parent, string label, int fontSize, Vector2 size, Vector2 pos, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(MenuButtonMotion));
        go.transform.SetParent(parent, false);
        CenterRect(go.GetComponent<RectTransform>(), size, pos);

        var img = go.GetComponent<Image>();
        img.sprite = cachedButton1;
        img.type = Image.Type.Sliced;
        img.preserveAspect = true;

        var btn = go.GetComponent<Button>();
        btn.transition = Selectable.Transition.SpriteSwap;
        var states = btn.spriteState;
        states.highlightedSprite = cachedButton2;
        states.pressedSprite = cachedButton2;
        states.selectedSprite = cachedButton2;
        btn.spriteState = states;
        btn.onClick.AddListener(action);

        go.GetComponent<MenuButtonMotion>().targetGraphic = img;

        var txt = CreateText("Label", go.transform, label, fontSize, TextAnchor.MiddleCenter, new Color(1f, 0.96f, 0.74f, 1f));
        Stretch(txt.rectTransform);
        txt.fontStyle = FontStyle.Bold;
        txt.raycastTarget = false;
        AddTextShadow(txt.gameObject, new Color(0.12f, 0.045f, 0.01f, 0.9f), new Vector2(1.4f, -1.4f));

        return go;
    }

    private void CreateCloseIcon(string name, Transform parent, Vector2 pos, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(MenuButtonMotion));
        go.transform.SetParent(parent, false);
        CenterRect(go.GetComponent<RectTransform>(), new Vector2(44f, 44f), pos);

        var img = go.GetComponent<Image>();
        img.sprite = cachedCancel;
        img.preserveAspect = true;

        var btn = go.GetComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.onClick.AddListener(action);

        go.GetComponent<MenuButtonMotion>().targetGraphic = img;
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        return img;
    }

    private static Text CreateText(string name, Transform parent, string value, int size, TextAnchor alignment, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>();
        text.font = cachedFont;
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        return text;
    }

    private static void AddTextShadow(GameObject target, Color color, Vector2 dist)
    {
        var outline = target.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = dist;
    }

    private static void Stretch(RectTransform rect) => SetAnchors(rect, 0f, 0f, 1f, 1f);

    private static void CenterRect(RectTransform rect, Vector2 size, Vector2 pos)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;
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

    #endregion

    #region Dữ liệu cẩm nang cây trồng phong phú

    private struct PlantDetailInfo
    {
        public string Toughness;
        public string Damage;
        public string Description;
        public string TacticalTip;
    }

    private static string GetPlantDisplayName(string key)
    {
        switch (key)
        {
            case "SunFlower": return "Hướng Dương";
            case "PeaShooter": return "Đậu Bắn Súng";
            case "WallNut": return "Quả Óc Chó";
            case "Squash": return "Bí Đè";
            case "TorchWood": return "Gốc Đuốc";
            case "MiaoMiao": return "Mèo Miu Xạ Thủ";
            case "SnowKing": return "Vua Băng Giá";
            case "SunNut": return "Óc Chó Nắng";
            case "RepeaterPea": return "Đậu Hai Nòng";
            case "SnowPea": return "Đậu Băng Giá";
            case "Threepeater": return "Đậu Ba Nòng";
            case "CherryBomb": return "Bom Anh Đào";
            case "PotatoMine": return "Mìn Khoai Tây";
            case "Chomper": return "Cây Ăn Thịt";
            case "PuffShroom": return "Nấm Bào Tử";
            case "SunShroom": return "Nấm Mặt Trời";
            case "ScaredyShroom": return "Nấm Nhút Nhát";
            case "HypnoShroom": return "Nấm Thôi Miên";
            case "IceShroom": return "Nấm Băng Hàn";
            case "Jalapeno": return "Ớt Thiêu Đốt";
            case "Spikeweed": return "Chông Gai";
            case "CabbagePult": return "Bắp Cải Bắn";
            case "KernelPult": return "Bắp Ngô Bơ";
            case "MelonPult": return "Dưa Hấu Ném";
            case "UmbrellaLeaf": return "Lá Dù Hộ Vệ";
            default: return key;
        }
    }

    private static PlantDetailInfo GetPlantAlmanacInfo(string key)
    {
        switch (key)
        {
            case "SunFlower":
                return new PlantDetailInfo
                {
                    Toughness = "300 (Bình thường)",
                    Damage = "Không có",
                    Description = "Sản xuất mặt trời định kỳ (25 Nắng). Cội nguồn năng lượng thiết yếu để nuôi dưỡng toàn bộ khu vườn chiến đấu.",
                    TacticalTip = "Nên trồng từ 1 đến 2 hàng đầu tiên ở phía sau để đảm bảo nền kinh tế vững chắc."
                };
            case "PeaShooter":
                return new PlantDetailInfo
                {
                    Toughness = "300 (Bình thường)",
                    Damage = "20 / viên (1.4s)",
                    Description = "Bắn những viên đậu xanh tròn trịa vào bất kỳ zombie nào dám tiến vào hàng ngang trước mặt.",
                    TacticalTip = "Tuyến phòng thủ cơ bản nhất, cực kỳ hữu dụng để đối phó những zombie xuất hiện sớm."
                };
            case "WallNut":
                return new PlantDetailInfo
                {
                    Toughness = "4000 (Rất cao)",
                    Damage = "Không có",
                    Description = "Sở hữu lớp vỏ dày bất khả xâm phạm. Có thể chặn đứng bước chân của nhiều zombie cùng lúc trong thời gian dài.",
                    TacticalTip = "Đặt ở các cột 5 - 7 phía trước để câu giờ cho các cây hỏa lực phía sau xả đạn."
                };
            case "Squash":
                return new PlantDetailInfo
                {
                    Toughness = "300 (Bình thường)",
                    Damage = "1800 (Cực lớn)",
                    Description = "Trừng mắt nhìn kẻ thù rồi bật nhảy đè bẹp bất kỳ zombie nào xuất hiện trong phạm vi 1 ô xung quanh.",
                    TacticalTip = "Giải pháp giá rẻ để lập tức tiêu diệt các zombie nguy hiểm như Mũ chóp, Đội xô hay Cầu thủ."
                };
            case "TorchWood":
                return new PlantDetailInfo
                {
                    Toughness = "300 (Bình thường)",
                    Damage = "Cường hóa x2",
                    Description = "Ngọn lửa bùng cháy liên tục. Khi hạt đậu bay qua sẽ biến thành cầu lửa gây gấp đôi sát thương và nổ lan!",
                    TacticalTip = "Đặt đằng sau các loại cây bắn đậu để tối đa hóa lượng sát thương đầu ra."
                };
            case "MiaoMiao":
                return new PlantDetailInfo
                {
                    Toughness = "400 (Khá)",
                    Damage = "Nặng / Xuyên phá",
                    Description = "Chiến binh mèo dũng mãnh, vung vuốt sắc lẹm và phóng tia lông tốc độ cao xé tan xác bầy zombie hung bạo.",
                    TacticalTip = "Sát thương dồn cực mạnh, thích hợp làm hỏa lực chủ lực dập tắt các đợt Huge Wave."
                };
            case "SnowKing":
                return new PlantDetailInfo
                {
                    Toughness = "500 (Cao)",
                    Damage = "Lạnh buốt lan tỏa",
                    Description = "Bậc thầy băng giá phương Bắc. Triệu hồi gió tuyết làm chậm toàn bộ kẻ địch và bắn tinh thể băng khổng lồ.",
                    TacticalTip = "Kiểm soát nhịp độ trận đấu tuyệt hảo, làm suy yếu toàn diện đoàn quân xác sống."
                };
            case "SunNut":
                return new PlantDetailInfo
                {
                    Toughness = "2500 (Cao)",
                    Damage = "Không có",
                    Description = "Kết hợp độc đáo: vừa là tấm khiên vững chãi chặn đường, vừa phát ra năng lượng Nắng rực rỡ mỗi khi bị cắn!",
                    TacticalTip = "Trồng ở tiền tuyến để vừa phòng thủ vừa thu hoạch nguồn nắng dồi dào."
                };
            case "RepeaterPea":
                return new PlantDetailInfo
                {
                    Toughness = "300 (Bình thường)",
                    Damage = "40 (2 viên / lượt)",
                    Description = "Bắn liên tiếp 2 hạt đậu mỗi lượt. Hỏa lực dồi dào gấp đôi so với Peashooter thông thường.",
                    TacticalTip = "Nâng cấp hoàn hảo cho những hàng chịu nhiều áp lực tấn công liên tục."
                };
            case "SnowPea":
                return new PlantDetailInfo
                {
                    Toughness = "300 (Bình thường)",
                    Damage = "20 + Chậm 50%",
                    Description = "Bắn những viên đậu băng giá buốt giá, làm giảm một nửa tốc độ di chuyển và cắn của kẻ thù.",
                    TacticalTip = "Chỉ cần 1 Đậu Băng trên mỗi hàng là đủ để kiểm soát tốc độ của cả đàn zombie."
                };
            case "Threepeater":
                return new PlantDetailInfo
                {
                    Toughness = "300 (Bình thường)",
                    Damage = "20 x 3 hàng",
                    Description = "Ba chiếc đầu linh hoạt cùng bắn đậu đồng thời sang cả hàng hiện tại, hàng trên và hàng dưới.",
                    TacticalTip = "Trồng ở hàng số 2 và số 4 để bao quát hỏa lực trên toàn bộ 5 làn sân cỏ."
                };
            case "CherryBomb":
                return new PlantDetailInfo
                {
                    Toughness = "300",
                    Damage = "1800 (Diện rộng 3x3)",
                    Description = "Hai quả anh đào nổi giận phát nổ ngay tức khắc, thổi bay toàn bộ zombie trong khu vực lân cận.",
                    TacticalTip = "Cứu nguy hoàn hảo khi phòng tuyến bị chọc thủng hoặc khi xe dọn băng xuất hiện."
                };
            case "PotatoMine":
                return new PlantDetailInfo
                {
                    Toughness = "300",
                    Damage = "1800 (1 ô)",
                    Description = "Chi phí siêu rẻ (25 Nắng). Cần khoảng 14 giây chuẩn bị trồi lên trước khi có thể phát nổ.",
                    TacticalTip = "Trồng từ sớm để hạ gục zombie đầu tiên, giúp bạn thảnh thơi tích lũy Nắng."
                };
            case "Chomper":
                return new PlantDetailInfo
                {
                    Toughness = "300",
                    Damage = "Nuốt trọn tức thì",
                    Description = "Đớp trọn một zombie vào bụng chỉ trong tích tắc. Cần một khoảng thời gian nhai trước khi đớp tiếp.",
                    TacticalTip = "Đặt sau Quả Óc Chó để an toàn nuốt chửng những kẻ địch mang giáp sắt."
                };
            case "PuffShroom":
                return new PlantDetailInfo
                {
                    Toughness = "300",
                    Damage = "20 (Tầm ngắn)",
                    Description = "Hoàn toàn miễn phí! Bắn các cụm bào tử tầm ngắn, thích hợp phòng ngự chớp nhoáng.",
                    TacticalTip = "Trồng liên tục không ngừng để quấy rối và làm bia đỡ đạn cho các cây phía sau."
                };
            case "SunShroom":
                return new PlantDetailInfo
                {
                    Toughness = "300",
                    Damage = "Không có",
                    Description = "Lúc nhỏ cho 15 nắng. Sau một thời gian sẽ lớn lên và cho 25 nắng như Hoa Hướng Dương.",
                    TacticalTip = "Lựa chọn kinh tế số 1 cho màn đêm và những màn chơi khởi đầu eo hẹp."
                };
            case "ScaredyShroom":
                return new PlantDetailInfo
                {
                    Toughness = "300",
                    Damage = "20 (Tầm xa)",
                    Description = "Bắn tỉa tầm cực xa với chi phí siêu rẻ. Tuy nhiên sẽ trốn dưới đất nếu zombie lại gần trong 1 ô.",
                    TacticalTip = "Trồng sâu ở các cột sau cùng để phát huy tối đa tầm bắn xa mà không bị hoảng sợ."
                };
            case "HypnoShroom":
                return new PlantDetailInfo
                {
                    Toughness = "300",
                    Damage = "Thôi miên",
                    Description = "Khi bị zombie cắn phải, kẻ địch sẽ quay đầu 180 độ và tấn công lại chính những zombie khác!",
                    TacticalTip = "Vũ khí bí mật tuyệt hảo để chuyển hóa Football Zombie hoặc Bucket Zombie thành đồng minh."
                };
            case "IceShroom":
                return new PlantDetailInfo
                {
                    Toughness = "300",
                    Damage = "20 + Đóng băng toàn sân",
                    Description = "Đóng băng cứng toàn bộ zombie trên toàn sân cỏ trong vài giây, tạo thời gian vàng cho hàng thủ.",
                    TacticalTip = "Kích hoạt vào thời điểm bùng nổ của đợt Final Wave để lật ngược thế cờ."
                };
            case "Jalapeno":
                return new PlantDetailInfo
                {
                    Toughness = "300",
                    Damage = "1800 (Toàn bộ hàng)",
                    Description = "Phun trào biển lửa hủy diệt quét sạch toàn bộ zombie trên cả một hàng ngang thẳng đứng.",
                    TacticalTip = "Giải cứu nhanh chóng một làn đường bị thất thủ hoặc làm tan chảy vệt băng của xe Zomboni."
                };
            case "Spikeweed":
                return new PlantDetailInfo
                {
                    Toughness = "300",
                    Damage = "20 / nhát đâm",
                    Description = "Nằm sát mặt đất đâm chọc chân kẻ thù đi qua. Làm nổ bánh xe Zomboni ngay lập tức.",
                    TacticalTip = "Zombie thông thường không thể cắn Spikeweed."
                };
            case "CabbagePult":
                return new PlantDetailInfo
                {
                    Toughness = "300",
                    Damage = "40 / quả",
                    Description = "Ném bắp cải theo quỹ đạo vòng cung, bay qua khiên chắn và địa hình dốc đứng.",
                    TacticalTip = "Khắc tinh hoàn hảo của Zombie Cầm Cửa và địa hình mái nhà dốc."
                };
            case "KernelPult":
                return new PlantDetailInfo
                {
                    Toughness = "300",
                    Damage = "20 (Hạt) / Choáng (Bơ)",
                    Description = "Ném hạt bắp ngô và thi thoảng phóng ra một tảng bơ dính chặt zombie, làm choáng hoàn toàn!",
                    TacticalTip = "Bơ có thể làm rơi bóng bay hoặc chặn đứng tức thì đà tiến của xe Zomboni."
                };
            case "MelonPult":
                return new PlantDetailInfo
                {
                    Toughness = "300",
                    Damage = "80 + Lan 26 (3x3)",
                    Description = "Cỗ máy pháo binh tối thượng, ném những quả dưa hấu hạng nặng gây sát thương lan hủy diệt.",
                    TacticalTip = "Xương sống hỏa lực bắt buộc trong các trận chiến sinh tồn và màn đêm khắc nghiệt."
                };
            case "UmbrellaLeaf":
                return new PlantDetailInfo
                {
                    Toughness = "300",
                    Damage = "Không có",
                    Description = "Xòe tán lá chắn vững chãi bảo vệ bản thân và 8 ô xung quanh khỏi đòn thả quân và đạn ném.",
                    TacticalTip = "Bảo hộ tuyệt đối cho các cây quan trọng khỏi Bungee Zombie và Catapult Zombie."
                };
            default:
                return new PlantDetailInfo
                {
                    Toughness = "300",
                    Damage = "Bình thường",
                    Description = "Chiến binh kiên cường của khu vườn với năng lực chiến đấu độc đáo.",
                    TacticalTip = "Sử dụng linh hoạt theo tình hình thực tế trên sân cỏ."
                };
        }
    }

    #endregion
}
