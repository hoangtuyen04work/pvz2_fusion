using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Giao diện Popup Bách Khoa Toàn Thư Của Dave (The Suburban Almanac Q&A)
/// Thiết kế cao cấp, chuẩn 100% phong cách Plants vs. Zombies:
/// - Toàn bộ popup có Backdrop mờ tối che phủ menu chính, loại bỏ hoàn toàn sự rối mắt.
/// - Khung cửa sổ cân đối, bo tròn viền gỗ sẫm / đất nung với Header vàng rực rỡ và Avatar Dave đội chảo.
/// - Khu vực trò chuyện thoáng đãng, bong bóng chat phân biệt rõ nét giữa Bạn và Dave.
/// - Chip gợi ý nhanh 1 dòng gọn gàng, ô nhập liệu hiện đại hỗ trợ Enter, nút GỬI nổi bật.
/// </summary>
public class AIQAPopup : MonoBehaviour
{
    public Font uiFont;
    public Sprite stoneButtonNormal;
    public Sprite stoneButtonHighlighted;
    public Sprite stoneButtonPressed;
    public Action onCloseCallback;

    private ScrollRect chatScrollRect;
    private RectTransform chatContent;
    private InputField questionInput;
    private Button sendButton;
    private Text sendButtonText;
    private GameObject thinkingObj;
    private bool isWaitingForResponse = false;

    private readonly string[] quickPrompts = new string[]
    {
        "Cơ chế ghép cây Fusion?",
        "Cách khắc chế Zombie Băng?",
        "Mẹo tích lũy Nắng ban đêm?"
    };

    public static AIQAPopup Create(Transform canvasTransform, Font font, Sprite stoneBtn, Action onClose)
    {
        // 1. Root Modal phủ kín toàn màn hình (Screen-Space Fullscreen)
        var modalRoot = new GameObject("Almanac_QA_Modal", typeof(RectTransform), typeof(CanvasGroup), typeof(AIQAPopup));
        modalRoot.transform.SetParent(canvasTransform, false);

        var rtRoot = modalRoot.GetComponent<RectTransform>();
        rtRoot.anchorMin = Vector2.zero;
        rtRoot.anchorMax = Vector2.one;
        rtRoot.offsetMin = Vector2.zero;
        rtRoot.offsetMax = Vector2.zero;

        var popup = modalRoot.GetComponent<AIQAPopup>();
        popup.uiFont = font != null ? font : Resources.Load<Font>("Fonts/Baloo2");
        popup.stoneButtonNormal = stoneBtn != null ? stoneBtn : Resources.Load<Sprite>("Sprites/UI/Menu/button_normal");
        popup.stoneButtonHighlighted = Resources.Load<Sprite>("Sprites/UI/Menu/button_highlighted");
        popup.stoneButtonPressed = Resources.Load<Sprite>("Sprites/UI/Menu/button_pressed");
        popup.onCloseCallback = onClose;

        popup.BuildUI();
        return popup;
    }

