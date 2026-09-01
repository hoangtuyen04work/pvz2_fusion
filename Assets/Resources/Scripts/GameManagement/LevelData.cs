using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelData
{
    public int level;  //Số thứ tự màn
    public string levelName;   //Tên màn

    public string mapSuffix;  //Hậu tố ảnh bản đồ
    public int rowCount;   //Tổng cộng bao nhiêu hàng
    public int landRowCount;   //Bao nhiêu hàng đất liền
    public bool isDay;   //Có phải ban ngày không
    public string plantingManagementSuffix;   //Hậu tố component quản lý trồng cây tương ứng
    public string backgroundSuffix;   //Hậu tố nhạc nền tương ứng

    public List<float> zombieInitPosY;   //Vị trí trục Y ban đầu của zombie từng hàng

    public List<string> plantCards;   //Dãy thẻ cây của màn này
    public int initialSun = 50;       //Lượng nắng khi bắt đầu màn
    public bool skipIntro = false;    //Màn chơi không có hội thoại mở đầu
}
