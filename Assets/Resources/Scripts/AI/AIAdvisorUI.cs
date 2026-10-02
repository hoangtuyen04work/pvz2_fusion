using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// ============================================================
// AIAdvisorUI: Điều phối toàn bộ hệ thống trợ lý AI trong game.
//
// Cách tích hợp vào Unity Editor:
//   1. Tạo một GameObject rỗng trong scene, đặt tên "AI Advisor"
//   2. Gắn script này vào GameObject đó
//   3. Kéo thả các thành phần UI vào các trường Inspector bên dưới:
//      - advisorPanel:    Panel chứa toàn bộ UI trợ lý
//      - adviceText:      Text hiển thị lời khuyên
//      - askButton:       Nút "Hỏi trợ lý" (Button)
//      - loadingPanel:    Panel hiển thị trạng thái đang chờ (có thể là spinner)
//      - loadingText:     Text "Đang phân tích..." (tùy chọn)
//      - errorText:       Text hiển thị lỗi kết nối (tùy chọn)
//      - closeButton:     Nút đóng panel (tùy chọn)
//
// Script sẽ tự tìm GameStateCollector và AIServiceConnector
// gắn trên cùng GameObject.
// ============================================================

[RequireComponent(typeof(GameStateCollector))]
[RequireComponent(typeof(AIServiceConnector))]
public class AIAdvisorUI : MonoBehaviour
{
    [Header("Tham chiếu UI (kéo thả từ Hierarchy)")]
    [Tooltip("Panel bao ngoài toàn bộ UI trợ lý")]
    public GameObject advisorPanel;

    [Tooltip("Text hiển thị lời khuyên AI")]
    public Text adviceText;

    [Tooltip("Nút người chơi nhấn để hỏi trợ lý")]
    public Button askButton;

    [Tooltip("Component hoạt họa nhân vật trợ lý (tùy chọn)")]
    public UIAnimatedAdvisor animatedAdvisor;

    [Tooltip("Panel/Spinner hiển thị khi đang chờ AI trả lời")]
    public GameObject loadingPanel;

    [Tooltip("Text 'Đang phân tích...' (tùy chọn)")]
    public Text loadingText;

    [Tooltip("Text hiển thị thông báo lỗi kết nối (tùy chọn)")]
    public Text errorText;

    [Tooltip("Nút đóng panel trợ lý (tùy chọn)")]
    public Button closeButton;

    [Tooltip("Nút nền để click ra ngoài đóng panel (tùy chọn)")]
    public Button backdropButton;

    [Tooltip("Ảnh avatar trong khung lời khuyên (tự đồng bộ theo avatar nút)")]
    public Image panelAvatarImage;

    [Tooltip("Tia sét/vòng cung năng lượng kết nối nút với khung lời khuyên (tùy chọn)")]
    public GameObject electricArc;

    [Header("Cài đặt hiển thị")]
    [Tooltip("Thời gian fade in/out của text lời khuyên (giây)")]
    public float fadeDuration = 0.4f;

    [Tooltip("Thời gian tự động ẩn lời khuyên sau khi hiện (0 = không tự ẩn)")]
    public float autoHideAfterSeconds = 8f;

    [Header("Cài đặt Tạm dừng Game (Tactical Pause)")]
    [Tooltip("Tự động tạm dừng game khi AI đang suy nghĩ và trả lời")]
    public bool pauseGameWhileAdvising = true;

    // ---- Component nội bộ ----
    private GameStateCollector collector;
    private AIServiceConnector connector;
    private bool isWaiting = false;
    private bool isPausedByAdvisor = false;

    // ---- Các thông điệp hiển thị ----
    private const string MSG_LOADING = "Đang phân tích chiến thuật...";
    private const string MSG_ERROR_PREFIX = "⚠ ";

    private void Awake()
    {
        collector = GetComponent<GameStateCollector>();
        connector = GetComponent<AIServiceConnector>();

        // Đăng ký sự kiện nhận phản hồi từ AIServiceConnector
        connector.OnAdviceReceived += HandleAdviceReceived;
        connector.OnError += HandleError;
    }

