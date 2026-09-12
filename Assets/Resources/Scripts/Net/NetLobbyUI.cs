using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tự gắn sảnh chờ vào Main Menu mà không cần sửa scene.
/// </summary>
public static class NetLobbyBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "MainMenu") return;
        if (Object.FindAnyObjectByType<NetLobbyUI>() != null) return;
        new GameObject("Net Lobby", typeof(NetLobbyUI));
    }
}

/// <summary>
/// Sảnh chờ: chọn chế độ, tạo phòng hoặc nhập IP để tham gia, rồi cùng vào màn chơi.
/// Toàn bộ giao diện dựng bằng code nên không phải chỉnh sửa file scene.
/// </summary>
public class NetLobbyUI : MonoBehaviour
{
    private static NetLobbyUI instance;

    private static readonly string[] LevelNames =
    {
        "Mèo Miu Xuất Trận",
        "Hành Trình Mới",
        "Thầy Luyện Xác",
        "Vùng Đất Bất Tử",
        "Sông Băng Địa Cực",
        "Sân Thử Nghiệm"
    };

    private enum Page { Home, Host, Join }

    private Font font;
    private Sprite buttonSprite;

    private GameObject root;
    private GameObject homePage;
    private GameObject hostPage;
    private GameObject joinPage;

    private Text noticeText;
    private Text hostAddressText;
    private Text hostStatusText;
    private Text hostLevelText;
    private Text joinStatusText;
    private Text modeDescriptionText;

    private Button startButton;
    private Button connectButton;
    private Image coopFrame;
    private Image pvpFrame;

    private InputField ipInput;

    private NetGameMode chosenMode = NetGameMode.Coop;
    private int chosenLevel = 1;
    private bool peerReady;
    private bool listening;
    private Page page = Page.Home;

    #region Vòng đời

    private void Awake()
    {
        instance = this;
        font = Resources.Load<Font>("Fonts/Baloo2");
        buttonSprite = Resources.Load<Sprite>("Sprites/UI/Menu/button_normal");
        Build();
        root.SetActive(false);
    }

    private void OnDestroy()
    {
        Unsubscribe();
        if (instance == this) instance = null;
    }

    /// <summary>Mở sảnh chờ. MainMenuController gọi hàm này.</summary>
    public static void Open()
    {
        Open(-1);
    }

    /// <summary>
    /// Mở sảnh chờ kèm màn chơi vừa chọn ở bảng Phiêu lưu.
    /// Màn ngoài danh sách chơi mạng (như Đấu trường Gargantuar) thì bỏ qua, giữ màn mặc định.
    /// </summary>
    public static void Open(int preferredLevel)
    {
        if (instance == null) return;

        if (preferredLevel >= 0 && preferredLevel < LevelNames.Length)
            instance.chosenLevel = preferredLevel;

        instance.Show();
    }

    private void Show()
    {
        root.SetActive(true);
        GoHome();
    }

    private void Subscribe()
    {
        NetManager manager = NetManager.Instance;
        manager.OnMessage += HandleMessage;
        manager.OnPeerConnected += HandlePeerConnected;
        manager.OnPeerLost += HandlePeerLost;
    }

    private void Unsubscribe()
    {
        if (!NetManager.Exists) return;
        NetManager manager = NetManager.Instance;
        manager.OnMessage -= HandleMessage;
        manager.OnPeerConnected -= HandlePeerConnected;
        manager.OnPeerLost -= HandlePeerLost;
    }

    #endregion

    #region Điều hướng trang

    private void GoHome()
    {
        page = Page.Home;
        peerReady = false;
        listening = false;
        homePage.SetActive(true);
        hostPage.SetActive(false);
        joinPage.SetActive(false);
        UpdateModeHighlight();
    }

    private void Close()
    {
        LeaveRoom();
        root.SetActive(false);
    }

    private void LeaveRoom()
    {
        Unsubscribe();
        if (NetManager.Exists) NetManager.Instance.Leave("Đối phương đã đóng phòng");
        peerReady = false;
        listening = false;
    }

    private void BackToHome()
    {
        LeaveRoom();
        GoHome();
    }

    #endregion

