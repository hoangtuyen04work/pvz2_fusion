using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Màn vào Sinh tồn và bảng xếp hạng cục bộ, dựng độc lập trên MainMenu.</summary>
public sealed class EndlessMenuOverlay : MonoBehaviour
{
    private const string PlayerNameKey = "Endless.PlayerName";
    private InputField playerNameInput;
    private Text subtitle;
    private Button playButton;
    private bool newRunArmed;

    private static Sprite cachedDialogMain;
    private static Sprite cachedDialogChild;
    private static Sprite cachedButton1;
    private static Sprite cachedButton2;
    private static Sprite cachedCancel;

    public static void Show()
    {
        if (FindAnyObjectByType<EndlessMenuOverlay>() != null) return;
        var root = new GameObject("Endless Menu", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(EndlessMenuOverlay));
        root.GetComponent<EndlessMenuOverlay>().Build();
    }

    private static void EnsureAssets()
    {
        if (cachedDialogMain == null) cachedDialogMain = Resources.Load<Sprite>("GameUI/dialog_main");
        if (cachedDialogChild == null) cachedDialogChild = Resources.Load<Sprite>("GameUI/dialog_child");
        if (cachedButton1 == null) cachedButton1 = Resources.Load<Sprite>("GameUI/button1");
        if (cachedButton2 == null) cachedButton2 = Resources.Load<Sprite>("GameUI/button2");
        if (cachedCancel == null) cachedCancel = Resources.Load<Sprite>("GameUI/cancel");
    }

    private void Build()
    {
        EnsureAssets();

        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1000f, 750f);
        scaler.matchWidthOrHeight = 0.5f;

        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        // 1. Nền mờ tối dịu mắt
        Image shade = ImageObject("Shade", transform, new Color(0f, 0f, 0f, 0.78f));
        Stretch(shade.rectTransform);
        var shadeBtn = shade.gameObject.AddComponent<Button>();
        shadeBtn.transition = Selectable.Transition.None;
        shadeBtn.onClick.AddListener(Close);

        // 2. Bảng gỗ đá PvZ chính
        var dialogObject = new GameObject("Endless Board Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dialogObject.transform.SetParent(transform, false);
        var dialogRect = dialogObject.GetComponent<RectTransform>();
        Anchor(dialogRect, 0.12f, 0.06f, 0.88f, 0.94f);

        Image panel = dialogObject.GetComponent<Image>();
        panel.sprite = cachedDialogMain;
        panel.type = cachedDialogMain != null ? Image.Type.Sliced : Image.Type.Simple;
        panel.color = Color.white;
        panel.raycastTarget = true;
        AddSoftShadow(dialogObject, new Vector2(4f, -5f));

        // Nút Đóng (X) góc trên bên phải
        if (cachedCancel != null)
        {
            var closeX = new GameObject("Close X", typeof(RectTransform), typeof(Image), typeof(Button), typeof(MenuButtonMotion));
            closeX.transform.SetParent(panel.transform, false);
            var xRect = closeX.GetComponent<RectTransform>();
            xRect.anchorMin = xRect.anchorMax = new Vector2(1f, 1f);
            xRect.pivot = new Vector2(0.5f, 0.5f);
            xRect.anchoredPosition = new Vector2(-42f, -38f);
            xRect.sizeDelta = new Vector2(46f, 46f);

            var xImg = closeX.GetComponent<Image>();
            xImg.sprite = cachedCancel;
            xImg.preserveAspect = true;

            var xBtn = closeX.GetComponent<Button>();
            xBtn.transition = Selectable.Transition.None;
            xBtn.onClick.AddListener(Close);
            closeX.GetComponent<MenuButtonMotion>().targetGraphic = xImg;
        }

        // 3. Tiêu đề chuẩn phong cách PvZ
        Text title = TextObject("Title", panel.transform, "SINH TỒN VÔ HẠN", 36,
            TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.38f, 1f));
        title.fontStyle = FontStyle.Bold;
        Anchor(title.rectTransform, 0.08f, 0.865f, 0.92f, 0.965f);
        AddTextOutline(title.gameObject, new Color(0.24f, 0.08f, 0.02f, 0.98f), new Vector2(1.8f, -1.8f));

