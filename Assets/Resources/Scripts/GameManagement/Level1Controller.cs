using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Level1Controller : LevelController
{
    public override void init()
    {
        GameManagement.levelData = new LevelData()
        {
            level = 1,   //Số thứ tự màn
            levelName = "Hành Trình Mới",   //Tên màn

            mapResourcePath = "Sprites/BackGround/BG_sanvuon",
            backgroundWorldSize = new Vector2(14f, 6f),
            animateBackground = false,
            rowCount = 5,       //Tổng cộng bao nhiêu hàng
            landRowCount = 5,   //Bao nhiêu hàng đất liền
            isDay = true,       //Có phải ban ngày không
            plantingManagementSuffix = "_OriginalLawn",   //Hậu tố component quản lý trồng cây tương ứng
            backgroundSuffix = "_Day",   //Hậu tố nhạc nền tương ứng

            //Vị trí trục Y ban đầu của zombie từng hàng
            zombieInitPosY = new List<float> { -1.79f, -1.02f, -0.27f, 0.47f, 1.21f },
            plantGridPosX = new List<float>
            {
                -1.81f, -1.00f, -0.20f, 0.60f, 1.41f, 2.21f, 3.02f, 3.82f, 4.62f
            },
            plantGridPosY = new List<float> { -1.79f, -1.02f, -0.27f, 0.47f, 1.21f },

            //Dãy thẻ cây của màn này
            plantCards = new List<string>
            {
                "SunFlower",
                "PeaShooter"
            }
        };
    }
}
