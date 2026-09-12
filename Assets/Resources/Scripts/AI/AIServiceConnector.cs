using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// ============================================================
// AIServiceConnector: Gửi game state lên server AI (Python FastAPI)
// và nhận lời khuyên chiến thuật trả về.
//
// Cách dùng:
//   StartCoroutine(RequestAdvice(jsonPayload, onSuccess, onError));
//
// [TODO - PHASE SERVER] Khi server AI đã sẵn sàng:
//   - Đổi SERVER_URL sang địa chỉ thực của server
//   - Xóa bỏ chế độ MOCK_MODE
// ============================================================

public class AIServiceConnector : MonoBehaviour
{
    // ---- Cấu hình server ----
    [Header("Cấu hình Server AI")]
    [Tooltip("Địa chỉ endpoint của server AI (để localhost khi demo)")]
    public string serverUrl = "http://localhost:8000/analyze";

    [Tooltip("Thời gian tối đa chờ phản hồi từ server (giây)")]
    public float timeoutSeconds = 20f;

    // ---- Chế độ Mock (dùng khi server chưa sẵn sàng) ----
    [Header("Chế độ Mock (bật khi chưa có server)")]
    [Tooltip("Bật để dùng phản hồi giả thay vì gọi server thật")]
    public bool useMockMode = false;

    // ---- Sự kiện thông báo trạng thái ----
    public event Action<string> OnAdviceReceived;   // Nhận được lời khuyên thành công
    public event Action<string> OnError;            // Xảy ra lỗi

    // -------------------------------------------------------
    // Gửi request lên server AI
    // jsonPayload: JSON string của GameStateSnapshot
    // -------------------------------------------------------
    public void RequestAdvice(string jsonPayload)
    {
        if (useMockMode)
        {
            StartCoroutine(SimulateMockResponse(jsonPayload));
        }
        else
        {
            StartCoroutine(SendRequest(jsonPayload));
        }
    }

    // ---- Gửi HTTP POST thực tế ----
    private IEnumerator SendRequest(string jsonPayload)
    {
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        using (UnityWebRequest request = new UnityWebRequest(serverUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = (int)timeoutSeconds;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseJson = request.downloadHandler.text;
                try
                {
                    AIResponse response = JsonUtility.FromJson<AIResponse>(responseJson);
                    OnAdviceReceived?.Invoke(response.advice);
                }
                catch (Exception ex)
                {
                    OnError?.Invoke($"Lỗi parse phản hồi: {ex.Message}");
                }
            }
            else
            {
                string responseBody = request.downloadHandler != null ? request.downloadHandler.text : "";
                Debug.LogError($"[AIAdvisor] Lỗi từ server ({request.responseCode}): {responseBody}");
                string errorMsg = request.result == UnityWebRequest.Result.ConnectionError
                    ? "Không thể kết nối server AI. Hãy đảm bảo server đang chạy trên localhost:8000."
                    : $"Lỗi từ server: {request.responseCode} - {request.error}";
                OnError?.Invoke(errorMsg);
            }
        }
    }

    // ---- Chế độ Mock: giả lập phản hồi AI ----
    private IEnumerator SimulateMockResponse(string jsonPayload)
    {
        // Giả lập độ trễ mạng 0.5-1.5 giây
        yield return new WaitForSeconds(UnityEngine.Random.Range(0.5f, 1.5f));

        // Phân tích sơ bộ game state để tạo lời khuyên mock thông minh hơn
        string advice = GenerateMockAdvice(jsonPayload);
        OnAdviceReceived?.Invoke(advice);
    }

    // Tạo lời khuyên mock dựa trên dữ liệu JSON đơn giản
    private string GenerateMockAdvice(string jsonPayload)
    {
        // Parse sơ bộ một số trường quan trọng từ JSON
        bool lowSun = jsonPayload.Contains("\"currentSun\": 0") || jsonPayload.Contains("\"currentSun\":0")
                      || jsonPayload.Contains("\"currentSun\": 25") || jsonPayload.Contains("\"currentSun\":25");
        bool hasFinalWave = jsonPayload.Contains("\"nextWaveZombieType\": \"Hết đợt\"");

        if (hasFinalWave)
            return "Đây là đợt cuối! Hãy tập trung bảo vệ các hàng có nhiều cây nhất và không cần trồng thêm!";
        if (lowSun)
            return "Mặt trời đang ít, hãy ưu tiên trồng Hoa Hướng Dương ở các hàng trống để tích lũy thêm nắng!";

        // Lời khuyên ngẫu nhiên khi chưa có server thật
        string[] genericTips = new string[]
        {
            "[Chế độ thử nghiệm] Server AI chưa kết nối. Hãy chắc chắn zombie không đột phá hàng giữa!",
            "[Chế độ thử nghiệm] Ưu tiên trồng cây phòng thủ (WallNut) ở các hàng có zombie mạnh.",
            "[Chế độ thử nghiệm] Kiểm tra các hàng trống — zombie sẽ tiến thẳng nếu không có cây cản!",
            "[Chế độ thử nghiệm] Nếu có SnowKing trên sân, hãy giữ nó khỏe mạnh — nó buff tất cả cây!",
        };
        return genericTips[UnityEngine.Random.Range(0, genericTips.Length)];
    }
}

// ---- Lớp dữ liệu nhận từ server ----
[Serializable]
public class AIResponse
{
    public string advice;    // Lời khuyên chiến thuật bằng tiếng Việt
    public string status;    // "ok" | "error"
}
