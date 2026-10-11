using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Quản lý kết nối mạng với endpoint Hỏi Đáp AI (/qa) trên FastAPI server.
/// Hỗ trợ xử lý lỗi mạng, timeout và chế độ phản hồi dự phòng (Mock).
/// </summary>
public class AIQAManager : MonoBehaviour
{
    private static AIQAManager _instance;
    public static AIQAManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("AIQAManager");
                _instance = go.AddComponent<AIQAManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    [Header("Cấu hình Endpoint")]
    public string qaEndpointUrl = "http://localhost:8000/qa";
    public float timeoutSeconds = 15f;
    public bool useMockFallback = true;

    [System.Serializable]
    public class QARequestDto
    {
        public string question;
        public string session_id;
    }

    [System.Serializable]
    public class QAResponseDto
    {
        public string answer;
        public string status;
    }

    public void AskDave(string question, Action<string> onResult, Action<string> onError = null)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            onResult?.Invoke("Wabby Wabbo! Anh bạn chưa nhập câu hỏi kìa! Hãy hỏi tôi về bất kỳ loại cây hoặc zombie nào nhé!");
            return;
        }

        StartCoroutine(SendQARequest(question.Trim(), onResult, onError));
    }

    private IEnumerator SendQARequest(string question, Action<string> onResult, Action<string> onError)
    {
        var dto = new QARequestDto { question = question, session_id = "menu_session" };
        string json = JsonUtility.ToJson(dto);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest req = new UnityWebRequest(qaEndpointUrl, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = (int)timeoutSeconds;

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                string resJson = req.downloadHandler.text;
                try
                {
                    var resDto = JsonUtility.FromJson<QAResponseDto>(resJson);
                    if (resDto != null && !string.IsNullOrEmpty(resDto.answer))
                    {
                        onResult?.Invoke(resDto.answer);
                        yield break;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[AIQAManager] Parse JSON error: {ex.Message}");
                }
            }

            // Nếu server lỗi hoặc chưa bật, dùng câu trả lời dự phòng thông minh nếu bật mock fallback
            if (useMockFallback)
            {
                string fallback = GetSmartFallback(question);
                onResult?.Invoke(fallback);
            }
            else
            {
                onError?.Invoke($"Không thể kết nối đến server Dave AI ({req.responseCode}).");
            }
        }
    }

    public static string GetSmartFallback(string question)
    {
        string q = question.ToLower();
        if (q.Contains("fusion") || q.Contains("ghép") || q.Contains("lai") || q.Contains("kết hợp"))
        {
            return "Wabby Wabbo! Trong PvZ Fusion, cơ chế ghép cây cực kỳ độc đáo: Anh bạn chỉ việc trồng một cây đè lên một cây tương thích khác trên cùng một ô đất!\n\nVí dụ: Ghép Quả Óc Chó (WallNut) với Đuốc Lửa (TorchWood) sẽ tạo ra Hạt Dẻ Lửa (FireWallNut) - vừa có máu trâu bò vừa phản sát thương lửa thiêu rụi zombie!";
        }
        if (q.Contains("đuốc") || q.Contains("torchwood") || q.Contains("băng") || q.Contains("lạnh") || q.Contains("cold"))
        {
            return "Wabby Wabbo! Cây Đuốc Lửa (TorchWood) là thần hộ mệnh số 1 khi đối đầu Zombie Băng!\n\n• Biến đạn đậu thường thành đạn lửa gây x2 sát thương.\n• Tỏa hơi ấm giải trừ ngay lập tức trạng thái đóng băng (Cold) cho tất cả các cây xung quanh!";
        }
        if (q.Contains("nắng") || q.Contains("mặt trời") || q.Contains("sun") || q.Contains("ban đêm"))
        {
            return "Bí kíp tài chính của Dave Điên đây:\n\n• Ban ngày: Trồng ngay 1-2 hàng Hướng Dương (cột 1 & 2 sát nhà) trước khi trồng cây tấn công.\n• Ban đêm: Không có nắng tự nhiên rơi xuống, bắt buộc phải trồng Hướng Dương thật sớm và dùng WallNut câu giờ ở phía trước để tích lũy tài nguyên!";
        }
        if (q.Contains("xô") || q.Contains("bucket") || q.Contains("gargantuar") || q.Contains("khổng lồ"))
        {
            return "Gặp Zombie Đội Xô hay Khổng Lồ Gargantuar ư? Đừng hoảng!\n\n• Dùng Quả Bí Ép (Squash): Nhảy bổ đè bẹp ngay 1 mục tiêu nguy hiểm.\n• Đặt WallNut phía trước kết hợp dàn đạn lửa TorchWood để dồn sát thương cực nhanh trước khi chúng tiến sát nhà!";
        }
        return "Wabby Wabbo! Dave Điên có lời khuyên vàng cho anh bạn đây: Hãy luôn dàn trận theo 3 lớp chuẩn chiến thuật:\n\n1. Lớp hậu cần: Hướng Dương ở cột 1-2 sát nhà để cấp nắng an toàn.\n2. Lớp hỏa lực: PeaShooter và TorchWood ở cột 3-4 để xả đạn lửa.\n3. Lớp phòng ngự: WallNut ở cột 5-6 để chặn đứng zombie.\n\nNếu zombie áp sát bất ngờ, hãy dùng ngay Squash để giải nguy!";
    }
}
