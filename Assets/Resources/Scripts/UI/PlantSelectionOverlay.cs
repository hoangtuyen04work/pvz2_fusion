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
        scaler.referenceResolution = new Vector2(1000f, 750f);
        root.GetComponent<PlantSelectionOverlay>().Build(level, onConfirmed);
    }

    private void Build(int level, Action callback)
    {
        onConfirmed = callback;
        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        var shade = ImageObject("Shade", transform, null, new Color(0f, 0f, 0f, 0.88f));
        Stretch(shade.rectTransform);
        var panel = ImageObject("Panel", transform, null, new Color(0.12f, 0.18f, 0.07f, 0.98f));
        Anchor(panel.rectTransform, 0.06f, 0.06f, 0.94f, 0.94f);

        var title = TextObject("Title", panel.transform, "CHOOSE YOUR PLANTS", 40, TextAnchor.MiddleCenter, new Color(0.65f, 1f, 0.28f));
        Anchor(title.rectTransform, 0.08f, 0.90f, 0.92f, 0.98f);
        var back = ButtonObject("Back", panel.transform, "BACK", () => Destroy(gameObject));
        Anchor(back.GetComponent<RectTransform>(), 0.03f, 0.91f, 0.15f, 0.97f);

        var bank=ImageObject("Selected Seed Bank",panel.transform,null,new Color(.34f,.24f,.10f,1)); Anchor(bank.rectTransform,.17f,.74f,.83f,.89f);
        var selectedObject=new GameObject("Selected Cards",typeof(RectTransform),typeof(HorizontalLayoutGroup)); selectedObject.transform.SetParent(bank.transform,false); Anchor(selectedObject.GetComponent<RectTransform>(),.03f,.04f,.97f,.96f);
        var selectedLayout=selectedObject.GetComponent<HorizontalLayoutGroup>(); selectedLayout.spacing=7; selectedLayout.childAlignment=TextAnchor.MiddleCenter; selectedLayout.childControlWidth=selectedLayout.childControlHeight=false; selectedLayout.childForceExpandWidth=selectedLayout.childForceExpandHeight=false; selectedBank=selectedObject.transform;
        var hint=TextObject("Hint",panel.transform,"Pick up to 6 plants. Click a selected packet to remove it.",18,TextAnchor.MiddleCenter,new Color(.9f,.92f,.76f)); Anchor(hint.rectTransform,.12f,.69f,.88f,.74f);

        var scrollObject = new GameObject("Plant Scroll View", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
        scrollObject.transform.SetParent(panel.transform, false);
        Anchor(scrollObject.GetComponent<RectTransform>(), 0.08f, 0.17f, 0.92f, 0.69f);
        scrollObject.GetComponent<Image>().color = new Color(.24f, .18f, .08f, .92f);

        var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
        viewportObject.transform.SetParent(scrollObject.transform, false);
        var viewport = viewportObject.GetComponent<RectTransform>();
        Anchor(viewport, 0.01f, 0.025f, 0.96f, 0.975f);
        viewportObject.GetComponent<Image>().color = new Color(.11f, .17f, .065f, .95f);

        var gridObject = new GameObject("Plant Grid", typeof(RectTransform), typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        gridObject.transform.SetParent(viewportObject.transform, false);
        var content = gridObject.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;
        var grid = gridObject.GetComponent<GridLayoutGroup>();
        grid.padding = new RectOffset(8, 8, 2, 2);
        grid.spacing = new Vector2(14f, 8f);
        grid.cellSize = SeedPacketFactory.ChoiceSize;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 7;
        gridObject.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scrollbarObject = new GameObject("Scrollbar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Scrollbar));
        scrollbarObject.transform.SetParent(scrollObject.transform, false);
        Anchor(scrollbarObject.GetComponent<RectTransform>(), .965f, .025f, .992f, .975f);
        scrollbarObject.GetComponent<Image>().color = new Color(.16f, .10f, .035f, 1f);
        var handle = ImageObject("Handle", scrollbarObject.transform, null, new Color(.72f, .52f, .18f, 1f));
        Stretch(handle.rectTransform);
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
        Anchor(status.rectTransform, 0.06f, 0.055f, 0.64f, 0.16f);
        confirm = ButtonObject("Confirm", panel.transform, "PLAY", Confirm).GetComponent<Button>();
        Anchor(confirm.GetComponent<RectTransform>(), 0.70f, 0.055f, 0.91f, 0.16f);

        foreach (string key in GameSession.SelectedPlants)
            if (selected.Count < PlantLoadoutCatalog.MaxSelected && PlantLoadoutCatalog.IsSelectionChoice(key) &&
                PlantLoadoutCatalog.TryGet(key, out _) && !selected.Contains(key))
                selected.Add(key);
        UpdateState();
    }

    private void CreateChoice(PlantLoadoutEntry entry, Transform parent)
    {
        packets.Add(entry.Key,SeedPacketFactory.CreateChoice(entry,parent,SeedPacketFactory.ChoiceSize,()=>Toggle(entry.Key)));
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
            if(PlantLoadoutCatalog.TryGet(key,out var entry)) SeedPacketFactory.CreateChoice(entry,selectedBank,new Vector2(56,76),()=>Toggle(key));
        status.text = "Selected: " + selected.Count + "/" + PlantLoadoutCatalog.MaxSelected;
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