    #region Tạo phòng

    private void HostRoom()
    {
        Subscribe();
        peerReady = false;

        if (!NetManager.Instance.StartHost(NetSession.DefaultPort))
        {
            ShowNotice(NetManager.Instance.LastError);
            Unsubscribe();
            return;
        }

        listening = true;
        page = Page.Host;
        homePage.SetActive(false);
        hostPage.SetActive(true);
        joinPage.SetActive(false);

        hostAddressText.text = BuildAddressText();
        hostStatusText.text = "Đang chờ người chơi thứ hai...";
        startButton.interactable = false;
        UpdateHostLevelText();
    }

    //Máy hay có nhiều card mạng, nên hiện thêm các địa chỉ dự phòng để người chơi thử lần lượt
    private string BuildAddressText()
    {
        string main = NetTransport.GetLocalIPv4();
        string text = "Địa chỉ phòng:  " + main + "  ·  cổng " + NetSession.DefaultPort;

        List<string> others = new List<string>();
        foreach (string address in NetTransport.GetAllLocalIPv4())
        {
            if (address != main) others.Add(address);
        }

        if (others.Count > 0)
            text += "\nNếu không vào được, thử: " + string.Join("  |  ", others.ToArray());

        return text;
    }

    private void ChangeLevel(int delta)
    {
        chosenLevel = Mathf.Clamp(chosenLevel + delta, 0, LevelNames.Length - 1);
        UpdateHostLevelText();
        if (peerReady)
            NetManager.Instance.Send(NetMessage.Of(NetMsg.Lobby).Int(chosenLevel));
    }

    private void UpdateHostLevelText()
    {
        hostLevelText.text = "Màn " + (chosenLevel + 1) + " — " + LevelNames[chosenLevel];
    }

    private void StartMatch()
    {
        if (!peerReady) return;

        NetSession.Mode = chosenMode;
        NetSession.Role = NetRole.Host;
        NetSession.Level = chosenLevel;
        GameSession.SelectedLevel = chosenLevel;

        NetManager.Instance.Send(
            NetMessage.Of(NetMsg.Start).Int(chosenLevel, (int)chosenMode));

        Unsubscribe();
        SceneManager.LoadScene("GameScene");
    }

    #endregion

    #region Tham gia phòng

    private void OpenJoinPage()
    {
        page = Page.Join;
        homePage.SetActive(false);
        hostPage.SetActive(false);
        joinPage.SetActive(true);
        joinStatusText.text = "Nhập địa chỉ phòng do người chơi kia đọc cho bạn.";
        connectButton.interactable = true;
    }

    private void JoinRoom()
    {
        Subscribe();
        string ip = ipInput != null ? ipInput.text : string.Empty;
        if (string.IsNullOrEmpty(ip)) ip = "127.0.0.1";

        connectButton.interactable = false;
        joinStatusText.text = "Đang kết nối tới " + ip + "...";
        NetManager.Instance.StartClient(ip, NetSession.DefaultPort);
    }

    #endregion

    #region Xử lý gói tin

    private void HandlePeerConnected()
    {
        //Phía khách: vừa nối được thì tự giới thiệu
        if (page == Page.Join)
        {
            joinStatusText.text = "Đã nối, đang xin vào phòng...";
            NetManager.Instance.Send(
                NetMessage.Of(NetMsg.Hello)
                    .Str(SystemInfo.deviceName)
                    .Int(NetSession.ProtocolVersion));
        }
    }

    private void HandlePeerLost(string reason)
    {
        if (page == Page.Host)
        {
            peerReady = false;
            startButton.interactable = false;
            hostStatusText.text = string.IsNullOrEmpty(reason)
                ? "Đối phương đã rời phòng."
                : reason + "\nHãy mở lại phòng.";
            listening = false;
        }
        else if (page == Page.Join)
        {
            connectButton.interactable = true;
            joinStatusText.text = string.IsNullOrEmpty(reason) ? "Mất kết nối." : reason;
        }
    }

