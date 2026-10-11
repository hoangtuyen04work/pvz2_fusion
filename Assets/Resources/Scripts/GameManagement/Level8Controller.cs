using System.Collections.Generic;
using UnityEngine;

// Màn 9: người chơi hoàn thành các mạch bằng cách trồng cây lên những nút phát sáng.
public class Level8Controller : LevelController
{
    private EnergyCircuitTemple circuitTemple;

    public override void init()
    {
        GameManagement.levelData = new LevelData
        {
            level = 8,
            levelName = "Đền Mạch Năng Lượng",
            mapResourcePath = "Sprites/Map9_Art/background/map9",
            backgroundWorldSize = new Vector2(14f, 6f),
            animateBackground = false,
            rowCount = 5,
            landRowCount = 5,
            isDay = true,
            plantingManagementSuffix = "_OriginalLawn",
            backgroundSuffix = "_Night_Bone",
            // Ảnh map9 có phần kiến trúc lớn phía trên. Giữ toàn bộ tâm hàng trong
            // vùng cỏ (xấp xỉ pixel y=267..650 trên ảnh gốc 1916x821).
            zombieInitPosY = new List<float> { -1.75f, -1.05f, -0.35f, 0.35f, 1.05f },
            plantGridPosX = new List<float>
            {
                -2.33f, -1.39f, -0.45f, 0.50f, 1.44f, 2.39f, 3.33f, 4.27f, 5.21f
            },
            plantGridPosY = new List<float> { -1.75f, -1.05f, -0.35f, 0.35f, 1.05f },
            plantCards = new List<string>
            {
                "SunFlower", "PeaShooter", "WallNut", "RepeaterPea", "CherryBomb", "PuffShroom"
            },
            initialSun = 200,
            skipIntro = true
        };

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            Vector3 position = mainCamera.transform.position;
            position.x = (GameManagement.levelData.plantGridPosX[0]
                + GameManagement.levelData.plantGridPosX[GameManagement.levelData.plantGridPosX.Count - 1]) * 0.5f;
            mainCamera.transform.position = position;
        }

        circuitTemple = gameObject.AddComponent<EnergyCircuitTemple>();
    }

    public override void activate()
    {
        SunManagement sun = Object.FindAnyObjectByType<SunManagement>();
        if (sun != null) sun.setDropInterval(6f, 9f);
        if (circuitTemple != null) circuitTemple.BeginGameplay();
    }
}
