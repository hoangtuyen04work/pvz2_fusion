using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Level0Controller : LevelController
{
    public override void init()
    {
        GameManagement.levelData = new LevelData()
        {
            level = 0,   //Số thứ tự màn
            levelName = "Hướng Dẫn Tân Thủ",   //Tên màn

            mapSuffix = "_Day", //Hậu tố ảnh bản đồ
            rowCount = 5,       //Tổng cộng bao nhiêu hàng
            landRowCount = 5,   //Bao nhiêu hàng đất liền
            isDay = true,       //Có phải ban ngày không
            plantingManagementSuffix = "_OriginalLawn",   //Hậu tố component quản lý trồng cây tương ứng
            backgroundSuffix = "_Roco_PetPark",   //Hậu tố nhạc nền tương ứng
            initialSun = 100,   //Thêm nắng khởi đầu giúp người chơi sớm trải nghiệm ghép cây

            //Vị trí trục Y ban đầu của zombie từng hàng
            zombieInitPosY = new List<float> { -2.25f, -1.25f, -0.25f, 0.75f, 1.75f },
            plantGridPosX = new List<float>
            {
                -2.60f, -1.75f, -0.90f, -0.05f, 0.80f, 1.65f, 2.50f, 3.35f, 4.20f
            },
            plantGridPosY = new List<float> { -2.25f, -1.25f, -0.25f, 0.75f, 1.75f },

            //Dãy thẻ cây của màn này
            plantCards = new List<string>
            {
                "SunFlower",
                "PeaShooter",
                "WallNut",
                "Squash",
                "TorchWood",
                "MiaoMiao"
            }
        };
    }
}