    private void Start()
    {
        if (animatedAdvisor == null && askButton != null)
        {
            animatedAdvisor = askButton.GetComponent<UIAnimatedAdvisor>();
        }

        // Thiết lập nút Hỏi trợ lý
        if (askButton != null)
        {
            askButton.onClick.AddListener(OnAskButtonClicked);
            // Tooltip trên nút
            var tooltip = askButton.GetComponentInChildren<Text>();
            if (tooltip != null && string.IsNullOrEmpty(tooltip.text))
                tooltip.text = "Hỏi trợ lý";
        }

        // Thiết lập nút đóng
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);

        // Thiết lập nút backdrop (nhấn ra ngoài để đóng)
        if (backdropButton != null)
            backdropButton.onClick.AddListener(ClosePanel);

        // Ẩn UI ban đầu
        if (advisorPanel != null) advisorPanel.SetActive(false);
        if (backdropButton != null) backdropButton.gameObject.SetActive(false);
        if (electricArc != null) electricArc.SetActive(false);
        SetLoadingVisible(false);
        SetAdviceVisible(false);
        SetErrorVisible(null);

        // Đồng bộ avatar từ nút bấm sang khung panel lời khuyên
        SyncAvatar();
    }

    public void SyncAvatar()
    {
        if (panelAvatarImage != null && animatedAdvisor != null && animatedAdvisor.avatarImage != null)
        {
            panelAvatarImage.sprite = animatedAdvisor.avatarImage.sprite;
        }
    }

    private void OnDestroy()
    {
        // Khôi phục timeScale nếu object bị hủy khi đang pause
        if (isPausedByAdvisor)
        {
            SetGamePaused(false);
        }

        // Hủy đăng ký sự kiện tránh memory leak
        if (connector != null)
        {
            connector.OnAdviceReceived -= HandleAdviceReceived;
            connector.OnError -= HandleError;
        }
    }

    private void OnDisable()
    {
        // Khôi phục timeScale nếu script bị disable
        if (isPausedByAdvisor)
        {
            SetGamePaused(false);
        }
    }

    // -------------------------------------------------------
    // Sự kiện bấm nút "Hỏi trợ lý"
    // -------------------------------------------------------
    private void OnAskButtonClicked()
    {
        // Nếu panel đang mở và không trong quá trình gửi request -> bấm lại sẽ đóng panel
        if (advisorPanel != null && advisorPanel.activeSelf && !isWaiting)
        {
            ClosePanel();
            return;
        }

        if (isWaiting) return;  // Đang chờ phản hồi thì không gửi thêm

        // Kích hoạt hiệu ứng phản hồi nhún nhảy và âm thanh hoạt họa
        if (animatedAdvisor != null)
        {
            animatedAdvisor.PlayClickReaction();
            animatedAdvisor.SetThinking(true);
        }

        // 1. Thu thập game state tại đúng thời điểm bấm nút (trạng thái chính xác nhất)
        string gameStateJson = collector.CollectAsJson();
        Debug.Log("[AIAdvisor] Game State JSON:\n" + gameStateJson);

        // 2. Tạm dừng toàn bộ màn chơi ngay lập tức (Zombie, cây, đạn, nắng dừng di chuyển)
        if (pauseGameWhileAdvising)
        {
            SetGamePaused(true);
        }

        // 3. Mở panel và chuyển sang trạng thái loading
        SyncAvatar();
        if (backdropButton != null) backdropButton.gameObject.SetActive(true);
        if (advisorPanel != null) advisorPanel.SetActive(true);
        if (electricArc != null) electricArc.SetActive(true);
        SetAdviceVisible(false);
        SetErrorVisible(null);
        SetLoadingVisible(true);

        if (loadingText != null) loadingText.text = MSG_LOADING;
        isWaiting = true;

        // 4. Gửi request lên server AI
        connector.RequestAdvice(gameStateJson);
    }

    // -------------------------------------------------------
    // Nhận lời khuyên thành công từ AI
    // -------------------------------------------------------
    private void HandleAdviceReceived(string advice)
    {
        isWaiting = false;
        SetLoadingVisible(false);
        if (askButton != null) askButton.interactable = true;

        if (animatedAdvisor != null)
        {
            animatedAdvisor.SetThinking(false);
            animatedAdvisor.SetSpeaking(true);
        }

        if (adviceText != null)
        {
            SetAdviceVisible(true);
            StartCoroutine(FadeInText(adviceText, advice));
        }

        SetErrorVisible(null);

        // Màn chơi vẫn TIẾP TỤC TẠM DỪNG để người chơi đọc chiến thuật.
        // Tự động tắt sau khoảng thời gian (dùng thời gian thực Realtime)
        float hideDelay = autoHideAfterSeconds > 0 ? autoHideAfterSeconds : 8f;
        StartCoroutine(AutoHideAfter(hideDelay));
    }

    // -------------------------------------------------------
    // Xử lý lỗi kết nối hoặc parse
    // -------------------------------------------------------
    private void HandleError(string errorMessage)
    {
        isWaiting = false;
        SetLoadingVisible(false);
        if (askButton != null) askButton.interactable = true;

        if (animatedAdvisor != null)
        {
            animatedAdvisor.SetThinking(false);
            animatedAdvisor.SetSpeaking(false);
        }

        SetAdviceVisible(false);
        SetErrorVisible(MSG_ERROR_PREFIX + errorMessage);
        Debug.LogWarning("[AIAdvisor] Lỗi: " + errorMessage);

        // Tự động đóng và khôi phục game sau 4 giây thời gian thực nếu gặp lỗi
        StartCoroutine(AutoHideAfter(4f));
    }

    // -------------------------------------------------------
    // Đóng panel trợ lý và khôi phục nhịp độ game
    // -------------------------------------------------------
    public void ClosePanel()
    {
        // Khôi phục nhịp độ game tiếp tục chạy
        if (isPausedByAdvisor)
        {
            SetGamePaused(false);
        }

        if (animatedAdvisor != null)
        {
            animatedAdvisor.SetThinking(false);
            animatedAdvisor.SetSpeaking(false);
        }

        if (backdropButton != null) backdropButton.gameObject.SetActive(false);
        if (advisorPanel != null) advisorPanel.SetActive(false);
        if (electricArc != null) electricArc.SetActive(false);
        SetAdviceVisible(false);
        SetLoadingVisible(false);
        SetErrorVisible(null);
        StopAllCoroutines();
        isWaiting = false;
        if (askButton != null) askButton.interactable = true;
    }

    // -------------------------------------------------------
    // Điều khiển tạm dừng / tiếp tục game
    // -------------------------------------------------------
    private void SetGamePaused(bool pause)
    {
        if (pause)
        {
            Time.timeScale = 0f;
            isPausedByAdvisor = true;
            Debug.Log("[AIAdvisor] Game đã TẠM DỪNG (Tactical Pause) để AI tư vấn.");
        }
        else
        {
            Time.timeScale = 1f;
            isPausedByAdvisor = false;
            Debug.Log("[AIAdvisor] Game đã TIẾP TỤC (Resume).");
        }
    }

    // -------------------------------------------------------
    // Hàm helper toggle visibility
    // -------------------------------------------------------
    private void SetLoadingVisible(bool visible)
    {
        if (loadingPanel != null) loadingPanel.SetActive(visible);
        if (loadingText != null) loadingText.gameObject.SetActive(visible);
    }

    private void SetAdviceVisible(bool visible)
    {
        if (adviceText != null) adviceText.gameObject.SetActive(visible);
    }

    private void SetErrorVisible(string message)
    {
        if (errorText == null) return;
        bool hasError = !string.IsNullOrEmpty(message);
        errorText.gameObject.SetActive(hasError);
        if (hasError) errorText.text = message;
    }

    // -------------------------------------------------------
    // Animation fade in text lời khuyên (dùng unscaledDeltaTime khi pause)
    // -------------------------------------------------------
    private IEnumerator FadeInText(Text target, string content)
    {
        target.text = content;
        Color c = target.color;
        c.a = 0f;
        target.color = c;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            c.a = Mathf.Clamp01(elapsed / fadeDuration);
            target.color = c;
            yield return null;
        }
        c.a = 1f;
        target.color = c;
    }

    // -------------------------------------------------------
    // Đếm ngược tự tắt dùng thời gian thực (không bị ảnh hưởng bởi Time.timeScale = 0)
    // -------------------------------------------------------
    private IEnumerator AutoHideAfter(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        ClosePanel();
    }
}
