using System.Collections.Generic;
using UnityEngine;

// Màn sandbox để thử cây và cơ chế fusion, không có phần hướng dẫn.
public class Level5Controller : LevelController
{
    public override void init()
    {
        GameManagement.levelData = new LevelData()
        {
            level = 5,
            levelName = "Sân Thử Nghiệm",
            mapSuffix = "_Day",
            rowCount = 5,
            landRowCount = 5,
            isDay = true,
            plantingManagementSuffix = "_OriginalLawn",
            backgroundSuffix = "_Roco_PetPark",
            zombieInitPosY = new List<float> { -2.3f, -1.25f, -0.35f, 0.7f, 1.7f },
            plantCards = new List<string>
            {
                "SunFlower",
                "PeaShooter",
                "WallNut",
                "Squash",
                "TorchWood",
                "MiaoMiao",
                "SnowKing"
            },
            initialSun = 2000,
            skipIntro = true
        };
    }

    public override void activate()
    {
        SunManagement sun = GameObject.Find("Sun Management").GetComponent<SunManagement>();
        sun.setDropInterval(0.8f, 1.4f);
    }
}
