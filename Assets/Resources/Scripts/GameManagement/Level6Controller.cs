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
            zombieInitPosY = new List<float> { -2.07f, -1.18f, -0.29f, 0.60f, 1.50f },
            plantGridPosX = new List<float>
            {
                -2.35f, -1.46f, -0.57f, 0.32f, 1.21f, 2.10f, 3.00f, 3.89f, 4.78f
            },
            plantGridPosY = new List<float> { -2.07f, -1.18f, -0.29f, 0.60f, 1.50f },
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
