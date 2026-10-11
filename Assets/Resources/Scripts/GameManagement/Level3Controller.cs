using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Level3Controller : LevelController
{

    public override void init()
    {
        GameManagement.levelData = new LevelData()
        {
            level = 3,   //Số thứ tự màn
            levelName = "Vùng Đất Bất Tử",   //Tên màn

            mapSuffix = "_Night_Bone", //Hậu tố ảnh bản đồ
            rowCount = 5,       //Tổng cộng bao nhiêu hàng
            landRowCount = 5,   //Bao nhiêu hàng đất liền
            isDay = false,       //Có phải ban ngày không
            plantingManagementSuffix = "_OriginalLawn",   //Hậu tố component quản lý trồng cây tương ứng
            backgroundSuffix = "_Night_Bone",   //Hậu tố nhạc nền tương ứng

            //Vị trí trục Y ban đầu của zombie từng hàng
            zombieInitPosY = new List<float> { -2.3f, -1.25f, -0.35f, 0.7f, 1.7f },

            //Dãy thẻ cây của màn này
            plantCards = new List<string>
            {
                "SunFlower",
                "PeaShooter",
                "WallNut",
                "Squash"
            }
        };
    }

    public override void activate()
    {
        Invoke("createFirstGhost", 45f);
    }

    private void createFirstGhost()
    {
        GameObject.Find("Zombie Management").GetComponent<ZombieManagement>().createGhost();
    }
}
