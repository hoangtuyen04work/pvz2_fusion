using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Bảng xếp hạng Top 50 Người Chơi Chế Độ Sinh Tồn Vô Hạn (Endless Leaderboard Overlay).
/// Thiết kế đồng bộ hoàn hảo với phong cách cổ điển của Plants vs. Zombies:
/// Khung bảng gỗ đá cổ điển, cuộn mượt mà danh sách 50 người chơi, hiển thị Hạng, Tên, Màn đạt được, Điểm số, Số Zombie diệt và Thời gian sống sót.
/// </summary>
public sealed class EndlessLeaderboardOverlay : MonoBehaviour
{
    private const float DialogWidth = 880f;
    private const float DialogHeight = 620f;
    private const float ListWidth = 800f;
    private const float ListHorizontalInset = 54f;
    private const float RowContentInset = 38f;

    // One shared grid keeps header labels and record values pixel-perfect aligned.
    private static readonly float[] ColumnEdges = { 0f, 0.105f, 0.42f, 0.555f, 0.735f, 1f };

    private static Font cachedFont;
    private static Sprite cachedDialogMain;
    private static Sprite cachedDialogChild;
    private static Sprite cachedButton1;
    private static Sprite cachedButton2;
    private static Sprite cachedCancel;

    public static void Show()
    {
        if (Object.FindAnyObjectByType<EndlessLeaderboardOverlay>() != null) return;

        var root = new GameObject("Endless Leaderboard Overlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(EndlessLeaderboardOverlay));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 350;

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1000f, 750f);
        // Keep the whole board visible on wide, narrow and portrait displays.
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        root.GetComponent<EndlessLeaderboardOverlay>().Build();
    }

    private void Awake()
    {
        LoadAssets();
    }

    private static void LoadAssets()
    {
        if (cachedFont == null) cachedFont = Resources.Load<Font>("Fonts/Baloo2") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (cachedDialogMain == null) cachedDialogMain = CreateSlicedSprite(Resources.Load<Sprite>("GameUI/dialog_main"), 0.12f, 0.14f);
        if (cachedDialogChild == null) cachedDialogChild = CreateSlicedSprite(Resources.Load<Sprite>("GameUI/dialog_child"), 0.12f, 0.20f);
        if (cachedButton1 == null) cachedButton1 = CreateSlicedSprite(Resources.Load<Sprite>("GameUI/button1"), 0.14f, 0.30f);
        if (cachedButton2 == null) cachedButton2 = CreateSlicedSprite(Resources.Load<Sprite>("GameUI/button2"), 0.18f, 0.28f);
        if (cachedCancel == null) cachedCancel = Resources.Load<Sprite>("GameUI/cancel");
    }

