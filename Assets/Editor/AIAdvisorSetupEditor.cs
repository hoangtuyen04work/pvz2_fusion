#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tool tiện ích trên Unity Editor: Tự động thiết lập GameObject AI Advisor
/// theo đúng thiết kế Capsule Bar đáy màn hình (Bottom HUD) của người dùng:
/// - Nút bấm: Vòng tròn kim loại Sci-Fi có vòng sáng Neon Cyan, badge AI, Avatar nằm trong Mask tròn.
/// - Khung lời khuyên: Thanh Capsule dẹt (Pill-shaped Banner) tinh tế nằm ngang mép dưới màn hình, 
///   kết nối trực tiếp với nút bấm bên trái, hoàn toàn KHÔNG che bất kỳ hàng cây nào trên sân cỏ!
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

        // --- 4.1. Tạo Backdrop / Raycast Blocker (Full-Screen) ---
        GameObject backdropObj = new GameObject("Advisor_Backdrop", typeof(RectTransform), typeof(Image), typeof(Button));
        backdropObj.transform.SetParent(uiRoot.transform, false);
        RectTransform rtBackdrop = backdropObj.GetComponent<RectTransform>();
        rtBackdrop.anchorMin = Vector2.zero;
        rtBackdrop.anchorMax = Vector2.one;
        rtBackdrop.offsetMin = Vector2.zero;
        rtBackdrop.offsetMax = Vector2.zero;

        Image backdropImg = backdropObj.GetComponent<Image>();
        backdropImg.color = new Color(0f, 0f, 0f, 0.30f); // Làm mờ nhẹ nền game
        Button backdropBtn = backdropObj.GetComponent<Button>();
        ColorBlock backdropColors = backdropBtn.colors;
        backdropColors.normalColor = new Color(0f, 0f, 0f, 0.30f);
        backdropColors.highlightedColor = new Color(0f, 0f, 0f, 0.30f);
        backdropColors.pressedColor = new Color(0f, 0f, 0f, 0.40f);
        backdropBtn.colors = backdropColors;
        backdropObj.SetActive(false);

        // --- 4.2. Tạo Khung Lời Khuyên Capsule Dẹt Mép Dưới (Bottom Capsule Banner) ---
        // Đặt ở vị trí trước nút bấm trong Hierarchy để nút bấm đè lên đầu bên trái của banner
        GameObject panelObj = new GameObject("Advisor_Panel", typeof(RectTransform), typeof(Image));
        panelObj.transform.SetParent(uiRoot.transform, false);
        RectTransform rtPanel = panelObj.GetComponent<RectTransform>();
        rtPanel.anchorMin = new Vector2(0.5f, 0f);
        rtPanel.anchorMax = new Vector2(0.5f, 0f);
        rtPanel.pivot = new Vector2(0.5f, 0.5f);
        rtPanel.anchoredPosition = new Vector2(30f, 48f); // Căn thẳng hàng Y với nút bấm ở mép dưới
        rtPanel.sizeDelta = new Vector2(490f, 62f); // Thanh Capsule thon gọn, thanh thoát

        Image panelImg = panelObj.GetComponent<Image>();
        Sprite capsuleSprite = Resources.Load<Sprite>("Sprites/UI/AIAdvisor/AI_Capsule_Banner_Bg");
        if (capsuleSprite != null)
        {
            panelImg.sprite = capsuleSprite;
            panelImg.type = Image.Type.Sliced;
            panelImg.color = Color.white;
        }
        else
        {
            panelImg.color = new Color(0.08f, 0.20f, 0.16f, 0.90f);
        }

        // Nút Đóng ("✕") ở góc phải thanh Capsule
        GameObject closeBtnObj = new GameObject("Close_Button", typeof(RectTransform), typeof(Image), typeof(Button));
        closeBtnObj.transform.SetParent(panelObj.transform, false);
        RectTransform rtClose = closeBtnObj.GetComponent<RectTransform>();
        rtClose.anchorMin = new Vector2(1f, 0.5f);
        rtClose.anchorMax = new Vector2(1f, 0.5f);
        rtClose.pivot = new Vector2(0.5f, 0.5f);
        rtClose.anchoredPosition = new Vector2(-22f, 0f);
        rtClose.sizeDelta = new Vector2(22f, 22f);

        Image closeImg = closeBtnObj.GetComponent<Image>();
        closeImg.sprite = Resources.Load<Sprite>("Sprites/UI/AIAdvisor/AI_Close_Btn");
        closeImg.color = Color.white;
        Button closeBtn = closeBtnObj.GetComponent<Button>();

        // Text Lời Khuyên Chiến Thuật (Căn giữa theo chiều dọc, hỗ trợ Rich Text bold)
        GameObject advTextObj = new GameObject("AdviceText", typeof(RectTransform), typeof(Text));
        advTextObj.transform.SetParent(panelObj.transform, false);
        RectTransform rtAdvText = advTextObj.GetComponent<RectTransform>();
        rtAdvText.anchorMin = Vector2.zero;
        rtAdvText.anchorMax = Vector2.one;
        // Để lề trái 48px cho nút avatar tròn đè lên, lề phải 38px cho nút đóng
        rtAdvText.offsetMin = new Vector2(48f, 6f);
        rtAdvText.offsetMax = new Vector2(-38f, -6f);

        Text advText = advTextObj.GetComponent<Text>();
        advText.text = "Lời khuyên chiến thuật từ AI sẽ xuất hiện tại đây...";
        advText.font = font;
        advText.fontSize = 14;
        advText.fontStyle = FontStyle.Normal;
        advText.supportRichText = true;
        advText.lineSpacing = 1.15f;
        advText.alignment = TextAnchor.MiddleLeft;
        advText.color = new Color(0.96f, 0.98f, 1f);
        advText.horizontalOverflow = HorizontalWrapMode.Wrap;
        advText.verticalOverflow = VerticalWrapMode.Truncate;

        Outline advOutline = advTextObj.AddComponent<Outline>();
        advOutline.effectColor = new Color(0.04f, 0.12f, 0.10f, 0.90f);
        advOutline.effectDistance = new Vector2(1f, -1f);

        // Loading Panel
        GameObject loadingPanelObj = new GameObject("LoadingPanel", typeof(RectTransform));
        loadingPanelObj.transform.SetParent(panelObj.transform, false);
        RectTransform rtLoading = loadingPanelObj.GetComponent<RectTransform>();
        rtLoading.anchorMin = Vector2.zero;
        rtLoading.anchorMax = Vector2.one;
        rtLoading.offsetMin = new Vector2(48f, 6f);
        rtLoading.offsetMax = new Vector2(-38f, -6f);

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
        loadText.fontSize = 14;
        loadText.fontStyle = FontStyle.Bold;
        loadText.alignment = TextAnchor.MiddleLeft;
        loadText.color = new Color(0.38f, 0.92f, 1f); // Xanh neon sáng
        loadingPanelObj.SetActive(false);

        // Error Text
        GameObject errTextObj = new GameObject("ErrorText", typeof(RectTransform), typeof(Text));
        errTextObj.transform.SetParent(panelObj.transform, false);
        RectTransform rtErrText = errTextObj.GetComponent<RectTransform>();
        rtErrText.anchorMin = Vector2.zero;
        rtErrText.anchorMax = Vector2.one;
        rtErrText.offsetMin = new Vector2(48f, 6f);
        rtErrText.offsetMax = new Vector2(-38f, -6f);

        Text errText = errTextObj.GetComponent<Text>();
        errText.text = "";
        errText.font = font;
        errText.fontSize = 13;
        errText.alignment = TextAnchor.MiddleLeft;
        errText.color = new Color(1f, 0.45f, 0.45f);
        errTextObj.SetActive(false);

        // Ẩn panel mặc định
        panelObj.SetActive(false);

        // --- 4.3. Tạo Nút Vòng Tròn Kim Loại Neon Cyan (Nằm đè lên đầu trái thanh Capsule) ---
        GameObject btnObj = new GameObject("AskAdvisor_Button", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(uiRoot.transform, false);
        RectTransform rtBtn = btnObj.GetComponent<RectTransform>();
        rtBtn.anchorMin = new Vector2(0.5f, 0f);
        rtBtn.anchorMax = new Vector2(0.5f, 0f);
        rtBtn.pivot = new Vector2(0.5f, 0.5f);
        rtBtn.anchoredPosition = new Vector2(-226f, 48f); // Cùng độ cao Y = 48f với thanh Capsule
        rtBtn.sizeDelta = new Vector2(68f, 68f);

        Image btnImg = btnObj.GetComponent<Image>();
        btnImg.color = Color.clear; // Nền trong suốt

        Button btn = btnObj.GetComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        btn.colors = colors;

        // Mask tròn bên trong: Bảo đảm BẤT KỲ ẢNH NÀO người dùng bỏ vào cũng được bo tròn tự nhiên
        GameObject maskObj = new GameObject("Inner_Mask", typeof(RectTransform), typeof(Image), typeof(Mask));
        maskObj.transform.SetParent(btnObj.transform, false);
        RectTransform rtMask = maskObj.GetComponent<RectTransform>();
        rtMask.anchorMin = Vector2.zero;
        rtMask.anchorMax = Vector2.one;
        rtMask.offsetMin = Vector2.zero;
        rtMask.offsetMax = Vector2.zero;

        Image maskImg = maskObj.GetComponent<Image>();
        maskImg.sprite = Resources.Load<Sprite>("Sprites/UI/AIAdvisor/AI_Circle_Mask");
        maskImg.color = new Color(0.06f, 0.12f, 0.18f, 0.95f);
        Mask mask = maskObj.GetComponent<Mask>();
        mask.showMaskGraphic = true;

        // Avatar bên trong Mask tròn
        GameObject avatarObj = new GameObject("Avatar_Image", typeof(RectTransform), typeof(Image));
        avatarObj.transform.SetParent(maskObj.transform, false);
        RectTransform rtAvatar = avatarObj.GetComponent<RectTransform>();
        rtAvatar.anchorMin = new Vector2(0.5f, 0.5f);
        rtAvatar.anchorMax = new Vector2(0.5f, 0.5f);
        rtAvatar.pivot = new Vector2(0.5f, 0.5f);
        rtAvatar.anchoredPosition = new Vector2(-1.5f, -1.5f);
        rtAvatar.sizeDelta = new Vector2(46f, 46f);

        Image avatarImg = avatarObj.GetComponent<Image>();
        avatarImg.preserveAspect = true;
        avatarImg.raycastTarget = false;

        // Nạp sprite đầu Dave nguyên vẹn (đầy đủ chảo, râu, không bị cắt xén)
        Sprite daveHeadSprite = Resources.Load<Sprite>("Sprites/CrazyDave/Dave_Head_Transparent");
        if (daveHeadSprite == null)
            daveHeadSprite = Resources.Load<Sprite>("Sprites/CrazyDave/Enter/CrazyDave_Enter0018");
        avatarImg.sprite = daveHeadSprite;

        // Vòng Bezel kim loại Neon Cyan phủ lên trên cùng
        GameObject bezelObj = new GameObject("Bezel_Overlay", typeof(RectTransform), typeof(Image));
        bezelObj.transform.SetParent(btnObj.transform, false);
        RectTransform rtBezel = bezelObj.GetComponent<RectTransform>();
        rtBezel.anchorMin = Vector2.zero;
        rtBezel.anchorMax = Vector2.one;
        rtBezel.offsetMin = Vector2.zero;
        rtBezel.offsetMax = Vector2.zero;

        Image bezelImg = bezelObj.GetComponent<Image>();
        bezelImg.sprite = Resources.Load<Sprite>("Sprites/UI/AIAdvisor/AI_Ring_Button_Bezel");
        bezelImg.color = Color.white;
        bezelImg.raycastTarget = false;

        // Gắn component hoạt họa sống động UIAnimatedAdvisor
        UIAnimatedAdvisor animAdvisor = btnObj.AddComponent<UIAnimatedAdvisor>();
        animAdvisor.avatarImage = avatarImg;
        animAdvisor.enableBreathing = true;
        animAdvisor.breathingFrequency = 3f;
        animAdvisor.breathingScaleAmount = 0.04f;
        animAdvisor.floatingYAmount = 2f;

        // Nạp âm thanh thoại ngắn vui nhộn của Dave nếu có
        List<AudioClip> voiceClips = new List<AudioClip>();
        for (int i = 1; i <= 3; i++)
        {
            AudioClip c = Resources.Load<AudioClip>($"Sounds/CrazyDave/CrazyDave_Short{i}");
            if (c != null) voiceClips.Add(c);
        }
        animAdvisor.clickVoiceClips = voiceClips.ToArray();
        animAdvisor.SetBasePosition(new Vector2(-226f, 48f));

        // --- 5. Liên kết tất cả vào AIAdvisorUI ---
        advisorUI.advisorPanel = panelObj;
        advisorUI.backdropButton = backdropBtn;
        advisorUI.adviceText = advText;
        advisorUI.askButton = btn;
        advisorUI.animatedAdvisor = animAdvisor;
        advisorUI.panelAvatarImage = null; // Avatar nút nằm ngay trên đầu bên trái của thanh capsule
        advisorUI.electricArc = null;
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
            "✅ Đã thiết lập UI dạng Capsule Bar mép dưới chuẩn 100% theo Mockup mới!\n\n" +
            "• Nút Avatar Sci-Fi: Nằm gọn ở mép dưới bên trái.\n" +
            "• Khung lời khuyên: Thanh Capsule dẹt nằm ngang dọc mép dưới, ăn khớp liền mạch với nút Avatar.\n" +
            "• Chiều cao nhỏ gọn (chỉ 62px): Hoàn toàn KHÔNG che bất kỳ cây trồng nào trên sân cỏ!\n\n" +
            "Hãy nhấn Ctrl + S để lưu Scene và bấm Play ▶️ để trải nghiệm!",
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
