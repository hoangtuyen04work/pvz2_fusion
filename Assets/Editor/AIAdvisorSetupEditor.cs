#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tool tiện ích trên Unity Editor: Tự động thiết lập GameObject AI Advisor
/// và toàn bộ hệ thống UI (Nút bấm, Panel lời khuyên, Loading, Kết nối) chỉ với 1 cú click.
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

        // --- 4.1. Tạo Nút "Hỏi Trợ Lý" (Top-Right) ---
        GameObject btnObj = new GameObject("AskAdvisor_Button", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(uiRoot.transform, false);
        RectTransform rtBtn = btnObj.GetComponent<RectTransform>();
        rtBtn.anchorMin = new Vector2(1, 1);
        rtBtn.anchorMax = new Vector2(1, 1);
        rtBtn.pivot = new Vector2(1, 1);
        rtBtn.anchoredPosition = new Vector2(-20, -20);
        rtBtn.sizeDelta = new Vector2(160, 46);

        Image btnImg = btnObj.GetComponent<Image>();
        btnImg.color = new Color(0.12f, 0.45f, 0.22f, 0.95f); // Xanh lá đậm PvZ
        Button btn = btnObj.GetComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = new Color(0.12f, 0.45f, 0.22f, 0.95f);
        colors.highlightedColor = new Color(0.18f, 0.60f, 0.30f, 1f);
        colors.pressedColor = new Color(0.08f, 0.35f, 0.15f, 1f);
        btn.colors = colors;

        GameObject btnTextObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        btnTextObj.transform.SetParent(btnObj.transform, false);
        RectTransform rtBtnText = btnTextObj.GetComponent<RectTransform>();
        rtBtnText.anchorMin = Vector2.zero;
        rtBtnText.anchorMax = Vector2.one;
        rtBtnText.offsetMin = Vector2.zero;
        rtBtnText.offsetMax = Vector2.zero;

        Text btnText = btnTextObj.GetComponent<Text>();
        btnText.text = "💡 Hỏi Trợ Lý";
        btnText.font = font;
        btnText.fontSize = 18;
        btnText.fontStyle = FontStyle.Bold;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.color = Color.white;

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

        // --- 4.3. Tạo Panel Lời Khuyên (Top-Center) ---
        GameObject panelObj = new GameObject("Advisor_Panel", typeof(RectTransform), typeof(Image));
        panelObj.transform.SetParent(uiRoot.transform, false);
        RectTransform rtPanel = panelObj.GetComponent<RectTransform>();
        rtPanel.anchorMin = new Vector2(0.5f, 1f);
        rtPanel.anchorMax = new Vector2(0.5f, 1f);
        rtPanel.pivot = new Vector2(0.5f, 1f);
        rtPanel.anchoredPosition = new Vector2(0, -75);
        rtPanel.sizeDelta = new Vector2(680, 115);

        Image panelImg = panelObj.GetComponent<Image>();
        panelImg.color = new Color(0.06f, 0.10f, 0.16f, 0.94f); // Dark Slate sang trọng

        // Text Lời Khuyên
        GameObject advTextObj = new GameObject("AdviceText", typeof(RectTransform), typeof(Text));
        advTextObj.transform.SetParent(panelObj.transform, false);
        RectTransform rtAdvText = advTextObj.GetComponent<RectTransform>();
        rtAdvText.anchorMin = Vector2.zero;
        rtAdvText.anchorMax = Vector2.one;
        rtAdvText.offsetMin = new Vector2(24, 12);
        rtAdvText.offsetMax = new Vector2(-48, -12);

        Text advText = advTextObj.GetComponent<Text>();
        advText.text = "Lời khuyên chiến thuật từ AI sẽ xuất hiện tại đây...";
        advText.font = font;
        advText.fontSize = 19;
        advText.fontStyle = FontStyle.Bold;
        advText.alignment = TextAnchor.MiddleLeft;
        advText.color = new Color(1f, 0.96f, 0.82f); // Vàng kem dễ đọc
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
        loadText.fontSize = 18;
        loadText.fontStyle = FontStyle.Italic;
        loadText.alignment = TextAnchor.MiddleCenter;
        loadText.color = new Color(0.4f, 0.85f, 1f); // Xanh dương sáng
        loadingPanelObj.SetActive(false);

        // Error Text
        GameObject errTextObj = new GameObject("ErrorText", typeof(RectTransform), typeof(Text));
        errTextObj.transform.SetParent(panelObj.transform, false);
        RectTransform rtErrText = errTextObj.GetComponent<RectTransform>();
        rtErrText.anchorMin = Vector2.zero;
        rtErrText.anchorMax = Vector2.one;
        rtErrText.offsetMin = new Vector2(24, 12);
        rtErrText.offsetMax = new Vector2(-48, -12);

        Text errText = errTextObj.GetComponent<Text>();
        errText.text = "";
        errText.font = font;
        errText.fontSize = 16;
        errText.alignment = TextAnchor.MiddleCenter;
        errText.color = new Color(1f, 0.4f, 0.4f); // Đỏ cảnh báo
        errTextObj.SetActive(false);

        // Close Button
        GameObject closeBtnObj = new GameObject("Close_Button", typeof(RectTransform), typeof(Image), typeof(Button));
        closeBtnObj.transform.SetParent(panelObj.transform, false);
        RectTransform rtClose = closeBtnObj.GetComponent<RectTransform>();
        rtClose.anchorMin = new Vector2(1, 1);
        rtClose.anchorMax = new Vector2(1, 1);
        rtClose.pivot = new Vector2(1, 1);
        rtClose.anchoredPosition = new Vector2(-8, -8);
        rtClose.sizeDelta = new Vector2(28, 28);

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
        closeText.fontSize = 15;
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
            "• Server URL: http://localhost:8000/analyze\n" +
            "• Chế độ Mock: Đã tắt (Use Mock Mode = false)\n" +
            "• Đã tạo nút [💡 Hỏi Trợ Lý] và Panel hiển thị lời khuyên.\n\n" +
            "Hãy nhấn Ctrl + S để lưu Scene, khởi động server Python và bấm Play ▶️ để thử nghiệm!",
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
