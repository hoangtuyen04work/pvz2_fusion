using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManagement : MonoBehaviour
{
    public int level;   //Số thứ tự màn hiện tại
    private LevelController levelController;
    private bool startWithoutDialog;
    public static LevelData levelData;   //Dữ liệu màn hiện tại

    public List<GameObject> awakeList;  //Danh sách chờ đánh thức, dùng để đánh thức các đối tượng sau khi kết thúc cốt truyện mở màn

    public GameObject endMenuPanel;   //Bảng kết thúc trò chơi
    public GameObject background;   //Đối tượng nền
    public GameObject zombieManagement;   //Đối tượng quản lý zombie
    public GameObject uiManagement;   //Đối tượng quản lý UI

    private void Awake()
    {
        // Nếu vào từ bảng chọn màn, ưu tiên level mà người chơi vừa chọn.
        if (GameSession.SelectedLevel >= 0)
            level = GameSession.SelectedLevel;

        levelController = 
            (LevelController)gameObject.AddComponent(Type.GetType("Level" + level + "Controller"));
        levelController.init();

        //Tải ảnh nền
        background.GetComponent<SpriteRenderer>().sprite =
            Resources.Load<Sprite>("Sprites/Background/Background" + levelData.mapSuffix);
        //Đặt nhạc nền
        background.GetComponent<BGMusicControl>()
            .changeMusic("Music" + levelData.backgroundSuffix);

        //Tải component quản lý trồng cây tương ứng
        GameObject pm = Instantiate(
            Resources.Load<GameObject>(
                "Prefabs/PlantingManagement/PlantingManagement" + levelData.plantingManagementSuffix),
            new Vector3(0, 0, 0),
            Quaternion.Euler(0, 0, 0)
        );
        pm.name = "Planting Management";

        //Tải UI
        uiManagement.GetComponent<UIManagement>().initUI();

        //Màn test hoặc màn không có prefab hội thoại sẽ vào gameplay trực tiếp.
        UnityEngine.Object dialog = Resources.Load<UnityEngine.Object>("Prefabs/UI/DialogPanel/DialogPanel-Level" + level);
        if (!levelData.skipIntro && dialog != null)
        {
            Instantiate(dialog, Vector3.zero, Quaternion.identity, GameObject.Find("TopCanvas").transform);
        }
        else
        {
            startWithoutDialog = true;
        }
    }

    private void Start()
    {
        if (startWithoutDialog) awakeAll();
    }

    public void awakeAll()
    {
        // Hội thoại mở đầu đã kết thúc, ẩn nút bỏ qua trước khi gameplay bắt đầu.
        StartupSkipController.HideForGameplay();

        foreach (GameObject gameObject in awakeList)
        {
            gameObject.SetActive(true);
        }
        uiManagement.GetComponent<UIManagement>().appear();
        levelController.activate();
        StartCoroutine(activateZombiesNextFrame());
    }

    private IEnumerator activateZombiesNextFrame()
    {
        //Đợi Start của ZombieManagement đọc xong JSON sau khi object vừa được bật.
        yield return null;
        zombieManagement.GetComponent<ZombieManagement>().activate();
    }

    public void gameOver()
    {
        endMenuPanel.GetComponent<EndMenu>().gameOver();
    }

    public void win()
    {
        endMenuPanel.GetComponent<EndMenu>().win();
    }
}
