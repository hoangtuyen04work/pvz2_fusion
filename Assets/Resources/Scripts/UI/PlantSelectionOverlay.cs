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
        new PlantLoadoutEntry("TorchWood", "TorchWood", "Torchwood", "Sprites/Plants/TorchWood", 175, 7.5f),
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
        new PlantLoadoutEntry("Spikeweed", "Spikeweed", "Spikeweed", "", 100, 7.5f)
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

    public static void Show(int level, Action onConfirmed)
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
        root.GetComponent<PlantSelectionOverlay>().Build(level, onConfirmed);
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

        var title = TextObject("Title", panel.transform, "CHOOSE YOUR PLANTS", 40, TextAnchor.MiddleCenter, new Color(0.65f, 1f, 0.28f));
        Anchor(title.rectTransform, 0.31f, 0.875f, 0.68f, 0.985f);
        var back = ButtonObject("Back", panel.transform, "BACK", () => Destroy(gameObject));
        back.GetComponent<Image>().color = Color.clear;
        Anchor(back.GetComponent<RectTransform>(), 0.03f, 0.825f, 0.15f, 0.97f);

        var bank=ImageObject("Selected Seed Bank",panel.transform,null,Color.clear); Anchor(bank.rectTransform,.205f,.655f,.80f,.835f);
        var selectedObject=new GameObject("Selected Cards",typeof(RectTransform),typeof(HorizontalLayoutGroup)); selectedObject.transform.SetParent(bank.transform,false); Anchor(selectedObject.GetComponent<RectTransform>(),0f,.02f,1f,.98f);
        var selectedLayout=selectedObject.GetComponent<HorizontalLayoutGroup>(); selectedLayout.spacing=40f; selectedLayout.childAlignment=TextAnchor.MiddleCenter; selectedLayout.childControlWidth=selectedLayout.childControlHeight=false; selectedLayout.childForceExpandWidth=selectedLayout.childForceExpandHeight=false; selectedBank=selectedObject.transform;
        var hint=TextObject("Hint",panel.transform,"CHỌN TỐI ĐA 6 CÂY",18,TextAnchor.MiddleCenter,new Color(.97f,.91f,.69f)); Anchor(hint.rectTransform,.34f,.615f,.66f,.65f);

        var scrollObject = new GameObject("Plant Scroll View", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
        scrollObject.transform.SetParent(panel.transform, false);
        Anchor(scrollObject.GetComponent<RectTransform>(), 0.065f, 0.185f, 0.955f, 0.575f);
        scrollObject.GetComponent<Image>().color = Color.clear;

        var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
        viewportObject.transform.SetParent(scrollObject.transform, false);
        var viewport = viewportObject.GetComponent<RectTransform>();
        Anchor(viewport, 0f, 0f, 0.945f, 1f);
        // Artwork mới đã có vùng gỗ trống; chỉ giữ một graphic gần trong suốt cho RectMask2D.
        viewportObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, .001f);

        var gridObject = new GameObject("Plant Flow", typeof(RectTransform), typeof(AdaptiveCardFlowLayout), typeof(ContentSizeFitter));
        gridObject.transform.SetParent(viewportObject.transform, false);
        var content = gridObject.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;
        var flow = gridObject.GetComponent<AdaptiveCardFlowLayout>();
        flow.padding = new RectOffset(18, 18, 18, 18);
        flow.horizontalSpacing = 18f;
        flow.verticalSpacing = 18f;
        gridObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scrollbarObject = new GameObject("Scrollbar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Scrollbar));
        scrollbarObject.transform.SetParent(scrollObject.transform, false);
        Anchor(scrollbarObject.GetComponent<RectTransform>(), .955f, .02f, .995f, .98f);
        scrollbarObject.GetComponent<Image>().color = Color.clear;

        // Vùng tương tác vẫn rộng để dễ kéo, nhưng rãnh và tay kéo nhìn thấy chỉ rộng khoảng 9 px.
        var slidingArea = new GameObject("Sliding Area", typeof(RectTransform));
        slidingArea.transform.SetParent(scrollbarObject.transform, false);
        Anchor(slidingArea.GetComponent<RectTransform>(), .39f, .02f, .61f, .98f);
        Sprite roundedSprite = GetRoundedUiSprite();
        var track = ImageObject("Thin Track", slidingArea.transform, roundedSprite,
            new Color(.12f, .045f, .018f, .72f));
        Stretch(track.rectTransform);
        track.type = Image.Type.Sliced;
        track.raycastTarget = false;

        var handle = ImageObject("Rounded Handle", slidingArea.transform, roundedSprite,
            new Color(.96f, .72f, .20f, .98f));
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
        scrollRect.decelerationRate = .12f;
        scrollRect.scrollSensitivity = 32f;
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        scrollRect.verticalNormalizedPosition = 1f;

        foreach (var entry in PlantLoadoutCatalog.All)
            if (PlantLoadoutCatalog.IsSelectionChoice(entry.Key))
                CreateChoice(entry, gridObject.transform);

        status = TextObject("Status", panel.transform, string.Empty, 23, TextAnchor.MiddleLeft, Color.white);
        status.color = new Color(.28f, .12f, .035f, 1f);
        status.alignment = TextAnchor.MiddleCenter;
        Anchor(status.rectTransform, 0.07f, 0.03f, 0.26f, 0.12f);
        confirm = ButtonObject("Confirm", panel.transform, "PLAY", Confirm).GetComponent<Button>();
        confirm.GetComponent<Image>().color = Color.clear;
        Anchor(confirm.GetComponent<RectTransform>(), 0.745f, 0.025f, 0.96f, 0.155f);

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
        confirm.interactable = selected.Count > 0;
    }

    private void Confirm()
    {
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

    private static void Stretch(RectTransform rect) => Anchor(rect, 0f, 0f, 1f, 1f);

    private static void Anchor(RectTransform rect, float x0, float y0, float x1, float y1)
    {
        rect.anchorMin = new Vector2(x0, y0);
        rect.anchorMax = new Vector2(x1, y1);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
