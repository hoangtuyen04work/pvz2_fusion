using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Giao diện Đăng Nhập & Đăng Ký tài khoản đồng bộ Cloud theo phong cách đồ họa PvZ.
/// Sử dụng bảng gỗ/đá, input và các nút bấm đặc trưng của game.
/// </summary>
public class LoginOverlay : MonoBehaviour
{
    private static LoginOverlay instance;

    private GameObject rootPanel;
    private CanvasGroup canvasGroup;
    private InputField accountInput;
    private InputField passwordInput;
    private Text messageText;
    private Text titleText;
    private Button loginBtn;
    private Button registerBtn;
    private Button guestBtn;
    private Button syncBtn;
    private Button signOutBtn;

    private GameObject formContainer;
    private GameObject profileContainer;
    private Text profileInfoText;

    private Action onLoginSuccessCallback;

    public static void Show(Action onLoginSuccess = null)
    {
        if (instance == null)
        {
            var go = new GameObject("LoginOverlayCanvas");
            instance = go.AddComponent<LoginOverlay>();
            instance.BuildUI();
        }

        instance.onLoginSuccessCallback = onLoginSuccess;
        instance.RefreshView();
        instance.rootPanel.SetActive(true);
        instance.StopAllCoroutines();
        instance.StartCoroutine(instance.FadeRoutine(0f, 1f, 0.2f));
    }

    public static void Hide()
    {
        if (instance != null && instance.rootPanel != null)
        {
            instance.StopAllCoroutines();
            instance.StartCoroutine(instance.FadeRoutine(1f, 0f, 0.15f, () => instance.rootPanel.SetActive(false)));
        }
    }

