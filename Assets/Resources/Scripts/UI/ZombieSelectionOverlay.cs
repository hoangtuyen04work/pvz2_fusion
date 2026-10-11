using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Giao diện chọn 6 Zombie cho phe Zombie trong chế độ Kết hợp Đối kháng (PvP).
/// Đồng bộ hoàn hảo phong cách chọn quân giống với PlantSelectionOverlay:
/// - Khung panel PvZ cổ điển
/// - Dãy 6 ô thẻ zombie đã chọn ở trên (Selected Bank)
/// - Danh sách cuộn toàn bộ zombie có thể chọn ở dưới (Scroll View)
/// - Giới hạn chọn chính xác 6 zombie, bấm thẻ để chọn / bỏ chọn
/// - Nút "SẴN SÀNG" / "XÁC NHẬN" để bước vào trận đấu
/// </summary>
public sealed class ZombieSelectionOverlay : MonoBehaviour
{
    public const int MaxSelected = 6;

    private static Sprite cachedDialogMain;
    private static Sprite cachedDialogChild;
    private static Sprite cachedButton1;
    private static Sprite cachedButton2;
    private static Sprite cachedCardFrame;

    private readonly List<string> selected = new List<string>();
    private readonly Dictionary<string, ZombieSelectionCardView> cards = new Dictionary<string, ZombieSelectionCardView>(StringComparer.OrdinalIgnoreCase);

    private Transform selectedBank;
    private Text statusText;
    private Button confirmButton;
    private Action onConfirmed;
    private bool allowCancel;
    private string titleText;
    private string confirmLabel;

    private sealed class ZombieSelectionCardView
    {
        public ZombieRoster.Entry Entry;
        public GameObject Root;
        public Image Frame;
        public GameObject CheckMark;
        public GameObject SelectionGlow;

        public void SetSelected(bool isSelected)
        {
            if (CheckMark != null) CheckMark.SetActive(isSelected);
            if (SelectionGlow != null) SelectionGlow.SetActive(isSelected);
            if (Frame != null)
            {
                Frame.color = isSelected
                    ? new Color(0.98f, 0.85f, 0.40f, 1f)
                    : new Color(0.85f, 0.80f, 0.74f, 1f);
            }
        }
    }

    public static void Show(Action onConfirmed, bool allowCancel = true, string title = "CHỌN 6 ZOMBIE VÀO TRẬN", string confirmText = "SẴN SÀNG")
    {
        if (FindAnyObjectByType<ZombieSelectionOverlay>() != null) return;

        var root = new GameObject("Zombie Selection", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(ZombieSelectionOverlay));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // This overlay can also be opened from NetLobbyUI (sorting order 400).
        // Keep the loadout picker above the lobby so its cards remain visible
        // and receive pointer events while the player is preparing a team.
        canvas.sortingOrder = 500;

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1672f, 941f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        var overlay = root.GetComponent<ZombieSelectionOverlay>();
        overlay.allowCancel = allowCancel;
        overlay.titleText = title;
        overlay.confirmLabel = confirmText;
        overlay.Build(onConfirmed);
    }

    private static void EnsureAssets()
    {
        if (cachedDialogMain == null) cachedDialogMain = Resources.Load<Sprite>("GameUI/dialog_main");
        if (cachedDialogChild == null) cachedDialogChild = Resources.Load<Sprite>("GameUI/dialog_child");
        if (cachedButton1 == null) cachedButton1 = Resources.Load<Sprite>("GameUI/button1");
        if (cachedButton2 == null) cachedButton2 = Resources.Load<Sprite>("GameUI/button2");
        if (cachedCardFrame == null) cachedCardFrame = Resources.Load<Sprite>("Sprites/UI/Intro/khung") ?? Resources.Load<Sprite>("Sprites/UI/Menu/button_normal");
    }

