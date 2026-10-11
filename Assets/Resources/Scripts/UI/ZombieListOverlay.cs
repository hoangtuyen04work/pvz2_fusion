using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Cẩm nang Zombie (Zombie Almanac / Codex).
/// Trình bày theo phong cách nghệ thuật đồng bộ với giao diện PvZ của game:
/// Khung gỗ đá cổ điển, phân chia 2 cột (danh mục thẻ zombie bên trái, bục giới thiệu chi tiết bên phải).
/// </summary>
public sealed class ZombieListOverlay : MonoBehaviour
{
    private static Font cachedFont;
    private static Sprite cachedDialogMain;
    private static Sprite cachedDialogChild;
    private static Sprite cachedButton1;
    private static Sprite cachedButton2;
    private static Sprite cachedCancel;
    private static Sprite cachedCardFrame;

    private readonly List<ZombieCardView> cardViews = new List<ZombieCardView>();
    private ZombieRoster.Entry selectedEntry;

    // Các thành phần thuộc khung chi tiết bên phải
    private Image showcasePortrait;
    private Text showcaseName;
    private Text showcaseSubtitle;
    private Text statFullNameText;
    private Text statCostText;
    private Text statCooldownText;
    private Text statToughnessText;
    private Text statSpeedText;
    private Text showcaseDescription;
    private Text showcaseTip;

    private sealed class ZombieCardView
    {
        public ZombieRoster.Entry Entry;
        public GameObject Root;
        public Image SelectionGlow;
        public RectTransform Rect;
    }

    public static void Show()
    {
        if (Object.FindAnyObjectByType<ZombieListOverlay>() != null) return;

        // Nếu đang mở cẩm nang cây trồng thì đóng lại để chuyển trang
        var existingPlant = Object.FindAnyObjectByType<PlantAlmanacOverlay>();
        if (existingPlant != null) Destroy(existingPlant.gameObject);

        var root = new GameObject("Zombie Almanac", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(ZombieListOverlay));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1000f, 750f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        root.GetComponent<ZombieListOverlay>().Build();
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
    }

    private void Build()
    {
        EnsureEventSystem();

        // 1. Lớp phủ tối mờ toàn màn hình
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

        // 3. Tiêu đề Cẩm Nang Zombie
        var titleText = CreateText("Tiêu đề", dialogObject.transform, "CẨM NANG ZOMBIE", 32, TextAnchor.MiddleCenter, new Color(1f, 0.88f, 0.70f, 1f));
        CenterRect(titleText.rectTransform, new Vector2(400f, 48f), new Vector2(0f, 276f));
        titleText.fontStyle = FontStyle.Bold;
        AddTextShadow(titleText.gameObject, new Color(0.22f, 0.05f, 0.02f, 0.95f), new Vector2(1.8f, -1.8f));

        // 4. Nút chuyển nhanh sang Cẩm nang Cây trồng (Góc trên trái)
        CreateStyledButton("Tab Cây Trồng", dialogObject.transform, "CÂY TRỒNG ➔", 16, new Vector2(146f, 40f), new Vector2(-360f, 274f), SwitchToPlantAlmanac);
        // Nút Đóng (X) góc trên phải
        CreateCloseIcon("Nút Đóng X", dialogObject.transform, new Vector2(430f, 274f), CloseAlmanac);

        // 5. Cột trái: Danh sách thẻ Zombie (Scroll View)
        BuildLeftCatalog(dialogObject.transform);

        // 6. Cột phải: Bục giới thiệu chi tiết (Showcase Podium & Codex)
        BuildRightShowcase(dialogObject.transform);

        // 7. Nút ĐÓNG to phía dưới cùng bên phải
        CreateStyledButton("Nút Đóng", dialogObject.transform, "ĐÓNG", 22, new Vector2(160f, 50f), new Vector2(350f, -270f), CloseAlmanac);

        // 8. Chọn mặc định zombie đầu tiên
        if (ZombieRoster.All != null && ZombieRoster.All.Length > 0)
        {
            SelectZombie(ZombieRoster.All[0]);
        }
    }