    private void BuildUI()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1000f, 750f);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            var es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            DontDestroyOnLoad(es);
        }

        // Root Dim Background
        rootPanel = new GameObject("RootPanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        rootPanel.transform.SetParent(transform, false);
        Stretch(rootPanel.GetComponent<RectTransform>());
        rootPanel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);
        canvasGroup = rootPanel.GetComponent<CanvasGroup>();

        // Main Dialog Frame
        var dialogObj = new GameObject("DialogFrame", typeof(RectTransform), typeof(Image));
        dialogObj.transform.SetParent(rootPanel.transform, false);
        var dialogRect = dialogObj.GetComponent<RectTransform>();
        dialogRect.anchorMin = dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
        dialogRect.sizeDelta = new Vector2(580f, 480f);
        dialogRect.anchoredPosition = Vector2.zero;

        var dialogImage = dialogObj.GetComponent<Image>();
        var dialogSprite = Resources.Load<Sprite>("GameUI/dialog_main");
        if (dialogSprite != null)
        {
            dialogImage.sprite = dialogSprite;
            dialogImage.type = Image.Type.Sliced;
        }
        else
        {
            dialogImage.color = new Color(0.18f, 0.12f, 0.08f, 0.95f);
        }

        Font font = Resources.Load<Font>("Fonts/Baloo2");
        if (font == null)
        {
            try
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            catch
            {
                font = Resources.Load<Font>("Fonts/造字工房甜栗体(非商用)");
            }
        }

        // Title
        titleText = CreateText("Title", dialogObj.transform, "TÀI KHOẢN CLOUD", font, 28, TextAnchor.MiddleCenter, new Color(0.98f, 0.88f, 0.45f));
        CenterRect(titleText.rectTransform, new Vector2(360f, 40f), new Vector2(0f, 195f));
        AddOutline(titleText.gameObject, new Color(0.15f, 0.05f, 0.02f), new Vector2(1.5f, -1.5f));

        // Nút Đóng (Góc trên phải)
        var closeObj = CreateButton("CloseBtn", dialogObj.transform, "X", font, 20, new Vector2(40f, 40f), new Vector2(250f, 205f), Hide);
        var cancelSprite = Resources.Load<Sprite>("GameUI/cancel");
        if (cancelSprite != null)
        {
            closeObj.GetComponent<Image>().sprite = cancelSprite;
            closeObj.GetComponentInChildren<Text>().text = "";
        }

        // ================= FORM CONTAINER (Khi chưa đăng nhập) =================
        formContainer = new GameObject("FormContainer", typeof(RectTransform));
        formContainer.transform.SetParent(dialogObj.transform, false);
        Stretch(formContainer.GetComponent<RectTransform>());

        // Nhãn & Input Tài khoản
        var accLabel = CreateText("AccLabel", formContainer.transform, "TÀI KHOẢN / EMAIL:", font, 18, TextAnchor.MiddleLeft, new Color(1f, 0.92f, 0.6f));
        CenterRect(accLabel.rectTransform, new Vector2(400f, 26f), new Vector2(0f, 120f));
        AddOutline(accLabel.gameObject, new Color(0.12f, 0.05f, 0.02f), new Vector2(1.2f, -1.2f));

        accountInput = CreateInputField("AccountInput", formContainer.transform, "Nhập tên tài khoản hoặc email...", font, new Vector2(400f, 48f), new Vector2(0f, 80f));

        // Nhãn & Input Mật khẩu
        var passLabel = CreateText("PassLabel", formContainer.transform, "MẬT KHẨU:", font, 18, TextAnchor.MiddleLeft, new Color(1f, 0.92f, 0.6f));
        CenterRect(passLabel.rectTransform, new Vector2(400f, 26f), new Vector2(0f, 32f));
        AddOutline(passLabel.gameObject, new Color(0.12f, 0.05f, 0.02f), new Vector2(1.2f, -1.2f));

        passwordInput = CreateInputField("PasswordInput", formContainer.transform, "Tối thiểu 6 ký tự...", font, new Vector2(400f, 48f), new Vector2(0f, -8f), isPassword: true);

        // Nút Đăng Nhập
        var loginObj = CreateButton("LoginBtn", formContainer.transform, "ĐĂNG NHẬP", font, 20, new Vector2(190f, 48f), new Vector2(-105f, -74f), OnClickSignIn);
        loginBtn = loginObj.GetComponent<Button>();

        // Nút Đăng Ký
        var regObj = CreateButton("RegisterBtn", formContainer.transform, "ĐĂNG KÝ", font, 20, new Vector2(190f, 48f), new Vector2(105f, -74f), OnClickSignUp);
        registerBtn = regObj.GetComponent<Button>();

        // Nút Chơi Ngay (Khách)
        var guestObj = CreateButton("GuestBtn", formContainer.transform, "CHƠI NGAY (KHÁCH)", font, 18, new Vector2(260f, 42f), new Vector2(0f, -130f), OnClickGuest);
        guestBtn = guestObj.GetComponent<Button>();

        // ================= PROFILE CONTAINER (Khi đã đăng nhập) =================
        profileContainer = new GameObject("ProfileContainer", typeof(RectTransform));
        profileContainer.transform.SetParent(dialogObj.transform, false);
        Stretch(profileContainer.GetComponent<RectTransform>());

        profileInfoText = CreateText("ProfileInfo", profileContainer.transform, "", font, 18, TextAnchor.MiddleCenter, new Color(0.95f, 0.95f, 0.85f));
        CenterRect(profileInfoText.rectTransform, new Vector2(480f, 160f), new Vector2(0f, 50f));

        var syncObj = CreateButton("SyncBtn", profileContainer.transform, "ĐỒNG BỘ TIẾN TRÌNH LÊN CLOUD", font, 18, new Vector2(320f, 46f), new Vector2(0f, -40f), OnClickSyncCloud);
        syncBtn = syncObj.GetComponent<Button>();

        var outObj = CreateButton("SignOutBtn", profileContainer.transform, "ĐĂNG XUẤT", font, 18, new Vector2(180f, 40f), new Vector2(0f, -100f), OnClickSignOut);
        signOutBtn = outObj.GetComponent<Button>();

        // Message text thông báo trạng thái
        messageText = CreateText("MessageText", dialogObj.transform, "", font, 16, TextAnchor.MiddleCenter, new Color(1f, 0.4f, 0.3f));
        CenterRect(messageText.rectTransform, new Vector2(500f, 32f), new Vector2(0f, -188f));

        RefreshView();
    }

    public void RefreshView()
    {
        messageText.text = "";
        bool loggedIn = FirebaseAuthService.IsLoggedIn;

        formContainer.SetActive(!loggedIn);
        profileContainer.SetActive(loggedIn);

        if (loggedIn)
        {
            titleText.text = "HỒ SƠ CLOUD";
            string user = FirebaseAuthService.GetCurrentPlayerName();

            // Lấy kỷ lục Sinh tồn từ bảng thành tích
            var topEndless = EndlessLeaderboard.GetPlayerTopRecords(user, 1);
            int endlessScore = topEndless.Count > 0 ? topEndless[0].score : 0;
            int endlessWave = topEndless.Count > 0 ? topEndless[0].wave : 0;

            profileInfoText.text = $"Xin chào:\n<color=#FFFF66>{user}</color>\n\n"
                                 + $"Chiến dịch: <color=#66FF66>{CampaignProgress.BestScore}</color> điểm (Màn {CampaignProgress.HighestMap})\n"
                                 + $"Sinh tồn: <color=#FFD700>{endlessScore:N0}</color> điểm (Màn {endlessWave}) | Thắng: {CampaignProgress.Wins}";
        }
        else
        {
            titleText.text = "TÀI KHOẢN CLOUD";
            accountInput.text = "";
            passwordInput.text = "";
        }
    }

    // ================= Xử lý Sự kiện nút =================

    private void OnClickSignIn()
    {
        string acc = accountInput.text.Trim();
        string pass = passwordInput.text.Trim();

        SetInteractable(false);
        messageText.color = new Color(1f, 0.9f, 0.4f);
        messageText.text = "Đang kết nối tới Cloud...";

        FirebaseAuthService.Instance.SignIn(acc, pass, (success, msg) =>
        {
            SetInteractable(true);
            messageText.color = success ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.4f, 0.3f);
            messageText.text = msg;

            if (success)
            {
                // Tải dữ liệu cloud về cập nhật local
                FirebaseAuthService.Instance.LoadPlayerDataFromCloud((loadOk, profile, loadMsg) =>
                {
                    if (loadOk && profile != null)
                    {
                        FirebaseAuthService.ApplyCloudProgress(profile);

                        // Đồng bộ kỷ lục Sinh tồn từ Cloud về Leaderboard cục bộ
                        if (profile.endlessBestScore > 0 || profile.endlessBestWave > 0)
                        {
                            var existing = EndlessLeaderboard.GetPlayerTopRecords(FirebaseAuthService.GetCurrentPlayerName(), 1);
                            if (existing.Count == 0 || profile.endlessBestScore > existing[0].score)
                            {
                                EndlessLeaderboard.Add(new EndlessScoreRecord
                                {
                                    playerName = FirebaseAuthService.GetCurrentPlayerName(),
                                    score = profile.endlessBestScore,
                                    wave = profile.endlessBestWave,
                                    kills = profile.endlessTotalKills,
                                    durationSeconds = 0f,
                                    seed = 0,
                                    playedAtUtc = DateTime.UtcNow.ToString("o"),
                                    gameVersion = Application.version
                                });
                            }
                        }
                    }
                    RefreshView();
                    onLoginSuccessCallback?.Invoke();
                });
            }
        });
    }

    private void OnClickSignUp()
    {
        string acc = accountInput.text.Trim();
        string pass = passwordInput.text.Trim();

        SetInteractable(false);
        messageText.color = new Color(1f, 0.9f, 0.4f);
        messageText.text = "Đang tạo tài khoản Cloud...";

        FirebaseAuthService.Instance.SignUp(acc, pass, (success, msg) =>
        {
            SetInteractable(true);
            messageText.color = success ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.4f, 0.3f);
            messageText.text = msg;

            if (success)
            {
                // Tự động lưu tiến trình hiện tại lên tài khoản mới
                OnClickSyncCloud();
                RefreshView();
                onLoginSuccessCallback?.Invoke();
            }
        });
    }

    private void OnClickGuest()
    {
        FirebaseAuthService.Instance.SignInGuest((success, msg) =>
        {
            messageText.color = new Color(0.4f, 1f, 0.4f);
            messageText.text = msg;
            RefreshView();
            onLoginSuccessCallback?.Invoke();
        });
    }

    private void OnClickSyncCloud()
    {
        messageText.color = new Color(1f, 0.9f, 0.4f);
        messageText.text = "Đang đồng bộ dữ liệu...";

        string playerName = FirebaseAuthService.GetCurrentPlayerName();
        var topEndless = EndlessLeaderboard.GetPlayerTopRecords(playerName, 1);
        int endlessScore = topEndless.Count > 0 ? topEndless[0].score : 0;
        int endlessWave = topEndless.Count > 0 ? topEndless[0].wave : 0;
        int endlessKills = topEndless.Count > 0 ? topEndless[0].kills : 0;

        var profile = new FirebaseAuthService.PlayerProfileData
        {
            username = playerName,
            email = FirebaseAuthService.CurrentUserEmail,
            bestScore = CampaignProgress.BestScore,
            highestMap = CampaignProgress.HighestMap,
            wins = CampaignProgress.Wins,
            losses = CampaignProgress.Losses,
            endlessBestScore = endlessScore,
            endlessBestWave = endlessWave,
            endlessTotalKills = endlessKills
        };

        FirebaseAuthService.MergeLocalProgress(profile);

        FirebaseAuthService.Instance.SavePlayerDataToCloud(profile, (success, msg) =>
        {
            messageText.color = success ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.4f, 0.3f);
            messageText.text = msg;
            RefreshView();
        });
    }

    private void OnClickSignOut()
    {
        FirebaseAuthService.Instance.SignOut();
        RefreshView();
        messageText.color = new Color(1f, 0.9f, 0.4f);
        messageText.text = "Đã đăng xuất!";
        onLoginSuccessCallback?.Invoke();
    }

    private void SetInteractable(bool interactable)
    {
        loginBtn.interactable = interactable;
        registerBtn.interactable = interactable;
        guestBtn.interactable = interactable;
        accountInput.interactable = interactable;
        passwordInput.interactable = interactable;
    }

    // ================= Helper Dựng UI =================

    private IEnumerator FadeRoutine(float from, float to, float duration, Action onFinish = null)
    {
        float t = 0f;
        canvasGroup.alpha = from;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        canvasGroup.alpha = to;
        onFinish?.Invoke();
    }

    private static Text CreateText(string name, Transform parent, string content, Font font, int size, TextAnchor anchor, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<Text>();
        text.font = font;
        text.text = content;
        text.fontSize = size;
        text.alignment = anchor;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static InputField CreateInputField(string name, Transform parent, string placeholderText, Font font, Vector2 size, Vector2 pos, bool isPassword = false)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField), typeof(Outline));
        go.transform.SetParent(parent, false);
        CenterRect(go.GetComponent<RectTransform>(), size, pos);

        // Nền ô nhập: màu nền đất sẫm có độ tương phản cao với chữ
        var bg = go.GetComponent<Image>();
        bg.color = new Color(0.12f, 0.08f, 0.05f, 0.96f);
        bg.raycastTarget = true;

        // Viền của ô nhập giúp nổi bật trên khung gỗ
        var outline = go.GetComponent<Outline>();
        outline.effectColor = new Color(0.45f, 0.32f, 0.18f, 1f);
        outline.effectDistance = new Vector2(2f, -2f);

        // Content Text (chữ người chơi gõ)
        var valText = CreateText("Text", go.transform, "", font, 20, TextAnchor.MiddleLeft, new Color(1f, 1f, 0.9f));
        Stretch(valText.rectTransform);
        valText.rectTransform.offsetMin = new Vector2(14f, 4f);
        valText.rectTransform.offsetMax = new Vector2(-14f, -4f);
        valText.horizontalOverflow = HorizontalWrapMode.Wrap;
        valText.verticalOverflow = VerticalWrapMode.Overflow;
        valText.supportRichText = false;
        valText.raycastTarget = false;

        // Placeholder Text (gợi ý khi chưa gõ)
        var phText = CreateText("Placeholder", go.transform, placeholderText, font, 18, TextAnchor.MiddleLeft, new Color(0.68f, 0.62f, 0.52f, 0.75f));
        Stretch(phText.rectTransform);
        phText.fontStyle = FontStyle.Italic;
        phText.rectTransform.offsetMin = new Vector2(14f, 4f);
        phText.rectTransform.offsetMax = new Vector2(-14f, -4f);
        phText.horizontalOverflow = HorizontalWrapMode.Wrap;
        phText.verticalOverflow = VerticalWrapMode.Overflow;
        phText.supportRichText = false;
        phText.raycastTarget = false;

        var input = go.GetComponent<InputField>();
        input.textComponent = valText;
        input.placeholder = phText;
        input.caretColor = new Color(1f, 0.9f, 0.2f, 1f); // Con trỏ nhấp nháy màu vàng sáng
        input.caretWidth = 2;
        input.selectionColor = new Color(0.2f, 0.6f, 1f, 0.5f);
        input.lineType = InputField.LineType.SingleLine;
        input.characterLimit = 40;

        if (isPassword)
        {
            input.contentType = InputField.ContentType.Password;
            input.inputType = InputField.InputType.Password;
        }
        else
        {
            input.contentType = InputField.ContentType.Standard;
        }
        return input;
    }

    private static GameObject CreateButton(string name, Transform parent, string label, Font font, int fontSize, Vector2 size, Vector2 pos, Action onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        CenterRect(go.GetComponent<RectTransform>(), size, pos);

        var image = go.GetComponent<Image>();
        var btnSprite = Resources.Load<Sprite>("GameUI/button1");
        if (btnSprite != null)
        {
            image.sprite = btnSprite;
            image.type = Image.Type.Sliced;
        }
        else
        {
            image.color = new Color(0.35f, 0.22f, 0.12f, 1f);
        }

        var btn = go.GetComponent<Button>();
        btn.onClick.AddListener(() => onClick?.Invoke());

        var text = CreateText("Text", go.transform, label, font, fontSize, TextAnchor.MiddleCenter, new Color(1f, 0.96f, 0.8f));
        Stretch(text.rectTransform);
        AddOutline(text.gameObject, new Color(0.12f, 0.05f, 0.02f), new Vector2(1.2f, -1.2f));

        return go;
    }

    private static void AddOutline(GameObject go, Color color, Vector2 dist)
    {
        var outline = go.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = dist;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void CenterRect(RectTransform rect, Vector2 size, Vector2 pos)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;
    }
}
