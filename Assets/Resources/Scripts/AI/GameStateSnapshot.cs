using System;
using System.Collections.Generic;

// ============================================================
// Các lớp dữ liệu mô tả trạng thái phiên chơi tại một thời điểm.
// Được dùng bởi GameStateCollector để serialize thành JSON
// và AIServiceConnector để gửi lên server AI.
// ============================================================

[Serializable]
public class PlantInfo
{
    public string type;        // Tên loại cây (VD: "SunFlower", "WallNut")
    public int row;            // Hàng (0-indexed)
    public int col;            // Cột (0-indexed, tính từ tên GameObject của PlantGrid)
    public float posX;         // Tọa độ X trong world
    public float posY;         // Tọa độ Y trong world
    public int hp;             // Máu hiện tại
    public int maxHp;          // Máu tối đa
    public string state;       // "Normal" | "Warm" | "Cold"
    public bool intensified;   // Có đang ở trạng thái tăng cường (SnowKing buff) không
}

[Serializable]
public class ZombieInfo
{
    public string type;        // Tên loại zombie (VD: "BucketZombie", "NormalZombie")
    public int row;            // Hàng zombie đang đứng
    public float posX;         // Vị trí X (càng nhỏ càng gần nhà)
    public int hp;             // Máu hiện tại
    public int maxHp;          // Máu tối đa
    public string state;       // "Normal" | "Cold" | "Parasiticed"
}

[Serializable]
public class GridInfo
{
    public int row;
    public int col;
    public float posX;
    public float posY;
}

[Serializable]
public class GameStateSnapshot
{
    public int level;                          // Số thứ tự màn
    public string levelName;                   // Tên màn chơi
    public bool isDay;                         // Ban ngày hay ban đêm
    public int currentSun;                     // Số mặt trời hiện tại
    public int currentWave;                    // Đợt zombie hiện tại (0-indexed)
    public int totalWaves;                     // Tổng số đợt zombie
    public int zombiesOnField;                 // Số zombie đang có trên màn
    public string nextWaveZombieType;          // Loại zombie của đợt kế tiếp (nếu còn)
    public List<string> availablePlantCards;   // Các thẻ cây người chơi được chọn trong màn này
    public List<PlantInfo> plants;             // Danh sách cây đang trồng
    public List<ZombieInfo> zombies;           // Danh sách zombie đang sống
    public List<GridInfo> emptyGrids;          // Các ô trống chưa trồng cây
}