    private void BuildLeftCatalog(Transform parent)
    {
        // Khung nền chìm màu gỗ sẫm
        var catalogFrame = new GameObject("Catalog Box", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        catalogFrame.transform.SetParent(parent, false);
        var frameRect = catalogFrame.GetComponent<RectTransform>();
        CenterRect(frameRect, new Vector2(506f, 486f), new Vector2(-192f, -8f));

        var frameImg = catalogFrame.GetComponent<Image>();
        frameImg.sprite = cachedDialogChild;
        frameImg.type = Image.Type.Sliced;
        frameImg.color = new Color(0.13f, 0.08f, 0.08f, 0.94f);

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

        // Sinh thẻ Zombie
        foreach (var entry in ZombieRoster.All)
        {
            CreateZombieCard(entry, contentGO.transform);
        }
    }

    private void CreateZombieCard(ZombieRoster.Entry entry, Transform parent)
    {
        var cardGO = new GameObject("Card_" + entry.name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(MenuButtonMotion));
        cardGO.transform.SetParent(parent, false);
        var cardRect = cardGO.GetComponent<RectTransform>();
        cardRect.sizeDelta = new Vector2(88f, 114f);

        var cardImg = cardGO.GetComponent<Image>();
        cardImg.sprite = cachedCardFrame;
        cardImg.type = Image.Type.Sliced;
        cardImg.color = new Color(0.85f, 0.78f, 0.74f, 1f);

        var btn = cardGO.GetComponent<Button>();
        btn.transition = Selectable.Transition.None;
        var captured = entry;
        btn.onClick.AddListener(() => {
            PlayClickSound();
            SelectZombie(captured);
        });

        var motion = cardGO.GetComponent<MenuButtonMotion>();
        motion.targetGraphic = cardImg;

        // Viền sáng cam lửa khi được chọn
        var glowGO = new GameObject("Glow", typeof(RectTransform), typeof(Image), typeof(Outline));
        glowGO.transform.SetParent(cardGO.transform, false);
        Stretch(glowGO.GetComponent<RectTransform>());
        var glowImg = glowGO.GetComponent<Image>();
        glowImg.color = new Color(1f, 0.50f, 0.18f, 0.25f);
        glowImg.raycastTarget = false;
        var glowOutline = glowGO.GetComponent<Outline>();
        glowOutline.effectColor = new Color(1f, 0.55f, 0.20f, 0.95f);
        glowOutline.effectDistance = new Vector2(2.5f, -2.5f);
        glowGO.SetActive(false);

        // Icon Zombie
        var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(cardGO.transform, false);
        SetAnchors(iconGO.GetComponent<RectTransform>(), 0.06f, 0.28f, 0.94f, 0.92f);
        var iconImg = iconGO.GetComponent<Image>();
        iconImg.sprite = ZombieIconHelper.GetIcon(entry.name);
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;

        // Tên Zombie rút gọn
        var nameText = CreateText("Name", cardGO.transform, entry.label, 12, TextAnchor.MiddleCenter, new Color(0.24f, 0.08f, 0.08f, 1f));
        nameText.fontStyle = FontStyle.Bold;
        SetAnchors(nameText.rectTransform, 0.04f, 0.18f, 0.96f, 0.32f);
        nameText.raycastTarget = false;

        // Tag giá Não ở đáy thẻ
        var costBack = CreateImage("CostTag", cardGO.transform, null, new Color(0.18f, 0.06f, 0.06f, 0.92f));
        SetAnchors(costBack.rectTransform, 0.04f, 0.03f, 0.96f, 0.19f);
        costBack.raycastTarget = false;

        var costText = CreateText("Cost", costBack.transform, entry.cost + " 🧠", 13, TextAnchor.MiddleCenter, new Color(1f, 0.78f, 0.82f, 1f));
        Stretch(costText.rectTransform);
        costText.fontStyle = FontStyle.Bold;
        costText.raycastTarget = false;

        var view = new ZombieCardView
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
        boxImg.color = new Color(0.15f, 0.09f, 0.09f, 0.94f);

        // 1. Bục đặt chân dung zombie (Pedestal)
        var pedestal = CreateImage("Pedestal", showcaseBox.transform, null, new Color(0.26f, 0.16f, 0.16f, 0.75f));
        CenterRect(pedestal.rectTransform, new Vector2(312f, 114f), new Vector2(0f, 176f));
        pedestal.gameObject.AddComponent<Outline>().effectColor = new Color(0.46f, 0.28f, 0.28f, 0.5f);

        // Chân dung lớn
        var portraitGO = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
        portraitGO.transform.SetParent(pedestal.transform, false);
        CenterRect(portraitGO.GetComponent<RectTransform>(), new Vector2(100f, 100f), Vector2.zero);
        showcasePortrait = portraitGO.GetComponent<Image>();
        showcasePortrait.preserveAspect = true;
        showcasePortrait.raycastTarget = false;

        // 2. Bảng tên gỗ riêng biệt (Name Plate Banner)
        var namePlate = CreateImage("NamePlate", showcaseBox.transform, null, new Color(0.24f, 0.12f, 0.12f, 0.92f));
        CenterRect(namePlate.rectTransform, new Vector2(312f, 44f), new Vector2(0f, 92f));
        namePlate.gameObject.AddComponent<Outline>().effectColor = new Color(0.52f, 0.28f, 0.22f, 0.7f);

        // Tên chính tiếng Việt
        showcaseName = CreateText("ShowcaseName", namePlate.transform, "TÊN ZOMBIE", 18, TextAnchor.MiddleCenter, new Color(1f, 0.88f, 0.70f, 1f));
        SetAnchors(showcaseName.rectTransform, 0.02f, 0.40f, 0.98f, 0.96f);
        showcaseName.fontStyle = FontStyle.Bold;
        AddTextShadow(showcaseName.gameObject, new Color(0.18f, 0.04f, 0.02f, 0.95f), new Vector2(1.2f, -1.2f));

        // Tên phụ / Tên định danh (English/ID)
        showcaseSubtitle = CreateText("ShowcaseSubtitle", namePlate.transform, "Zombie Identifier", 12, TextAnchor.MiddleCenter, new Color(0.85f, 0.75f, 0.70f, 0.90f));
        SetAnchors(showcaseSubtitle.rectTransform, 0.02f, 0.04f, 0.98f, 0.42f);
        showcaseSubtitle.fontStyle = FontStyle.Italic;

        // 3. Khung thông số chiến đấu & Định danh (Stats & Identity Panel)
        var statsPanel = CreateImage("StatsPanel", showcaseBox.transform, null, new Color(0.08f, 0.04f, 0.04f, 0.75f));
        CenterRect(statsPanel.rectTransform, new Vector2(312f, 94f), new Vector2(0f, 18f));

        // Dòng Tên đầy đủ trên thông số
        statFullNameText = CreateText("StatFullName", statsPanel.transform, "🧟 Zombie: Zombie Thường (Basic Zombie)", 14, TextAnchor.MiddleLeft, new Color(1f, 0.82f, 0.70f, 1f));
        statFullNameText.fontStyle = FontStyle.Bold;
        SetAnchors(statFullNameText.rectTransform, 0.05f, 0.68f, 0.95f, 0.96f);

        statCostText = CreateText("StatCost", statsPanel.transform, "🧠 Não: 25", 13, TextAnchor.MiddleLeft, new Color(1f, 0.78f, 0.82f, 1f));
        SetAnchors(statCostText.rectTransform, 0.05f, 0.36f, 0.48f, 0.66f);

        statCooldownText = CreateText("StatCooldown", statsPanel.transform, "⏱ Hồi: 3.0s", 13, TextAnchor.MiddleLeft, new Color(0.90f, 0.90f, 1f, 1f));
        SetAnchors(statCooldownText.rectTransform, 0.52f, 0.36f, 0.95f, 0.66f);

        statToughnessText = CreateText("StatToughness", statsPanel.transform, "🛡 Độ trâu: Bình thường", 13, TextAnchor.MiddleLeft, new Color(1f, 0.82f, 0.70f, 1f));
        SetAnchors(statToughnessText.rectTransform, 0.05f, 0.05f, 0.48f, 0.35f);

        statSpeedText = CreateText("StatSpeed", statsPanel.transform, "⚡ Tốc độ: Chậm", 13, TextAnchor.MiddleLeft, new Color(0.80f, 1f, 0.85f, 1f));
        SetAnchors(statSpeedText.rectTransform, 0.52f, 0.05f, 0.95f, 0.35f);

        // 4. Khung mô tả chi tiết & Cốt truyện (Codex Lore)
        var descPanel = CreateImage("DescPanel", showcaseBox.transform, null, new Color(0.18f, 0.10f, 0.10f, 0.88f));
        CenterRect(descPanel.rectTransform, new Vector2(312f, 120f), new Vector2(0f, -94f));

        showcaseDescription = CreateText("ShowcaseDesc", descPanel.transform, "Mô tả tập tính, nguy hiểm và đặc điểm di chuyển của zombie trên bãi cỏ.", 13, TextAnchor.UpperLeft, new Color(0.96f, 0.92f, 0.90f, 1f));
        SetAnchors(showcaseDescription.rectTransform, 0.04f, 0.05f, 0.96f, 0.95f);
        showcaseDescription.horizontalOverflow = HorizontalWrapMode.Wrap;
        showcaseDescription.verticalOverflow = VerticalWrapMode.Truncate;

        // 5. Khung Mẹo khắc chế
        var tipPanel = CreateImage("TipPanel", showcaseBox.transform, null, new Color(0.18f, 0.08f, 0.06f, 0.90f));
        CenterRect(tipPanel.rectTransform, new Vector2(312f, 50f), new Vector2(0f, -184f));
        tipPanel.gameObject.AddComponent<Outline>().effectColor = new Color(0.50f, 0.22f, 0.18f, 0.6f);

        showcaseTip = CreateText("ShowcaseTip", tipPanel.transform, "💡 Khắc chế: Dùng các loại cây phù hợp để hóa giải năng lực.", 12, TextAnchor.MiddleLeft, new Color(1f, 0.85f, 0.65f, 1f));
        SetAnchors(showcaseTip.rectTransform, 0.04f, 0.04f, 0.96f, 0.96f);
        showcaseTip.horizontalOverflow = HorizontalWrapMode.Wrap;
    }

    private void SelectZombie(ZombieRoster.Entry entry)
    {
        if (entry == null) return;
        selectedEntry = entry;

        // Cập nhật viền sáng chọn lựa
        foreach (var card in cardViews)
        {
            bool isCurrent = card.Entry.name == entry.name;
            if (card.SelectionGlow != null)
                card.SelectionGlow.gameObject.SetActive(isCurrent);
        }

        string vnName = entry.label;
        string engName = GetZombieEnglishName(entry.name);
        string fullName = vnName + " (" + engName + ")";

        // Cập nhật chân dung & thông tin bên phải
        if (showcasePortrait != null)
            showcasePortrait.sprite = ZombieIconHelper.GetIcon(entry.name);

        if (showcaseName != null)
            showcaseName.text = vnName.ToUpper();

        if (showcaseSubtitle != null)
            showcaseSubtitle.text = engName;

        if (statFullNameText != null)
            statFullNameText.text = "🧟 Zombie: " + fullName;

        var info = GetZombieAlmanacInfo(entry.name);

        if (statCostText != null)
            statCostText.text = "🧠 Não: " + entry.cost;

        if (statCooldownText != null)
            statCooldownText.text = "⏱ Hồi: " + entry.cooldown.ToString("0.#") + "s";

        if (statToughnessText != null)
            statToughnessText.text = "🛡 Độ trâu: " + info.Toughness;

        if (statSpeedText != null)
            statSpeedText.text = "⚡ Tốc độ: " + info.Speed;

        if (showcaseDescription != null)
            showcaseDescription.text = "<b>" + fullName + "</b>:\n" + info.Description;

        if (showcaseTip != null)
            showcaseTip.text = "💡 Khắc chế: " + info.CounterTip;
    }

    private static string GetZombieEnglishName(string name)
    {
        switch (name)
        {
            case "ZombieNormal": return "Basic Zombie";
            case "ConeZombie": return "Conehead Zombie";
            case "BucketZombie": return "Buckethead Zombie";
            case "FlagZombie": return "Flag Zombie";
            case "PoleVaultingZombie": return "Pole Vaulting Zombie";
            case "NewspaperZombie": return "Newspaper Zombie";
            case "ScreenDoorZombie": return "Screen Door Zombie";
            case "FootballZombie": return "Football Zombie";
            case "DancingZombie": return "Dancing Zombie";
            case "BackupDancer": return "Backup Dancer";
            case "DolphinRiderZombie": return "Dolphin Rider Zombie";
            case "SnorkelZombie": return "Snorkel Zombie";
            case "Zomboni": return "Zomboni Machine";
            case "BalloonZombie": return "Balloon Zombie";
            case "JackinTheBoxZombie": return "Jack-in-the-Box Zombie";
            case "Imp": return "Imp Zombie";
            case "ChineseZombie": return "Chinese Zombie";
            case "Ghost": return "Ghost Zombie";
            case "SnowZombie": return "Snow Zombie";
            case "BoneZombie": return "Bone Zombie";
            case "IceBlockZombie": return "Ice Block Zombie";
            case "YetiZombie": return "Yeti Zombie";
            case "GatlingZombie": return "Gatling Pea Zombie";
            case "ConeBucketZombie": return "Reinforced Cone-Bucket Zombie";
            case "FireImpZombie": return "Fire Imp Zombie";
            default: return name;
        }
    }

    private void SwitchToPlantAlmanac()
    {
        PlayClickSound();
        Destroy(gameObject);
        PlantAlmanacOverlay.Show();
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

        var track = CreateImage("Track", slidingArea.transform, null, new Color(0.12f, 0.05f, 0.04f, 0.75f));
        Stretch(track.rectTransform);

        var handle = CreateImage("Handle", slidingArea.transform, null, new Color(0.88f, 0.42f, 0.25f, 0.95f));
        Stretch(handle.rectTransform);
        handle.gameObject.AddComponent<Outline>().effectColor = new Color(0.38f, 0.15f, 0.10f, 0.9f);

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

    #region Dữ liệu cẩm nang zombie

    private struct ZombieDetailInfo
    {
        public string Toughness;
        public string Speed;
        public string Description;
        public string CounterTip;
    }

    private static ZombieDetailInfo GetZombieAlmanacInfo(string zombieName)
    {
        switch (zombieName)
        {
            case "ZombieNormal":
                return new ZombieDetailInfo
                {
                    Toughness = "Cơ bản (200)",
                    Speed = "Chậm",
                    Description = "Kẻ dạo bộ điển hình trên bãi cỏ. Chậm chạp, ngây ngô nhưng không bao giờ từ bỏ niềm đam mê thưởng thức não bộ.",
                    CounterTip = "1 cây Peashooter cơ bản là đủ để hạ gục gã trước khi kịp bước tới."
                };
            case "ConeZombie":
                return new ZombieDetailInfo
                {
                    Toughness = "Trung bình (560)",
                    Speed = "Chậm",
                    Description = "Chiếc nón giao thông biến gã thành kẻ kiên cường gấp 2.8 lần. Cần gấp đôi lượng đạn để phá vỡ nón bảo hộ.",
                    CounterTip = "Bí đè (Squash) hoặc tập trung 2 cây Peashooter trên cùng một làn."
                };
            case "BucketZombie":
                return new ZombieDetailInfo
                {
                    Toughness = "Rất cao (1300)",
                    Speed = "Chậm",
                    Description = "Chiếc xô sắt bảo vệ đầu cực kỳ kiên cố, chống chọi bền bỉ trước mọi loại đạn thông thường.",
                    CounterTip = "Dùng Nam châm hút xô sắt, hoặc Nấm thôi miên (Hypno-shroom) để biến thành đồng minh."
                };
            case "FlagZombie":
                return new ZombieDetailInfo
                {
                    Toughness = "Cơ bản (200)",
                    Speed = "Khá nhanh",
                    Description = "Kẻ cầm lá cờ đỏ dẫn đầu các đợt tấn công lớn (Huge Wave). Di chuyển nhanh hơn zombie thông thường.",
                    CounterTip = "Báo hiệu chuẩn bị tinh thần kích nổ Bom hoặc làm chậm toàn sân."
                };
            case "PoleVaultingZombie":
                return new ZombieDetailInfo
                {
                    Toughness = "Cao (500)",
                    Speed = "Rất nhanh",
                    Description = "Cầm sào tre chạy thục mạng và phóng qua cái cây đầu tiên gặp phải. Sau khi mất sào sẽ đi chậm lại.",
                    CounterTip = "Dùng Khoai tây hoặc cây giá rẻ hy sinh phía trước để gã mất sào sớm."
                };
            case "NewspaperZombie":
                return new ZombieDetailInfo
                {
                    Toughness = "Khá (350)",
                    Speed = "Chậm ➔ Rất nhanh",
                    Description = "Đọc báo rất chăm chú. Nhưng khi tờ báo bị bắn rách, gã sẽ nổi cơn lôi đình và lao như bay!",
                    CounterTip = "Làm chậm bằng Snow Pea hoặc đặt Quả Óc Chó chặn đứng cơn thịnh nộ."
                };
            case "ScreenDoorZombie":
                return new ZombieDetailInfo
                {
                    Toughness = "Rất cao (1300)",
                    Speed = "Chậm",
                    Description = "Cầm cánh cửa lưới sắt chắn trước ngực. Vô hiệu hóa hoàn toàn mọi đạn đậu bắn thẳng hàng.",
                    CounterTip = "Dùng máy ném bắp cải (Cabbage-pult) hoặc Nấm khói để xuyên qua cửa lưới."
                };
            case "FootballZombie":
                return new ZombieDetailInfo
                {
                    Toughness = "Cực cao (1600)",
                    Speed = "Rất nhanh",
                    Description = "Cỗ xe tăng bọc thép của phe zombie. Vừa trâu bò vừa lao đi với tốc độ đáng kinh ngạc.",
                    CounterTip = "Khắc tinh hoàn hảo là Nấm Thôi Miên, Bom Anh Đào hoặc Bí Đè dứt điểm tức thì."
                };
            case "DancingZombie":
                return new ZombieDetailInfo
                {
                    Toughness = "Khá (500)",
                    Speed = "Theo nhịp",
                    Description = "Ngôi sao vũ đạo của thế giới xác sống. Luôn triệu hồi 4 vũ công phụ họa vây quanh theo từng nhịp nhảy.",
                    CounterTip = "Hạ gục nhanh bằng đạn nổ diện rộng trước khi gã kịp tái triệu hồi vũ công."
                };
            case "BackupDancer":
                return new ZombieDetailInfo
                {
                    Toughness = "Cơ bản (200)",
                    Speed = "Theo nhịp",
                    Description = "Đội nhảy hỗ trợ của Zombie Vũ Công, liên tục trồi lên từ mặt cỏ để bảo vệ đội trưởng.",
                    CounterTip = "Sát thương lan hoặc Chông gai dưới đất sẽ quét sạch dễ dàng."
                };
            case "DolphinRiderZombie":
                return new ZombieDetailInfo
                {
                    Toughness = "Cao (500)",
                    Speed = "Rất nhanh",
                    Description = "Lướt trên lưng chú cá heo dưới nước và phi thân nhảy qua loài thực vật thủy sinh đầu tiên.",
                    CounterTip = "Dùng Rong bèo hoặc Lá súng làm mồi nhử để hắn mất cá heo sớm."
                };
            case "SnorkelZombie":
                return new ZombieDetailInfo
                {
                    Toughness = "Cơ bản (200)",
                    Speed = "Bình thường",
                    Description = "Đeo ống thở lặn sâu dưới mặt nước để né tránh mọi phát đạn thẳng hàng. Chỉ ngoi lên khi cắn cây.",
                    CounterTip = "Dùng Rong biển bắt dìm xuống nước hoặc máy ném bắp cải bắn từ trên cao."
                };
            case "Zomboni":
                return new ZombieDetailInfo
                {
                    Toughness = "Khủng (1350)",
                    Speed = "Bình thường",
                    Description = "Cỗ máy ủi băng nghiền nát mọi cây trồng thành bụi và để lại vệt băng lạnh không thể trồng cây.",
                    CounterTip = "Chông gai (Spikeweed) đâm nổ lốp xe lập tức, hoặc dùng Ớt Jalapeno làm tan chảy."
                };
            case "BalloonZombie":
                return new ZombieDetailInfo
                {
                    Toughness = "Cơ bản (200)",
                    Speed = "Bay lơ lửng",
                    Description = "Bay lơ lửng trên không trung né tránh hầu hết các đòn tấn công mặt đất của cây trồng.",
                    CounterTip = "Dùng Cây Thổi Gió thổi bay hoặc bắn bơ (Kernel-pult) làm nổ bóng bay."
                };
            case "JackinTheBoxZombie":
                return new ZombieDetailInfo
                {
                    Toughness = "Khá (500)",
                    Speed = "Nhanh",
                    Description = "Vừa chạy vừa quay hộp nhạc hề. Bất thình lình nắp hộp bật mở và phát nổ quét sạch diện tích 3x3!",
                    CounterTip = "Cần dồn hỏa lực tiêu diệt từ xa bằng Snow Pea và súng cối trước khi kịp phát nổ."
                };
            case "Imp":
                return new ZombieDetailInfo
                {
                    Toughness = "Yếu (200)",
                    Speed = "Nhanh nhẹn",
                    Description = "Quỷ lùn nhỏ bé, tinh quái. Thường bị Gargantuar ném thẳng vào sâu trong trận địa phòng ngự.",
                    CounterTip = "Dọn dẹp nhanh chóng bằng Peashooter hoặc Chông gai Spikeweed."
                };
            case "ChineseZombie":
                return new ZombieDetailInfo
                {
                    Toughness = "Cao (600)",
                    Speed = "Nhảy cóc",
                    Description = "Cương thi huyền bí phương Đông. Di chuyển bằng những bước nhảy ma mị và bùa chú ma quái.",
                    CounterTip = "Hỏa lực dồn liên tục kết hợp làm chậm để ngăn chặn những bước nhảy."
                };
            case "Ghost":
                return new ZombieDetailInfo
                {
                    Toughness = "Khá (400)",
                    Speed = "Bay lượn",
                    Description = "Linh hồn u ám bay lượn trên sân cỏ, thoắt ẩn thoắt hiện quấy rối hàng phòng thủ.",
                    CounterTip = "Sát thương ánh sáng hoặc đạn nổ diện rộng."
                };
            case "SnowZombie":
                return new ZombieDetailInfo
                {
                    Toughness = "Cao (750)",
                    Speed = "Chậm",
                    Description = "Chiến binh băng tuyết mang lớp khiên tinh thể lạnh giá, kháng hoàn toàn hiệu ứng làm chậm.",
                    CounterTip = "Dùng các loại cây hệ Hỏa như Gốc Đuốc (Torchwood) hoặc Ớt Jalapeno để làm tan giáp."
                };
            case "BoneZombie":
                return new ZombieDetailInfo
                {
                    Toughness = "Cao (900)",
                    Speed = "Chậm",
                    Description = "Bộ xương hóa thạch rắn chắc, không hề biết đau đớn hay sợ hãi.",
                    CounterTip = "Hỏa lực đập dập như Bí Đè hoặc Máy ném dưa hấu."
                };
            case "IceBlockZombie":
                return new ZombieDetailInfo
                {
                    Toughness = "Rất cao (1100)",
                    Speed = "Rất chậm",
                    Description = "Bị đóng băng hoàn toàn trong tảng đá lạnh khổng lồ, trở thành lá chắn di động chịu đòn cực tốt.",
                    CounterTip = "Cần đạn lửa để phá vỡ khối băng bên ngoài nhanh nhất có thể."
                };
            case "YetiZombie":
                return new ZombieDetailInfo
                {
                    Toughness = "Khủng (1350)",
                    Speed = "Bình thường",
                    Description = "Sinh vật thần bí xuất hiện chớp nhoáng rồi tìm đường tẩu thoát. Tiêu diệt gã sẽ đem lại phần thưởng lớn!",
                    CounterTip = "Đóng băng bằng Nấm Băng Hàn và dồn toàn lực hạ gục trước khi trốn thoát."
                };
            case "GatlingZombie":
                return new ZombieDetailInfo
                {
                    Toughness = "Rất cao (1200)",
                    Speed = "Chậm",
                    Description = "Zombie lai tạo mang tháp súng Gatling Repeater trên đầu! Vừa di chuyển vừa nã đạn đậu ngược vào hàng phòng thủ.",
                    CounterTip = "Dùng Quả Óc Chó hoặc Bí Đè để cản đường và dồn hỏa lực tầm xa tiêu diệt nhanh."
                };
            case "ConeBucketZombie":
                return new ZombieDetailInfo
                {
                    Toughness = "Siêu cấp (1850)",
                    Speed = "Chậm",
                    Description = "Sự kết hợp phòng ngự tối thượng giữa Nón Giao Thông và Xô Sắt. Lớp giáp 2 tầng thách thức mọi loại đạn.",
                    CounterTip = "Dùng Nam châm hút bớt xô sắt, hoặc Nấm thôi miên (Hypno-shroom) để biến thành cỗ xe tăng bảo vệ phe ta."
                };
            case "FireImpZombie":
                return new ZombieDetailInfo
                {
                    Toughness = "Khá (320)",
                    Speed = "Siêu tốc",
                    Description = "Quỷ lùn Imp được bọc hào quang lửa rực cháy. Chạy cực nhanh và khi bị hạ gục sẽ phát nổ thiêu đốt ô đất!",
                    CounterTip = "Làm nguội bằng Snow Pea hoặc bẫy bằng Quả Óc Chó từ xa."
                };
            default:
                return new ZombieDetailInfo
                {
                    Toughness = "Trung bình",
                    Speed = "Bình thường",
                    Description = "Quân đoàn zombie khát não luôn tìm mọi cách đột nhập vào ngôi nhà của bạn.",
                    CounterTip = "Bố trí trận địa phòng thủ đa tầng để ngăn chặn."
                };
        }
    }

    #endregion
}
