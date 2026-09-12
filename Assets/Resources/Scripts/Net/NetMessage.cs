using System;
using UnityEngine;

/// <summary>
/// Tên các loại gói tin. Dùng hằng chuỗi cho dễ đọc log khi gỡ lỗi.
/// </summary>
public static class NetMsg
{
    //Bắt tay và sảnh chờ
    public const string Hello = "hello";           //Khách -> Chủ: xin vào phòng
    public const string Welcome = "welcome";       //Chủ -> Khách: chấp nhận hoặc từ chối
    public const string Lobby = "lobby";           //Chủ -> Khách: cập nhật màn chơi đang chọn
    public const string Start = "start";           //Chủ -> Khách: vào màn
    public const string Ping = "ping";
    public const string Pong = "pong";
    public const string Bye = "bye";               //Chủ động rời phòng

    //Trồng cây
    public const string PlantReq = "plantReq";     //Khách -> Chủ: xin trồng cây
    public const string PlantAck = "plant";        //Chủ -> Khách: cây đã được trồng
    public const string ShovelReq = "shovelReq";   //Khách -> Chủ: xin đào cây
    public const string PlantRemove = "plantOut";  //Chủ -> Khách: cây biến mất

    //Nắng
    public const string SunSpawn = "sunSpawn";     //Chủ -> Khách: sinh một mặt trời
    public const string SunPickReq = "sunPickReq"; //Khách -> Chủ: xin nhặt mặt trời
    public const string SunPicked = "sunPicked";   //Chủ -> Khách: mặt trời đã bị nhặt
    public const string SunSet = "sunSet";         //Chủ -> Khách: đồng bộ tổng số nắng
    public const string CardCool = "cardCool";     //Chủ -> Khách: thẻ cây vào hồi chiêu

    //Zombie
    public const string ZombieSpawn = "zSpawn";    //Chủ -> Khách: sinh một zombie
    public const string ZombieBatch = "zBatch";    //Chủ -> Khách: đồng bộ vị trí và máu hàng loạt
    public const string ZombieDie = "zDie";        //Chủ -> Khách: zombie chết

    //Đối kháng
    public const string ZombieCmdReq = "zCmd";     //Khách (phe zombie) -> Chủ: xin thả zombie
    public const string BrainSet = "brainSet";     //Chủ -> Khách: đồng bộ số não
    public const string CmdReply = "cmdReply";     //Chủ -> Khách: chấp nhận hay từ chối lệnh thả

    //Kết thúc
    public const string GameEnd = "end";           //Chủ -> Khách: kết thúc màn
    public const string Caption = "caption";       //Chủ -> Khách: hiện chữ "Một đợt lớn zombie"
}

/// <summary>
/// Một gói tin. Cố ý dùng vài trường dùng chung thay vì tạo hàng chục lớp riêng,
/// vì JsonUtility của Unity không hỗ trợ đa hình.
/// </summary>
[Serializable]
public class NetMessage
{
    public string t;   //Loại gói tin
    public string s;   //Chuỗi thứ nhất (tên ô đất, tên zombie, chuỗi gộp...)
    public string u;   //Chuỗi thứ hai (tên cây, lý do...)

    public int i;      //Số nguyên thứ nhất (id, tổng nắng, phiên bản...)
    public int j;      //Số nguyên thứ hai (hàng, chế độ...)
    public int k;      //Số nguyên thứ ba (thời gian ngủ tính bằng mili giây...)
    public int p;      //Id của đối tượng đứng trước, dùng cho đội zombie xếp hàng

    public float x;
    public float y;
    public float z;

    public bool b;

    public NetMessage() { }

    public NetMessage(string type)
    {
        t = type;
    }

    #region Hàm dựng nhanh

    public static NetMessage Of(string type)
    {
        return new NetMessage(type);
    }

    public NetMessage Str(string first)
    {
        s = first;
        return this;
    }

    public NetMessage Str(string first, string second)
    {
        s = first;
        u = second;
        return this;
    }

    public NetMessage Int(int first)
    {
        i = first;
        return this;
    }

    public NetMessage Int(int first, int second)
    {
        i = first;
        j = second;
        return this;
    }

    public NetMessage Int(int first, int second, int third)
    {
        i = first;
        j = second;
        k = third;
        return this;
    }

    public NetMessage Pos(float px, float py)
    {
        x = px;
        y = py;
        return this;
    }

    public NetMessage Flt(float fx, float fy, float fz)
    {
        x = fx;
        y = fy;
        z = fz;
        return this;
    }

    public NetMessage Bool(bool value)
    {
        b = value;
        return this;
    }

    #endregion

    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }

    /// <summary>Đọc gói tin từ một dòng JSON. Trả về null nếu dòng hỏng.</summary>
    public static NetMessage FromJson(string line)
    {
        if (string.IsNullOrEmpty(line)) return null;
        try
        {
            NetMessage message = JsonUtility.FromJson<NetMessage>(line);
            if (message == null || string.IsNullOrEmpty(message.t)) return null;
            return message;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public override string ToString()
    {
        return t + "(" + s + "," + u + "," + i + "," + j + ")";
    }
}