        subtitle = TextObject("Subtitle", panel.transform,
            "Cầm cự qua càng nhiều đợt zombie càng tốt", 19,
            TextAnchor.MiddleCenter, new Color(0.96f, 0.94f, 0.82f, 1f));
        subtitle.fontStyle = FontStyle.Bold;
        Anchor(subtitle.rectTransform, 0.08f, 0.795f, 0.92f, 0.860f);
        AddTextOutline(subtitle.gameObject, new Color(0.18f, 0.08f, 0.02f, 0.95f), new Vector2(1.2f, -1.2f));

        // 4. Nhập tên người chơi với khung gỗ nhỏ hài hòa
        Text nameLabel = TextObject("Name Label", panel.transform, "TÊN NGƯỜI CHƠI:", 19,
            TextAnchor.MiddleRight, new Color(1f, 0.88f, 0.45f, 1f));
        nameLabel.fontStyle = FontStyle.Bold;
        Anchor(nameLabel.rectTransform, 0.08f, 0.710f, 0.36f, 0.780f);
        AddTextOutline(nameLabel.gameObject, new Color(0.20f, 0.08f, 0.02f, 0.95f), new Vector2(1.2f, -1.2f));

        playerNameInput = InputObject(panel.transform);
        Anchor(playerNameInput.GetComponent<RectTransform>(), 0.38f, 0.710f, 0.88f, 0.780f);
        playerNameInput.text = FirebaseAuthService.GetCurrentPlayerName();

        // 5. Khung bảng thành tích Top 3 của người chơi (bảng gỗ phụ dialog_child)
        var boardChildObj = new GameObject("Board Container", typeof(RectTransform), typeof(Image));
        boardChildObj.transform.SetParent(panel.transform, false);
        var boardChildRect = boardChildObj.GetComponent<RectTransform>();
        Anchor(boardChildRect, 0.06f, 0.20f, 0.94f, 0.690f);

        var boardChildImg = boardChildObj.GetComponent<Image>();
        boardChildImg.sprite = cachedDialogChild;
        boardChildImg.type = cachedDialogChild != null ? Image.Type.Sliced : Image.Type.Simple;
        boardChildImg.color = cachedDialogChild != null ? new Color(0.96f, 0.94f, 0.88f, 0.98f) : new Color(0.12f, 0.08f, 0.04f, 0.88f);
        AddSoftShadow(boardChildObj, new Vector2(2f, -3f));

        Text boardTitle = TextObject("Board Title", boardChildObj.transform, "★ KỶ LỤC CỦA BẠN (TOP 3 CAO NHẤT) ★", 21,
            TextAnchor.MiddleCenter, new Color(0.36f, 0.16f, 0.04f, 1f));
        boardTitle.fontStyle = FontStyle.Bold;
        Anchor(boardTitle.rectTransform, 0.04f, 0.86f, 0.96f, 0.97f);

        // Header các cột
        var headerRow = new GameObject("Header Row", typeof(RectTransform), typeof(Image));
        headerRow.transform.SetParent(boardChildObj.transform, false);
        var headerRect = headerRow.GetComponent<RectTransform>();
        Anchor(headerRect, 0.03f, 0.73f, 0.97f, 0.85f);

        var headerImg = headerRow.GetComponent<Image>();
        headerImg.sprite = cachedButton1;
        headerImg.type = Image.Type.Sliced;
        headerImg.color = new Color(0.85f, 0.74f, 0.58f, 0.92f);

