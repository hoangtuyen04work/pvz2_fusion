using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelData
{
    public int level;  //Số thứ tự màn
    public string levelName;   //Tên màn

    public string mapSuffix;  //Hậu tố ảnh bản đồ
    public string mapResourcePath; //Đường dẫn Resources đầy đủ cho map tùy biến
    public Vector2 backgroundWorldSize; //Kích thước nền trong world; zero giữ nguyên scale
    public bool animateBackground = true; //Nền có được phép trôi nhẹ hay không
    public int rowCount;   //Tổng cộng bao nhiêu hàng
    public int landRowCount;   //Bao nhiêu hàng đất liền
    public bool isDay;   //Có phải ban ngày không
    public string plantingManagementSuffix;   //Hậu tố component quản lý trồng cây tương ứng
    public string backgroundSuffix;   //Hậu tố nhạc nền tương ứng

    public List<float> zombieInitPosY;   //Vị trí trục Y ban đầu của zombie từng hàng
    public List<float> plantGridPosX; //Tâm các cột trồng cây; rỗng thì dùng prefab gốc
    public List<float> plantGridPosY; //Tâm các hàng, từ hàng 0 ở dưới lên

    public List<string> plantCards;   //Dãy thẻ cây của màn này
    public int initialSun = 50;       //Lượng nắng khi bắt đầu màn
    public bool skipIntro = false;    //Màn chơi không có hội thoại mở đầu
    public bool isTestMode = false;
}