    private void Build()
    {
        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        // 1. Nền tối mờ toàn màn hình
        var shade = CreateImage("Shade", transform, null, new Color(0f, 0f, 0f, 0.78f));
        Stretch(shade.rectTransform);
        var shadeBtn = shade.gameObject.AddComponent<Button>();
        shadeBtn.transition = Selectable.Transition.None;
        shadeBtn.onClick.AddListener(Close);

        // 2. Khung thoại chính bằng bảng gỗ đá PvZ
        var dialogObject = new GameObject("Leaderboard Board", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dialogObject.transform.SetParent(transform, false);
        var dialogRect = dialogObject.GetComponent<RectTransform>();
        CenterRect(dialogRect, new Vector2(DialogWidth, DialogHeight), Vector2.zero);

        var dialogImage = dialogObject.GetComponent<Image>();
        dialogImage.sprite = cachedDialogMain;
        dialogImage.type = Image.Type.Sliced;
        dialogImage.color = Color.white;
        dialogImage.raycastTarget = true;
        AddSoftShadow(dialogObject, new Vector2(4f, -5f));

        // 3. Tiêu đề
        var titleText = CreateText("Tiêu đề", dialogObject.transform, "BẢNG XẾP HẠNG THẾ GIỚI", 32, TextAnchor.MiddleCenter, new Color(1f, 0.90f, 0.40f, 1f));
        CenterRect(titleText.rectTransform, new Vector2(620f, 48f), new Vector2(0f, 276f));
        titleText.fontStyle = FontStyle.Bold;
        ConfigureSingleLineText(titleText, 24);
        AddTextOutline(titleText.gameObject, new Color(0.24f, 0.08f, 0.02f, 0.98f), new Vector2(1.8f, -1.8f));

        var subTitleText = CreateText("Phụ đề", dialogObject.transform, "SINH TỒN VÔ HẠN  •  TOP 50", 18, TextAnchor.MiddleCenter, new Color(0.92f, 0.95f, 0.80f, 1f));
        CenterRect(subTitleText.rectTransform, new Vector2(460f, 30f), new Vector2(0f, 240f));
        subTitleText.fontStyle = FontStyle.Bold;
        ConfigureSingleLineText(subTitleText, 15);
        AddTextOutline(subTitleText.gameObject, new Color(0.18f, 0.08f, 0.02f, 0.95f), new Vector2(1.2f, -1.2f));

        // Nút Đóng (X) góc trên phải
        CreateCloseIcon("Nút Đóng X", dialogObject.transform, new Vector2(405f, 272f), Close);

        // 4. Khung chứa danh sách cuộn
        BuildLeaderboardList(dialogObject.transform);

        // 5. Nút Đóng phía dưới
        CreateStyledButton("Nút Đóng", dialogObject.transform, "ĐÓNG", 22, new Vector2(160f, 48f), new Vector2(0f, -270f), Close);
    }

    private void BuildLeaderboardList(Transform parent)
    {
        // Khung nền bảng xếp hạng
        var listFrame = new GameObject("List Frame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        listFrame.transform.SetParent(parent, false);
        var frameRect = listFrame.GetComponent<RectTransform>();
        CenterRect(frameRect, new Vector2(ListWidth, 440f), new Vector2(0f, -10f));

        var frameImg = listFrame.GetComponent<Image>();
        frameImg.sprite = cachedDialogChild;
        frameImg.type = Image.Type.Sliced;
        frameImg.color = new Color(0.96f, 0.93f, 0.86f, 0.96f);
        AddSoftShadow(listFrame, new Vector2(2f, -3f));

        // Thanh tiêu đề các cột (Header Row)
        var headerObj = new GameObject("Header Row", typeof(RectTransform), typeof(Image));
        headerObj.transform.SetParent(listFrame.transform, false);
        var headerRect = headerObj.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.anchoredPosition = new Vector2(0f, -10f);
        headerRect.sizeDelta = new Vector2(-ListHorizontalInset * 2f, 40f);

        var headerImg = headerObj.GetComponent<Image>();
        headerImg.sprite = cachedButton1;
        headerImg.type = Image.Type.Sliced;
        headerImg.color = new Color(0.88f, 0.78f, 0.62f, 0.90f);

        Transform headerGrid = CreateColumnGrid(headerObj.transform);
        BuildRowColumns(
            headerGrid,
            rankText: "HẠNG",
            nameText: "NGƯỜI CHƠI",
            waveText: "MÀN",
            scoreText: "ĐIỂM",
            statsText: "HẠ GỤC  •  THỜI GIAN",
            isHeader: true,
            rankColor: new Color(0.38f, 0.18f, 0.05f),
            nameColor: new Color(0.38f, 0.18f, 0.05f),
            waveColor: new Color(0.38f, 0.18f, 0.05f),
            scoreColor: new Color(0.38f, 0.18f, 0.05f),
            statsColor: new Color(0.38f, 0.18f, 0.05f)
        );

        // Scroll View
        var scrollObj = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        scrollObj.transform.SetParent(listFrame.transform, false);
        var scrollRectTransform = scrollObj.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0f, 0f);
        scrollRectTransform.anchorMax = new Vector2(1f, 1f);
        scrollRectTransform.offsetMin = new Vector2(ListHorizontalInset, 14f);
        scrollRectTransform.offsetMax = new Vector2(-ListHorizontalInset, -56f);

        var scrollBg = scrollObj.GetComponent<Image>();
        scrollBg.color = Color.clear;

        var scrollRect = scrollObj.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 32f;

        // Viewport
        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
        viewport.transform.SetParent(scrollObj.transform, false);
        Stretch(viewport.GetComponent<RectTransform>());
        viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.001f);

        // Content
        var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = Vector2.zero;

        var layout = content.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 4f;
        layout.padding = new RectOffset(0, 0, 4, 8);
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fitter = content.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.content = contentRect;

        List<EndlessScoreRecord> localRecords = EndlessLeaderboard.Load();
        if (FirebaseAuthService.IsLoggedIn && FirebaseConfig.IsConfigured &&
            !FirebaseAuthService.CurrentUserId.StartsWith("guest_"))
        {
            ShowStatus(content.transform, "Đang tải bảng xếp hạng thế giới…");
            FirebaseAuthService.Instance.LoadGlobalLeaderboard((ok, cloudRecords, message) =>
            {
                if (this == null || content == null) return;
                ClearContent(content.transform);
                PopulateRecords(content.transform, ok ? cloudRecords : localRecords,
                    ok ? null : message + "\nĐang hiển thị dữ liệu cục bộ.");
            });
        }
        else
        {
            PopulateRecords(content.transform, localRecords, null);
        }
    }

