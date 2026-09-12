using System.Collections.Generic;
using UnityEngine;

// MÃ n sandbox Ä‘á»ƒ thá»­ cÃ¢y vÃ  cÆ¡ cháº¿ fusion, khÃ´ng cÃ³ pháº§n hÆ°á»›ng dáº«n.
public class Level5Controller : LevelController
{
    public override void init()
    {
        GameManagement.levelData = new LevelData()
        {
            level = 5,
            levelName = "SÃ¢n Thá»­ Nghiá»‡m",
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
                "SunNut",
                "PeaShooter",
                "WallNut",
                "Squash",
                "TorchWood",
                "MiaoMiao",
                "SnowKing"
            },
            initialSun = 0,
            skipIntro = true,
            isTestMode = true
        };
    }

    public override void activate()
    {
        SunManagement sun = GameObject.Find("Sun Management").GetComponent<SunManagement>();
        sun.setDropInterval(0.8f, 1.4f);

        GameObject.Find("Zombie Management").AddComponent<TestZombieSpawner>();
    }
}

