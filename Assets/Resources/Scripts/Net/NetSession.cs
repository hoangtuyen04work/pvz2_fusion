using UnityEngine;

/// <summary>
/// Ba chế độ chơi của trò chơi.
/// </summary>
public enum NetGameMode
{
    Single,   //Chơi một mình, y hệt bản gốc
    Coop,     //Hai người cùng phe trồng cây
    Pvp       //Một người trồng cây, một người điều khiển zombie
}

/// <summary>
/// Vai trò của máy này trong phiên chơi mạng.
/// </summary>
public enum NetRole
{
    Offline,  //Không nối mạng
    Host,     //Máy chủ, chạy toàn bộ logic trò chơi
    Client    //Máy khách, chỉ gửi thao tác và vẽ lại kết quả
}

/// <summary>
/// Trạng thái phiên chơi hiện tại, dùng chung cho cả Main Menu lẫn GameScene.
/// Đây là nơi duy nhất mọi script khác hỏi "tôi có đang chơi mạng không, tôi là ai".
/// </summary>
public static class NetSession
{
    public const int ProtocolVersion = 1;   //Hai máy khác phiên bản giao thức thì từ chối nối
    public const int DefaultPort = 7777;    //Cổng mặc định

    public static NetGameMode Mode = NetGameMode.Single;
    public static NetRole Role = NetRole.Offline;

    public static int Level = -1;              //Màn chơi do host chọn
    public static string LocalName = "Người chơi";
    public static string PeerName = "Đối phương";

    /// <summary>Có đang trong một phiên chơi mạng không.</summary>
    public static bool IsOnline { get { return Role != NetRole.Offline; } }

    public static bool IsHost { get { return Role == NetRole.Host; } }
    public static bool IsClient { get { return Role == NetRole.Client; } }

    /// <summary>
    /// Máy này có quyền quyết định trạng thái trò chơi không (máu, số nắng, sống chết).
    /// Chơi đơn và host đều có quyền; client thì không, client chỉ vẽ lại.
    /// </summary>
    public static bool IsAuthority { get { return Role != NetRole.Client; } }

    /// <summary>Máy này có được trồng cây không.</summary>
    public static bool ControlsPlants
    {
        get { return Mode != NetGameMode.Pvp || Role != NetRole.Client; }
    }

    /// <summary>Máy này có điều khiển zombie không (chỉ đúng ở phía zombie của PvP).</summary>
    public static bool ControlsZombies
    {
        get { return Mode == NetGameMode.Pvp && Role == NetRole.Client; }
    }

    /// <summary>Đợt zombie theo trục thời gian trong file JSON có được chạy không.</summary>
    public static bool UseZombieTimeline { get { return Mode != NetGameMode.Pvp; } }

    /// <summary>Chơi mạng thì bỏ hội thoại mở màn để hai máy không lệch nhịp.</summary>
    public static bool SkipIntroDialog { get { return IsOnline; } }

    /// <summary>Tên chế độ để hiển thị lên giao diện.</summary>
    public static string ModeLabel
    {
        get
        {
            if (Mode == NetGameMode.Coop) return "Đồng đội";
            if (Mode == NetGameMode.Pvp) return "Đối kháng";
            return "Chơi đơn";
        }
    }

    /// <summary>Vai trò để hiển thị lên giao diện.</summary>
    public static string RoleLabel
    {
        get
        {
            if (Role == NetRole.Offline) return "";
            if (Mode == NetGameMode.Pvp)
                return Role == NetRole.Host ? "Phe Cây" : "Phe Zombie";
            return Role == NetRole.Host ? "Chủ phòng" : "Khách";
        }
    }

    /// <summary>Trả mọi thứ về chơi đơn. Gọi khi thoát phòng hoặc mất kết nối.</summary>
    public static void Reset()
    {
        Mode = NetGameMode.Single;
        Role = NetRole.Offline;
        Level = -1;
        PeerName = "Đối phương";
    }
}