    private void HandleMessage(NetMessage message)
    {
        switch (message.t)
        {
            case NetMsg.Hello:
                HandleHello(message);
                break;

            case NetMsg.Welcome:
                HandleWelcome(message);
                break;

            case NetMsg.Lobby:
                chosenLevel = Mathf.Clamp(message.i, 0, LevelNames.Length - 1);
                if (page == Page.Join)
                    joinStatusText.text = "Đã vào phòng.\nChủ phòng chọn: Màn " + (chosenLevel + 1)
                        + " — " + LevelNames[chosenLevel] + "\nĐang chờ bắt đầu...";
                break;

            case NetMsg.Start:
                HandleStart(message);
                break;
        }
    }

    private void HandleHello(NetMessage message)
    {
        if (page != Page.Host) return;

        if (message.i != NetSession.ProtocolVersion)
        {
            NetManager.Instance.Send(
                NetMessage.Of(NetMsg.Welcome)
                    .Bool(false)
                    .Str("", "Hai máy chạy hai phiên bản khác nhau."));
            hostStatusText.text = "Từ chối: đối phương dùng bản khác phiên bản.";
            return;
        }

        peerReady = true;
        NetSession.PeerName = string.IsNullOrEmpty(message.s) ? "Người chơi 2" : message.s;

        NetManager.Instance.Send(
            NetMessage.Of(NetMsg.Welcome)
                .Bool(true)
                .Str(SystemInfo.deviceName)
                .Int(chosenLevel, (int)chosenMode));

        hostStatusText.text = NetSession.PeerName + " đã vào phòng.\nBấm BẮT ĐẦU khi cả hai sẵn sàng.";
        startButton.interactable = true;
    }

    private void HandleWelcome(NetMessage message)
    {
        if (page != Page.Join) return;

        if (!message.b)
        {
            joinStatusText.text = "Bị từ chối: " + message.u;
            connectButton.interactable = true;
            if (NetManager.Exists) NetManager.Instance.Leave("");
            return;
        }

        chosenLevel = Mathf.Clamp(message.i, 0, LevelNames.Length - 1);
        chosenMode = (NetGameMode)message.j;
        NetSession.PeerName = string.IsNullOrEmpty(message.s) ? "Chủ phòng" : message.s;

        string role = chosenMode == NetGameMode.Pvp ? "Bạn sẽ điều khiển PHE ZOMBIE." : "Hai người cùng phe trồng cây.";
        joinStatusText.text = "Đã vào phòng của " + NetSession.PeerName + ".\n"
            + "Chế độ: " + (chosenMode == NetGameMode.Pvp ? "Đối kháng" : "Đồng đội") + " — " + role + "\n"
            + "Màn " + (chosenLevel + 1) + " — " + LevelNames[chosenLevel] + "\nĐang chờ chủ phòng bắt đầu...";
    }

    private void HandleStart(NetMessage message)
    {
        if (page != Page.Join) return;

        NetSession.Mode = (NetGameMode)message.j;
        NetSession.Role = NetRole.Client;
        NetSession.Level = Mathf.Clamp(message.i, 0, LevelNames.Length - 1);
        GameSession.SelectedLevel = NetSession.Level;

        Unsubscribe();
        SceneManager.LoadScene("GameScene");
    }

    #endregion

    private void Update()
    {
        if (root == null || !root.activeSelf) return;

        if (page == Page.Host && listening && !peerReady && NetManager.Exists)
        {
            NetStatus status = NetManager.Instance.Status;
            if (status == NetStatus.Failed)
            {
                hostStatusText.text = NetManager.Instance.LastError;
                listening = false;
            }
        }
        else if (page == Page.Join && NetManager.Exists)
        {
            NetStatus status = NetManager.Instance.Status;
            if (status == NetStatus.Failed && connectButton.interactable == false)
            {
                joinStatusText.text = NetManager.Instance.LastError;
                connectButton.interactable = true;
            }
        }
    }

    #region Dựng giao diện

