using System.Collections.Generic;
using UnityEngine;

// Màn 8: bãi trồng ven biển thay đổi theo chu kỳ thủy triều.
public class Level7Controller : LevelController
{
    private ParadiseTideMap tideMap;

    public override void init()
    {
        GameManagement.levelData = new LevelData
        {
            level = 7,
            levelName = "Đảo Thiên Đường",
            mapResourcePath = "Sprites/BackGround/map8",
            backgroundWorldSize = new Vector2(14f, 6f),
            animateBackground = false,
            rowCount = 5,
            landRowCount = 5,
            isDay = true,
            plantingManagementSuffix = "_OriginalLawn",
            backgroundSuffix = "_Roco_PetPark",
            zombieInitPosY = new List<float> { -1.82f, -0.94f, -0.06f, 0.82f, 1.70f },
            plantGridPosX = new List<float>
            {
                -2.33f, -1.39f, -0.45f, 0.50f, 1.44f, 2.39f, 3.33f, 4.27f, 5.21f
            },
            plantGridPosY = new List<float> { -1.82f, -0.94f, -0.06f, 0.82f, 1.70f },
            plantCards = new List<string>
            {
                "SunFlower", "PeaShooter", "WallNut", "Squash", "TorchWood", "SunNut"
            },
            initialSun = 450,
            skipIntro = true
        };

        // Tâm chín cột là 1.44, không phải gốc thế giới. Căn camera theo tâm lưới
        // giúp thấy đủ cột ngoài cùng và tạo khoảng thở cân đối ở hai bên sân trồng.
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Vector3 cameraPosition = mainCamera.transform.position;
            cameraPosition.x = (GameManagement.levelData.plantGridPosX[0]
                + GameManagement.levelData.plantGridPosX[GameManagement.levelData.plantGridPosX.Count - 1]) * 0.5f;
            mainCamera.transform.position = cameraPosition;
        }

        tideMap = gameObject.AddComponent<ParadiseTideMap>();
    }

    public override void activate()
    {
        SunManagement sun = Object.FindAnyObjectByType<SunManagement>();
        if (sun != null) sun.setDropInterval(3f, 5f);
        if (tideMap != null) tideMap.BeginGameplay();
    }
}
