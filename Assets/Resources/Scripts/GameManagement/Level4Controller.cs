using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Level4Controller : LevelController
{
    protected override void Start()
    {
        //Tải hiệu ứng tuyết rơi
        Instantiate(Resources.Load<Object>("Prefabs/Effects/Weather/Snow"),
                    new Vector3(0, 4, 0),
                    Quaternion.Euler(-90, 0, 0));
    }

    public override void init()
    {
        GameManagement.levelData = new LevelData()
        {
            level = 4,   //Số thứ tự màn
            levelName = "Sông Băng Địa Cực",   //Tên màn

            mapSuffix = "_Ice", //Hậu tố ảnh bản đồ
            rowCount = 5,       //Tổng cộng bao nhiêu hàng
            landRowCount = 5,   //Bao nhiêu hàng đất liền
            isDay = true,       //Có phải ban ngày không
            plantingManagementSuffix = "_Ice",   //Hậu tố component quản lý trồng cây tương ứng
            backgroundSuffix = "_Ice",   //Hậu tố nhạc nền tương ứng

            //Vị trí trục Y ban đầu của zombie từng hàng
            zombieInitPosY = new List<float> { -2.168f, -1.24f, -0.2f, 0.85f, 1.82f },

            //Dãy thẻ cây của màn này
            plantCards = new List<string>
            {
                "SunFlower",
                "PeaShooter",
                "WallNut",
                "Squash",
                "TorchWood",
                "SnowKing"
            }
        };
    }

    public override void activate()
    {
        GameObject.Find("Planting Management").AddComponent<Winter>();
    }
}
