using System.Collections.Generic;
using UnityEngine;

// ============================================================
// GameStateCollector: Thu thập toàn bộ trạng thái game hiện tại
// và đóng gói thành một GameStateSnapshot để gửi lên server AI.
//
// Cách dùng: gọi Collect() để lấy snapshot tại thời điểm hiện tại.
// ============================================================

public class GameStateCollector : MonoBehaviour
{
    // ---- Tham chiếu các đối tượng quản lý trong scene ----
    private ZombieManagement zombieManager;
    private SunNumber sunNumber;

    private void Awake()
    {
        // Tìm các đối tượng quản lý trong scene
        GameObject zm = GameObject.Find("Zombie Management");
        if (zm != null) zombieManager = zm.GetComponent<ZombieManagement>();

        GameObject sunText = GameObject.Find("Sun Text");
        if (sunText != null) sunNumber = sunText.GetComponent<SunNumber>();
    }

    // Hàm chính: Thu thập và trả về snapshot trạng thái game hiện tại
    public GameStateSnapshot Collect()
    {
        var snapshot = new GameStateSnapshot();
        LevelData data = GameManagement.levelData;

        // Lazy find nếu chưa tìm thấy lúc Awake
        if (zombieManager == null)
        {
            GameObject zm = GameObject.Find("Zombie Management");
            if (zm != null) zombieManager = zm.GetComponent<ZombieManagement>();
        }
        if (sunNumber == null)
        {
            GameObject sunText = GameObject.Find("Sun Text");
            if (sunText != null) sunNumber = sunText.GetComponent<SunNumber>();
        }

        // ---- Thông tin màn chơi ----
        if (data != null)
        {
            snapshot.level = data.level;
            snapshot.levelName = data.levelName;
            snapshot.isDay = data.isDay;
            snapshot.availablePlantCards = data.plantCards != null ? new List<string>(data.plantCards) : new List<string>();
        }
        else
        {
            snapshot.level = 1;
            snapshot.levelName = "Màn chơi";
            snapshot.isDay = true;
            snapshot.availablePlantCards = new List<string>();
        }

        // ---- Số mặt trời hiện tại ----
        snapshot.currentSun = sunNumber != null ? sunNumber.NowSun : 0;

        // ---- Thông tin đợt zombie ----
        snapshot.zombiesOnField = 0;
        snapshot.currentWave = 0;
        snapshot.totalWaves = 1;
        snapshot.nextWaveZombieType = "Hết đợt";

        if (zombieManager != null)
        {
            snapshot.zombiesOnField = Mathf.Max(0, zombieManager.ZombieNum_now);
            snapshot.currentWave = Mathf.Max(0, zombieManager.NowNode_index);
            snapshot.totalWaves = Mathf.Max(1, zombieManager.NodeCount);

            // Loại zombie của đợt kế tiếp (nếu còn)
            int nextIdx = zombieManager.NowNode_index + 1;
            snapshot.nextWaveZombieType = (nextIdx < zombieManager.NodeCount && zombieManager.NowNode != null && !string.IsNullOrEmpty(zombieManager.NowNode.zombie))
                ? zombieManager.NowNode.zombie
                : "Hết đợt";
        }

        // ---- Danh sách cây đang trồng & ô trống ----
        snapshot.plants = new List<PlantInfo>();
        snapshot.emptyGrids = new List<GridInfo>();

        PlantGrid[] grids = FindObjectsByType<PlantGrid>(FindObjectsSortMode.None);
        foreach (PlantGrid grid in grids)
        {
            // Parse cột từ tên GameObject (Định dạng chuẩn của game: Plant-{col}-{row})
            int gridCol = 0;
            string[] parts = grid.gameObject.name.Split('-');
            if (parts.Length >= 2 && int.TryParse(parts[1], out int parsedCol))
            {
                gridCol = Mathf.Clamp(parsedCol, 0, 8);
            }
            // Chuẩn hóa hàng theo quy ước 0-indexed từ trên xuống dưới (trong game gốc: row 0 ở dưới cùng, row 4 ở trên cùng)
            int gridRow = Mathf.Clamp(4 - grid.row, 0, 4);

            if (grid.HavePlanted && grid.NowPlant != null)
            {
                Plant plant = grid.NowPlant.GetComponent<Plant>();
                if (plant != null)
                {
                    snapshot.plants.Add(new PlantInfo
                    {
                        type = grid.NowPlant.name.Replace("(Clone)", "").Trim(),
                        row = gridRow,
                        col = gridCol,
                        posX = grid.transform.position.x,
                        posY = grid.transform.position.y,
                        hp = Mathf.Max(0, plant.bloodVolume),
                        maxHp = Mathf.Max(1, plant.BloodVolumeMax),
                        state = plant.state.ToString(),
                        intensified = plant.Intensified
                    });
                }
            }
            else
            {
                snapshot.emptyGrids.Add(new GridInfo
                {
                    row = gridRow,
                    col = gridCol,
                    posX = grid.transform.position.x,
                    posY = grid.transform.position.y
                });
            }
        }

        // ---- Danh sách zombie đang sống trên màn ----
        snapshot.zombies = new List<ZombieInfo>();
        if (zombieManager != null)
        {
            // Duyệt tất cả children của Zombie Management
            foreach (Transform child in zombieManager.transform)
            {
                if (!child.gameObject.activeSelf) continue;
                Zombie zombie = child.GetComponent<Zombie>();
                if (zombie == null) continue;

                snapshot.zombies.Add(new ZombieInfo
                {
                    type = child.name.Replace("(Clone)", "").Trim(),
                    row = Mathf.Clamp(4 - zombie.pos_row, 0, 4),
                    posX = child.transform.position.x,
                    hp = Mathf.Max(0, zombie.bloodVolume),
                    maxHp = Mathf.Max(1, zombie.BloodVolumeMax),
                    state = zombie.state.ToString()
                });
            }
        }

        return snapshot;
    }

    // Serialize snapshot thành JSON string để gửi HTTP
    public string CollectAsJson()
    {
        GameStateSnapshot snapshot = Collect();
        return JsonUtility.ToJson(snapshot, prettyPrint: true);
    }
}
