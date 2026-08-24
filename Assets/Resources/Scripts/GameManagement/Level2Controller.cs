using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Level2Controller : LevelController
{
    protected override void Start()
    {
        GameObject.Find("Zombie Management").GetComponent<ZombieManagement>()
            .generateFunc = "generateZombies_level2";
    }

    public override void init()
    {
        GameManagement.levelData = new LevelData()
        {
            level = 2,   //Số thứ tự màn
            levelName = "Thầy Luyện Xác",   //Tên màn

            mapSuffix = "_Night_Wall", //Hậu tố ảnh bản đồ
            rowCount = 5,       //Tổng cộng bao nhiêu hàng
            landRowCount = 5,   //Bao nhiêu hàng đất liền
            isDay = false,       //Có phải ban ngày không
            plantingManagementSuffix = "_Wall",   //Hậu tố component quản lý trồng cây tương ứng
            backgroundSuffix = "_Night_Wall",   //Hậu tố nhạc nền tương ứng

            //Vị trí trục Y ban đầu của zombie từng hàng
            zombieInitPosY = new List<float> { -2.45f, -1.5f, -0.5f, 0.55f, 1.55f },

            //Dãy thẻ cây của màn này
            plantCards = new List<string>
            {
                "SunFlower",
                "PeaShooter",
                "WallNut"
            }
        };
    }
}
