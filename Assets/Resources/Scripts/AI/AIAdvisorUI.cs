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

    [Tooltip("Panel/Spinner hiển thị khi đang chờ AI trả lời")]
    public GameObject loadingPanel;

    [Tooltip("Text 'Đang phân tích...' (tùy chọn)")]
    public Text loadingText;

    [Tooltip("Text hiển thị thông báo lỗi kết nối (tùy chọn)")]
    public Text errorText;

    [Tooltip("Nút đóng panel trợ lý (tùy chọn)")]
    public Button closeButton;

    [Header("Cài đặt hiển thị")]
    [Tooltip("Thời gian fade in/out của text lời khuyên (giây)")]
    public float fadeDuration = 0.4f;

    [Tooltip("Thời gian tự động ẩn lời khuyên sau khi hiện (0 = không tự ẩn)")]
    public float autoHideAfterSeconds = 0f;

    // ---- Component nội bộ ----
    private GameStateCollector collector;
    private AIServiceConnector connector;
    private bool isWaiting = false;

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

        // Ẩn UI ban đầu
        if (advisorPanel != null) advisorPanel.SetActive(false);
        SetLoadingVisible(false);
        SetAdviceVisible(false);
        SetErrorVisible(null);
    }

    private void OnDestroy()
    {
        // Hủy đăng ký sự kiện tránh memory leak
        if (connector != null)
        {
            connector.OnAdviceReceived -= HandleAdviceReceived;
            connector.OnError -= HandleError;
        }
    }

    // -------------------------------------------------------
    // Sự kiện bấm nút "Hỏi trợ lý"
    // -------------------------------------------------------
    private void OnAskButtonClicked()
    {
        if (isWaiting) return;  // Đang chờ phản hồi thì không gửi thêm

        // Mở panel và chuyển sang trạng thái loading
        if (advisorPanel != null) advisorPanel.SetActive(true);
        SetAdviceVisible(false);
        SetErrorVisible(null);
        SetLoadingVisible(true);

        if (loadingText != null) loadingText.text = MSG_LOADING;
        if (askButton != null) askButton.interactable = false;
        isWaiting = true;

        // Thu thập game state và gửi lên AI
        string gameStateJson = collector.CollectAsJson();
        Debug.Log("[AIAdvisor] Game State JSON:\n" + gameStateJson);  // Để debug dễ dàng
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

        if (adviceText != null)
        {
            SetAdviceVisible(true);
            StartCoroutine(FadeInText(adviceText, advice));
        }

        SetErrorVisible(null);

        // Tự động ẩn sau một khoảng thời gian (mặc định 6 giây nếu để 0)
        float hideDelay = autoHideAfterSeconds > 0 ? autoHideAfterSeconds : 6f;
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
        SetAdviceVisible(false);
        SetErrorVisible(MSG_ERROR_PREFIX + errorMessage);
        Debug.LogWarning("[AIAdvisor] Lỗi: " + errorMessage);
    }

    // -------------------------------------------------------
    // Đóng panel trợ lý
    // -------------------------------------------------------
    public void ClosePanel()
    {
        if (advisorPanel != null) advisorPanel.SetActive(false);
        SetAdviceVisible(false);
        SetLoadingVisible(false);
        SetErrorVisible(null);
        StopAllCoroutines();
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
    // Animation fade in text lời khuyên
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
            elapsed += Time.deltaTime;
            c.a = Mathf.Clamp01(elapsed / fadeDuration);
            target.color = c;
            yield return null;
        }
        c.a = 1f;
        target.color = c;
    }

    private IEnumerator AutoHideAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        ClosePanel();
    }
}
