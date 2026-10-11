using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public enum NetStatus
{
    Idle,        //Chưa làm gì
    Listening,   //Đang mở phòng chờ người vào
    Connecting,  //Đang nối tới phòng
    Connected,   //Đã nối xong
    Failed,      //Nối thất bại
    Closed       //Đã đóng hoặc bị ngắt
}

/// <summary>
/// Lớp socket TCP thô. Một luồng đọc, một luồng ghi, hai hàng đợi an toàn đa luồng.
/// Không đụng gì tới Unity API nên chạy được thoải mái trên luồng nền.
/// </summary>
public class NetTransport
{
    private TcpListener listener;
    private TcpClient socket;
    private StreamReader reader;
    private StreamWriter writer;

    private Thread acceptThread;
    private Thread readThread;
    private Thread writeThread;

    private readonly ConcurrentQueue<NetMessage> inbox = new ConcurrentQueue<NetMessage>();
    private BlockingCollection<string> outbox;

    private volatile bool running;
    private volatile int statusCode = (int)NetStatus.Idle;
    private volatile string lastError = string.Empty;

    public NetStatus Status { get { return (NetStatus)statusCode; } }
    public string LastError { get { return lastError; } }
    public bool IsConnected { get { return statusCode == (int)NetStatus.Connected; } }

    private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

    #region Mở phòng và vào phòng

    /// <summary>Mở phòng, chờ đúng một người vào.</summary>
    public bool StartHost(int port)
    {
        Stop();
        try
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
        }
        catch (Exception e)
        {
            lastError = "Không mở được cổng " + port + ": " + e.Message;
            statusCode = (int)NetStatus.Failed;
            return false;
        }

        running = true;
        statusCode = (int)NetStatus.Listening;