    private void PopulateRecords(Transform content, List<EndlessScoreRecord> records, string statusMessage)
    {
        if (records == null || records.Count == 0)
        {
            ShowStatus(content, string.IsNullOrEmpty(statusMessage)
                ? "Chưa có thành tích Sinh tồn nào được ghi nhận.\nHãy tham gia chế độ Sinh Tồn để ghi tên vào bảng vàng!"
                : statusMessage);
            return;
        }

        if (!string.IsNullOrEmpty(statusMessage))
        {
            var notice = CreateText("Cloud Status", content, statusMessage, 13, TextAnchor.MiddleCenter, new Color(0.55f, 0.25f, 0.08f));
            var noticeElement = notice.gameObject.AddComponent<LayoutElement>();
            noticeElement.preferredHeight = 40f;
        }

        int count = Mathf.Min(50, records.Count);
        for (int i = 0; i < count; i++) CreateRecordRow(content, i + 1, records[i]);
    }

    private static void ShowStatus(Transform content, string message)
    {
            var emptyText = CreateText("Status", content, message, 18, TextAnchor.MiddleCenter, new Color(0.45f, 0.25f, 0.10f));
            var emptyElement = emptyText.GetComponent<LayoutElement>() ?? emptyText.gameObject.AddComponent<LayoutElement>();
            emptyElement.preferredHeight = 160f;
            emptyText.fontStyle = FontStyle.Bold;
    }

    private static void ClearContent(Transform content)
    {
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
    }

