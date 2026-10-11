using System.Collections.Generic;
using UnityEngine;

// Màn 7: sân rừng có hai pha ánh sáng và nhật thực luân phiên.
public class Level6Controller : LevelController
{
    public override void init()
    {
        GameManagement.levelData = new LevelData()
        {
            level = 6,
            levelName = "Rừng Nhật Thực",
            mapResourcePath = "Sprites/BackGround/map7",
            backgroundWorldSize = new Vector2(14f, 6f),
            animateBackground = false,
            rowCount = 5,
            landRowCount = 5,
            isDay = true,
            plantingManagementSuffix = "_OriginalLawn",
            backgroundSuffix = "_Night_Bone",
            zombieInitPosY = new List<float> { -1.87f, -1.07f, -0.27f, 0.54f, 1.34f },
            plantGridPosX = new List<float>
            {
                -2.25f, -1.39f, -0.52f, 0.34f, 1.20f, 2.06f, 2.92f, 3.78f, 4.65f
            },
            plantGridPosY = new List<float> { -1.87f, -1.07f, -0.27f, 0.54f, 1.34f },
            plantCards = new List<string>
            {
                "SunFlower", "PeaShooter", "WallNut", "TorchWood", "PuffShroom", "SunShroom"
            },
            initialSun = 200,
            skipIntro = true
        };
    }

    public override void activate()
    {
        SunManagement sunManagement = Object.FindAnyObjectByType<SunManagement>();
        if (sunManagement != null) sunManagement.setDropInterval(5f, 8f);

        if (GetComponent<EclipseForest>() == null)
            gameObject.AddComponent<EclipseForest>();
    }
}