    private void BuildUI()
    {
        // --- 1. Backdrop Dimmer Overlay (Màn đen mờ che phủ toàn màn hình) ---
        var backdropObj = new GameObject("Backdrop_Dimmer", typeof(RectTransform), typeof(Image), typeof(Button));
        backdropObj.transform.SetParent(transform, false);
        var rtBackdrop = backdropObj.GetComponent<RectTransform>();
        rtBackdrop.anchorMin = Vector2.zero;
        rtBackdrop.anchorMax = Vector2.one;
        rtBackdrop.offsetMin = Vector2.zero;
        rtBackdrop.offsetMax = Vector2.zero;

        var backdropImg = backdropObj.GetComponent<Image>();
        backdropImg.color = new Color(0f, 0f, 0f, 0.72f);
        var backdropBtn = backdropObj.GetComponent<Button>();
        backdropBtn.transition = Selectable.Transition.None;
        backdropBtn.onClick.AddListener(ClosePopup);

        // --- 2. Cửa sổ Popup chính (Tỷ lệ vàng 740x570 đặt ở trung tâm) ---
        var windowObj = new GameObject("Dialog_Window", typeof(RectTransform), typeof(Image));
        windowObj.transform.SetParent(transform, false);
        var rtWindow = windowObj.GetComponent<RectTransform>();
        rtWindow.anchorMin = new Vector2(0.5f, 0.5f);
        rtWindow.anchorMax = new Vector2(0.5f, 0.5f);
        rtWindow.pivot = new Vector2(0.5f, 0.5f);
        rtWindow.anchoredPosition = Vector2.zero;
        rtWindow.sizeDelta = new Vector2(740f, 570f);

        // Nền phiến gỗ/đất rêu sẫm đặc trưng của PvZ
        var windowImg = windowObj.GetComponent<Image>();
        windowImg.color = new Color(0.085f, 0.12f, 0.065f, 0.985f);

        // Viền ngoài nổi khối giả lập khung gỗ cổ (Window Frame Border)
        var borderObj = new GameObject("Window_Border", typeof(RectTransform), typeof(Image));
        borderObj.transform.SetParent(windowObj.transform, false);
        var rtBorder = borderObj.GetComponent<RectTransform>();
        rtBorder.anchorMin = Vector2.zero;
        rtBorder.anchorMax = Vector2.one;
        rtBorder.offsetMin = new Vector2(-4f, -4f);
        rtBorder.offsetMax = new Vector2(4f, 4f);
        var borderImg = borderObj.GetComponent<Image>();
        borderImg.color = new Color(0.38f, 0.28f, 0.17f, 1f);
        borderObj.transform.SetAsFirstSibling();

        // --- 3. Thanh Tiêu Đề (Header Bar) ---
        var headerObj = new GameObject("Header_Bar", typeof(RectTransform), typeof(Image));
        headerObj.transform.SetParent(windowObj.transform, false);
        var rtHeader = headerObj.GetComponent<RectTransform>();
        rtHeader.anchorMin = new Vector2(0f, 1f);
        rtHeader.anchorMax = new Vector2(1f, 1f);
        rtHeader.pivot = new Vector2(0.5f, 1f);
        rtHeader.anchoredPosition = Vector2.zero;
        rtHeader.sizeDelta = new Vector2(0f, 68f);
        var headerImg = headerObj.GetComponent<Image>();
        headerImg.color = new Color(0.15f, 0.11f, 0.07f, 0.95f);

        // Khung tròn chứa Avatar Crazy Dave
        var avatarCircleObj = new GameObject("Avatar_Ring", typeof(RectTransform), typeof(Image));
        avatarCircleObj.transform.SetParent(headerObj.transform, false);
        var rtAvCircle = avatarCircleObj.GetComponent<RectTransform>();
        rtAvCircle.anchorMin = new Vector2(0f, 0.5f);
        rtAvCircle.anchorMax = new Vector2(0f, 0.5f);
        rtAvCircle.pivot = new Vector2(0.5f, 0.5f);
        rtAvCircle.anchoredPosition = new Vector2(40f, 0f);
        rtAvCircle.sizeDelta = new Vector2(54f, 54f);
        var avCircleImg = avatarCircleObj.GetComponent<Image>();
        avCircleImg.color = new Color(0.95f, 0.75f, 0.20f, 1f);

        var avatarObj = new GameObject("Dave_Avatar", typeof(RectTransform), typeof(Image));
        avatarObj.transform.SetParent(avatarCircleObj.transform, false);
        var rtAvatar = avatarObj.GetComponent<RectTransform>();
        rtAvatar.anchorMin = new Vector2(0.5f, 0.5f);
        rtAvatar.anchorMax = new Vector2(0.5f, 0.5f);
        rtAvatar.pivot = new Vector2(0.5f, 0.5f);
        rtAvatar.anchoredPosition = new Vector2(-1f, -1f);
        rtAvatar.sizeDelta = new Vector2(48f, 48f);
        var avatarImg = avatarObj.GetComponent<Image>();
        avatarImg.sprite = Resources.Load<Sprite>("Sprites/CrazyDave/Dave_Head_Transparent");
        if (avatarImg.sprite == null)
            avatarImg.sprite = Resources.Load<Sprite>("Sprites/CrazyDave/Enter/CrazyDave_Enter0018");
        avatarImg.preserveAspect = true;

        // Tiêu đề 2 dòng
        var titleText = CreateText("Title_Main", headerObj.transform, "📖 BÁCH KHOA TOÀN THƯ CỦA DAVE", 25, TextAnchor.MiddleLeft, new Color(1f, 0.88f, 0.22f));
        var rtT1 = titleText.rectTransform;
        rtT1.anchorMin = new Vector2(0f, 0.45f);
        rtT1.anchorMax = new Vector2(0.85f, 1f);
        rtT1.offsetMin = new Vector2(75f, 0f);
        rtT1.offsetMax = Vector2.zero;

        var subTitleText = CreateText("Title_Sub", headerObj.transform, "Hỏi đáp chiến thuật & cơ chế cùng Crazy Dave", 15, TextAnchor.MiddleLeft, new Color(0.68f, 0.88f, 0.52f));
        var rtT2 = subTitleText.rectTransform;
        rtT2.anchorMin = new Vector2(0f, 0f);
        rtT2.anchorMax = new Vector2(0.85f, 0.48f);
        rtT2.offsetMin = new Vector2(75f, 0f);
        rtT2.offsetMax = Vector2.zero;

        // Nút Đóng [✕] góc trên bên phải
        var closeBtnObj = CreateStoneButton("Close_Btn", headerObj.transform, "✕", 22, ClosePopup);
        var rtClose = closeBtnObj.GetComponent<RectTransform>();
        rtClose.anchorMin = new Vector2(1f, 0.5f);
        rtClose.anchorMax = new Vector2(1f, 0.5f);
        rtClose.pivot = new Vector2(1f, 0.5f);
        rtClose.anchoredPosition = new Vector2(-12f, 0f);
        rtClose.sizeDelta = new Vector2(44f, 40f);

        // --- 4. Khu Vực Trò Chuyện (Chat Scroll Area) ---
        var chatFrameObj = new GameObject("Chat_Frame", typeof(RectTransform), typeof(Image), typeof(Mask));
        chatFrameObj.transform.SetParent(windowObj.transform, false);
        var rtChatFrame = chatFrameObj.GetComponent<RectTransform>();
        rtChatFrame.anchorMin = new Vector2(0f, 0.205f);
        rtChatFrame.anchorMax = new Vector2(1f, 0.865f);
        rtChatFrame.offsetMin = new Vector2(18f, 0f);
        rtChatFrame.offsetMax = new Vector2(-18f, 0f);
        var chatFrameImg = chatFrameObj.GetComponent<Image>();
        chatFrameImg.color = new Color(0.045f, 0.07f, 0.035f, 0.70f);
        chatFrameObj.GetComponent<Mask>().showMaskGraphic = true;

        var contentObj = new GameObject("Chat_Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObj.transform.SetParent(chatFrameObj.transform, false);
        chatContent = contentObj.GetComponent<RectTransform>();
        chatContent.anchorMin = new Vector2(0f, 1f);
        chatContent.anchorMax = new Vector2(1f, 1f);
        chatContent.pivot = new Vector2(0.5f, 1f);
        chatContent.anchoredPosition = Vector2.zero;
        chatContent.sizeDelta = Vector2.zero;

        var vlg = contentObj.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(14, 14, 14, 14);
        vlg.spacing = 12f;
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var csf = contentObj.GetComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        chatScrollRect = chatFrameObj.AddComponent<ScrollRect>();
        chatScrollRect.viewport = rtChatFrame;
        chatScrollRect.content = chatContent;
        chatScrollRect.horizontal = false;
        chatScrollRect.vertical = true;
        chatScrollRect.movementType = ScrollRect.MovementType.Elastic;
        chatScrollRect.scrollSensitivity = 38f;

        // --- 5. Thanh Gợi Ý Nhanh (Quick Suggestions Chips) ---
        var quickRowObj = new GameObject("Quick_Chips_Row", typeof(RectTransform));
        quickRowObj.transform.SetParent(windowObj.transform, false);
        var rtQuickRow = quickRowObj.GetComponent<RectTransform>();
        rtQuickRow.anchorMin = new Vector2(0f, 0.125f);
        rtQuickRow.anchorMax = new Vector2(1f, 0.190f);
        rtQuickRow.offsetMin = new Vector2(18f, 0f);
        rtQuickRow.offsetMax = new Vector2(-18f, 0f);

        // Nhãn "💡 Gợi ý:"
        var hintLabel = CreateText("Hint_Label", quickRowObj.transform, "💡 Gợi ý:", 14, TextAnchor.MiddleLeft, new Color(1f, 0.90f, 0.45f));
        var rtHint = hintLabel.rectTransform;
        rtHint.anchorMin = new Vector2(0f, 0f);
        rtHint.anchorMax = new Vector2(0.12f, 1f);
        rtHint.offsetMin = Vector2.zero;
        rtHint.offsetMax = Vector2.zero;

        // Khung chứa 3 nút chip
        var chipsContainer = new GameObject("Chips_Container", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        chipsContainer.transform.SetParent(quickRowObj.transform, false);
        var rtChips = chipsContainer.GetComponent<RectTransform>();
        rtChips.anchorMin = new Vector2(0.13f, 0f);
        rtChips.anchorMax = new Vector2(1f, 1f);
        rtChips.offsetMin = Vector2.zero;
        rtChips.offsetMax = Vector2.zero;

        var hlg = chipsContainer.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10f;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = true;

        for (int i = 0; i < quickPrompts.Length; i++)
        {
            string pText = quickPrompts[i];
            var chipObj = CreateChipButton($"Chip_{i}", chipsContainer.transform, pText, () => OnClickChip(pText));
        }

        // Dòng chữ chỉ báo suy nghĩ (Thinking Indicator)
        thinkingObj = CreateText("Thinking_Text", windowObj.transform, "🧠 Dave đang lục lọi trong trí nhớ...", 15, TextAnchor.MiddleLeft, new Color(1f, 0.88f, 0.35f)).gameObject;
        var rtThink = thinkingObj.GetComponent<RectTransform>();
        rtThink.anchorMin = new Vector2(0f, 0.195f);
        rtThink.anchorMax = new Vector2(0.70f, 0.225f);
        rtThink.offsetMin = new Vector2(22f, 0f);
        rtThink.offsetMax = Vector2.zero;
        thinkingObj.SetActive(false);

        // --- 6. Hàng Nhập Liệu & Nút Gửi (Input & Action Row) ---
        var inputRowObj = new GameObject("Input_Row", typeof(RectTransform));
        inputRowObj.transform.SetParent(windowObj.transform, false);
        var rtInputRow = inputRowObj.GetComponent<RectTransform>();
        rtInputRow.anchorMin = new Vector2(0f, 0.025f);
        rtInputRow.anchorMax = new Vector2(1f, 0.115f);
        rtInputRow.offsetMin = new Vector2(18f, 0f);
        rtInputRow.offsetMax = new Vector2(-18f, 0f);

        // Khung InputField
        var inputFieldObj = new GameObject("Input_Field", typeof(RectTransform), typeof(Image), typeof(InputField));
        inputFieldObj.transform.SetParent(inputRowObj.transform, false);
        var rtInput = inputFieldObj.GetComponent<RectTransform>();
        rtInput.anchorMin = new Vector2(0f, 0f);
        rtInput.anchorMax = new Vector2(0.815f, 1f);
        rtInput.offsetMin = Vector2.zero;
        rtInput.offsetMax = Vector2.zero;

        var inputImg = inputFieldObj.GetComponent<Image>();
        inputImg.color = new Color(0.12f, 0.16f, 0.09f, 0.98f);

        var placeholderText = CreateText("Placeholder", inputFieldObj.transform, "Hỏi Dave điều gì đó (cây lai, zombie, chiến thuật)...", 16, TextAnchor.MiddleLeft, new Color(0.55f, 0.68f, 0.50f, 0.75f));
        var rtPlace = placeholderText.rectTransform;
        rtPlace.anchorMin = Vector2.zero;
        rtPlace.anchorMax = Vector2.one;
        rtPlace.offsetMin = new Vector2(14f, 0f);
        rtPlace.offsetMax = new Vector2(-10f, 0f);

        var inputText = CreateText("Text", inputFieldObj.transform, "", 18, TextAnchor.MiddleLeft, Color.white);
        var rtText = inputText.rectTransform;
        rtText.anchorMin = Vector2.zero;
        rtText.anchorMax = Vector2.one;
        rtText.offsetMin = new Vector2(14f, 0f);
        rtText.offsetMax = new Vector2(-10f, 0f);

        questionInput = inputFieldObj.GetComponent<InputField>();
        questionInput.textComponent = inputText;
        questionInput.placeholder = placeholderText;
        questionInput.lineType = InputField.LineType.SingleLine;
        questionInput.onEndEdit.AddListener((val) =>
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                OnSubmitQuestion();
        });

        // Nút GỬI
        var sendBtnObj = CreateStoneButton("Send_Btn", inputRowObj.transform, "GỬI 🚀", 19, OnSubmitQuestion);
        var rtSend = sendBtnObj.GetComponent<RectTransform>();
        rtSend.anchorMin = new Vector2(0.835f, 0f);
        rtSend.anchorMax = new Vector2(1f, 1f);
        rtSend.offsetMin = Vector2.zero;
        rtSend.offsetMax = Vector2.zero;

        sendButton = sendBtnObj.GetComponent<Button>();
        sendButtonText = sendBtnObj.GetComponentInChildren<Text>();
        if (sendButtonText != null)
            sendButtonText.color = new Color(1f, 0.92f, 0.30f);

        // Lời chào mở đầu thân thiện từ Crazy Dave
        AddChatBubble("Crazy Dave", "Wabby Wabbo! Chào anh bạn! Tôi là Dave Điên đây! 🤪\nAnh bạn chưa hiểu cơ chế nào, muốn tra cứu công thức ghép cây Fusion hay cách đối phó các loại zombie thì cứ hỏi tôi nhé!", false);
    }

    private void Update()
    {
        if (gameObject.activeInHierarchy && Input.GetKeyDown(KeyCode.Escape))
        {
            ClosePopup();
        }
    }

    private void OnClickChip(string prompt)
    {
        if (isWaitingForResponse) return;
        questionInput.text = prompt;
        OnSubmitQuestion();
    }

    private void OnSubmitQuestion()
    {
        string q = questionInput.text.Trim();
        if (string.IsNullOrEmpty(q) || isWaitingForResponse) return;

        PlayClick();

        // 1. Thêm tin nhắn của người chơi (bên phải)
        AddChatBubble("Bạn", q, true);
        questionInput.text = "";

        // 2. Chuyển sang trạng thái chờ
        SetWaitingState(true);

        // 3. Gửi lên server AI
        AIQAManager.Instance.AskDave(q,
            onResult: (answer) =>
            {
                SetWaitingState(false);
                AddChatBubble("Crazy Dave", answer, false);
                PlayDaveVoice();
            },
            onError: (err) =>
            {
                SetWaitingState(false);
                AddChatBubble("Crazy Dave", "Ối chà! " + err + "\nNhưng đừng lo, cứ thử hỏi lại tôi nhé!", false);
            }
        );
    }

    private void SetWaitingState(bool waiting)
    {
        isWaitingForResponse = waiting;
        if (thinkingObj != null) thinkingObj.SetActive(waiting);
        if (sendButton != null) sendButton.interactable = !waiting;
        if (questionInput != null) questionInput.interactable = !waiting;
        if (sendButtonText != null)
            sendButtonText.text = waiting ? "⏳ ĐỢI" : "GỬI 🚀";

        if (!waiting && questionInput != null)
        {
            questionInput.ActivateInputField();
        }
    }

    public void AddChatBubble(string senderName, string content, bool isUser)
    {
        float frameWidth = 704f;
        if (chatScrollRect != null && chatScrollRect.viewport != null && chatScrollRect.viewport.rect.width > 100f)
        {
            frameWidth = chatScrollRect.viewport.rect.width;
        }
        float bubbleWidth = Mathf.Max(300f, frameWidth - 28f);
        float textWidth = bubbleWidth - 36f; // 18px padding lề mỗi bên

        var bubbleObj = new GameObject("Bubble_" + senderName, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter), typeof(LayoutElement));
        bubbleObj.transform.SetParent(chatContent, false);

        var rtBubble = bubbleObj.GetComponent<RectTransform>();
        rtBubble.anchorMin = new Vector2(0f, 1f);
        rtBubble.anchorMax = new Vector2(1f, 1f);
        rtBubble.pivot = new Vector2(0.5f, 1f);
        rtBubble.sizeDelta = new Vector2(bubbleWidth, 0f);

        var bubbleImg = bubbleObj.GetComponent<Image>();
        bubbleImg.sprite = stoneButtonNormal;
        bubbleImg.type = Image.Type.Sliced;
        // Người chơi: Xanh lá vườn dịu; Dave: Nâu gỗ ấm đặc trưng PvZ
        bubbleImg.color = isUser ? new Color(0.16f, 0.35f, 0.12f, 0.98f) : new Color(0.24f, 0.19f, 0.13f, 0.98f);

        var vlg = bubbleObj.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(18, 18, 14, 16);
        vlg.spacing = 6f;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var csfBubble = bubbleObj.GetComponent<ContentSizeFitter>();
        csfBubble.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csfBubble.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Tên người gửi
        Color nameColor = isUser ? new Color(0.55f, 1f, 0.50f) : new Color(1f, 0.85f, 0.25f);
        string displayName = isUser ? "Bạn" : "Crazy Dave";
        var senderText = CreateText("Sender", bubbleObj.transform, "<b>" + displayName + "</b>", 16, TextAnchor.MiddleLeft, nameColor);
        var leSender = senderText.gameObject.AddComponent<LayoutElement>();
        leSender.minHeight = 22f;
        leSender.preferredHeight = 22f;

        // Nội dung tin nhắn
        var msgText = CreateText("Message", bubbleObj.transform, content, 17, TextAnchor.UpperLeft, Color.white);
        msgText.lineSpacing = 1.18f;
        msgText.horizontalOverflow = HorizontalWrapMode.Wrap;
        msgText.verticalOverflow = VerticalWrapMode.Overflow;

        // Đặt trước chiều rộng cho msgText để TextGenerator tính toán chuẩn xác chiều cao khi xuống dòng
        var rtMsg = msgText.rectTransform;
        rtMsg.sizeDelta = new Vector2(textWidth, 0f);

        // Tính toán chiều cao chính xác cho toàn bộ khối text dựa theo textWidth
        float textPreferredHeight = CalculateTextHeight(msgText, content, textWidth);
        var leMsg = msgText.gameObject.AddComponent<LayoutElement>();
        leMsg.minHeight = textPreferredHeight;
        leMsg.preferredHeight = textPreferredHeight;

        // Gán LayoutElement cho bubble để chatContent luôn dành đủ không gian chính xác
        float totalBubbleHeight = vlg.padding.top + 22f + vlg.spacing + textPreferredHeight + vlg.padding.bottom;
        var leBubble = bubbleObj.GetComponent<LayoutElement>();
        leBubble.minHeight = totalBubbleHeight;
        leBubble.preferredHeight = totalBubbleHeight;
        rtBubble.sizeDelta = new Vector2(bubbleWidth, totalBubbleHeight);

        // Force rebuild layout ngay lập tức
        LayoutRebuilder.ForceRebuildLayoutImmediate(rtBubble);
        if (chatContent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent);
        }

