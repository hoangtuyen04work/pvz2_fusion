using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Quản lý xác thực Firebase Auth qua REST API.
/// Nhẹ, độc lập, không phụ thuộc vào Firebase SDK nặng nề.
/// Hỗ trợ cả Mock Cloud an toàn khi người dùng chưa điền API Key thật.
/// </summary>
public class FirebaseAuthService : MonoBehaviour
{
    private static FirebaseAuthService instance;
    public static FirebaseAuthService Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("FirebaseAuthService");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<FirebaseAuthService>();
            }
            return instance;
        }
    }

    [Serializable]
    public class AuthResponse
    {
        public string idToken;
        public string email;
        public string refreshToken;
        public string expiresIn;
        public string localId; // User ID trên Firebase
        public string displayName;
    }

    [Serializable]
    private class AuthRequestPayload
    {
        public string email;
        public string password;
        public bool returnSecureToken = true;
    }

    [Serializable]
    public class PlayerProfileData
    {
        public int progressVersion;
        public string username;
        public string email;
        public int bestScore;
        public int highestMap;
        public int wins;
        public int losses;
        public int unlockedLevelsCount;
        public int campaignCheckpoint;
        public int campaignCheckpointHealth;
        public int campaignCheckpointScore;
        public int[] mapClears;
        public string[] mapLastClears;
        public int endlessBestScore;
        public int endlessBestWave;
        public int endlessTotalKills;
        public string lastUpdated;
    }

    public static string CurrentUserId
    {
        get
        {
            if (string.IsNullOrEmpty(currentUserId))
                currentUserId = PlayerPrefs.GetString(PrefSavedUserId, string.Empty);
            return currentUserId;
        }
        private set => currentUserId = value;
    }
    private static string currentUserId = string.Empty;

    public static string CurrentUserEmail
    {
        get
        {
            if (string.IsNullOrEmpty(currentUserEmail))
                currentUserEmail = PlayerPrefs.GetString(PrefSavedEmail, string.Empty);
            return currentUserEmail;
        }
        private set => currentUserEmail = value;
    }
    private static string currentUserEmail = string.Empty;

    public static string CurrentIdToken
    {
        get
        {
            if (string.IsNullOrEmpty(currentIdToken))
                currentIdToken = PlayerPrefs.GetString(PrefSavedToken, string.Empty);
            return currentIdToken;
        }
        private set => currentIdToken = value;
    }
    private static string currentIdToken = string.Empty;

    public static bool IsLoggedIn => !string.IsNullOrEmpty(CurrentUserId);

    /// <summary>
    /// Trả về tên hiển thị chuẩn nhất của người chơi hiện tại:
    /// Nếu đã đăng nhập thì lấy username (bỏ đuôi @pvzgame.com), nếu chưa thì lấy tên cục bộ đã lưu.
    /// </summary>
    public static string GetCurrentPlayerName()
    {
        if (IsLoggedIn && !string.IsNullOrEmpty(CurrentUserEmail))
        {
            string name = CurrentUserEmail;
            if (name.Contains("@pvzgame.com"))
                name = name.Replace("@pvzgame.com", "");
            else if (name.Contains("@"))
                name = name.Substring(0, name.IndexOf('@'));
            return string.IsNullOrWhiteSpace(name) ? "Người chơi" : name.Trim();
        }

        string localName = PlayerPrefs.GetString("Endless.PlayerName", string.Empty);
        if (!string.IsNullOrWhiteSpace(localName))
            return localName.Trim();

        return !string.IsNullOrWhiteSpace(NetSession.LocalName) ? NetSession.LocalName.Trim() : "Người chơi";
    }

    private const string PrefSavedEmail = "PvZ_Cloud_SavedEmail";
    private const string PrefSavedToken = "PvZ_Cloud_SavedToken";
    private const string PrefSavedUserId = "PvZ_Cloud_SavedUserId";

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        // Nạp session đăng nhập đã lưu nếu có
        CurrentUserId = PlayerPrefs.GetString(PrefSavedUserId, string.Empty);
        CurrentUserEmail = PlayerPrefs.GetString(PrefSavedEmail, string.Empty);
        CurrentIdToken = PlayerPrefs.GetString(PrefSavedToken, string.Empty);

        // Đồng bộ tên người chơi với NetSession
        if (IsLoggedIn)
        {
            string disp = GetCurrentPlayerName();
            NetSession.LocalName = disp;
            PlayerPrefs.SetString("Endless.PlayerName", disp);
        }
    }

    /// <summary>
    /// Đăng ký tài khoản mới qua Firebase Auth REST API
    /// </summary>
    public void SignUp(string email, string password, Action<bool, string> onComplete)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            onComplete?.Invoke(false, "Vui lòng nhập đầy đủ tài khoản và mật khẩu!");
            return;
        }

        if (password.Length < 6)
        {
            onComplete?.Invoke(false, "Mật khẩu phải có ít nhất 6 ký tự!");
            return;
        }

        // Tự động thêm đuôi @pvz.com nếu người chơi chỉ nhập username đơn giản
        string formattedEmail = FormatEmail(email);

        if (!FirebaseConfig.IsConfigured)
        {
            // Chế độ Mock Cloud (Khi chưa gắn Web API Key Firebase)
            StartCoroutine(MockSignUpRoutine(formattedEmail, password, onComplete));
            return;
        }

        string url = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={FirebaseConfig.ApiKey}";
        StartCoroutine(AuthRequestRoutine(url, formattedEmail, password, onComplete, isSignUp: true));
    }

    /// <summary>
    /// Đăng nhập tài khoản qua Firebase Auth REST API
    /// </summary>
    public void SignIn(string email, string password, Action<bool, string> onComplete)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            onComplete?.Invoke(false, "Vui lòng nhập tài khoản và mật khẩu!");
            return;
        }

        string formattedEmail = FormatEmail(email);

        if (!FirebaseConfig.IsConfigured)
        {
            // Chế độ Mock Cloud
            StartCoroutine(MockSignInRoutine(formattedEmail, password, onComplete));
            return;
        }

        string url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={FirebaseConfig.ApiKey}";
        StartCoroutine(AuthRequestRoutine(url, formattedEmail, password, onComplete, isSignUp: false));
    }

    /// <summary>
    /// Đăng nhập nhanh dạng Khách (Chơi ngay)
    /// </summary>
    public void SignInGuest(Action<bool, string> onComplete)
    {
        string guestId = "guest_" + SystemInfo.deviceUniqueIdentifier.Substring(0, Mathf.Min(8, SystemInfo.deviceUniqueIdentifier.Length));
        CurrentUserId = guestId;
        CurrentUserEmail = "Khách (" + guestId.Substring(0, 6) + ")";
        CurrentIdToken = "guest_token";

        SaveSession();
        onComplete?.Invoke(true, "Đăng nhập Khách thành công!");
    }

    /// <summary>
    /// Đăng xuất khỏi hệ thống
    /// </summary>
    public void SignOut()
    {
        CurrentUserId = string.Empty;
        CurrentUserEmail = string.Empty;
        CurrentIdToken = string.Empty;

        PlayerPrefs.DeleteKey(PrefSavedEmail);
        PlayerPrefs.DeleteKey(PrefSavedToken);
        PlayerPrefs.DeleteKey(PrefSavedUserId);

        NetSession.LocalName = "Người chơi";
        PlayerPrefs.SetString("Endless.PlayerName", "Người chơi");
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Lưu thông tin người chơi lên Firebase Realtime Database
    /// </summary>
    public void SavePlayerDataToCloud(PlayerProfileData data, Action<bool, string> onComplete)
    {
        if (!IsLoggedIn)
        {
            onComplete?.Invoke(false, "Chưa đăng nhập!");
            return;
        }

        data.lastUpdated = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
        string json = JsonUtility.ToJson(data);

        if (!FirebaseConfig.IsConfigured || CurrentUserId.StartsWith("guest_"))
        {
            // Mock Cloud / Local PlayerPrefs
            PlayerPrefs.SetString("MockCloud_Data_" + CurrentUserId, json);
            PlayerPrefs.Save();
            onComplete?.Invoke(true, "Đã đồng bộ lên Cloud (Local Mock)!");
            return;
        }

        string dbUrl = FirebaseConfig.DatabaseUrl.TrimEnd('/') + $"/users/{CurrentUserId}.json?auth={CurrentIdToken}";
        StartCoroutine(PutDatabaseRoutine(dbUrl, json, onComplete));
    }

    /// <summary>Gộp tiến trình cục bộ vào hồ sơ trước khi tải lên Cloud.</summary>
    public static void MergeLocalProgress(PlayerProfileData data)
    {
        if (data == null) return;

        bool upgradingOldProfile = data.progressVersion < 2;
        data.progressVersion = 2;
        data.bestScore = Mathf.Max(data.bestScore, CampaignProgress.BestScore);
        data.highestMap = Mathf.Max(data.highestMap, CampaignProgress.HighestMap);
        data.wins = Mathf.Max(data.wins, CampaignProgress.Wins);
        data.losses = Mathf.Max(data.losses, CampaignProgress.Losses);
        data.unlockedLevelsCount = Mathf.Max(data.unlockedLevelsCount, CampaignProgress.UnlockedLevelsCount);

        int localCheckpoint = CampaignProgress.Checkpoint;
        int localCheckpointScore = CampaignProgress.CheckpointScore;
        if (upgradingOldProfile || localCheckpoint > data.campaignCheckpoint ||
            (localCheckpoint == data.campaignCheckpoint && localCheckpointScore > data.campaignCheckpointScore))
        {
            data.campaignCheckpoint = localCheckpoint;
            data.campaignCheckpointScore = localCheckpointScore;
            data.campaignCheckpointHealth = CampaignProgress.CheckpointHealth();
        }

        const int campaignMapCount = 3;
        if (data.mapClears == null || data.mapClears.Length != campaignMapCount)
            data.mapClears = new int[campaignMapCount];
        if (data.mapLastClears == null || data.mapLastClears.Length != campaignMapCount)
            data.mapLastClears = new string[campaignMapCount];

        for (int index = 0; index < campaignMapCount; index++)
        {
            int map = index + 1;
            int localClears = CampaignProgress.MapClears(map);
            if (localClears >= data.mapClears[index])
            {
                data.mapClears[index] = localClears;
                data.mapLastClears[index] = CampaignProgress.MapLastClear(map);
            }
        }
    }

    /// <summary>Gộp tiến trình Cloud về máy, không hạ thấp dữ liệu hiện có.</summary>
    public static void ApplyCloudProgress(PlayerProfileData data)
    {
        if (data == null) return;

        PlayerPrefs.SetInt("ThreeWorlds.BestScore", Mathf.Max(CampaignProgress.BestScore, data.bestScore));
        PlayerPrefs.SetInt("ThreeWorlds.HighestMap", Mathf.Max(CampaignProgress.HighestMap, data.highestMap));
        PlayerPrefs.SetInt("ThreeWorlds.Wins", Mathf.Max(CampaignProgress.Wins, data.wins));
        PlayerPrefs.SetInt("ThreeWorlds.Losses", Mathf.Max(CampaignProgress.Losses, data.losses));

        // progressVersion giữ tương thích với hồ sơ Cloud cũ, nơi các trường mới mặc định bằng 0.
        if (data.progressVersion >= 2)
        {
            PlayerPrefs.SetInt(CampaignProgress.PrefUnlockedLevels,
                Mathf.Max(CampaignProgress.UnlockedLevelsCount,
                    Mathf.Clamp(data.unlockedLevelsCount, 2, 8)));

            int localCheckpoint = CampaignProgress.Checkpoint;
            int localCheckpointScore = CampaignProgress.CheckpointScore;
            if (data.campaignCheckpoint > localCheckpoint ||
                (data.campaignCheckpoint == localCheckpoint && data.campaignCheckpointScore > localCheckpointScore))
            {
                PlayerPrefs.SetInt("ThreeWorlds.Checkpoint", Mathf.Clamp(data.campaignCheckpoint, 0, 2));
                PlayerPrefs.SetInt("ThreeWorlds.CheckpointScore", Mathf.Max(0, data.campaignCheckpointScore));
                PlayerPrefs.SetInt("ThreeWorlds.CheckpointHealth",
                    Mathf.Clamp(data.campaignCheckpointHealth, 25, 100));
            }

            int mapCount = Mathf.Min(3, data.mapClears != null ? data.mapClears.Length : 0);
            for (int index = 0; index < mapCount; index++)
            {
                int map = index + 1;
                int cloudClears = Mathf.Max(0, data.mapClears[index]);
                if (cloudClears > CampaignProgress.MapClears(map))
                {
                    PlayerPrefs.SetInt("ThreeWorlds.Map" + map + "Clears", cloudClears);
                    if (data.mapLastClears != null && index < data.mapLastClears.Length &&
                        !string.IsNullOrEmpty(data.mapLastClears[index]))
                        PlayerPrefs.SetString("ThreeWorlds.Map" + map + "Last", data.mapLastClears[index]);
                }
            }
        }

        PlayerPrefs.Save();
    }

    /// <summary>
    /// Tự động cập nhật kết quả màn chơi Sinh tồn của người chơi lên Cloud
    /// </summary>
    public void RecordEndlessResult(int score, int wave, int kills)
    {
        if (!IsLoggedIn) return;

        LoadPlayerDataFromCloud((ok, profile, msg) =>
        {
            if (profile == null)
            {
                profile = new PlayerProfileData
                {
                    username = GetCurrentPlayerName(),
                    email = CurrentUserEmail,
                    bestScore = CampaignProgress.BestScore,
                    highestMap = CampaignProgress.HighestMap,
                    wins = CampaignProgress.Wins,
                    losses = CampaignProgress.Losses
                };
            }

            profile.username = GetCurrentPlayerName();
            MergeLocalProgress(profile);
            if (score > profile.endlessBestScore) profile.endlessBestScore = score;
            if (wave > profile.endlessBestWave) profile.endlessBestWave = wave;
            profile.endlessTotalKills += kills;

            SavePlayerDataToCloud(profile, null);
        });
    }

    /// <summary>
    /// Tải thông tin người chơi từ Firebase Realtime Database
    /// </summary>
    public void LoadPlayerDataFromCloud(Action<bool, PlayerProfileData, string> onComplete)
    {
        if (!IsLoggedIn)
        {
            onComplete?.Invoke(false, null, "Chưa đăng nhập!");
            return;
        }

        if (!FirebaseConfig.IsConfigured || CurrentUserId.StartsWith("guest_"))
        {
            string localJson = PlayerPrefs.GetString("MockCloud_Data_" + CurrentUserId, string.Empty);
            if (!string.IsNullOrEmpty(localJson))
            {
                try
                {
                    var profile = JsonUtility.FromJson<PlayerProfileData>(localJson);
                    onComplete?.Invoke(true, profile, "Nạp dữ liệu từ Cloud thành công!");
                    return;
                }
                catch { }
            }

            // Mặc định dữ liệu rỗng ban đầu
            var defaultProfile = new PlayerProfileData
            {
                username = CurrentUserEmail,
                email = CurrentUserEmail,
                bestScore = CampaignProgress.BestScore,
                highestMap = CampaignProgress.HighestMap,
                wins = CampaignProgress.Wins,
                losses = CampaignProgress.Losses
            };
            onComplete?.Invoke(true, defaultProfile, "Tạo hồ sơ mới!");
            return;
        }

        string dbUrl = FirebaseConfig.DatabaseUrl.TrimEnd('/') + $"/users/{CurrentUserId}.json?auth={CurrentIdToken}";
        StartCoroutine(GetDatabaseRoutine(dbUrl, onComplete));
    }

    // ==================== HTTP Routines ====================

    private IEnumerator AuthRequestRoutine(string url, string email, string password, Action<bool, string> onComplete, bool isSignUp)
    {
        var payload = new AuthRequestPayload { email = email, password = password, returnSecureToken = true };
        string jsonPayload = JsonUtility.ToJson(payload);

        using (var request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var response = JsonUtility.FromJson<AuthResponse>(request.downloadHandler.text);
                    CurrentUserId = response.localId;
                    CurrentUserEmail = response.email;
                    CurrentIdToken = response.idToken;
                    SaveSession();

                    onComplete?.Invoke(true, isSignUp ? "Đăng ký thành công!" : "Đăng nhập thành công!");
                }
                catch (Exception ex)
                {
                    onComplete?.Invoke(false, "Lỗi phân tích phản hồi từ Firebase: " + ex.Message);
                }
            }
            else
            {
                string errorMsg = ParseFirebaseError(request.downloadHandler.text);
                onComplete?.Invoke(false, errorMsg);
            }
        }
    }

    private IEnumerator PutDatabaseRoutine(string url, string json, Action<bool, string> onComplete)
    {
        using (var request = new UnityWebRequest(url, "PUT"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
                onComplete?.Invoke(true, "Lưu dữ liệu thành công!");
            else
                onComplete?.Invoke(false, "Lỗi lưu đám mây: " + request.error);
        }
    }

    private IEnumerator GetDatabaseRoutine(string url, Action<bool, PlayerProfileData, string> onComplete)
    {
        using (var request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                if (string.IsNullOrEmpty(json) || json == "null")
                {
                    onComplete?.Invoke(true, null, "Hồ sơ chưa có dữ liệu.");
                }
                else
                {
                    try
                    {
                        var data = JsonUtility.FromJson<PlayerProfileData>(json);
                        onComplete?.Invoke(true, data, "Đã nạp hồ sơ người chơi!");
                    }
                    catch
                    {
                        onComplete?.Invoke(false, null, "Lỗi đọc dữ liệu đám mây.");
                    }
                }
            }
            else
            {
                onComplete?.Invoke(false, null, "Không thể kết nối đến máy chủ Cloud: " + request.error);
            }
        }
    }

    // ==================== Mock Cloud Simulators ====================

    private IEnumerator MockSignUpRoutine(string email, string password, Action<bool, string> onComplete)
    {
        yield return new WaitForSecondsRealtime(0.5f);
        string userKey = "MockUser_" + email;
        if (PlayerPrefs.HasKey(userKey))
        {
            onComplete?.Invoke(false, "Tài khoản này đã tồn tại trên Cloud!");
            yield break;
        }

        PlayerPrefs.SetString(userKey, password);
        PlayerPrefs.Save();

        CurrentUserId = "mock_uid_" + Mathf.Abs(email.GetHashCode());
        CurrentUserEmail = email;
        CurrentIdToken = "mock_token";
        SaveSession();

        onComplete?.Invoke(true, "Đăng ký thành công (Chế độ Cloud Demo)!");
    }

    private IEnumerator MockSignInRoutine(string email, string password, Action<bool, string> onComplete)
    {
        yield return new WaitForSecondsRealtime(0.5f);
        string userKey = "MockUser_" + email;
        if (!PlayerPrefs.HasKey(userKey))
        {
            onComplete?.Invoke(false, "Tài khoản không tồn tại! Hãy bấm Đăng Ký.");
            yield break;
        }

        string savedPass = PlayerPrefs.GetString(userKey);
        if (savedPass != password)
        {
            onComplete?.Invoke(false, "Mật khẩu không chính xác!");
            yield break;
        }

        CurrentUserId = "mock_uid_" + Mathf.Abs(email.GetHashCode());
        CurrentUserEmail = email;
        CurrentIdToken = "mock_token";
        SaveSession();

        onComplete?.Invoke(true, "Đăng nhập thành công!");
    }

    private void SaveSession()
    {
        PlayerPrefs.SetString(PrefSavedUserId, CurrentUserId);
        PlayerPrefs.SetString(PrefSavedEmail, CurrentUserEmail);
        PlayerPrefs.SetString(PrefSavedToken, CurrentIdToken);

        string playerName = GetCurrentPlayerName();
        NetSession.LocalName = playerName;
        PlayerPrefs.SetString("Endless.PlayerName", playerName);

        PlayerPrefs.Save();
    }

    private string FormatEmail(string input)
    {
        input = input.Trim();
        if (!input.Contains("@"))
            return input + "@pvzgame.com";
        return input;
    }

    private string ParseFirebaseError(string json)
    {
        if (string.IsNullOrEmpty(json)) return "Lỗi kết nối máy chủ!";
        if (json.Contains("EMAIL_EXISTS")) return "Tài khoản này đã tồn tại!";
        if (json.Contains("EMAIL_NOT_FOUND")) return "Tài khoản không tồn tại!";
        if (json.Contains("INVALID_PASSWORD") || json.Contains("INVALID_LOGIN_CREDENTIALS")) return "Mật khẩu không chính xác!";
        if (json.Contains("WEAK_PASSWORD")) return "Mật khẩu quá yếu (cần tối thiểu 6 ký tự)!";
        if (json.Contains("TOO_MANY_ATTEMPTS_TRY_LATER")) return "Thử quá nhiều lần, vui lòng thử lại sau!";
        return "Lỗi xác thực: " + json;
    }
}