    private void Build(Action callback)
    {
        EnsureAssets();
        onConfirmed = callback;

        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        // 1. Nền mờ tối dịu mắt
        var shade = ImageObject("Shade", transform, null, new Color(0f, 0f, 0f, 0.88f));
        Stretch(shade.rectTransform);

        // 2. Panel nền danh sách (dùng panel artwork nếu có, hoặc dialog_main sliced)
        Sprite panelSprite = Resources.Load<Sprite>("Prefabs/UI_management_list_plant");
        var panel = ImageObject("Panel", transform, panelSprite ?? cachedDialogMain,
            panelSprite != null ? Color.white : new Color(0.16f, 0.10f, 0.08f, 0.98f));
        Anchor(panel.rectTransform, 0.01f, 0.01f, 0.99f, 0.99f);
        var panelAspect = panel.gameObject.AddComponent<AspectRatioFitter>();
        panelAspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        panelAspect.aspectRatio = 1672f / 941f;
        panel.raycastTarget = true;

        // 3. Tiêu đề
        var title = TextObject("Title", panel.transform,
            string.IsNullOrWhiteSpace(titleText) ? "CHỌN 6 ZOMBIE VÀO TRẬN" : titleText,
            34, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.40f, 1f));
        title.resizeTextForBestFit = true;
        title.resizeTextMinSize = 20;
        title.resizeTextMaxSize = 36;
        title.fontStyle = FontStyle.Bold;
        AddTextShadow(title.gameObject, new Color(0.24f, 0.08f, 0.02f, 0.95f), new Vector2(2f, -2f));
        Anchor(title.rectTransform, 0.345f, 0.865f, 0.645f, 0.970f);

        // Nút Trở về nếu được phép huỷ
        if (allowCancel)
        {
            var back = ButtonObject("Back", panel.transform, "TRỞ VỀ", () => Destroy(gameObject));
            var backImg = back.GetComponent<Image>();
            if (cachedButton1 != null)
            {
                backImg.sprite = cachedButton1;
                backImg.type = Image.Type.Sliced;
                backImg.color = Color.white;
                var backBtn = back.GetComponent<Button>();
                backBtn.transition = Selectable.Transition.SpriteSwap;
                SpriteState st = backBtn.spriteState;
                st.highlightedSprite = cachedButton2;
                st.pressedSprite = cachedButton2;
                st.selectedSprite = cachedButton2;
                backBtn.spriteState = st;
            }
            else
            {
                backImg.color = Color.clear;
            }

            var backMotion = back.AddComponent<MenuButtonMotion>();
            var backText = back.GetComponentInChildren<Text>();
            if (backText != null)
            {
                backText.fontSize = 22;
                backText.color = new Color(1f, 0.95f, 0.72f, 1f);
                backText.fontStyle = FontStyle.Bold;
                AddTextShadow(backText.gameObject, new Color(0.24f, 0.10f, 0.02f, 0.95f), new Vector2(1.8f, -1.8f));
                backMotion.targetGraphic = backImg;
            }
            Anchor(back.GetComponent<RectTransform>(), 0.048f, 0.850f, 0.145f, 0.945f);
        }