    private void CreateRecordRow(Transform parent, int rank, EndlessScoreRecord record)
    {
        var rowObj = new GameObject("Row_" + rank, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        rowObj.transform.SetParent(parent, false);

        var layoutElement = rowObj.GetComponent<LayoutElement>();
        layoutElement.preferredHeight = 44f;
        layoutElement.minHeight = 44f;
        layoutElement.flexibleWidth = 1f;

        var rowImg = rowObj.GetComponent<Image>();
        rowImg.sprite = cachedButton1;
        rowImg.type = Image.Type.Sliced;

        // Đổi màu nền cho Top 1, Top 2, Top 3
        Color rankColor;
        Color nameColor = new Color(0.18f, 0.08f, 0.02f);
        Color waveColor = new Color(0.15f, 0.38f, 0.10f);
        Color scoreColor = new Color(0.75f, 0.20f, 0.05f);
        Color statsColor = new Color(0.38f, 0.24f, 0.12f);

        if (rank == 1)
        {
            rowImg.color = new Color(1f, 0.88f, 0.40f, 0.95f); // Vàng rực rỡ
            rankColor = new Color(0.65f, 0.12f, 0.05f);
        }
        else if (rank == 2)
        {
            rowImg.color = new Color(0.90f, 0.93f, 0.98f, 0.95f); // Bạc sáng
            rankColor = new Color(0.40f, 0.18f, 0.08f);
        }
        else if (rank == 3)
        {
            rowImg.color = new Color(0.94f, 0.82f, 0.65f, 0.95f); // Đồng ánh kim
            rankColor = new Color(0.45f, 0.20f, 0.08f);
        }
        else
        {
            rowImg.color = (rank % 2 == 0) ? new Color(1f, 1f, 1f, 0.85f) : new Color(0.93f, 0.91f, 0.84f, 0.85f);
            rankColor = new Color(0.35f, 0.20f, 0.10f);
        }

        if (rank <= 3)
        {
            var accent = CreateImage("Top Accent", rowObj.transform, null, rankColor);
            var accentRect = accent.rectTransform;
            accentRect.anchorMin = new Vector2(0f, 0.12f);
            accentRect.anchorMax = new Vector2(0f, 0.88f);
            accentRect.pivot = new Vector2(0f, 0.5f);
            accentRect.anchoredPosition = new Vector2(3f, 0f);
            accentRect.sizeDelta = new Vector2(4f, 0f);
            accent.raycastTarget = false;
        }

        string rankStr = rank.ToString();
        if (rank == 1) rankStr = "★ 1 ★";
        else if (rank == 2) rankStr = "2";
        else if (rank == 3) rankStr = "3";

        string displayName = FormatPlayerName(record.playerName);
        string waveStr = record.wave.ToString();
        string scoreStr = record.score.ToString("N0");

        int minutes = Mathf.FloorToInt(record.durationSeconds / 60f);
        int seconds = Mathf.FloorToInt(record.durationSeconds % 60f);
        string timeStr = minutes.ToString("00") + ":" + seconds.ToString("00");
        string statStr = record.kills.ToString("N0") + "  •  " + timeStr;

        Transform rowGrid = CreateColumnGrid(rowObj.transform);
        BuildRowColumns(
            rowGrid,
            rankText: rankStr,
            nameText: displayName,
            waveText: waveStr,
            scoreText: scoreStr,
            statsText: statStr,
            isHeader: false,
            rankColor: rankColor,
            nameColor: nameColor,
            waveColor: waveColor,
            scoreColor: scoreColor,
            statsColor: statsColor
        );
    }

    private static void BuildRowColumns(
        Transform parent,
        string rankText,
        string nameText,
        string waveText,
        string scoreText,
        string statsText,
        bool isHeader,
        Color rankColor,
        Color nameColor,
        Color waveColor,
        Color scoreColor,
        Color statsColor)
    {
        int mainSize = isHeader ? 15 : 17;
        int statSize = isHeader ? 14 : 15;

        CreateCell(parent, "Cell_Rank", rankText, ColumnEdges[0], ColumnEdges[1], TextAnchor.MiddleCenter, rankColor, mainSize, true, 8f, 6f);

        CreateCell(parent, "Cell_Name", nameText, ColumnEdges[1], ColumnEdges[2], TextAnchor.MiddleLeft, nameColor, mainSize, true, 14f, 10f);

        CreateCell(parent, "Cell_Wave", waveText, ColumnEdges[2], ColumnEdges[3], TextAnchor.MiddleCenter, waveColor, mainSize, true, 8f, 8f);

        CreateCell(parent, "Cell_Score", scoreText, ColumnEdges[3], ColumnEdges[4], TextAnchor.MiddleRight, scoreColor, isHeader ? mainSize : 18, true, 8f, 12f);

        CreateCell(parent, "Cell_Stats", statsText, ColumnEdges[4], ColumnEdges[5], TextAnchor.MiddleRight, statsColor, statSize, isHeader, 12f, 14f);

        for (int i = 1; i < ColumnEdges.Length - 1; i++)
            CreateColumnDivider(parent, ColumnEdges[i], isHeader);
    }

    private static Transform CreateColumnGrid(Transform parent)
    {
        var grid = new GameObject("Column Grid", typeof(RectTransform));
        grid.transform.SetParent(parent, false);
        var rect = grid.GetComponent<RectTransform>();
        Stretch(rect);
        rect.offsetMin = new Vector2(RowContentInset, 3f);
        rect.offsetMax = new Vector2(-RowContentInset, -3f);
        return grid.transform;
    }

    private static Text CreateCell(Transform parent, string name, string value, float xMin, float xMax, TextAnchor alignment, Color color, int fontSize, bool isBold, float padLeft = 4f, float padRight = 4f)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(xMin, 0f);
        rect.anchorMax = new Vector2(xMax, 1f);
        rect.offsetMin = new Vector2(padLeft, 0f);
        rect.offsetMax = new Vector2(-padRight, 0f);

        var text = go.GetComponent<Text>();
        text.font = cachedFont;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = isBold ? FontStyle.Bold : FontStyle.Normal;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 11;
        text.resizeTextMaxSize = fontSize;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.lineSpacing = 0.9f;

        return text;
    }