        // Tự động cuộn xuống đáy
        StartCoroutine(ScrollToBottom());
    }

    private float CalculateTextHeight(Text textComponent, string content, float width)
    {
        if (textComponent == null || string.IsNullOrEmpty(content)) return 24f;

        var settings = textComponent.GetGenerationSettings(new Vector2(width, 0.0f));
        float h = textComponent.cachedTextGeneratorForLayout.GetPreferredHeight(content, settings);
        float ppu = textComponent.pixelsPerUnit > 0 ? textComponent.pixelsPerUnit : 1f;
        float actualHeight = h / ppu;

        // Bổ sung 6px dự phòng cho các ký tự tiếng Việt có dấu mũ / móc / nặng bên dưới
        return Mathf.Max(26f, Mathf.Ceil(actualHeight) + 6f);
    }

    private IEnumerator ScrollToBottom()
    {
        yield return null;
        Canvas.ForceUpdateCanvases();
        if (chatContent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent);
        }
        yield return null;
        if (chatScrollRect != null)
        {
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private void PlayDaveVoice()
    {
        int rand = UnityEngine.Random.Range(1, 4);
        var clip = Resources.Load<AudioClip>($"Sounds/CrazyDave/CrazyDave_Talk{rand}");
        if (clip == null)
            clip = Resources.Load<AudioClip>($"Sounds/CrazyDave/CrazyDave_Short{rand}");
        if (clip != null)
            AudioSource.PlayClipAtPoint(clip, Vector3.zero);
    }

    private void PlayClick()
    {
        var clip = Resources.Load<AudioClip>("Sounds/UI/graveButtonClick");
        if (clip != null) AudioSource.PlayClipAtPoint(clip, Vector3.zero);
    }

    public void ClosePopup()
    {
        PlayClick();
        if (onCloseCallback != null)
        {
            onCloseCallback.Invoke();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private Text CreateText(string name, Transform parent, string value, int size, TextAnchor alignment, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>();
        text.font = uiFont;
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.supportRichText = true;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private GameObject CreateStoneButton(string name, Transform parent, string label, int fontSize, Action onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(MenuButtonMotion));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.sprite = stoneButtonNormal;
        image.type = Image.Type.Sliced;

        var button = go.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(() => onClick?.Invoke());

        var motion = go.GetComponent<MenuButtonMotion>();
        motion.targetGraphic = image;

        var text = CreateText("Label", go.transform, label, fontSize, TextAnchor.MiddleCenter, Color.white);
        var rt = text.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        text.raycastTarget = false;

        return go;
    }

    private GameObject CreateChipButton(string name, Transform parent, string label, Action onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(MenuButtonMotion));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.sprite = stoneButtonNormal;
        image.type = Image.Type.Sliced;
        image.color = new Color(0.20f, 0.28f, 0.16f, 0.95f);

        var button = go.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(() => onClick?.Invoke());

        var motion = go.GetComponent<MenuButtonMotion>();
        motion.targetGraphic = image;

        var text = CreateText("ChipText", go.transform, label, 14, TextAnchor.MiddleCenter, new Color(0.92f, 0.96f, 0.88f));
        var rt = text.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(6f, 2f);
        rt.offsetMax = new Vector2(-6f, -2f);
        text.raycastTarget = false;

        return go;
    }
}
