using System;
using System.Collections;
using System.Collections.Generic;
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
    public static bool HasNetworkConnection =>
        Application.internetReachability != NetworkReachability.NotReachable;

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
    private class RefreshTokenResponse
    {
        public string id_token = null;
        public string refresh_token = null;
        public string expires_in = null;
        public string user_id = null;
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
        public int endlessBestKills;
        public float endlessBestDuration;
        public string endlessBestPlayedAtUtc;
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
    private const string PrefSavedRefreshToken = "PvZ_Cloud_RefreshToken";
    private const string PrefTokenExpiryTicks = "PvZ_Cloud_TokenExpiryTicks";
    private const string PrefProgressOwner = "PvZ_ProgressOwnerUserId";

    private static string currentRefreshToken = string.Empty;
    private static long tokenExpiryUtcTicks;

    public static bool IsCurrentUserProgressOwner =>
        IsLoggedIn && PlayerPrefs.GetString(PrefProgressOwner, string.Empty) == CurrentUserId;

    public static void MarkCurrentUserAsProgressOwner()
    {
        if (!IsLoggedIn) return;
        PlayerPrefs.SetString(PrefProgressOwner, CurrentUserId);
        PlayerPrefs.Save();
    }

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
        currentRefreshToken = PlayerPrefs.GetString(PrefSavedRefreshToken, string.Empty);
        long.TryParse(PlayerPrefs.GetString(PrefTokenExpiryTicks, "0"), out tokenExpiryUtcTicks);

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
        currentRefreshToken = string.Empty;
        tokenExpiryUtcTicks = 0;

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
        PlayerPrefs.DeleteKey(PrefSavedRefreshToken);
        PlayerPrefs.DeleteKey(PrefTokenExpiryTicks);
        currentRefreshToken = string.Empty;
        tokenExpiryUtcTicks = 0;

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

        string dbUrl = FirebaseConfig.DatabaseUrl.TrimEnd('/') + $"/users/{CurrentUserId}.json";
        StartCoroutine(PutDatabaseRoutine(dbUrl, json, onComplete));
    }

    public void SyncPlayerDataWithCloud(PlayerProfileData localData,
        Action<bool, PlayerProfileData, string> onComplete)
    {
        if (!IsLoggedIn)
        {
            onComplete?.Invoke(false, null, "Chưa đăng nhập!");
            return;
        }

        if (!FirebaseConfig.IsConfigured || CurrentUserId.StartsWith("guest_"))
        {
            LoadPlayerDataFromCloud((loaded, cloud, loadMessage) =>
            {
                if (!loaded) { onComplete?.Invoke(false, null, loadMessage); return; }
                PlayerProfileData merged = MergeProfiles(cloud, localData);
                SavePlayerDataToCloud(merged, (saved, saveMessage) =>
                    onComplete?.Invoke(saved, saved ? merged : null, saveMessage));
            });
            return;
        }

        StartCoroutine(MergeProfileTransactionRoutine(localData, onComplete));
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

    /// <summary>Hợp nhất hai hồ sơ mà không làm giảm tiến trình hoặc phá cặp kỷ lục Sinh tồn.</summary>
    public static PlayerProfileData MergeProfiles(PlayerProfileData cloud, PlayerProfileData local)
    {
        if (cloud == null) return local;
        if (local == null) return cloud;

        cloud.progressVersion = Mathf.Max(cloud.progressVersion, local.progressVersion);
        cloud.username = string.IsNullOrWhiteSpace(local.username) ? cloud.username : local.username;
        cloud.email = string.IsNullOrWhiteSpace(local.email) ? cloud.email : local.email;
        cloud.bestScore = Mathf.Max(cloud.bestScore, local.bestScore);
        cloud.highestMap = Mathf.Max(cloud.highestMap, local.highestMap);
        cloud.wins = Mathf.Max(cloud.wins, local.wins);
        cloud.losses = Mathf.Max(cloud.losses, local.losses);
        cloud.unlockedLevelsCount = Mathf.Max(cloud.unlockedLevelsCount, local.unlockedLevelsCount);

        if (local.campaignCheckpoint > cloud.campaignCheckpoint ||
            (local.campaignCheckpoint == cloud.campaignCheckpoint && local.campaignCheckpointScore > cloud.campaignCheckpointScore))
        {
            cloud.campaignCheckpoint = local.campaignCheckpoint;
            cloud.campaignCheckpointScore = local.campaignCheckpointScore;
            cloud.campaignCheckpointHealth = local.campaignCheckpointHealth;
        }

        const int mapCount = 3;
        if (cloud.mapClears == null || cloud.mapClears.Length != mapCount) cloud.mapClears = new int[mapCount];
        if (cloud.mapLastClears == null || cloud.mapLastClears.Length != mapCount) cloud.mapLastClears = new string[mapCount];
        if (local.mapClears != null)
        {
            for (int i = 0; i < Mathf.Min(mapCount, local.mapClears.Length); i++)
            {
                if (local.mapClears[i] > cloud.mapClears[i])
                {
                    cloud.mapClears[i] = local.mapClears[i];
                    if (local.mapLastClears != null && i < local.mapLastClears.Length)
                        cloud.mapLastClears[i] = local.mapLastClears[i];
                }
            }
        }

        if (IsBetterEndlessRecord(local.endlessBestWave, local.endlessBestScore, local.endlessBestDuration,
                cloud.endlessBestWave, cloud.endlessBestScore, cloud.endlessBestDuration))
        {
            cloud.endlessBestWave = local.endlessBestWave;
            cloud.endlessBestScore = local.endlessBestScore;
            cloud.endlessBestKills = local.endlessBestKills;
            cloud.endlessBestDuration = local.endlessBestDuration;
            cloud.endlessBestPlayedAtUtc = local.endlessBestPlayedAtUtc;
        }
        cloud.endlessTotalKills = Mathf.Max(cloud.endlessTotalKills, local.endlessTotalKills);
        return cloud;
    }

    private static bool IsBetterEndlessRecord(int wave, int score, float duration, int otherWave, int otherScore, float otherDuration)
    {
        if (wave != otherWave) return wave > otherWave;
        if (score != otherScore) return score > otherScore;
        if (duration <= 0f) return false;
        return otherDuration <= 0f || duration < otherDuration;
    }

    /// <summary>Gộp tiến trình Cloud về máy, không hạ thấp dữ liệu hiện có.</summary>
    public static void ApplyCloudProgress(PlayerProfileData data, bool replaceLocal = false)
    {
        if (data == null) return;

        bool cloudHasAdventureProgress = data.highestMap > 0 || data.bestScore > 0 ||
            data.wins > 0 || data.unlockedLevelsCount > 2 || data.campaignCheckpoint > 0;
        if (!cloudHasAdventureProgress && data.mapClears != null)
            foreach (int clears in data.mapClears)
                if (clears > 0)
                {
                    cloudHasAdventureProgress = true;
                    break;
                }

        PlayerPrefs.SetInt("ThreeWorlds.BestScore", replaceLocal ? Mathf.Max(0, data.bestScore) : Mathf.Max(CampaignProgress.BestScore, data.bestScore));
        PlayerPrefs.SetInt("ThreeWorlds.HighestMap", replaceLocal ? Mathf.Clamp(data.highestMap, 0, 3) : Mathf.Max(CampaignProgress.HighestMap, data.highestMap));
        PlayerPrefs.SetInt("ThreeWorlds.Wins", replaceLocal ? Mathf.Max(0, data.wins) : Mathf.Max(CampaignProgress.Wins, data.wins));
        PlayerPrefs.SetInt("ThreeWorlds.Losses", replaceLocal ? Mathf.Max(0, data.losses) : Mathf.Max(CampaignProgress.Losses, data.losses));

        // progressVersion giữ tương thích với hồ sơ Cloud cũ, nơi các trường mới mặc định bằng 0.
        if (data.progressVersion >= 2 || replaceLocal)
        {
            // Account progress is authoritative after login. Do not let a higher
            // guest/local save unlock levels beyond what this account has earned.
            PlayerPrefs.SetInt(CampaignProgress.PrefUnlockedLevels,
                Mathf.Clamp(data.unlockedLevelsCount, 2, 8));

            int localCheckpoint = replaceLocal ? -1 : CampaignProgress.Checkpoint;
            int localCheckpointScore = CampaignProgress.CheckpointScore;
            if (data.campaignCheckpoint > localCheckpoint ||
                (data.campaignCheckpoint == localCheckpoint && data.campaignCheckpointScore > localCheckpointScore))
            {
                PlayerPrefs.SetInt("ThreeWorlds.Checkpoint", Mathf.Clamp(data.campaignCheckpoint, 0, 2));
                PlayerPrefs.SetInt("ThreeWorlds.CheckpointScore", Mathf.Max(0, data.campaignCheckpointScore));
                PlayerPrefs.SetInt("ThreeWorlds.CheckpointHealth",
                    Mathf.Clamp(data.campaignCheckpointHealth, 25, 100));
            }

            if (replaceLocal)
            {
                for (int map = 1; map <= 3; map++)
                {
                    PlayerPrefs.SetInt("ThreeWorlds.Map" + map + "Clears", 0);
                    PlayerPrefs.DeleteKey("ThreeWorlds.Map" + map + "Last");
                }
            }

            int mapCount = Mathf.Min(3, data.mapClears != null ? data.mapClears.Length : 0);
            for (int index = 0; index < mapCount; index++)
            {
                int map = index + 1;
                int cloudClears = Mathf.Max(0, data.mapClears[index]);
                if (replaceLocal || cloudClears > CampaignProgress.MapClears(map))
                {
                    PlayerPrefs.SetInt("ThreeWorlds.Map" + map + "Clears", cloudClears);
                    if (data.mapLastClears != null && index < data.mapLastClears.Length &&
                        !string.IsNullOrEmpty(data.mapLastClears[index]))
                        PlayerPrefs.SetString("ThreeWorlds.Map" + map + "Last", data.mapLastClears[index]);
                }
            }
        }


        // Tutorial completion was not part of older cloud profiles. A profile
        // that already owns Adventure progress must never be forced through it.
        if (cloudHasAdventureProgress)
            PlayerPrefs.SetInt(DialogLevel0.FirstTimeTutorialKey, 1);
        else if (replaceLocal)
            PlayerPrefs.DeleteKey(DialogLevel0.FirstTimeTutorialKey);

        MarkCurrentUserAsProgressOwner();
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Tự động cập nhật kết quả màn chơi Sinh tồn của người chơi lên Cloud
    /// </summary>
    public void RecordEndlessResult(int score, int wave, int kills, float durationSeconds,
        Action<bool, string> onComplete = null)
    {
        if (!IsLoggedIn)
        {
            onComplete?.Invoke(false, "Kết quả đã lưu trên máy. Hãy đăng nhập để đưa lên bảng xếp hạng Cloud.");
            return;
        }

        if (FirebaseConfig.IsConfigured && !CurrentUserId.StartsWith("guest_"))
        {
            StartCoroutine(UpdateEndlessTransactionRoutine(score, wave, kills, durationSeconds, onComplete));
            return;
        }

        LoadPlayerDataFromCloud((ok, profile, msg) =>
        {
            if (!ok)
            {
                Debug.LogWarning("Không thể cập nhật kỷ lục Cloud: " + msg);
                onComplete?.Invoke(false, msg);
                return;
            }

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
            profile.endlessTotalKills += kills;

            if (IsBetterEndlessRecord(wave, score, durationSeconds,
                    profile.endlessBestWave, profile.endlessBestScore, profile.endlessBestDuration))
            {
                profile.endlessBestScore = score;
                profile.endlessBestWave = wave;
                profile.endlessBestKills = kills;
                profile.endlessBestDuration = durationSeconds;
                profile.endlessBestPlayedAtUtc = DateTime.UtcNow.ToString("o");
            }

            SavePlayerDataToCloud(profile, (saved, saveMessage) =>
            {
                if (!saved)
                {
                    Debug.LogWarning("Không thể lưu kỷ lục Cloud: " + saveMessage);
                    onComplete?.Invoke(false, saveMessage);
                    return;
                }

                SaveGlobalLeaderboardRecord(new EndlessScoreRecord
                {
                    playerName = profile.username,
                    score = profile.endlessBestScore,
                    wave = profile.endlessBestWave,
                    kills = profile.endlessBestKills,
                    durationSeconds = profile.endlessBestDuration,
                    playedAtUtc = profile.endlessBestPlayedAtUtc,
                    gameVersion = Application.version
                }, onComplete);
            });
        });
    }

    public void SaveGlobalLeaderboardRecord(EndlessScoreRecord record, Action<bool, string> onComplete)
    {
        if (!IsLoggedIn || CurrentUserId.StartsWith("guest_") || !FirebaseConfig.IsConfigured)
        {
            onComplete?.Invoke(false, "Bảng xếp hạng thế giới yêu cầu tài khoản Cloud.");
            return;
        }

        string json = JsonUtility.ToJson(record);
        string url = FirebaseConfig.DatabaseUrl.TrimEnd('/') + $"/leaderboard/{CurrentUserId}.json";
        StartCoroutine(PutDatabaseRoutine(url, json, onComplete));
    }

    public void LoadGlobalLeaderboard(Action<bool, List<EndlessScoreRecord>, string> onComplete)
    {
        if (!FirebaseConfig.IsConfigured)
        {
            onComplete?.Invoke(false, null, "Đang hiển thị bảng xếp hạng cục bộ.");
            return;
        }

        string publicUrl = FirebaseConfig.DatabaseUrl.TrimEnd('/') + "/leaderboard.json";
        StartCoroutine(GetLeaderboardRoutine(publicUrl, onComplete));
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

        string dbUrl = FirebaseConfig.DatabaseUrl.TrimEnd('/') + $"/users/{CurrentUserId}.json";
        StartCoroutine(GetDatabaseRoutine(dbUrl, onComplete));
    }

    // ==================== HTTP Routines ====================

    private IEnumerator WithValidToken(Action<bool, string> onComplete)
    {
        if (!FirebaseConfig.IsConfigured || CurrentUserId.StartsWith("guest_"))
        {
            onComplete?.Invoke(true, string.Empty);
            yield break;
        }

        bool tokenStillValid = !string.IsNullOrEmpty(CurrentIdToken) &&
            tokenExpiryUtcTicks > DateTime.UtcNow.AddMinutes(1).Ticks;
        if (tokenStillValid)
        {
            onComplete?.Invoke(true, string.Empty);
            yield break;
        }

        if (string.IsNullOrEmpty(currentRefreshToken))
        {
            onComplete?.Invoke(false, "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
            yield break;
        }

        string url = $"https://securetoken.googleapis.com/v1/token?key={FirebaseConfig.ApiKey}";
        string form = "grant_type=refresh_token&refresh_token=" + UnityWebRequest.EscapeURL(currentRefreshToken);
        using (var request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(form));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                onComplete?.Invoke(false, "Không thể làm mới phiên đăng nhập. Vui lòng đăng nhập lại.");
                yield break;
            }

            RefreshTokenResponse response;
            try
            {
                response = JsonUtility.FromJson<RefreshTokenResponse>(request.downloadHandler.text);
            }
            catch
            {
                response = null;
            }

            if (response == null || string.IsNullOrEmpty(response.id_token))
            {
                onComplete?.Invoke(false, "Phản hồi làm mới phiên đăng nhập không hợp lệ.");
                yield break;
            }

            CurrentIdToken = response.id_token;
            if (!string.IsNullOrEmpty(response.refresh_token)) currentRefreshToken = response.refresh_token;
            if (!string.IsNullOrEmpty(response.user_id)) CurrentUserId = response.user_id;
            SetTokenExpiry(response.expires_in);
            SaveSession();
            onComplete?.Invoke(true, string.Empty);
        }
    }

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
                    currentRefreshToken = response.refreshToken;
                    SetTokenExpiry(response.expiresIn);
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

    private IEnumerator MergeProfileTransactionRoutine(PlayerProfileData localData,
        Action<bool, PlayerProfileData, string> onComplete)
    {
        string url = FirebaseConfig.DatabaseUrl.TrimEnd('/') + $"/users/{CurrentUserId}.json";
        const int maxAttempts = 3;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            string etag;
            PlayerProfileData cloudData = null;
            using (var get = UnityWebRequest.Get(url))
            {
                get.SetRequestHeader("X-Firebase-ETag", "true");
                yield return get.SendWebRequest();
                if (get.result != UnityWebRequest.Result.Success)
                {
                    onComplete?.Invoke(false, null, "Không thể tải dữ liệu Cloud: " + get.error);
                    yield break;
                }
                etag = get.GetResponseHeader("ETag");
                string body = get.downloadHandler.text;
                if (!string.IsNullOrWhiteSpace(body) && body != "null")
                    cloudData = JsonUtility.FromJson<PlayerProfileData>(body);
            }

            PlayerProfileData merged = MergeProfiles(cloudData, localData);
            merged.lastUpdated = DateTime.UtcNow.ToString("o");
            using (var put = new UnityWebRequest(url, "PUT"))
            {
                put.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(merged)));
                put.downloadHandler = new DownloadHandlerBuffer();
                put.SetRequestHeader("Content-Type", "application/json");
                if (!string.IsNullOrEmpty(etag)) put.SetRequestHeader("if-match", etag);
                yield return put.SendWebRequest();

                if (put.result == UnityWebRequest.Result.Success)
                {
                    onComplete?.Invoke(true, merged, "Đã đồng bộ hai chiều với Cloud!");
                    yield break;
                }
                if (put.responseCode != 412)
                {
                    onComplete?.Invoke(false, null, "Không thể lưu dữ liệu Cloud: " + put.error);
                    yield break;
                }
            }
        }
        onComplete?.Invoke(false, null, "Dữ liệu Cloud vừa thay đổi trên thiết bị khác. Vui lòng đồng bộ lại.");
    }

    private IEnumerator UpdateEndlessTransactionRoutine(int score, int wave, int kills, float durationSeconds,
        Action<bool, string> onComplete)
    {
        string url = FirebaseConfig.DatabaseUrl.TrimEnd('/') + $"/users/{CurrentUserId}.json";
        const int maxAttempts = 3;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            string etag;
            PlayerProfileData profile;
            using (var get = UnityWebRequest.Get(url))
            {
                get.SetRequestHeader("X-Firebase-ETag", "true");
                yield return get.SendWebRequest();
                if (get.result != UnityWebRequest.Result.Success)
                {
                    string message = "Không thể tải hồ sơ để cập nhật Sinh tồn: " + get.error;
                    Debug.LogWarning(message);
                    onComplete?.Invoke(false, message);
                    yield break;
                }
                etag = get.GetResponseHeader("ETag");
                string body = get.downloadHandler.text;
                profile = string.IsNullOrWhiteSpace(body) || body == "null"
                    ? new PlayerProfileData()
                    : JsonUtility.FromJson<PlayerProfileData>(body);
            }

            profile.username = GetCurrentPlayerName();
            profile.email = CurrentUserEmail;
            MergeLocalProgress(profile);
            profile.endlessTotalKills = Mathf.Max(0, profile.endlessTotalKills) + Mathf.Max(0, kills);
            if (IsBetterEndlessRecord(wave, score, durationSeconds,
                    profile.endlessBestWave, profile.endlessBestScore, profile.endlessBestDuration))
            {
                profile.endlessBestScore = score;
                profile.endlessBestWave = wave;
                profile.endlessBestKills = kills;
                profile.endlessBestDuration = durationSeconds;
                profile.endlessBestPlayedAtUtc = DateTime.UtcNow.ToString("o");
            }
            profile.lastUpdated = DateTime.UtcNow.ToString("o");

            using (var put = new UnityWebRequest(url, "PUT"))
            {
                put.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(profile)));
                put.downloadHandler = new DownloadHandlerBuffer();
                put.SetRequestHeader("Content-Type", "application/json");
                if (!string.IsNullOrEmpty(etag)) put.SetRequestHeader("if-match", etag);
                yield return put.SendWebRequest();
                if (put.result == UnityWebRequest.Result.Success)
                {
                    var record = new EndlessScoreRecord
                    {
                        playerName = profile.username,
                        score = profile.endlessBestScore,
                        wave = profile.endlessBestWave,
                        kills = profile.endlessBestKills,
                        durationSeconds = profile.endlessBestDuration,
                        playedAtUtc = profile.endlessBestPlayedAtUtc,
                        gameVersion = Application.version
                    };
                    SaveGlobalLeaderboardRecord(record, onComplete);
                    yield break;
                }
                if (put.responseCode != 412)
                {
                    string message = "Không thể cập nhật kết quả Sinh tồn: " + put.error;
                    Debug.LogWarning(message);
                    onComplete?.Invoke(false, message);
                    yield break;
                }
            }
        }
        const string conflictMessage = "Hồ sơ thay đổi đồng thời trên thiết bị khác; kết quả Sinh tồn chưa được đồng bộ.";
        Debug.LogWarning(conflictMessage);
        onComplete?.Invoke(false, conflictMessage);
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

    private IEnumerator GetLeaderboardRoutine(string url, Action<bool, List<EndlessScoreRecord>, string> onComplete)
    {
        using (var request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                onComplete?.Invoke(false, null, "Không thể tải bảng xếp hạng thế giới: " + request.error);
                yield break;
            }

            try
            {
                List<EndlessScoreRecord> records = ParseFirebaseObjectValues<EndlessScoreRecord>(request.downloadHandler.text);
                records.Sort((left, right) =>
                {
                    int byWave = right.wave.CompareTo(left.wave);
                    if (byWave != 0) return byWave;
                    int byScore = right.score.CompareTo(left.score);
                    if (byScore != 0) return byScore;
                    return left.durationSeconds.CompareTo(right.durationSeconds);
                });
                if (records.Count > 50) records.RemoveRange(50, records.Count - 50);
                onComplete?.Invoke(true, records, "Đã tải bảng xếp hạng thế giới.");
            }
            catch (Exception exception)
            {
                onComplete?.Invoke(false, null, "Dữ liệu bảng xếp hạng không hợp lệ: " + exception.Message);
            }
        }
    }

    private static List<T> ParseFirebaseObjectValues<T>(string json) where T : class
    {
        var values = new List<T>();
        if (string.IsNullOrWhiteSpace(json) || json.Trim() == "null") return values;

        int depth = 0;
        int valueStart = -1;
        bool inString = false;
        bool escaped = false;
        for (int i = 0; i < json.Length; i++)
        {
            char c = json[i];
            if (inString)
            {
                if (escaped) escaped = false;
                else if (c == '\\') escaped = true;
                else if (c == '"') inString = false;
                continue;
            }
            if (c == '"') { inString = true; continue; }
            if (c == '{')
            {
                depth++;
                if (depth == 2) valueStart = i;
            }
            else if (c == '}')
            {
                if (depth == 2 && valueStart >= 0)
                {
                    string valueJson = json.Substring(valueStart, i - valueStart + 1);
                    T value = JsonUtility.FromJson<T>(valueJson);
                    if (value != null) values.Add(value);
                    valueStart = -1;
                }
                depth--;
            }
        }
        return values;
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
        PlayerPrefs.SetString(PrefSavedRefreshToken, currentRefreshToken ?? string.Empty);
        PlayerPrefs.SetString(PrefTokenExpiryTicks, tokenExpiryUtcTicks.ToString());

        string playerName = GetCurrentPlayerName();
        NetSession.LocalName = playerName;
        PlayerPrefs.SetString("Endless.PlayerName", playerName);

        PlayerPrefs.Save();
    }

    private static void SetTokenExpiry(string expiresInSeconds)
    {
        if (!double.TryParse(expiresInSeconds, out double seconds) || seconds <= 0d)
            seconds = 3600d;
        tokenExpiryUtcTicks = DateTime.UtcNow.AddSeconds(seconds).Ticks;
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