        acceptThread = new Thread(AcceptLoop);
        acceptThread.IsBackground = true;
        acceptThread.Start();
        return true;
    }

    /// <summary>Nối tới một phòng đang mở.</summary>
    public void StartClient(string host, int port)
    {
        Stop();
        running = true;
        statusCode = (int)NetStatus.Connecting;

        string target = string.IsNullOrEmpty(host) ? "127.0.0.1" : host.Trim();

        acceptThread = new Thread(delegate ()
        {
            TcpClient attempt = null;
            try
            {
                attempt = new TcpClient();
                IAsyncResult handle = attempt.BeginConnect(target, port, null, null);
                //Chờ tối đa 8 giây, tránh treo giao diện khi gõ nhầm IP
                if (!handle.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(8)))
                    throw new TimeoutException("Hết thời gian chờ");
                attempt.EndConnect(handle);

                if (!running)
                {
                    attempt.Close();
                    return;
                }

                BindSocket(attempt);
            }
            catch (Exception e)
            {
                if (attempt != null)
                {
                    try { attempt.Close(); } catch (Exception) { }
                }
                lastError = "Không nối được tới " + target + ":" + port + " (" + e.Message + ")";
                statusCode = (int)NetStatus.Failed;
                running = false;
            }
        });
        acceptThread.IsBackground = true;
        acceptThread.Start();
    }

    private void AcceptLoop()
    {
        try
        {
            TcpClient incoming = listener.AcceptTcpClient();
            if (!running)
            {
                incoming.Close();
                return;
            }

            //Chỉ nhận đúng một người, đóng cửa phòng lại ngay
            try { listener.Stop(); } catch (Exception) { }
            listener = null;

            BindSocket(incoming);
        }
        catch (Exception e)
        {
            if (running)
            {
                lastError = "Phòng gặp lỗi: " + e.Message;
                statusCode = (int)NetStatus.Failed;
                running = false;
            }
        }
    }

    /// <summary>Gắn socket đã nối xong vào luồng đọc và luồng ghi.</summary>
    private void BindSocket(TcpClient client)
    {
        socket = client;
        socket.NoDelay = true;   //Gói tin nhỏ, không cần gom, ưu tiên độ trễ thấp

        NetworkStream networkStream = socket.GetStream();
        reader = new StreamReader(networkStream, Utf8NoBom);
        writer = new StreamWriter(networkStream, Utf8NoBom);
        writer.AutoFlush = false;

        outbox = new BlockingCollection<string>(new ConcurrentQueue<string>());

        statusCode = (int)NetStatus.Connected;

        readThread = new Thread(ReadLoop);
        readThread.IsBackground = true;
        readThread.Start();

        writeThread = new Thread(WriteLoop);
        writeThread.IsBackground = true;
        writeThread.Start();
    }

    #endregion

    #region Đọc và ghi

    private void ReadLoop()
    {
        try
        {
            while (running)
            {
                string line = reader.ReadLine();
                if (line == null)   //Đầu kia đã đóng kết nối
                {
                    Drop("Đối phương đã ngắt kết nối");
                    return;
                }

                NetMessage message = NetMessage.FromJson(line);
                if (message != null) inbox.Enqueue(message);
            }
        }
        catch (Exception e)
        {
            if (running) Drop("Mất kết nối: " + e.Message);
        }
    }

    private void WriteLoop()
    {
        try
        {
            foreach (string line in outbox.GetConsumingEnumerable())
            {
                if (!running) return;
                writer.Write(line);
                writer.Write("\n");
                writer.Flush();
            }
        }
        catch (Exception e)
        {
            if (running) Drop("Không gửi được dữ liệu: " + e.Message);
        }
    }

    /// <summary>Đưa gói tin vào hàng đợi gửi. Gọi được từ luồng chính.</summary>
    public void Send(NetMessage message)
    {
        if (message == null) return;
        BlockingCollection<string> queue = outbox;
        if (queue == null || !IsConnected) return;
        try
        {
            queue.Add(message.ToJson());
        }
        catch (Exception)
        {
            //Hàng đợi đã đóng, bỏ qua
        }
    }

    /// <summary>Lấy một gói tin đã nhận. Trả về false khi hết.</summary>
    public bool TryReceive(out NetMessage message)
    {
        return inbox.TryDequeue(out message);
    }

    #endregion

    #region Đóng kết nối

    private void Drop(string reason)
    {
        if (!running) return;
        lastError = reason;
        running = false;
        statusCode = (int)NetStatus.Closed;
        CloseHandles();
    }

    public void Stop()
    {
        running = false;
        CloseHandles();

        acceptThread = null;
        readThread = null;
        writeThread = null;

        NetMessage discard;
        while (inbox.TryDequeue(out discard)) { }

        if (statusCode != (int)NetStatus.Failed) statusCode = (int)NetStatus.Idle;
    }

    private void CloseHandles()
    {
        BlockingCollection<string> queue = outbox;
        outbox = null;
        if (queue != null)
        {
            try { queue.CompleteAdding(); } catch (Exception) { }
        }

        if (listener != null)
        {
            try { listener.Stop(); } catch (Exception) { }
            listener = null;
        }

        if (socket != null)
        {
            try { socket.Close(); } catch (Exception) { }
            socket = null;
        }

        reader = null;
        writer = null;
    }

    #endregion

    /// <summary>
    /// Địa chỉ IPv4 để đọc cho người chơi kia gõ vào.
    /// Máy thường có nhiều card mạng (VMware, VirtualBox, VPN...) nên không thể chọn bừa theo dải số.
    /// Cách chắc ăn là hỏi hệ điều hành xem nó sẽ đi ra ngoài bằng card nào.
    /// </summary>
    public static string GetLocalIPv4()
    {
        try
        {
            //Chỉ là thao tác chọn đường, không hề gửi gói tin nào ra ngoài
            using (Socket probe = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                probe.Connect("8.8.8.8", 65530);
                IPEndPoint local = probe.LocalEndPoint as IPEndPoint;
                if (local != null && !local.Address.ToString().StartsWith("127."))
                    return local.Address.ToString();
            }
        }
        catch (Exception)
        {
            //Máy đang ngắt mạng thì rơi xuống cách dò bên dưới
        }

        List<string> all = GetAllLocalIPv4();
        return all.Count > 0 ? all[0] : "127.0.0.1";
    }

    /// <summary>
    /// Toàn bộ địa chỉ IPv4 của máy, để hiện thêm khi cách dò chính chọn nhầm card ảo.
    /// </summary>
    public static List<string> GetAllLocalIPv4()
    {
        List<string> found = new List<string>();
        try
        {
            IPHostEntry entry = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress address in entry.AddressList)
            {
                if (address.AddressFamily != AddressFamily.InterNetwork) continue;
                string text = address.ToString();
                if (text.StartsWith("127.")) continue;
                if (!found.Contains(text)) found.Add(text);
            }
        }
        catch (Exception)
        {
            //Không dò được thì trả về danh sách rỗng
        }
        return found;
    }
}
