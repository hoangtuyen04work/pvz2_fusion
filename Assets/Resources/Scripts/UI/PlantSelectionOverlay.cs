using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class PlantLoadoutEntry
{
    public readonly string Key;
    public readonly string PlantName;
    public readonly string DisplayName;
    public readonly string IconPath;
    public readonly int Cost;
    public readonly float Cooldown;

    public PlantLoadoutEntry(string key, string plantName, string displayName, string iconPath, int cost, float cooldown)
    {
        Key = key;
        PlantName = plantName;
        DisplayName = displayName;
        IconPath = iconPath;
        Cost = cost;
        Cooldown = cooldown;
    }
}

/// <summary>
/// Xếp thẻ theo kích thước ảnh thực và tự xuống hàng, không phụ thuộc các ô cố định trên nền.
/// </summary>
public sealed class AdaptiveCardFlowLayout : LayoutGroup
{
    public float horizontalSpacing = 18f;
    public float verticalSpacing = 18f;

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
        SetLayoutInputForAxis(padding.horizontal, padding.horizontal, -1f, 0);
    }

    public override void CalculateLayoutInputVertical()
    {
        float height = MeasureAndArrange(false);
        SetLayoutInputForAxis(height, height, -1f, 1);
    }

    public override void SetLayoutHorizontal() => MeasureAndArrange(true);
    public override void SetLayoutVertical() => MeasureAndArrange(true);

    private float MeasureAndArrange(bool arrange)
    {
        float availableWidth = Mathf.Max(1f, rectTransform.rect.width - padding.horizontal);
        float x = padding.left;
        float y = padding.top;
        float rowHeight = 0f;

        foreach (RectTransform child in rectChildren)
        {
            float width = Mathf.Max(1f, LayoutUtility.GetPreferredWidth(child));
            float height = Mathf.Max(1f, LayoutUtility.GetPreferredHeight(child));
            if (x > padding.left && x + width > padding.left + availableWidth)
            {
                x = padding.left;
                y += rowHeight + verticalSpacing;
                rowHeight = 0f;
            }

            if (arrange)
            {
                SetChildAlongAxis(child, 0, x, width);
                SetChildAlongAxis(child, 1, y, height);
            }
            x += width + horizontalSpacing;
            rowHeight = Mathf.Max(rowHeight, height);
        }

        return y + rowHeight + padding.bottom;
    }
}

public static class PlantLoadoutCatalog
{
    public const int MaxSelected = 6;

    public static readonly PlantLoadoutEntry[] All =
    {
        new PlantLoadoutEntry("SunFlower", "SunFlower", "Sunflower", "Sprites/Plants/SunFlower", 50, 7.5f),
        new PlantLoadoutEntry("PeaShooter", "PeaShooterSingle", "Peashooter", "Sprites/Plants/PeaShooterSingle", 100, 7.5f),
        new PlantLoadoutEntry("WallNut", "WallNut", "Wall-nut", "Sprites/Plants/WallNut", 50, 30f),
        new PlantLoadoutEntry("Squash", "Squash", "Squash", "Sprites/Plants/Squash", 50, 30f),
        new PlantLoadoutEntry("TorchWood", "TorchWood", "Torchwood", "Sprites/Plants/Torchwood", 175, 7.5f),
        new PlantLoadoutEntry("MiaoMiao", "MiaoMiao", "Miao Miao", "Sprites/Plants/MiaoMiao", 200, 7.5f),
        new PlantLoadoutEntry("SnowKing", "SnowKing", "Snow King", "Sprites/Plants/SnowKing", 275, 7.5f),
        new PlantLoadoutEntry("SunNut", "SunNut", "Sun-nut", "Sprites/Plants/SunNut/States/SunNut0", 125, 7.5f),
        new PlantLoadoutEntry("RepeaterPea", "RepeaterPea", "Repeater", "", 200, 7.5f),
        new PlantLoadoutEntry("SnowPea", "SnowPea", "Snow Pea", "", 175, 7.5f),
        new PlantLoadoutEntry("Threepeater", "Threepeater", "Threepeater", "", 325, 7.5f),
        new PlantLoadoutEntry("CherryBomb", "CherryBomb", "Cherry Bomb", "", 150, 50f),
        new PlantLoadoutEntry("PotatoMine", "PotatoMine", "Potato Mine", "", 25, 30f),
        new PlantLoadoutEntry("Chomper", "Chomper", "Chomper", "", 150, 7.5f),
        new PlantLoadoutEntry("PuffShroom", "PuffShroom", "Puff-shroom", "", 0, 7.5f),
        new PlantLoadoutEntry("SunShroom", "SunShroom", "Sun-shroom", "", 25, 7.5f),
        new PlantLoadoutEntry("ScaredyShroom", "ScaredyShroom", "Scaredy-shroom", "", 25, 7.5f),
        new PlantLoadoutEntry("HypnoShroom", "HypnoShroom", "Hypno-shroom", "", 75, 30f),
        new PlantLoadoutEntry("IceShroom", "IceShroom", "Ice-shroom", "", 75, 50f),
        new PlantLoadoutEntry("Jalapeno", "Jalapeno", "Jalapeno", "", 125, 50f),
        new PlantLoadoutEntry("Spikeweed", "Spikeweed", "Spikeweed", "", 100, 7.5f),
        new PlantLoadoutEntry("CabbagePult", "CabbagePult", "Cabbage-pult", "", 100, 7.5f),
        new PlantLoadoutEntry("KernelPult", "KernelPult", "Kernel-pult", "", 100, 7.5f),
        new PlantLoadoutEntry("MelonPult", "MelonPult", "Melon-pult", "", 300, 7.5f),
        new PlantLoadoutEntry("UmbrellaLeaf", "UmbrellaLeaf", "Umbrella Leaf", "", 100, 7.5f)
    };