        // 4. Khay chứa 6 Zombie đã chọn ở phía trên (Selected Zombie Bank)
        var bank = ImageObject("Selected Zombie Bank", panel.transform, null, Color.clear);
        Anchor(bank.rectTransform, 0.165f, 0.660f, 0.835f, 0.815f);
        var selectedObject = new GameObject("Selected Cards", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        selectedObject.transform.SetParent(bank.transform, false);
        Stretch(selectedObject.GetComponent<RectTransform>());
        var selectedLayout = selectedObject.GetComponent<HorizontalLayoutGroup>();
        selectedLayout.spacing = 26f;
        selectedLayout.childAlignment = TextAnchor.MiddleCenter;
        selectedLayout.childControlWidth = selectedLayout.childControlHeight = false;
        selectedLayout.childForceExpandWidth = selectedLayout.childForceExpandHeight = false;
        selectedBank = selectedObject.transform;

        var hint = TextObject("Hint", panel.transform, "CHỌN TỐI ĐA 6 ZOMBIE", 18, TextAnchor.MiddleCenter, new Color(0.42f, 0.20f, 0.08f, 0.90f));
        hint.fontStyle = FontStyle.Bold;
        Anchor(hint.rectTransform, 0.35f, 0.620f, 0.65f, 0.655f);

        // 5. Danh sách cuộn toàn bộ Zombie để lựa chọn (Scroll View)
        var scrollObject = new GameObject("Zombie Scroll View", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
        scrollObject.transform.SetParent(panel.transform, false);
        Anchor(scrollObject.GetComponent<RectTransform>(), 0.042f, 0.192f, 0.950f, 0.586f);
        scrollObject.GetComponent<Image>().color = Color.clear;

        var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
        viewportObject.transform.SetParent(scrollObject.transform, false);
        var viewport = viewportObject.GetComponent<RectTransform>();
        Anchor(viewport, 0f, 0f, 0.915f, 1f);
        viewportObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.001f);

        var gridObject = new GameObject("Zombie Flow", typeof(RectTransform), typeof(AdaptiveCardFlowLayout), typeof(ContentSizeFitter));
        gridObject.transform.SetParent(viewportObject.transform, false);
        var content = gridObject.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;

        var flow = gridObject.GetComponent<AdaptiveCardFlowLayout>();
        flow.padding = new RectOffset(16, 16, 16, 16);
        flow.horizontalSpacing = 16f;
        flow.verticalSpacing = 16f;
        gridObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Thanh cuộn Scrollbar
        var scrollbarObject = new GameObject("Scrollbar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Scrollbar));
        scrollbarObject.transform.SetParent(scrollObject.transform, false);
        Anchor(scrollbarObject.GetComponent<RectTransform>(), 0.925f, 0.08f, 0.952f, 0.92f);
        scrollbarObject.GetComponent<Image>().color = Color.clear;

        var slidingArea = new GameObject("Sliding Area", typeof(RectTransform));
        slidingArea.transform.SetParent(scrollbarObject.transform, false);
        Stretch(slidingArea.GetComponent<RectTransform>());

        var handle = ImageObject("Handle", slidingArea.transform, cachedButton1, Color.white);
        handle.type = Image.Type.Sliced;
        var scrollbar = scrollbarObject.GetComponent<Scrollbar>();
        scrollbar.handleRect = handle.rectTransform;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        var scrollRect = scrollObject.GetComponent<ScrollRect>();
        scrollRect.viewport = viewport;
        scrollRect.content = content;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.12f;
        scrollRect.scrollSensitivity = 38f;
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        scrollRect.verticalNormalizedPosition = 1f;

        // Tạo thẻ cho toàn bộ danh sách zombie
        foreach (ZombieRoster.Entry entry in ZombieRoster.All)
        {
            CreateChoiceCard(entry, gridObject.transform);
        }

        // 6. Bảng trạng thái góc dưới bên trái
        statusText = TextObject("Status", panel.transform, string.Empty, 24, TextAnchor.MiddleCenter,
            new Color(0.24f, 0.10f, 0.02f, 1f));
        statusText.fontStyle = FontStyle.Bold;
        Anchor(statusText.rectTransform, 0.066f, 0.038f, 0.254f, 0.113f);

        // 7. Nút Xác nhận / Chơi góc dưới bên phải
        confirmButton = ButtonObject("Confirm", panel.transform, confirmLabel, Confirm).GetComponent<Button>();
        confirmButton.GetComponent<Image>().color = Color.clear;
        var confirmMotion = confirmButton.gameObject.AddComponent<MenuButtonMotion>();
        var confirmTextComponent = confirmButton.GetComponentInChildren<Text>();
        if (confirmTextComponent != null)
        {
            confirmTextComponent.fontSize = 36;
            confirmTextComponent.color = new Color(1f, 0.98f, 0.82f, 1f);
            confirmTextComponent.fontStyle = FontStyle.Bold;
            AddTextShadow(confirmTextComponent.gameObject, new Color(0.12f, 0.32f, 0.06f, 0.95f), new Vector2(2.2f, -2.2f));
            confirmMotion.targetGraphic = confirmTextComponent;
        }
        Anchor(confirmButton.GetComponent<RectTransform>(), 0.745f, 0.028f, 0.960f, 0.155f);

        // Tải các thẻ zombie đã lưu trước đó nếu có
        if (NetSession.SelectedZombies != null && NetSession.SelectedZombies.Count > 0)
        {
            foreach (string name in NetSession.SelectedZombies)
            {
                if (selected.Count < MaxSelected && !selected.Contains(name) && IsValidZombie(name))
                    selected.Add(name);
            }
        }

        // Mặc định chọn trước một số zombie cơ bản nếu danh sách rỗng
        if (selected.Count == 0)
        {
            string[] defaults = { "ZombieNormal", "ConeZombie", "ChineseZombie", "BucketZombie", "PoleVaultingZombie", "FootballZombie" };
            foreach (string d in defaults)
            {
                if (selected.Count < MaxSelected && IsValidZombie(d))
                    selected.Add(d);
            }
        }

        UpdateState();
    }

    private static bool IsValidZombie(string name)
    {
        foreach (var entry in ZombieRoster.All)
        {
            if (entry.name.Equals(name, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private void CreateChoiceCard(ZombieRoster.Entry entry, Transform parent)
    {
        var cardGO = new GameObject("Choice_" + entry.name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(MenuButtonMotion));
        cardGO.transform.SetParent(parent, false);

        Vector2 size = new Vector2(96f, 128f);
        var rect = cardGO.GetComponent<RectTransform>();
        rect.sizeDelta = size;

        var layout = cardGO.GetComponent<LayoutElement>();
        layout.minWidth = layout.preferredWidth = size.x;
        layout.minHeight = layout.preferredHeight = size.y;

        var cardImg = cardGO.GetComponent<Image>();
        cardImg.sprite = cachedCardFrame;
        cardImg.type = Image.Type.Sliced;
        cardImg.color = new Color(0.85f, 0.80f, 0.74f, 1f);

        var btn = cardGO.GetComponent<Button>();
        btn.transition = Selectable.Transition.None;
        string captured = entry.name;
        btn.onClick.AddListener(() => Toggle(captured));

        var motion = cardGO.GetComponent<MenuButtonMotion>();
        motion.targetGraphic = cardImg;

        // Glow
        var glowGO = new GameObject("Glow", typeof(RectTransform), typeof(Image), typeof(Outline));
        glowGO.transform.SetParent(cardGO.transform, false);
        Stretch(glowGO.GetComponent<RectTransform>());
        var glowImg = glowGO.GetComponent<Image>();
        glowImg.color = new Color(1f, 0.60f, 0.15f, 0.25f);
        glowImg.raycastTarget = false;
        var glowOutline = glowGO.GetComponent<Outline>();
        glowOutline.effectColor = new Color(1f, 0.60f, 0.15f, 0.95f);
        glowOutline.effectDistance = new Vector2(2f, -2f);
        glowGO.SetActive(false);

        // Icon Zombie
        var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(cardGO.transform, false);
        Anchor(iconGO.GetComponent<RectTransform>(), 0.08f, 0.28f, 0.92f, 0.92f);
        var iconImg = iconGO.GetComponent<Image>();
        iconImg.sprite = ZombieIconHelper.GetIcon(entry.name);
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;

        // Tên Zombie
        var nameText = TextObject("Name", cardGO.transform, entry.label, 12, TextAnchor.MiddleCenter, new Color(0.24f, 0.08f, 0.08f, 1f));
        nameText.fontStyle = FontStyle.Bold;
        Anchor(nameText.rectTransform, 0.02f, 0.14f, 0.98f, 0.28f);
        nameText.raycastTarget = false;

        // Giá Não
        var costText = TextObject("Cost", cardGO.transform, "🧠 " + entry.cost, 12, TextAnchor.MiddleCenter, new Color(0.85f, 0.25f, 0.15f, 1f));
        costText.fontStyle = FontStyle.Bold;
        Anchor(costText.rectTransform, 0.02f, 0.02f, 0.98f, 0.14f);
        costText.raycastTarget = false;

        // Dấu tích chọn (Checkmark)
        var markGO = new GameObject("SelectedMark", typeof(RectTransform), typeof(Image));
        markGO.transform.SetParent(cardGO.transform, false);
        Stretch(markGO.GetComponent<RectTransform>(), 0.03f);
        var markImg = markGO.GetComponent<Image>();
        markImg.color = new Color(0.22f, 0.72f, 0.16f, 0.35f);
        markImg.raycastTarget = false;

        var tickText = TextObject("Tick", markGO.transform, "✓", 28, TextAnchor.UpperRight, Color.white);
        Anchor(tickText.rectTransform, 0.5f, 0.5f, 0.95f, 0.95f);
        tickText.fontStyle = FontStyle.Bold;
        tickText.raycastTarget = false;
        markGO.SetActive(false);

        cards[entry.name] = new ZombieSelectionCardView
        {
            Entry = entry,
            Root = cardGO,
            Frame = cardImg,
            CheckMark = markGO,
            SelectionGlow = glowGO
        };
    }

    private void Toggle(string zombieName)
    {
        if (selected.Contains(zombieName))
        {
            selected.Remove(zombieName);
        }
        else if (selected.Count < MaxSelected)
        {
            selected.Add(zombieName);
        }
        UpdateState();
    }

    private void UpdateState()
    {
        foreach (var pair in cards)
        {
            pair.Value.SetSelected(selected.Contains(pair.Key));
        }

        // Cập nhật các ô trong ngân hàng thẻ phía trên
        for (int i = selectedBank.childCount - 1; i >= 0; i--)
        {
            Destroy(selectedBank.GetChild(i).gameObject);
        }

        foreach (string name in selected)
        {
            ZombieRoster.Entry entry = null;
            foreach (var e in ZombieRoster.All)
            {
                if (e.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    entry = e;
                    break;
                }
            }

            if (entry != null)
                CreateBankCard(entry, selectedBank);
        }

        statusText.text = "ĐÃ CHỌN  " + selected.Count + "/" + MaxSelected;
        confirmButton.interactable = selected.Count > 0;
    }

    private void CreateBankCard(ZombieRoster.Entry entry, Transform parent)
    {
        var cardGO = new GameObject("Bank_" + entry.name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(MenuButtonMotion));
        cardGO.transform.SetParent(parent, false);

        Vector2 size = new Vector2(80f, 106f);
        var rect = cardGO.GetComponent<RectTransform>();
        rect.sizeDelta = size;

        var layout = cardGO.GetComponent<LayoutElement>();
        layout.minWidth = layout.preferredWidth = size.x;
        layout.minHeight = layout.preferredHeight = size.y;

        var cardImg = cardGO.GetComponent<Image>();
        cardImg.sprite = cachedCardFrame;
        cardImg.type = Image.Type.Sliced;
        cardImg.color = new Color(0.96f, 0.90f, 0.80f, 1f);

        var btn = cardGO.GetComponent<Button>();
        btn.transition = Selectable.Transition.None;
        string captured = entry.name;
        btn.onClick.AddListener(() => Toggle(captured));

        var motion = cardGO.GetComponent<MenuButtonMotion>();
        motion.targetGraphic = cardImg;

        // Icon Zombie
        var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(cardGO.transform, false);
        Anchor(iconGO.GetComponent<RectTransform>(), 0.08f, 0.28f, 0.92f, 0.92f);
        var iconImg = iconGO.GetComponent<Image>();
        iconImg.sprite = ZombieIconHelper.GetIcon(entry.name);
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;

        // Tên Zombie
        var nameText = TextObject("Name", cardGO.transform, entry.label, 11, TextAnchor.MiddleCenter, new Color(0.24f, 0.08f, 0.08f, 1f));
        nameText.fontStyle = FontStyle.Bold;
        Anchor(nameText.rectTransform, 0.02f, 0.14f, 0.98f, 0.28f);
        nameText.raycastTarget = false;

        // Giá Não
        var costText = TextObject("Cost", cardGO.transform, "🧠 " + entry.cost, 11, TextAnchor.MiddleCenter, new Color(0.85f, 0.25f, 0.15f, 1f));
        costText.fontStyle = FontStyle.Bold;
        Anchor(costText.rectTransform, 0.02f, 0.02f, 0.98f, 0.14f);
        costText.raycastTarget = false;
    }

    private void Confirm()
    {
        if (selected.Count == 0) return;

        NetSession.SelectedZombies.Clear();
        NetSession.SelectedZombies.AddRange(selected);

        var callback = onConfirmed;
        Destroy(gameObject);
        callback?.Invoke();
    }

    private static GameObject ButtonObject(string name, Transform parent, string label, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.37f, 0.55f, 0.18f, 1f);
        var button = go.GetComponent<Button>();
        button.onClick.AddListener(action);
        var text = TextObject("Label", go.transform, label, 28, TextAnchor.MiddleCenter, Color.white);
        Stretch(text.rectTransform);
        text.raycastTarget = false;
        return go;
    }

    private static Image ImageObject(string name, Transform parent, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        return image;
    }

    private static Text TextObject(string name, Transform parent, string value, int size, TextAnchor alignment, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>();
        text.font = Resources.Load<Font>("Fonts/Baloo2");
        text.text = value;
        text.fontSize = size;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 10;
        text.resizeTextMaxSize = size;
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

    private static void Stretch(RectTransform rect, float padding = 0f) => Anchor(rect, padding, padding, 1f - padding, 1f - padding);

    private static void Anchor(RectTransform rect, float x0, float y0, float x1, float y1)
    {
        rect.anchorMin = new Vector2(x0, y0);
        rect.anchorMax = new Vector2(x1, y1);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