    private void Build()
    {
        GameObject canvasObject = new GameObject("NetLobbyCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1000f, 750f);
        scaler.matchWidthOrHeight = 0.5f;

        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        root = new GameObject("Nền mờ", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(canvasObject.transform, false);
        Stretch(root.GetComponent<RectTransform>());
        root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.72f);

        GameObject frame = new GameObject("Khung 4x3", typeof(RectTransform), typeof(AspectRatioFitter));
        frame.transform.SetParent(root.transform, false);
        Stretch(frame.GetComponent<RectTransform>());
        AspectRatioFitter fitter = frame.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 4f / 3f;

        GameObject panel = CreatePanel("Bảng sảnh chờ", frame.transform,
            0.11f, 0.10f, 0.89f, 0.90f, new Color(0.075f, 0.10f, 0.055f, 0.98f));

        Text title = CreateText("Tiêu đề", panel.transform, "CHƠI MẠNG HAI NGƯỜI", 40,
            TextAnchor.MiddleCenter, new Color(0.62f, 1f, 0.25f));
        SetAnchors(title.rectTransform, 0.05f, 0.885f, 0.95f, 0.975f);

        noticeText = CreateText("Thông báo", panel.transform, "", 20,
            TextAnchor.MiddleCenter, new Color(1f, 0.55f, 0.35f));
        SetAnchors(noticeText.rectTransform, 0.05f, 0.02f, 0.95f, 0.075f);

        BuildHomePage(panel.transform);
        BuildHostPage(panel.transform);
        BuildJoinPage(panel.transform);
    }

    private void BuildHomePage(Transform parent)
    {
        homePage = CreateGroup("Trang chính", parent);

        Text hint = CreateText("Hướng dẫn", homePage.transform,
            "Chọn kiểu chơi, sau đó một người TẠO PHÒNG và đọc địa chỉ cho người kia THAM GIA.",
            21, TextAnchor.MiddleCenter, new Color(0.85f, 0.88f, 0.80f));
        SetAnchors(hint.rectTransform, 0.06f, 0.795f, 0.94f, 0.875f);

        //Hai ô chọn chế độ
        coopFrame = BuildModeCard(homePage.transform, 0.06f, 0.45f, 0.485f, 0.78f,
            "ĐỒNG ĐỘI",
            "Hai người cùng một phe.\nChung kho nắng, chung dãy thẻ cây,\ncùng chống lại các đợt zombie.",
            NetGameMode.Coop);

        pvpFrame = BuildModeCard(homePage.transform, 0.515f, 0.45f, 0.94f, 0.78f,
            "ĐỐI KHÁNG",
            "Một người trồng cây phòng thủ.\nNgười kia làm chỉ huy zombie,\ndùng não để thả quân theo từng hàng.",
            NetGameMode.Pvp);

        modeDescriptionText = CreateText("Mô tả chế độ", homePage.transform, "", 20,
            TextAnchor.MiddleCenter, new Color(0.75f, 0.95f, 0.60f));
        SetAnchors(modeDescriptionText.rectTransform, 0.06f, 0.355f, 0.94f, 0.43f);

        CreateButton("Tạo phòng", homePage.transform, "TẠO PHÒNG", 27,
            0.09f, 0.20f, 0.47f, 0.33f, HostRoom);

        CreateButton("Tham gia", homePage.transform, "THAM GIA", 27,
            0.53f, 0.20f, 0.91f, 0.33f, OpenJoinPage);

        CreateButton("Đóng sảnh", homePage.transform, "ĐÓNG", 24,
            0.36f, 0.085f, 0.64f, 0.185f, Close);
    }

    private Image BuildModeCard(Transform parent, float xMin, float yMin, float xMax, float yMax,
        string title, string body, NetGameMode mode)
    {
        GameObject card = new GameObject("Chế độ " + title,
            typeof(RectTransform), typeof(Image), typeof(Button));
        card.transform.SetParent(parent, false);
        SetAnchors(card.GetComponent<RectTransform>(), xMin, yMin, xMax, yMax);

        Image frame = card.GetComponent<Image>();
        frame.sprite = buttonSprite;
        frame.type = Image.Type.Sliced;

        Button button = card.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        NetGameMode captured = mode;
        button.onClick.AddListener(delegate { SelectMode(captured); });

        Text head = CreateText("Tên", card.transform, title, 28, TextAnchor.MiddleCenter, Color.white);
        SetAnchors(head.rectTransform, 0.05f, 0.72f, 0.95f, 0.95f);
        head.raycastTarget = false;

        Text description = CreateText("Mô tả", card.transform, body, 19,
            TextAnchor.UpperCenter, new Color(0.88f, 0.92f, 0.84f));
        SetAnchors(description.rectTransform, 0.07f, 0.08f, 0.93f, 0.70f);
        description.raycastTarget = false;

        return frame;
    }

    private void SelectMode(NetGameMode mode)
    {
        chosenMode = mode;
        UpdateModeHighlight();
    }

    private void UpdateModeHighlight()
    {
        Color on = new Color(0.64f, 1f, 0.30f);
        Color off = new Color(0.74f, 0.76f, 0.70f);
        if (coopFrame != null) coopFrame.color = chosenMode == NetGameMode.Coop ? on : off;
        if (pvpFrame != null) pvpFrame.color = chosenMode == NetGameMode.Pvp ? on : off;

        if (modeDescriptionText != null)
            modeDescriptionText.text = chosenMode == NetGameMode.Pvp
                ? "Chủ phòng cầm phe Cây, người tham gia cầm phe Zombie."
                : "Cả hai cùng trồng cây, thắng thua chia đều.";
    }

    private void BuildHostPage(Transform parent)
    {
        hostPage = CreateGroup("Trang tạo phòng", parent);

        Text head = CreateText("Nhãn", hostPage.transform, "PHÒNG CỦA BẠN", 30,
            TextAnchor.MiddleCenter, new Color(0.62f, 1f, 0.25f));
        SetAnchors(head.rectTransform, 0.06f, 0.79f, 0.94f, 0.87f);

        hostAddressText = CreateText("Địa chỉ", hostPage.transform, "", 24,
            TextAnchor.MiddleCenter, Color.white);
        SetAnchors(hostAddressText.rectTransform, 0.06f, 0.69f, 0.94f, 0.78f);

        hostStatusText = CreateText("Trạng thái", hostPage.transform, "", 21,
            TextAnchor.MiddleCenter, new Color(0.85f, 0.90f, 0.80f));
        SetAnchors(hostStatusText.rectTransform, 0.06f, 0.50f, 0.94f, 0.68f);

        Text levelLabel = CreateText("Nhãn màn", hostPage.transform, "MÀN CHƠI", 22,
            TextAnchor.MiddleCenter, new Color(0.75f, 0.80f, 0.70f));
        SetAnchors(levelLabel.rectTransform, 0.06f, 0.42f, 0.94f, 0.48f);

        CreateButton("Màn trước", hostPage.transform, "<", 28,
            0.10f, 0.30f, 0.20f, 0.41f, delegate { ChangeLevel(-1); });

        hostLevelText = CreateText("Tên màn", hostPage.transform, "", 24,
            TextAnchor.MiddleCenter, Color.white);
        SetAnchors(hostLevelText.rectTransform, 0.21f, 0.30f, 0.79f, 0.41f);

        CreateButton("Màn sau", hostPage.transform, ">", 28,
            0.80f, 0.30f, 0.90f, 0.41f, delegate { ChangeLevel(1); });

        GameObject start = CreateButton("Bắt đầu", hostPage.transform, "BẮT ĐẦU", 28,
            0.30f, 0.155f, 0.70f, 0.275f, StartMatch);
        startButton = start.GetComponent<Button>();
        startButton.interactable = false;

        CreateButton("Huỷ phòng", hostPage.transform, "QUAY LẠI", 22,
            0.36f, 0.07f, 0.64f, 0.145f, BackToHome);
    }

    private void BuildJoinPage(Transform parent)
    {
        joinPage = CreateGroup("Trang tham gia", parent);

        Text head = CreateText("Nhãn", joinPage.transform, "THAM GIA PHÒNG", 30,
            TextAnchor.MiddleCenter, new Color(0.62f, 1f, 0.25f));
        SetAnchors(head.rectTransform, 0.06f, 0.79f, 0.94f, 0.87f);

        Text label = CreateText("Nhãn IP", joinPage.transform,
            "Địa chỉ phòng (ví dụ 192.168.1.12)", 21,
            TextAnchor.MiddleCenter, new Color(0.85f, 0.88f, 0.80f));
        SetAnchors(label.rectTransform, 0.06f, 0.70f, 0.94f, 0.77f);

        ipInput = CreateInputField("Ô nhập IP", joinPage.transform,
            0.20f, 0.575f, 0.80f, 0.685f, "192.168.1.10");

        GameObject connect = CreateButton("Kết nối", joinPage.transform, "KẾT NỐI", 27,
            0.32f, 0.43f, 0.68f, 0.55f, JoinRoom);
        connectButton = connect.GetComponent<Button>();

        joinStatusText = CreateText("Trạng thái", joinPage.transform, "", 21,
            TextAnchor.UpperCenter, new Color(0.85f, 0.90f, 0.80f));
        SetAnchors(joinStatusText.rectTransform, 0.06f, 0.16f, 0.94f, 0.41f);

        CreateButton("Quay lại", joinPage.transform, "QUAY LẠI", 22,
            0.36f, 0.07f, 0.64f, 0.15f, BackToHome);
    }

    private void ShowNotice(string message)
    {
        if (noticeText != null) noticeText.text = message;
    }

    #endregion

    #region Hàm dựng UI dùng chung

    private GameObject CreateGroup(string name, Transform parent)
    {
        GameObject group = new GameObject(name, typeof(RectTransform));
        group.transform.SetParent(parent, false);
        Stretch(group.GetComponent<RectTransform>());
        return group;
    }

    private GameObject CreatePanel(string name, Transform parent,
        float xMin, float yMin, float xMax, float yMax, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        SetAnchors(panel.GetComponent<RectTransform>(), xMin, yMin, xMax, yMax);
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    private GameObject CreateButton(string name, Transform parent, string label, int fontSize,
        float xMin, float yMin, float xMax, float yMax, UnityEngine.Events.UnityAction action)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        SetAnchors(go.GetComponent<RectTransform>(), xMin, yMin, xMax, yMax);

        Image image = go.GetComponent<Image>();
        image.sprite = buttonSprite;
        image.type = Image.Type.Sliced;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);

        Text text = CreateText("Chữ", go.transform, label, fontSize, TextAnchor.MiddleCenter, Color.white);
        Stretch(text.rectTransform);
        text.raycastTarget = false;

        return go;
    }

