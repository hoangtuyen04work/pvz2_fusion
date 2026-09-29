using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Màn vào Sinh tồn và bảng xếp hạng cục bộ, dựng độc lập trên MainMenu.</summary>
public sealed class EndlessMenuOverlay : MonoBehaviour
{
    private const string PlayerNameKey = "Endless.PlayerName";
    private InputField playerNameInput;

    public static void Show()
    {
        if (FindAnyObjectByType<EndlessMenuOverlay>() != null) return;
        var root = new GameObject("Endless Menu", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(EndlessMenuOverlay));
        root.GetComponent<EndlessMenuOverlay>().Build();
    }

    private void Build()
    {
        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1000f, 750f);
        scaler.matchWidthOrHeight = 0.5f;

        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        Image shade = ImageObject("Shade", transform, new Color(0f, 0f, 0f, 0.84f));
        Stretch(shade.rectTransform);
        Image panel = ImageObject("Panel", transform, new Color(0.09f, 0.16f, 0.055f, 0.98f));
        Anchor(panel.rectTransform, 0.16f, 0.08f, 0.84f, 0.92f);

        Text title = TextObject("Title", panel.transform, "SINH TỒN VÔ HẠN", 43,
            TextAnchor.MiddleCenter, new Color(0.65f, 1f, 0.28f));
        Anchor(title.rectTransform, 0.08f, 0.86f, 0.92f, 0.97f);

        Text subtitle = TextObject("Subtitle", panel.transform,
            "Cầm cự qua càng nhiều đợt zombie càng tốt", 22,
            TextAnchor.MiddleCenter, new Color(0.9f, 0.94f, 0.78f));
        Anchor(subtitle.rectTransform, 0.08f, 0.79f, 0.92f, 0.86f);

        Text nameLabel = TextObject("Name Label", panel.transform, "TÊN NGƯỜI CHƠI", 20,
            TextAnchor.MiddleLeft, Color.white);
        Anchor(nameLabel.rectTransform, 0.10f, 0.70f, 0.39f, 0.77f);
        playerNameInput = InputObject(panel.transform);
        Anchor(playerNameInput.GetComponent<RectTransform>(), 0.39f, 0.70f, 0.90f, 0.77f);
        playerNameInput.text = PlayerPrefs.GetString(PlayerNameKey, "Người chơi");

        Text boardTitle = TextObject("Board Title", panel.transform, "TOP 10 CỤC BỘ", 25,
            TextAnchor.MiddleCenter, new Color(1f, 0.84f, 0.28f));
        Anchor(boardTitle.rectTransform, 0.08f, 0.62f, 0.92f, 0.69f);
        Text board = TextObject("Leaderboard", panel.transform, EndlessLeaderboard.Format(), 21,
            TextAnchor.UpperCenter, Color.white);
        board.fontStyle = FontStyle.Bold;
        Anchor(board.rectTransform, 0.08f, 0.22f, 0.92f, 0.62f);

        GameObject close = ButtonObject("Close", panel.transform, "QUAY LẠI", new Color(0.30f, 0.40f, 0.20f), Close);
        Anchor(close.GetComponent<RectTransform>(), 0.10f, 0.07f, 0.43f, 0.17f);
        GameObject play = ButtonObject("Play", panel.transform, "CHỌN CÂY & CHƠI", new Color(0.45f, 0.70f, 0.14f), Play);
        Anchor(play.GetComponent<RectTransform>(), 0.48f, 0.07f, 0.90f, 0.17f);
    }

    private void Play()
    {
        string playerName = string.IsNullOrWhiteSpace(playerNameInput.text)
            ? "Người chơi" : playerNameInput.text.Trim();
        if (playerName.Length > 20) playerName = playerName.Substring(0, 20);
        PlayerPrefs.SetString(PlayerNameKey, playerName);
        PlayerPrefs.Save();
        NetSession.LocalName = playerName;

        Destroy(gameObject);
        PlantSelectionOverlay.Show(0, () =>
        {
            EndlessRun.Begin();
            SceneManager.LoadScene(EndlessRun.EntrySceneName);
        });
    }

    private void Close() => Destroy(gameObject);

    private static InputField InputObject(Transform parent)
    {
        var root = new GameObject("Player Name", typeof(RectTransform), typeof(Image), typeof(InputField));
        root.transform.SetParent(parent, false);
        root.GetComponent<Image>().color = new Color(0.18f, 0.25f, 0.12f, 1f);
        Text value = TextObject("Text", root.transform, string.Empty, 22, TextAnchor.MiddleLeft, Color.white);
        Stretch(value.rectTransform);
        value.rectTransform.offsetMin = new Vector2(12f, 4f);
        value.rectTransform.offsetMax = new Vector2(-12f, -4f);
        InputField input = root.GetComponent<InputField>();
        input.textComponent = value;
        input.characterLimit = 20;
        input.lineType = InputField.LineType.SingleLine;
        return input;
    }

    private static GameObject ButtonObject(string name, Transform parent, string label, Color color,
        UnityEngine.Events.UnityAction action)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        root.transform.SetParent(parent, false);
        root.GetComponent<Image>().color = color;
        root.GetComponent<Button>().onClick.AddListener(action);
        Text text = TextObject("Label", root.transform, label, 25, TextAnchor.MiddleCenter, Color.white);
        Stretch(text.rectTransform);
        text.raycastTarget = false;
        return root;
    }

    internal static Image ImageObject(string name, Transform parent, Color color)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        root.transform.SetParent(parent, false);
        Image image = root.GetComponent<Image>();
        image.color = color;
        return image;
    }

    internal static Text TextObject(string name, Transform parent, string value, int size,
        TextAnchor alignment, Color color)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        root.transform.SetParent(parent, false);
        Text text = root.GetComponent<Text>();
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

    internal static void Stretch(RectTransform rect) => Anchor(rect, 0f, 0f, 1f, 1f);

    internal static void Anchor(RectTransform rect, float x0, float y0, float x1, float y1)
    {
        rect.anchorMin = new Vector2(x0, y0);
        rect.anchorMax = new Vector2(x1, y1);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
