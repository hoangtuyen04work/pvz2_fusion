using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

/// <summary>
/// Hỗ trợ tìm kiếm phòng qua UDP broadcast và sinh mã phòng 6 số dựa trên IP của máy chủ.
/// Giúp người chơi chỉ cần đọc mã phòng 6 số mà không bị lộ địa chỉ IP trên giao diện.
/// </summary>
public static class NetDiscovery
{
    public const int DiscoveryPort = 7779;
    private const string QueryPrefix = "PVZ_ROOM_QUERY:";
    private const string AckPrefix = "PVZ_ROOM_ACK:";

    private static Thread hostThread;
    private static volatile bool hostRunning;
    private static string currentHostCode;
    private static string currentHostIp;

    /// <summary>
    /// Chuyển đổi IPv4 thành mã phòng 6 chữ số.
    /// </summary>
    public static string IpToRoomCode(string ip)
    {
        if (string.IsNullOrEmpty(ip)) return "000000";
        try
        {
            string[] parts = ip.Trim().Split('.');
            if (parts.Length != 4) return "000000";
            uint b0 = uint.Parse(parts[0]);
            uint b1 = uint.Parse(parts[1]);
            uint b2 = uint.Parse(parts[2]);
            uint b3 = uint.Parse(parts[3]);
            uint val = (b0 << 24) | (b1 << 16) | (b2 << 8) | b3;
            uint codeNum = (val ^ 0x5A3C96E7) % 1000000;
            return codeNum.ToString("D6");
        }
        catch
        {
            return "000000";
        }
    }

    /// <summary>
    /// Bắt đầu lắng nghe yêu cầu tìm phòng qua UDP broadcast khi làm Host.
    /// </summary>
    public static void StartHostBeacon(string ip, string roomCode)
    {
        StopHostBeacon();
        currentHostIp = ip;
        currentHostCode = roomCode;
        hostRunning = true;

        hostThread = new Thread(HostBeaconLoop);
        hostThread.IsBackground = true;
        hostThread.Start();
    }

    public static void StopHostBeacon()
    {
        hostRunning = false;
        currentHostCode = null;
        currentHostIp = null;
        if (hostThread != null)
        {
            try { hostThread.Abort(); } catch { }
            hostThread = null;
        }
    }

    private static void HostBeaconLoop()
    {
        UdpClient listener = null;
        try
        {
            listener = new UdpClient();
            listener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            listener.Client.Bind(new IPEndPoint(IPAddress.Any, DiscoveryPort));

            byte[] buffer = new byte[1024];
            while (hostRunning)
            {
                IPEndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = listener.Receive(ref remoteEp);
                if (data == null || data.Length == 0) continue;

                string msg = Encoding.UTF8.GetString(data);
                if (msg.StartsWith(QueryPrefix))
                {
                    string reqCode = msg.Substring(QueryPrefix.Length).Trim();
                    if (!string.IsNullOrEmpty(currentHostCode) && reqCode == currentHostCode)
                    {
                        string reply = AckPrefix + currentHostIp + ":" + NetSession.DefaultPort;
                        byte[] replyBytes = Encoding.UTF8.GetBytes(reply);
                        listener.Send(replyBytes, replyBytes.Length, remoteEp);
                    }
                }
            }
        }
        catch
        {
            // Kết thúc luồng an toàn khi tắt beacon
        }
        finally
        {
            if (listener != null)
            {
                try { listener.Close(); } catch { }
            }
        }
    }

    /// <summary>
    /// Tìm địa chỉ IP của host tương ứng với mã phòng 6 số bằng cách gửi UDP Broadcast.
    /// Nếu tìm thấy trả về IP thật, nếu không tìm thấy sau timeout trả về null.
    /// </summary>
    public static string ResolveRoomCode(string code, int timeoutMs = 2500)
    {
        if (string.IsNullOrEmpty(code)) return null;
        string targetCode = code.Trim();

        UdpClient client = null;
        try
        {
            client = new UdpClient();
            client.EnableBroadcast = true;
            client.Client.ReceiveTimeout = timeoutMs;

            string query = QueryPrefix + targetCode;
            byte[] bytes = Encoding.UTF8.GetBytes(query);
            IPEndPoint broadcastEp = new IPEndPoint(IPAddress.Broadcast, DiscoveryPort);
            client.Send(bytes, bytes.Length, broadcastEp);

            DateTime deadline = DateTime.Now.AddMilliseconds(timeoutMs);
            while (DateTime.Now < deadline)
            {
                IPEndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);
                byte[] resp = client.Receive(ref remoteEp);
                if (resp != null && resp.Length > 0)
                {
                    string respStr = Encoding.UTF8.GetString(resp);
                    if (respStr.StartsWith(AckPrefix))
                    {
                        string target = respStr.Substring(AckPrefix.Length).Trim();
                        string ip = target.Split(':')[0];
                        return ip;
                    }
                }
            }
        }
        catch
        {
            // Hết giờ chờ hoặc không tìm thấy
        }
        finally
        {
            if (client != null)
            {
                try { client.Close(); } catch { }
            }
        }

        return null;
    }

    private static readonly System.Collections.Generic.Queue<Action> mainThreadQueue = new System.Collections.Generic.Queue<Action>();

    /// <summary>
    /// Được gọi mỗi frame ở luồng chính (NetLobbyUI) để thực thi các callback xong xuôi từ luồng nền.
    /// </summary>
    public static void Update()
    {
        lock (mainThreadQueue)
        {
            while (mainThreadQueue.Count > 0)
            {
                try
                {
                    mainThreadQueue.Dequeue()?.Invoke();
                }
                catch { }
            }
        }
    }

    /// <summary>
    /// Tìm IP phòng bất đồng bộ trên luồng nền để tránh khựng giao diện Unity.
    /// </summary>
    public static void ResolveRoomCodeAsync(string code, Action<string> onCompleted, int timeoutMs = 2500)
    {
        Thread thread = new Thread(() =>
        {
            string ip = ResolveRoomCode(code, timeoutMs);
            lock (mainThreadQueue)
            {
                mainThreadQueue.Enqueue(() => onCompleted?.Invoke(ip));
            }
        });
        thread.IsBackground = true;
        thread.Start();
    }
}