    private InputField CreateInputField(string name, Transform parent,
        float xMin, float yMin, float xMax, float yMax, string placeholder)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(InputField));
        go.transform.SetParent(parent, false);
        SetAnchors(go.GetComponent<RectTransform>(), xMin, yMin, xMax, yMax);
        go.GetComponent<Image>().color = new Color(0.16f, 0.20f, 0.12f, 1f);

        Text valueText = CreateText("Giá trị", go.transform, "", 24, TextAnchor.MiddleCenter, Color.white);
        SetAnchors(valueText.rectTransform, 0.04f, 0.05f, 0.96f, 0.95f);
        valueText.resizeTextForBestFit = false;
        valueText.supportRichText = false;

        Text placeholderText = CreateText("Gợi ý", go.transform, placeholder, 24,
            TextAnchor.MiddleCenter, new Color(0.65f, 0.68f, 0.60f, 0.8f));
        SetAnchors(placeholderText.rectTransform, 0.04f, 0.05f, 0.96f, 0.95f);
        placeholderText.resizeTextForBestFit = false;

        InputField field = go.GetComponent<InputField>();
        field.textComponent = valueText;
        field.placeholder = placeholderText;
        field.lineType = InputField.LineType.SingleLine;
        field.characterLimit = 40;
        return field;
    }

    private Text CreateText(string name, Transform parent, string value, int size,
        TextAnchor alignment, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        Text text = go.GetComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 12;
        text.resizeTextMaxSize = size;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        SetAnchors(rect, 0f, 0f, 1f, 1f);
    }

    private static void SetAnchors(RectTransform rect, float xMin, float yMin, float xMax, float yMax)
    {
        rect.anchorMin = new Vector2(xMin, yMin);
        rect.anchorMax = new Vector2(xMax, yMax);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    #endregion
}
