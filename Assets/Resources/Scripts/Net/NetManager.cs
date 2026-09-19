using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Cầu nối giữa socket (chạy ở luồng nền) và trò chơi (chạy ở luồng chính).
/// Sống xuyên suốt mọi scene, mỗi khung hình bơm hết gói tin đã nhận ra cho người đăng ký.
/// </summary>
public class NetManager : MonoBehaviour
{
    private static NetManager instance;

    /// <summary>Lấy đối tượng quản lý, tự tạo nếu chưa có.</summary>
    public static NetManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject holder = new GameObject("Net Manager");
                instance = holder.AddComponent<NetManager>();
                DontDestroyOnLoad(holder);
            }
            return instance;
        }
    }

    /// <summary>Có đối tượng quản lý đang sống không (không tự tạo mới).</summary>
    public static bool Exists { get { return instance != null; } }

    private readonly NetTransport transport = new NetTransport();

    /// <summary>Nhận mọi gói tin đã tới. Đăng ký ở OnEnable, huỷ ở OnDisable.</summary>
    public event Action<NetMessage> OnMessage;

    /// <summary>Bắn khi hai máy vừa nối được với nhau.</summary>
    public event Action OnPeerConnected;

    /// <summary>Bắn khi mất kết nối, kèm lý do để hiện lên màn hình.</summary>
    public event Action<string> OnPeerLost;

    private NetStatus lastStatus = NetStatus.Idle;
    private float nextPingTime;
    private float lastPongTime;

    //Gói tin tới trong lúc chưa ai đăng ký nhận (đang chuyển cảnh) được giữ lại ở đây
    private readonly System.Collections.Generic.List<NetMessage> pending =
        new System.Collections.Generic.List<NetMessage>();

    public NetStatus Status { get { return transport.Status; } }
    public string LastError { get { return transport.LastError; } }
    public bool IsConnected { get { return transport.IsConnected; } }

    /// <summary>Độ trễ khứ hồi tính bằng mili giây, để hiện lên HUD.</summary>
    public int PingMs { get; private set; }

    /// <summary>Lý do mất kết nối gần nhất, dùng cho màn hình kết thúc.</summary>
    public string DisconnectReason { get; private set; }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    #region Điều khiển kết nối

    public bool StartHost(int port)
    {
        DisconnectReason = null;
        lastStatus = NetStatus.Idle;
        return transport.StartHost(port);
    }

    public void StartClient(string ip, int port)
    {
        DisconnectReason = null;
        lastStatus = NetStatus.Idle;
        transport.StartClient(ip, port);
    }

    /// <summary>Chủ động rời phòng: báo cho đối phương rồi đóng socket.</summary>
    public void Leave(string reason)
    {
        if (transport.IsConnected)
        {
            transport.Send(NetMessage.Of(NetMsg.Bye).Str(reason));
            //Cho luồng ghi kịp đẩy gói tin cuối đi
            System.Threading.Thread.Sleep(30);
        }
        transport.Stop();
        lastStatus = NetStatus.Idle;
        NetSession.Reset();
    }

    public void Send(NetMessage message)
    {
        transport.Send(message);
    }

    /// <summary>
    /// Lấy các gói tin đã tới trong lúc chưa có ai đăng ký nhận.
    /// Màn chơi gọi hàm này ngay sau khi đăng ký, để không bỏ sót gói tin lúc đang chuyển cảnh.
    /// </summary>
    public System.Collections.Generic.List<NetMessage> TakePending()
    {
        System.Collections.Generic.List<NetMessage> copy =
            new System.Collections.Generic.List<NetMessage>(pending);
        pending.Clear();
        return copy;
    }

    #endregion

    private void Update()
    {
        //Theo dõi đổi trạng thái để bắn sự kiện đúng một lần
        NetStatus current = transport.Status;
        if (current != lastStatus)
        {
            NetStatus previous = lastStatus;
            lastStatus = current;

            if (current == NetStatus.Connected)
            {
                PingMs = 0;
                lastPongTime = Time.unscaledTime;
                nextPingTime = Time.unscaledTime + 2f;
                if (OnPeerConnected != null) OnPeerConnected();
            }
            else if ((current == NetStatus.Closed || current == NetStatus.Failed)
                     && previous == NetStatus.Connected)
            {
                DisconnectReason = transport.LastError;
                if (OnPeerLost != null) OnPeerLost(transport.LastError);
            }
        }

        //Bơm gói tin ra cho phần còn lại của trò chơi
        NetMessage message;
        int guard = 0;
        while (transport.TryReceive(out message) && guard < 512)
        {
            guard++;
            if (HandleInternal(message)) continue;

            if (OnMessage != null) OnMessage(message);
            else if (pending.Count < 512) pending.Add(message);
        }

        SendKeepAlive();
    }

    /// <summary>Xử lý các gói tin thuộc về tầng mạng, trả về true nếu đã nuốt gói tin.</summary>
    private bool HandleInternal(NetMessage message)
    {
        if (message.t == NetMsg.Ping)
        {
            transport.Send(NetMessage.Of(NetMsg.Pong).Int(message.i));
            return true;
        }

        if (message.t == NetMsg.Pong)
        {
            PingMs = Mathf.RoundToInt((Time.unscaledTime - message.x) * 1000f);
            lastPongTime = Time.unscaledTime;
            return true;
        }

        if (message.t == NetMsg.Bye)
        {
            DisconnectReason = string.IsNullOrEmpty(message.s)
                ? "Đối phương đã rời phòng"
                : message.s;
            transport.Stop();
            lastStatus = NetStatus.Idle;
            if (OnPeerLost != null) OnPeerLost(DisconnectReason);
            return true;
        }

        return false;
    }

    private void SendKeepAlive()
    {
        if (!transport.IsConnected) return;
        if (Time.unscaledTime < nextPingTime) return;

        nextPingTime = Time.unscaledTime + 2f;
        NetMessage ping = NetMessage.Of(NetMsg.Ping);
        ping.x = Time.unscaledTime;   //Đối phương gửi lại nguyên xi để tính độ trễ
        transport.Send(ping);
    }

    private void OnApplicationQuit()
    {
        transport.Stop();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            transport.Stop();
            instance = null;
        }
    }

    /// <summary>Gửi gói tin nếu đang nối mạng. Viết gọn cho các script gameplay.</summary>
    public static void Broadcast(NetMessage message)
    {
        if (!NetSession.IsOnline || !Exists) return;
        instance.Send(message);
    }
}

/// <summary>
/// Bảo đảm về Main Menu là mọi dấu vết phiên chơi mạng đều được dọn sạch,
/// tránh trường hợp bấm thoát giữa màn rồi vào lại vẫn tưởng đang chơi mạng.
/// </summary>
public static class NetSessionGuard
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
        if (!NetSession.IsOnline) return;

        //Rời màn chơi mạng để về menu thì cắt kết nối luôn
        if (NetManager.Exists) NetManager.Instance.Leave("Đối phương đã thoát về menu");
        else NetSession.Reset();
    }
}