    public static bool TryGet(string key, out PlantLoadoutEntry entry)
    {
        foreach (var candidate in All)
            if (string.Equals(candidate.Key, key, StringComparison.OrdinalIgnoreCase)) { entry=candidate; return true; }
        entry=null; return false;
    }

    public static bool IsSelectionChoice(string key)
    {
        return !string.Equals(key, "SunNut", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class PlantSelectionOverlay : MonoBehaviour
{
    private static Sprite roundedUiSprite;
    private readonly List<string> selected = new List<string>();
    private readonly Dictionary<string, SeedPacketView> packets = new Dictionary<string, SeedPacketView>();
    private Text status;
    private Button confirm;
    private Transform selectedBank;
    private Action onConfirmed;
    private bool allowCancel = true;
    private string titleOverride;
    private string confirmLabel = "PLAY";

    public static void Show(int level, Action onConfirmed)
    {
        Show(level, onConfirmed, true, null, "PLAY");
    }

    public static void ShowAlmanac()
    {
        PlantAlmanacOverlay.Show();
    }

    public static void Show(int level, Action onConfirmed, bool allowCancel, string title, string confirmText)
    {
        if (FindAnyObjectByType<PlantSelectionOverlay>() != null) return;
        var root = new GameObject("Plant Selection", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(PlantSelectionOverlay));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1672f, 941f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        PlantSelectionOverlay overlay = root.GetComponent<PlantSelectionOverlay>();
        overlay.allowCancel = allowCancel;
        overlay.titleOverride = title;
        overlay.confirmLabel = string.IsNullOrWhiteSpace(confirmText) ? "PLAY" : confirmText;
        overlay.Build(level, onConfirmed);
    }

    private void Build(int level, Action callback)
    {
        onConfirmed = callback;
        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        var shade = ImageObject("Shade", transform, null, new Color(0f, 0f, 0f, 0.88f));
        Stretch(shade.rectTransform);
        Sprite panelSprite = Resources.Load<Sprite>("Prefabs/UI_management_list_plant");
        var panel = ImageObject("Panel", transform, panelSprite,
            panelSprite != null ? Color.white : new Color(0.12f, 0.18f, 0.07f, 0.98f));
        Anchor(panel.rectTransform, 0.01f, 0.01f, 0.99f, 0.99f);
        var panelAspect = panel.gameObject.AddComponent<AspectRatioFitter>();
        panelAspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        panelAspect.aspectRatio = 1672f / 941f;
        panel.raycastTarget = true;

        var title = TextObject("Title", panel.transform,
            string.IsNullOrWhiteSpace(titleOverride) ? "CHỌN CÂY VÀO TRẬN" : titleOverride,
            34, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.72f, 1f));
        title.resizeTextForBestFit = true;
        title.resizeTextMinSize = 20;
        title.resizeTextMaxSize = 36;
        title.fontStyle = FontStyle.Bold;
        AddTextShadow(title.gameObject, new Color(0.24f, 0.10f, 0.02f, 0.95f), new Vector2(2f, -2f));
        Anchor(title.rectTransform, 0.345f, 0.865f, 0.645f, 0.970f);

        if (allowCancel)
        {
            var back = ButtonObject("Back", panel.transform, "TRỞ VỀ", () => Destroy(gameObject));
            var backImg = back.GetComponent<Image>();
            Sprite btnSprite = Resources.Load<Sprite>("GameUI/button1");
            if (btnSprite != null)
            {
                backImg.sprite = btnSprite;
                backImg.type = Image.Type.Sliced;
                backImg.color = Color.white;
                var backBtn = back.GetComponent<Button>();
                backBtn.transition = Selectable.Transition.SpriteSwap;
                Sprite btnHigh = Resources.Load<Sprite>("GameUI/button2");
                SpriteState st = backBtn.spriteState;
                st.highlightedSprite = btnHigh;
                st.pressedSprite = btnHigh;
                st.selectedSprite = btnHigh;
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

        var bank = ImageObject("Selected Seed Bank", panel.transform, null, Color.clear);
        Anchor(bank.rectTransform, 0.165f, 0.660f, 0.835f, 0.815f);
        var selectedObject = new GameObject("Selected Cards", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        selectedObject.transform.SetParent(bank.transform, false);
        Stretch(selectedObject.GetComponent<RectTransform>());
        var selectedLayout = selectedObject.GetComponent<HorizontalLayoutGroup>();
        selectedLayout.spacing = 30f;
        selectedLayout.childAlignment = TextAnchor.MiddleCenter;
        selectedLayout.childControlWidth = selectedLayout.childControlHeight = false;
        selectedLayout.childForceExpandWidth = selectedLayout.childForceExpandHeight = false;
        selectedBank = selectedObject.transform;

        var hint = TextObject("Hint", panel.transform, "CHỌN TỐI ĐA 6 CÂY", 18, TextAnchor.MiddleCenter, new Color(0.40f, 0.22f, 0.08f, 0.90f));
        hint.fontStyle = FontStyle.Bold;
        Anchor(hint.rectTransform, 0.35f, 0.620f, 0.65f, 0.655f);

        var scrollObject = new GameObject("Plant Scroll View", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
        scrollObject.transform.SetParent(panel.transform, false);
        Anchor(scrollObject.GetComponent<RectTransform>(), 0.042f, 0.192f, 0.950f, 0.586f);
        scrollObject.GetComponent<Image>().color = Color.clear;

        var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
        viewportObject.transform.SetParent(scrollObject.transform, false);
        var viewport = viewportObject.GetComponent<RectTransform>();
        Anchor(viewport, 0f, 0f, 0.915f, 1f);
        // Artwork có sẵn khung gỗ rỗng; graphic gần trong suốt cho RectMask2D hoạt động mượt mà.
        viewportObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.001f);

        var gridObject = new GameObject("Plant Flow", typeof(RectTransform), typeof(AdaptiveCardFlowLayout), typeof(ContentSizeFitter));
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

        // Scrollbar ăn khớp hoàn hảo vào rãnh trượt bên phải của bảng gỗ artwork
        var scrollbarObject = new GameObject("Scrollbar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Scrollbar));
        scrollbarObject.transform.SetParent(scrollObject.transform, false);
        Anchor(scrollbarObject.GetComponent<RectTransform>(), 0.925f, 0.08f, 0.952f, 0.92f);
        scrollbarObject.GetComponent<Image>().color = Color.clear;

        var slidingArea = new GameObject("Sliding Area", typeof(RectTransform));
        slidingArea.transform.SetParent(scrollbarObject.transform, false);
        Stretch(slidingArea.GetComponent<RectTransform>());

        Sprite roundedSprite = GetRoundedUiSprite();
        var track = ImageObject("Wood Groove Track", slidingArea.transform, roundedSprite,
            new Color(0.18f, 0.08f, 0.03f, 0.55f));
        Stretch(track.rectTransform);
        track.type = Image.Type.Sliced;
        track.raycastTarget = false;

        var handle = ImageObject("Wooden Handle", slidingArea.transform, roundedSprite,
            new Color(0.96f, 0.74f, 0.22f, 0.95f));
        Stretch(handle.rectTransform);
        handle.type = Image.Type.Sliced;

        var scrollbar = scrollbarObject.GetComponent<Scrollbar>();
        scrollbar.handleRect = handle.rectTransform;
        scrollbar.targetGraphic = handle;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        var scrollRect = scrollObject.GetComponent<ScrollRect>();
        scrollRect.viewport = viewport;
        scrollRect.content = content;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.inertia = true;
        scrollRect.decelerationRate = 0.12f;
        scrollRect.scrollSensitivity = 36f;
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        scrollRect.verticalNormalizedPosition = 1f;

        foreach (var entry in PlantLoadoutCatalog.All)
            if (PlantLoadoutCatalog.IsSelectionChoice(entry.Key))
                CreateChoice(entry, gridObject.transform);

        // Bảng trạng thái góc dưới bên trái (khung giấy kem có đinh tán)
        status = TextObject("Status", panel.transform, string.Empty, 24, TextAnchor.MiddleCenter,
            new Color(0.24f, 0.10f, 0.02f, 1f));
        status.fontStyle = FontStyle.Bold;
        Anchor(status.rectTransform, 0.066f, 0.038f, 0.254f, 0.113f);

        // Nút Xác nhận / Chơi góc dưới bên phải (nút bấm màu xanh viền gỗ)
        confirm = ButtonObject("Confirm", panel.transform, confirmLabel, Confirm).GetComponent<Button>();
        confirm.GetComponent<Image>().color = Color.clear;
        var confirmMotion = confirm.gameObject.AddComponent<MenuButtonMotion>();
        var confirmText = confirm.GetComponentInChildren<Text>();
        if (confirmText != null)
        {
            confirmText.fontSize = 36;
            confirmText.color = new Color(1f, 0.98f, 0.82f, 1f);
            confirmText.fontStyle = FontStyle.Bold;
            AddTextShadow(confirmText.gameObject, new Color(0.12f, 0.32f, 0.06f, 0.95f), new Vector2(2.2f, -2.2f));
            confirmMotion.targetGraphic = confirmText;
        }
        Anchor(confirm.GetComponent<RectTransform>(), 0.745f, 0.028f, 0.960f, 0.155f);

        foreach (string key in GameSession.SelectedPlants)
            if (selected.Count < PlantLoadoutCatalog.MaxSelected && PlantLoadoutCatalog.IsSelectionChoice(key) &&
                PlantLoadoutCatalog.TryGet(key, out _) && !selected.Contains(key))
                selected.Add(key);
        UpdateState();
    }

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
        texture.Apply(false, true);
        roundedUiSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size),
            new Vector2(.5f, .5f), 100f, 0u, SpriteMeshType.FullRect,
            new Vector4(7f, 7f, 7f, 7f));
        roundedUiSprite.name = "Runtime Rounded UI Sprite";
        roundedUiSprite.hideFlags = HideFlags.HideAndDontSave;
        return roundedUiSprite;
    }

    private void CreateChoice(PlantLoadoutEntry entry, Transform parent)
    {
        packets.Add(entry.Key,SeedPacketFactory.CreateSelectionCard(entry,parent,150f,()=>Toggle(entry.Key)));
    }

    private void Toggle(string key)
    {
        if (!PlantLoadoutCatalog.IsSelectionChoice(key)) return;
        if (selected.Contains(key)) selected.Remove(key);
        else if (selected.Count < PlantLoadoutCatalog.MaxSelected) selected.Add(key);
        UpdateState();
    }

    private void UpdateState()
    {
        foreach(var pair in packets) pair.Value.SetSelected(selected.Contains(pair.Key));
        for(int i=selectedBank.childCount-1;i>=0;i--) Destroy(selectedBank.GetChild(i).gameObject);
        foreach(string key in selected)
            if(PlantLoadoutCatalog.TryGet(key,out var entry)) SeedPacketFactory.CreateSelectionCard(entry,selectedBank,112f,()=>Toggle(key));
        status.text = "ĐÃ CHỌN  " + selected.Count + "/" + PlantLoadoutCatalog.MaxSelected;
        confirm.interactable = onConfirmed == null || selected.Count > 0;
    }

    private void Confirm()
    {
        if (onConfirmed == null)
        {
            if (selected.Count > 0)
            {
                GameSession.SelectedPlants.Clear();
                GameSession.SelectedPlants.AddRange(selected);
            }
            Destroy(gameObject);
            return;
        }
        if (selected.Count == 0) return;
        GameSession.SelectedPlants.Clear();
        GameSession.SelectedPlants.AddRange(selected);
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
        text.resizeTextMinSize = 12;
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

    private static void Stretch(RectTransform rect) => Anchor(rect, 0f, 0f, 1f, 1f);

    private static void Anchor(RectTransform rect, float x0, float y0, float x1, float y1)
    {
        rect.anchorMin = new Vector2(x0, y0);
        rect.anchorMax = new Vector2(x1, y1);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
