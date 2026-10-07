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
            zombieInitPosY = new List<float> { -1.97f, -1.17f, -0.36f, 0.44f, 1.25f },
            plantGridPosX = new List<float>
            {
                -2.03f, -1.19f, -0.35f, 0.49f, 1.33f, 2.17f, 3.01f, 3.85f, 4.69f
            },
            plantGridPosY = new List<float> { -1.97f, -1.17f, -0.36f, 0.44f, 1.25f },

            //Dãy thẻ cây của màn này
            plantCards = new List<string>
            {
                "SunFlower",
                "PeaShooter"
            }
        };
    }
}