        BuildTop3Columns(headerRow.transform, "HẠNG", "MÀN ĐẠT", "ĐIỂM SỐ", "DIỆT / THỜI GIAN", true,
            new Color(0.36f, 0.16f, 0.04f), new Color(0.36f, 0.16f, 0.04f), new Color(0.36f, 0.16f, 0.04f), new Color(0.36f, 0.16f, 0.04f));

        // Khung chứa 3 hàng
        var rowsContainer = new GameObject("Rows Container", typeof(RectTransform));
        rowsContainer.transform.SetParent(boardChildObj.transform, false);
        var rowsRect = rowsContainer.GetComponent<RectTransform>();
        Anchor(rowsRect, 0.03f, 0.04f, 0.97f, 0.71f);

        string currentPlayer = playerNameInput.text;
        BuildTop3Rows(rowsContainer.transform, currentPlayer);

        playerNameInput.onEndEdit.AddListener(newName =>
        {
            SavePlayerName();
            BuildTop3Rows(rowsContainer.transform, newName);
        });

        // 6. Các nút bấm hành động (chuẩn nút GameUI/button1 và button2)
        bool hasSave = EndlessRun.HasSavedProgress;

        GameObject close = ThemedButtonObject("Close", panel.transform, "QUAY LẠI", cachedButton2,
            new Color(0.95f, 0.95f, 0.95f, 1f), Close);
        Anchor(close.GetComponent<RectTransform>(), hasSave ? 0.06f : 0.12f, 0.06f, hasSave ? 0.32f : 0.44f, 0.165f);

        if (hasSave)
        {
            GameObject resume = ThemedButtonObject("Resume", panel.transform, "TIẾP TỤC", cachedButton1,
                new Color(1f, 0.96f, 0.70f, 1f), Resume);
            Anchor(resume.GetComponent<RectTransform>(), 0.36f, 0.06f, 0.64f, 0.165f);
        }

