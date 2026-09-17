#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tool tiện ích trên Unity Editor: Tự động thiết lập GameObject AI Advisor
/// và toàn bộ hệ thống UI (Nút bấm hoạt họa, Panel lời khuyên, Loading, Kết nối) chỉ với 1 cú click.
/// Vị trí:
/// - Nút nhân vật hoạt họa: Góc dưới bên trái (cạnh xe 3 bánh / hiên nhà).
/// - Panel lời khuyên AI: Mép dưới sân cỏ (tránh che nút Tùy chọn và hàng cây).
/// </summary>
public static class AIAdvisorSetupEditor
{
    private const string FONT_PATH = "Fonts/Baloo2";

    [MenuItem("Tools/PvZ AI Advisor/Tự Động Thiết Lập AI Advisor Vào Scene", false, 1)]
    public static void SetupAIAdvisor()
    {
        // 1. Tìm hoặc tạo Canvas phù hợp
        Canvas targetCanvas = FindBestCanvas();
        if (targetCanvas == null)
        {
            GameObject canvasObj = new GameObject("AdvisorCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            targetCanvas = canvasObj.GetComponent<Canvas>();
            targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            targetCanvas.sortingOrder = 999;
            Undo.RegisterCreatedObjectUndo(canvasObj, "Tạo AdvisorCanvas");
        }

        Font font = Resources.Load<Font>(FONT_PATH);
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        // 2. Tìm hoặc tạo GameObject AI Advisor
        GameObject advisorObj = GameObject.Find("AI Advisor");
        if (advisorObj == null)
        {
            advisorObj = new GameObject("AI Advisor");
            Undo.RegisterCreatedObjectUndo(advisorObj, "Tạo GameObject AI Advisor");
        }

        GameStateCollector collector = advisorObj.GetComponent<GameStateCollector>() ?? advisorObj.AddComponent<GameStateCollector>();
        AIServiceConnector connector = advisorObj.GetComponent<AIServiceConnector>() ?? advisorObj.AddComponent<AIServiceConnector>();
        AIAdvisorUI advisorUI = advisorObj.GetComponent<AIAdvisorUI>() ?? advisorObj.AddComponent<AIAdvisorUI>();

        connector.serverUrl = "http://localhost:8000/analyze";
        connector.useMockMode = false;
        connector.timeoutSeconds = 20f;

        // 3. Xóa UI cũ nếu đã tồn tại để tạo mới chuẩn xác
        Transform existingUI = targetCanvas.transform.Find("AIAdvisor_UIRoot");
        if (existingUI != null)
        {
            Undo.DestroyObjectImmediate(existingUI.gameObject);
        }

        // 4. Tạo Root UI
        GameObject uiRoot = new GameObject("AIAdvisor_UIRoot", typeof(RectTransform));
        uiRoot.transform.SetParent(targetCanvas.transform, false);
        RectTransform rtRoot = uiRoot.GetComponent<RectTransform>();
        rtRoot.anchorMin = Vector2.zero;
        rtRoot.anchorMax = Vector2.one;
        rtRoot.offsetMin = Vector2.zero;
        rtRoot.offsetMax = Vector2.zero;
        Undo.RegisterCreatedObjectUndo(uiRoot, "Tạo AIAdvisor_UIRoot");

        // --- 4.1. Tạo Nút Trợ Lý Hoạt Họa (Bottom-Left: cạnh xe 3 bánh / hiên nhà) ---
        GameObject btnObj = new GameObject("AskAdvisor_Button", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(uiRoot.transform, false);
        RectTransform rtBtn = btnObj.GetComponent<RectTransform>();
        rtBtn.anchorMin = new Vector2(0.5f, 0f);
        rtBtn.anchorMax = new Vector2(0.5f, 0f);
        rtBtn.pivot = new Vector2(0.5f, 0.5f);
        rtBtn.anchoredPosition = new Vector2(-235f, 55f); // Tọa độ chuẩn khớp vùng tròn đỏ
        rtBtn.sizeDelta = new Vector2(74f, 74f);

        Image btnImg = btnObj.GetComponent<Image>();
        btnImg.color = new Color(0.12f, 0.32f, 0.18f, 0.95f); // Nền xanh rêu sẫm PvZ
        
        // Viền vàng kim nổi bật
        Outline btnOutline = btnObj.AddComponent<Outline>();
        btnOutline.effectColor = new Color(0.95f, 0.78f, 0.22f, 0.95f);
        btnOutline.effectDistance = new Vector2(2f, -2f);

        Button btn = btnObj.GetComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        btn.colors = colors;

        // Avatar nhân vật hoạt họa (Crazy Dave / Cartoon sprite)
        GameObject avatarObj = new GameObject("Avatar_Image", typeof(RectTransform), typeof(Image));
        avatarObj.transform.SetParent(btnObj.transform, false);
        RectTransform rtAvatar = avatarObj.GetComponent<RectTransform>();
        rtAvatar.anchorMin = Vector2.zero;
        rtAvatar.anchorMax = Vector2.one;
        rtAvatar.offsetMin = new Vector2(3f, 3f);
        rtAvatar.offsetMax = new Vector2(-3f, -3f);

        Image avatarImg = avatarObj.GetComponent<Image>();
        avatarImg.preserveAspect = true;
        avatarImg.raycastTarget = false;

        // Tự động nạp sprite mặc định của Crazy Dave
        Sprite defaultSprite = Resources.Load<Sprite>("Sprites/CrazyDave/Enter/CrazyDave_Enter0018");
        if (defaultSprite == null)
            defaultSprite = Resources.Load<Sprite>("Sprites/CrazyDave/Idle/CrazyDave_Idle0001");
        if (defaultSprite == null)
            defaultSprite = Resources.Load<Sprite>("Sprites/Icon");
        avatarImg.sprite = defaultSprite;

        // Badge góc "AI" nhỏ xinh báo hiệu nút trợ lý
        GameObject badgeObj = new GameObject("Badge", typeof(RectTransform), typeof(Image));
        badgeObj.transform.SetParent(btnObj.transform, false);
        RectTransform rtBadge = badgeObj.GetComponent<RectTransform>();
        rtBadge.anchorMin = new Vector2(1f, 1f);
        rtBadge.anchorMax = new Vector2(1f, 1f);
        rtBadge.pivot = new Vector2(0.8f, 0.8f);
        rtBadge.anchoredPosition = Vector2.zero;
        rtBadge.sizeDelta = new Vector2(26f, 18f);

        Image badgeImg = badgeObj.GetComponent<Image>();
        badgeImg.color = new Color(0.88f, 0.22f, 0.16f, 0.95f); // Đỏ nổi bật
        badgeImg.raycastTarget = false;

        GameObject badgeTextObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        badgeTextObj.transform.SetParent(badgeObj.transform, false);
        RectTransform rtBadgeText = badgeTextObj.GetComponent<RectTransform>();
        rtBadgeText.anchorMin = Vector2.zero;
        rtBadgeText.anchorMax = Vector2.one;
        rtBadgeText.offsetMin = Vector2.zero;
        rtBadgeText.offsetMax = Vector2.zero;

        Text badgeText = badgeTextObj.GetComponent<Text>();
        badgeText.text = "AI";
        badgeText.font = font;
        badgeText.fontSize = 11;
        badgeText.fontStyle = FontStyle.Bold;
        badgeText.alignment = TextAnchor.MiddleCenter;
        badgeText.color = Color.white;
        badgeText.raycastTarget = false;

        // Gắn component hoạt họa sống động UIAnimatedAdvisor
        UIAnimatedAdvisor animAdvisor = btnObj.AddComponent<UIAnimatedAdvisor>();
        animAdvisor.avatarImage = avatarImg;
        animAdvisor.enableBreathing = true;
        animAdvisor.breathingFrequency = 3.2f;
        animAdvisor.breathingScaleAmount = 0.05f;
        animAdvisor.floatingYAmount = 2.5f;

        // Nạp chuỗi frame Idle (Crazy Dave) nếu có
        List<Sprite> idles = new List<Sprite>();
        for (int i = 1; i <= 6; i++)
        {
            Sprite s = Resources.Load<Sprite>($"Sprites/CrazyDave/Idle/CrazyDave_Idle{i:D4}");
            if (s != null) idles.Add(s);
        }
        animAdvisor.idleFrames = idles.ToArray();

        // Nạp chuỗi frame Talk (Crazy Dave) nếu có
        List<Sprite> talks = new List<Sprite>();
        for (int i = 1; i <= 6; i++)
        {
            Sprite s = Resources.Load<Sprite>($"Sprites/CrazyDave/Talk/CrazyDave_Talk{i:D4}");
            if (s != null) talks.Add(s);
        }
        animAdvisor.talkFrames = talks.ToArray();

        // Nạp âm thanh thoại ngắn vui nhộn của Dave
        List<AudioClip> voiceClips = new List<AudioClip>();
        for (int i = 1; i <= 3; i++)
        {
            AudioClip c = Resources.Load<AudioClip>($"Sounds/CrazyDave/CrazyDave_Short{i}");
            if (c != null) voiceClips.Add(c);
        }
        animAdvisor.clickVoiceClips = voiceClips.ToArray();
        animAdvisor.SetBasePosition(new Vector2(-235f, 55f));

        // --- 4.2. Tạo Backdrop / Raycast Blocker (Full-Screen) ---
        // Phủ kín màn hình để chặn click nhầm vào cây/nắng trong lúc game tạm dừng,
        // đồng thời cho phép người chơi click ra ngoài vùng panel để đóng nhanh lời khuyên.
        GameObject backdropObj = new GameObject("Advisor_Backdrop", typeof(RectTransform), typeof(Image), typeof(Button));
        backdropObj.transform.SetParent(uiRoot.transform, false);
        RectTransform rtBackdrop = backdropObj.GetComponent<RectTransform>();
        rtBackdrop.anchorMin = Vector2.zero;
        rtBackdrop.anchorMax = Vector2.one;
        rtBackdrop.offsetMin = Vector2.zero;
        rtBackdrop.offsetMax = Vector2.zero;

        Image backdropImg = backdropObj.GetComponent<Image>();
        backdropImg.color = new Color(0f, 0f, 0f, 0.40f); // Làm mờ nhẹ nền game
        Button backdropBtn = backdropObj.GetComponent<Button>();
        ColorBlock backdropColors = backdropBtn.colors;
        backdropColors.normalColor = new Color(0f, 0f, 0f, 0.40f);
        backdropColors.highlightedColor = new Color(0f, 0f, 0f, 0.40f);
        backdropColors.pressedColor = new Color(0f, 0f, 0f, 0.48f);
        backdropBtn.colors = backdropColors;
        backdropObj.SetActive(false);

        // --- 4.3. Tạo Panel Lời Khuyên (Bottom-Center: dải ngang mép dưới sân cỏ) ---
        GameObject panelObj = new GameObject("Advisor_Panel", typeof(RectTransform), typeof(Image));
        panelObj.transform.SetParent(uiRoot.transform, false);
        RectTransform rtPanel = panelObj.GetComponent<RectTransform>();
        rtPanel.anchorMin = new Vector2(0.5f, 0f);
        rtPanel.anchorMax = new Vector2(0.5f, 0f);
        rtPanel.pivot = new Vector2(0.5f, 0.5f);
        rtPanel.anchoredPosition = new Vector2(25f, 48f); // Khớp chuẩn dải chữ nhật đỏ
        rtPanel.sizeDelta = new Vector2(440f, 72f);

        Image panelImg = panelObj.GetComponent<Image>();
        panelImg.color = new Color(0.08f, 0.12f, 0.16f, 0.94f); // Dark Slate sang trọng

        // Viền vàng kim tinh tế
        Outline panelOutline = panelObj.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.85f, 0.68f, 0.25f, 0.92f);
        panelOutline.effectDistance = new Vector2(2f, -2f);

        // Text Lời Khuyên
        GameObject advTextObj = new GameObject("AdviceText", typeof(RectTransform), typeof(Text));
        advTextObj.transform.SetParent(panelObj.transform, false);
        RectTransform rtAdvText = advTextObj.GetComponent<RectTransform>();
        rtAdvText.anchorMin = Vector2.zero;
        rtAdvText.anchorMax = Vector2.one;
        rtAdvText.offsetMin = new Vector2(16f, 6f);
        rtAdvText.offsetMax = new Vector2(-36f, -6f);

        Text advText = advTextObj.GetComponent<Text>();
        advText.text = "Lời khuyên chiến thuật từ AI sẽ xuất hiện tại đây...";
        advText.font = font;
        advText.fontSize = 15;
        advText.fontStyle = FontStyle.Bold;
        advText.alignment = TextAnchor.MiddleLeft;
        advText.color = new Color(1f, 0.96f, 0.85f); // Vàng kem dễ đọc
        advText.horizontalOverflow = HorizontalWrapMode.Wrap;
        advText.verticalOverflow = VerticalWrapMode.Truncate;

        // Loading Panel
        GameObject loadingPanelObj = new GameObject("LoadingPanel", typeof(RectTransform));
        loadingPanelObj.transform.SetParent(panelObj.transform, false);
        RectTransform rtLoading = loadingPanelObj.GetComponent<RectTransform>();
        rtLoading.anchorMin = Vector2.zero;
        rtLoading.anchorMax = Vector2.one;
        rtLoading.offsetMin = Vector2.zero;
        rtLoading.offsetMax = Vector2.zero;

        GameObject loadingTextObj = new GameObject("LoadingText", typeof(RectTransform), typeof(Text));
        loadingTextObj.transform.SetParent(loadingPanelObj.transform, false);
        RectTransform rtLoadText = loadingTextObj.GetComponent<RectTransform>();
        rtLoadText.anchorMin = Vector2.zero;
        rtLoadText.anchorMax = Vector2.one;
        rtLoadText.offsetMin = Vector2.zero;
        rtLoadText.offsetMax = Vector2.zero;

        Text loadText = loadingTextObj.GetComponent<Text>();
        loadText.text = "⏳ Đang phân tích chiến thuật...";
        loadText.font = font;
        loadText.fontSize = 15;
        loadText.fontStyle = FontStyle.Italic;
        loadText.alignment = TextAnchor.MiddleCenter;
        loadText.color = new Color(0.35f, 0.88f, 1f); // Xanh dương sáng
        loadingPanelObj.SetActive(false);

        // Error Text
        GameObject errTextObj = new GameObject("ErrorText", typeof(RectTransform), typeof(Text));
        errTextObj.transform.SetParent(panelObj.transform, false);
        RectTransform rtErrText = errTextObj.GetComponent<RectTransform>();
        rtErrText.anchorMin = Vector2.zero;
        rtErrText.anchorMax = Vector2.one;
        rtErrText.offsetMin = new Vector2(16f, 6f);
        rtErrText.offsetMax = new Vector2(-36f, -6f);

        Text errText = errTextObj.GetComponent<Text>();
        errText.text = "";
        errText.font = font;
        errText.fontSize = 14;
        errText.alignment = TextAnchor.MiddleCenter;
        errText.color = new Color(1f, 0.4f, 0.4f); // Đỏ cảnh báo
        errTextObj.SetActive(false);

        // Close Button
        GameObject closeBtnObj = new GameObject("Close_Button", typeof(RectTransform), typeof(Image), typeof(Button));
        closeBtnObj.transform.SetParent(panelObj.transform, false);
        RectTransform rtClose = closeBtnObj.GetComponent<RectTransform>();
        rtClose.anchorMin = new Vector2(1f, 1f);
        rtClose.anchorMax = new Vector2(1f, 1f);
        rtClose.pivot = new Vector2(1f, 1f);
        rtClose.anchoredPosition = new Vector2(-6f, -6f);
        rtClose.sizeDelta = new Vector2(22f, 22f);

        Image closeImg = closeBtnObj.GetComponent<Image>();
        closeImg.color = new Color(0.3f, 0.35f, 0.4f, 0.7f);
        Button closeBtn = closeBtnObj.GetComponent<Button>();

        GameObject closeTextObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        closeTextObj.transform.SetParent(closeBtnObj.transform, false);
        RectTransform rtCloseText = closeTextObj.GetComponent<RectTransform>();
        rtCloseText.anchorMin = Vector2.zero;
        rtCloseText.anchorMax = Vector2.one;
        rtCloseText.offsetMin = Vector2.zero;
        rtCloseText.offsetMax = Vector2.zero;

        Text closeText = closeTextObj.GetComponent<Text>();
        closeText.text = "✕";
        closeText.font = font;
        closeText.fontSize = 13;
        closeText.fontStyle = FontStyle.Bold;
        closeText.alignment = TextAnchor.MiddleCenter;
        closeText.color = Color.white;

        // Ẩn panel mặc định
        panelObj.SetActive(false);

        // --- 5. Liên kết tất cả vào AIAdvisorUI ---
        advisorUI.advisorPanel = panelObj;
        advisorUI.backdropButton = backdropBtn;
        advisorUI.adviceText = advText;
        advisorUI.askButton = btn;
        advisorUI.animatedAdvisor = animAdvisor;
        advisorUI.loadingPanel = loadingPanelObj;
        advisorUI.loadingText = loadText;
        advisorUI.errorText = errText;
        advisorUI.closeButton = closeBtn;
        advisorUI.fadeDuration = 0.4f;
        advisorUI.autoHideAfterSeconds = 8f;
        advisorUI.pauseGameWhileAdvising = true;

        EditorUtility.SetDirty(advisorObj);
        EditorUtility.SetDirty(advisorUI);
        EditorUtility.SetDirty(connector);
        EditorSceneManager.MarkSceneDirty(advisorObj.scene);

        Selection.activeGameObject = advisorObj;

        EditorUtility.DisplayDialog(
            "Cài đặt hoàn tất!",
            "✅ Đã thiết lập AI Advisor thành công vào Scene!\n\n" +
            "• Vị trí nút hoạt họa: Góc dưới bên trái (cạnh hiên nhà).\n" +
            "• Vị trí khung lời khuyên: Dải mép dưới sân cỏ.\n" +
            "• Nút TÙY CHỌN ở góc trên đã hoàn toàn thông thoáng!\n\n" +
            "Hãy nhấn Ctrl + S để lưu Scene và bấm Play ▶️ để thử nghiệm!",
            "Tuyệt vời!"
        );
    }

    private static Canvas FindBestCanvas()
    {
        // Ưu tiên TopCanvas
        GameObject topCanvasObj = GameObject.Find("TopCanvas");
        if (topCanvasObj != null)
        {
            Canvas c = topCanvasObj.GetComponent<Canvas>();
            if (c != null) return c;
        }

        // Sau đó tìm Canvas bất kỳ trong Scene
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        Canvas highestOrderCanvas = null;
        int maxOrder = int.MinValue;

        foreach (var c in canvases)
        {
            if (c.isRootCanvas && c.sortingOrder > maxOrder)
            {
                maxOrder = c.sortingOrder;
                highestOrderCanvas = c;
            }
        }

        return highestOrderCanvas;
    }
}
#endif