    private static void CreateColumnDivider(Transform parent, float anchorX, bool isHeader)
    {
        var divider = CreateImage("Column Divider", parent, null,
            isHeader ? new Color(0.28f, 0.13f, 0.04f, 0.35f) : new Color(0.35f, 0.22f, 0.10f, 0.16f));
        var rect = divider.rectTransform;
        rect.anchorMin = new Vector2(anchorX, 0.18f);
        rect.anchorMax = new Vector2(anchorX, 0.82f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(1f, 0f);
        divider.raycastTarget = false;
    }

    private static string FormatPlayerName(string playerName)
    {
        string value = string.IsNullOrWhiteSpace(playerName) ? "Người chơi" : playerName.Trim();
        value = value.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
        while (value.Contains("  ")) value = value.Replace("  ", " ");
        return value.Length > 28 ? value.Substring(0, 27) + "…" : value;
    }

    private static void ConfigureSingleLineText(Text text, int minSize)
    {
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = minSize;
        text.resizeTextMaxSize = text.fontSize;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
    }

    private static Sprite CreateSlicedSprite(Sprite source, float horizontalBorderRatio, float verticalBorderRatio)
    {
        if (source == null) return null;
        Rect rect = source.rect;
        Vector4 border = new Vector4(
            rect.width * horizontalBorderRatio,
            rect.height * verticalBorderRatio,
            rect.width * horizontalBorderRatio,
            rect.height * verticalBorderRatio);

        Vector2 normalizedPivot = new Vector2(source.pivot.x / rect.width, source.pivot.y / rect.height);
        var sliced = Sprite.Create(source.texture, rect, normalizedPivot,
            source.pixelsPerUnit, 0, SpriteMeshType.FullRect, border);
        sliced.name = source.name + "_Sliced";
        return sliced;
    }

    private void CreateStyledButton(string name, Transform parent, string label, int fontSize, Vector2 size, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        var btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        CenterRect(btnObj.GetComponent<RectTransform>(), size, position);

        var image = btnObj.GetComponent<Image>();
        image.sprite = cachedButton2;
        image.type = Image.Type.Sliced;
        image.color = Color.white;

        var button = btnObj.GetComponent<Button>();
        button.onClick.AddListener(action);
        AddSoftShadow(btnObj, new Vector2(2f, -3f));

        var text = CreateText("Label", btnObj.transform, label, fontSize, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.85f));
        Stretch(text.rectTransform);
        text.fontStyle = FontStyle.Bold;
        text.raycastTarget = false;
        AddTextOutline(text.gameObject, new Color(0.24f, 0.08f, 0.02f, 0.95f), new Vector2(1.2f, -1.2f));
    }

    private void CreateCloseIcon(string name, Transform parent, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        var btnObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        CenterRect(btnObj.GetComponent<RectTransform>(), new Vector2(46f, 48f), position);

        var image = btnObj.GetComponent<Image>();
        image.sprite = cachedCancel;
        image.preserveAspect = true;

        var button = btnObj.GetComponent<Button>();
        button.onClick.AddListener(action);
        AddSoftShadow(btnObj, new Vector2(1.5f, -2f));
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        return image;
    }

    private static Text CreateText(string name, Transform parent, string value, int fontSize, TextAnchor alignment, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>();
        text.font = cachedFont;
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static void AddTextOutline(GameObject go, Color color, Vector2 dist)
    {
        var outline = go.GetComponent<Outline>();
        if (outline == null) outline = go.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = dist;
    }

    private static void AddSoftShadow(GameObject go, Vector2 dist)
    {
        var shadow = go.GetComponent<Shadow>();
        if (shadow == null) shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        shadow.effectDistance = dist;
    }

    private static void CenterRect(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void Close()
    {
        Destroy(gameObject);
    }
}