        GameObject play = ThemedButtonObject("Play", panel.transform, hasSave ? "CHƠI MỚI" : "CHỌN CÂY & CHƠI",
            cachedButton1, new Color(1f, 0.96f, 0.70f, 1f), Play);
        Anchor(play.GetComponent<RectTransform>(), hasSave ? 0.68f : 0.56f, 0.06f, hasSave ? 0.94f : 0.88f, 0.165f);
        playButton = play.GetComponent<Button>();
    }

    private void Play()
    {
        if (EndlessRun.HasSavedProgress && !newRunArmed)
        {
            newRunArmed = true;
            subtitle.text = "Lượt đang lưu sẽ bị thay thế. Bấm lần nữa để xác nhận chơi mới.";
            Text label = playButton != null ? playButton.GetComponentInChildren<Text>() : null;
            if (label != null) label.text = "XÁC NHẬN CHƠI MỚI";
            return;
        }

        SavePlayerName();

        Destroy(gameObject);
        PlantSelectionOverlay.Show(0, () =>
        {
            EndlessRun.Begin();
            SceneManager.LoadScene(EndlessRun.EntrySceneName);
        });
    }

    private void Resume()
    {
        SavePlayerName();
        if (!EndlessRun.TryResume())
        {
            subtitle.text = "Không thể đọc lượt đã lưu. Bạn có thể bắt đầu lượt mới.";
            return;
        }
        Destroy(gameObject);
        SceneManager.LoadScene(EndlessRun.EntrySceneName);
    }

    private void SavePlayerName()
    {
        string playerName = string.IsNullOrWhiteSpace(playerNameInput.text)
            ? "Người chơi" : playerNameInput.text.Trim();
        if (playerName.Length > 20) playerName = playerName.Substring(0, 20);
        PlayerPrefs.SetString(PlayerNameKey, playerName);
        PlayerPrefs.Save();
        NetSession.LocalName = playerName;
    }

    private static void BuildTop3Rows(Transform container, string playerName)
    {
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Destroy(container.GetChild(i).gameObject);
        }

        var topRecords = EndlessLeaderboard.GetPlayerTopRecords(playerName, 3);
        if (topRecords == null || topRecords.Count == 0)
        {
            var emptyObj = new GameObject("Empty", typeof(RectTransform));
            emptyObj.transform.SetParent(container, false);
            Stretch(emptyObj.GetComponent<RectTransform>());

            var emptyText = TextObject("EmptyMsg", emptyObj.transform,
                "Bạn chưa có lượt chơi Sinh tồn nào được ghi lại.\nHãy bắt đầu chơi để ghi danh vào bảng vàng!",
                18, TextAnchor.MiddleCenter, new Color(0.40f, 0.20f, 0.08f));
            emptyText.fontStyle = FontStyle.Bold;
            Stretch(emptyText.rectTransform);
            return;
        }

        float rowHeight = 0.30f;
        float gap = 0.04f;

        for (int i = 0; i < topRecords.Count; i++)
        {
            var record = topRecords[i];
            int rank = i + 1;

            float y1 = 1f - (i * (rowHeight + gap));
            float y0 = y1 - rowHeight;

            var rowObj = new GameObject("Row_" + rank, typeof(RectTransform), typeof(Image));
            rowObj.transform.SetParent(container, false);
            var rowRect = rowObj.GetComponent<RectTransform>();
            Anchor(rowRect, 0f, Mathf.Max(0f, y0), 1f, y1);

            var rowImg = rowObj.GetComponent<Image>();
            rowImg.sprite = cachedButton1;
            rowImg.type = Image.Type.Sliced;

            Color rankColor;
            if (rank == 1)
            {
                rowImg.color = new Color(1f, 0.88f, 0.42f, 0.95f);
                rankColor = new Color(0.65f, 0.12f, 0.05f);
            }
            else if (rank == 2)
            {
                rowImg.color = new Color(0.90f, 0.93f, 0.98f, 0.95f);
                rankColor = new Color(0.40f, 0.18f, 0.08f);
            }
            else
            {
                rowImg.color = new Color(0.94f, 0.84f, 0.68f, 0.95f);
                rankColor = new Color(0.45f, 0.20f, 0.08f);
            }

            string rankStr = rank == 1 ? "★ 1 ★" : (rank == 2 ? "2" : "3");
            string waveStr = "Màn " + record.wave;
            string scoreStr = record.score.ToString("N0");

            int minutes = Mathf.FloorToInt(record.durationSeconds / 60f);
            int seconds = Mathf.FloorToInt(record.durationSeconds % 60f);
            string statStr = record.kills + " zmb • " + minutes.ToString("00") + ":" + seconds.ToString("00");

            BuildTop3Columns(rowObj.transform, rankStr, waveStr, scoreStr, statStr, false,
                rankColor, new Color(0.15f, 0.38f, 0.10f), new Color(0.75f, 0.20f, 0.05f), new Color(0.38f, 0.24f, 0.12f));
        }
    }

    private static void BuildTop3Columns(Transform parent, string rankText, string waveText, string scoreText, string statsText, bool isHeader,
        Color rankColor, Color waveColor, Color scoreColor, Color statsColor)
    {
        int mainSize = isHeader ? 16 : 17;
        int statSize = isHeader ? 16 : 15;

        // Cột 1: Hạng (0% -> 15%) - Canh giữa
        CreateTop3Cell(parent, "Cell_Rank", rankText, 0.00f, 0.15f, TextAnchor.MiddleCenter, rankColor, mainSize, true, 4f, 4f);

        // Cột 2: Màn đạt (15% -> 38%) - Canh giữa
        CreateTop3Cell(parent, "Cell_Wave", waveText, 0.15f, 0.38f, TextAnchor.MiddleCenter, waveColor, mainSize, true, 4f, 4f);

        // Cột 3: Điểm số (38% -> 66%) - Canh phải
        CreateTop3Cell(parent, "Cell_Score", scoreText, 0.38f, 0.66f, TextAnchor.MiddleRight, scoreColor, isHeader ? mainSize : 18, true, 4f, 10f);

        // Cột 4: Diệt / Thời gian (66% -> 100%) - Canh phải
        CreateTop3Cell(parent, "Cell_Stats", statsText, 0.66f, 1.00f, TextAnchor.MiddleRight, statsColor, statSize, isHeader, 4f, 12f);
    }

    private static Text CreateTop3Cell(Transform parent, string name, string value, float xMin, float xMax, TextAnchor alignment, Color color, int fontSize, bool isBold, float padLeft, float padRight)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(xMin, 0f);
        rect.anchorMax = new Vector2(xMax, 1f);
        rect.offsetMin = new Vector2(padLeft, 0f);
        rect.offsetMax = new Vector2(-padRight, 0f);

        var text = go.GetComponent<Text>();
        text.font = Resources.Load<Font>("Fonts/Baloo2") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = isBold ? FontStyle.Bold : FontStyle.Normal;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 11;
        text.resizeTextMaxSize = fontSize;

        return text;
    }

    private void Close() => Destroy(gameObject);

    private static InputField InputObject(Transform parent)
    {
        var root = new GameObject("Player Name", typeof(RectTransform), typeof(Image), typeof(InputField));
        root.transform.SetParent(parent, false);
        var rootImg = root.GetComponent<Image>();
        rootImg.sprite = cachedDialogChild;
        rootImg.type = cachedDialogChild != null ? Image.Type.Sliced : Image.Type.Simple;
        rootImg.color = cachedDialogChild != null ? Color.white : new Color(0.18f, 0.25f, 0.12f, 1f);

        Text value = TextObject("Text", root.transform, string.Empty, 20, TextAnchor.MiddleLeft, new Color(0.24f, 0.10f, 0.02f, 1f));
        value.fontStyle = FontStyle.Bold;
        Stretch(value.rectTransform);
        value.rectTransform.offsetMin = new Vector2(14f, 4f);
        value.rectTransform.offsetMax = new Vector2(-14f, -4f);

        InputField input = root.GetComponent<InputField>();
        input.textComponent = value;
        input.characterLimit = 20;
        input.lineType = InputField.LineType.SingleLine;
        return input;
    }

    private static GameObject ThemedButtonObject(string name, Transform parent, string label, Sprite sprite,
        Color textColor, UnityEngine.Events.UnityAction action)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(MenuButtonMotion));
        root.transform.SetParent(parent, false);

        var img = root.GetComponent<Image>();
        img.sprite = sprite;
        img.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        img.color = Color.white;

        var button = root.GetComponent<Button>();
        button.transition = Selectable.Transition.SpriteSwap;
        Sprite highlighted = cachedButton2 ?? sprite;
        SpriteState states = button.spriteState;
        states.highlightedSprite = highlighted;
        states.pressedSprite = highlighted;
        states.selectedSprite = highlighted;
        button.spriteState = states;
        button.onClick.AddListener(action);

        root.GetComponent<MenuButtonMotion>().targetGraphic = img;

        Text text = TextObject("Label", root.transform, label, 20, TextAnchor.MiddleCenter, textColor);
        text.fontStyle = FontStyle.Bold;
        Stretch(text.rectTransform);
        text.raycastTarget = false;
        AddTextOutline(text.gameObject, new Color(0.14f, 0.05f, 0.01f, 0.95f), new Vector2(1.3f, -1.3f));

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
        text.font = Resources.Load<Font>("Fonts/Baloo2") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.text = value;
        text.fontSize = size;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 12;
        text.resizeTextMaxSize = size;
        text.alignment = alignment;
        text.color = color;
        return text;
    }

    internal static void AddTextOutline(GameObject target, Color color, Vector2 dist)
    {
        var outline = target.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = dist;
    }

    internal static void AddSoftShadow(GameObject target, Vector2 dist)
    {
        var shadow = target.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.65f);
        shadow.effectDistance = dist;
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
