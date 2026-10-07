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
    private UnityEngine.Object introDialogPrefab;
    private bool gameplayStarted;
    private bool gameEnding;
    private GameObject zombiePreviewRoot;
    public static LevelData levelData;   //Dữ liệu màn hiện tại

    private const float ZombiePreviewHold = 1.25f;
    private const float CameraReturnDuration = 2.1f;
    private const float GameOverCameraPanDuration = 1.35f;
    private const float GameOverHouseHold = 0.8f;

    public List<GameObject> awakeList;  //Danh sách chờ đánh thức, dùng để đánh thức các đối tượng sau khi kết thúc cốt truyện mở màn

    public GameObject endMenuPanel;   //Bảng kết thúc trò chơi
    public GameObject background;   //Đối tượng nền
    public GameObject zombieManagement;   //Đối tượng quản lý zombie
    public GameObject uiManagement;   //Đối tượng quản lý UI

    private void Awake()
    {
        // Chơi mạng thì màn do chủ phòng quyết định, sau đó mới tới bảng chọn màn.
        if (NetSession.IsOnline && NetSession.Level >= 0)
            level = NetSession.Level;
        else if (GameSession.SelectedLevel >= 0)
            level = GameSession.SelectedLevel;

        levelController = 
            (LevelController)gameObject.AddComponent(Type.GetType("Level" + level + "Controller"));
        levelController.init();

        // Endless chỉ mượn sân và prefab của màn ngày; luật chơi do hệ thống riêng quản lý.
        if (EndlessRun.Active)
        {
            levelData.levelName = "Sinh Tồn Vô Hạn";
            levelData.initialSun = 150;
            levelData.skipIntro = true;
        }

        // A selection made in the main menu overrides the level's legacy
        // default deck. Direct scene launches still keep the old defaults.
        if (GameSession.SelectedPlants.Count > 0)
            levelData.plantCards = new List<string>(GameSession.SelectedPlants);

        //Tải ảnh nền
        string mapPath = string.IsNullOrEmpty(levelData.mapResourcePath)
            ? "Sprites/BackGround/Background" + levelData.mapSuffix
            : levelData.mapResourcePath;
        Sprite mapSprite = Resources.Load<Sprite>(mapPath);
        background.GetComponent<SpriteRenderer>().sprite = mapSprite;
        if (mapSprite == null)
            Debug.LogError("Missing level background: " + mapPath, this);
        else if (levelData.backgroundWorldSize.x > 0f && levelData.backgroundWorldSize.y > 0f)
        {
            Vector2 spriteSize = mapSprite.bounds.size;
            background.transform.localScale = new Vector3(
                levelData.backgroundWorldSize.x / spriteSize.x,
                levelData.backgroundWorldSize.y / spriteSize.y,
                1f);
        }
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
        applyCustomGridLayout(pm);

        //Tải UI
        uiManagement.GetComponent<UIManagement>().initUI();

        //Màn test hoặc màn không có prefab hội thoại sẽ vào gameplay trực tiếp.
        introDialogPrefab = Resources.Load<UnityEngine.Object>("Prefabs/UI/DialogPanel/DialogPanel-Level" + level);
        //Chơi mạng thì bỏ hội thoại mở màn, tránh hai máy lệch nhịp
        if (levelData.skipIntro || NetSession.SkipIntroDialog || introDialogPrefab == null)
            startWithoutDialog = true;
    }

    private void applyCustomGridLayout(GameObject plantingRoot)
    {
        if (levelData.plantGridPosX == null || levelData.plantGridPosY == null ||
            levelData.plantGridPosX.Count == 0 || levelData.plantGridPosY.Count == 0) return;

        foreach (PlantGrid grid in plantingRoot.GetComponentsInChildren<PlantGrid>(true))
        {
            string[] parts = grid.gameObject.name.Split('-');
            if (parts.Length != 3 || !int.TryParse(parts[1], out int column) ||
                !int.TryParse(parts[2], out int row)) continue;
            if (column < 0 || column >= levelData.plantGridPosX.Count ||
                row < 0 || row >= levelData.plantGridPosY.Count) continue;

            Vector3 local = grid.transform.localPosition;
            grid.transform.localPosition = new Vector3(
                levelData.plantGridPosX[column], levelData.plantGridPosY[row], local.z);
        }
    }

    private void Start()
    {
        StartCoroutine(playOpeningCamera());
    }

    // Xem trước đội zombie ở mép phải, sau đó trượt camera về sân bên trái.
    // Các zombie preview không đăng ký vào ZombieManagement nên không ảnh hưởng thắng/thua.
    private IEnumerator playOpeningCamera()
    {
        yield return null;

        Camera mainCamera = Camera.main;
        SpriteRenderer backgroundRenderer = background != null
            ? background.GetComponent<SpriteRenderer>()
            : null;
        if (mainCamera != null && backgroundRenderer != null && backgroundRenderer.sprite != null)
        {
            Vector3 gameplayPosition = mainCamera.transform.position;
            float halfCameraWidth = mainCamera.orthographicSize * mainCamera.aspect;
            float rightPreviewX = Mathf.Max(gameplayPosition.x,
                backgroundRenderer.bounds.max.x - halfCameraWidth);
            Vector3 previewPosition = new Vector3(rightPreviewX,
                gameplayPosition.y, gameplayPosition.z);

            mainCamera.transform.position = previewPosition;
            createZombiePreview();

            float hold = 0f;
            while (hold < ZombiePreviewHold)
            {
                hold += Time.unscaledDeltaTime;
                yield return null;
            }

            float elapsed = 0f;
            while (elapsed < CameraReturnDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / CameraReturnDuration);
                progress = progress * progress * (3f - 2f * progress);
                mainCamera.transform.position = Vector3.Lerp(previewPosition, gameplayPosition, progress);
                yield return null;
            }
            mainCamera.transform.position = gameplayPosition;
        }

        clearZombiePreview();

        if (!startWithoutDialog && introDialogPrefab != null)
        {
            GameObject topCanvas = GameObject.Find("TopCanvas");
            Instantiate(introDialogPrefab, Vector3.zero, Quaternion.identity,
                topCanvas != null ? topCanvas.transform : null);
        }
        else
        {
            awakeAll();
        }
    }

    private void createZombiePreview()
    {
        ZombieManagement manager = zombieManagement != null
            ? zombieManagement.GetComponent<ZombieManagement>()
            : null;
        if (manager == null || manager.zombies == null) return;

        var zombieTypes = new List<string>();
        TextAsset json = Resources.Load<TextAsset>("Json/ZombieData/Level" + levelData.level);
        TimeNodes nodes = json != null ? JsonUtility.FromJson<TimeNodes>(json.text) : null;
        if (nodes != null && nodes.info != null)
        {
            foreach (TimeNode node in nodes.info)
                if (!string.IsNullOrEmpty(node.zombie) && !zombieTypes.Contains(node.zombie))
                    zombieTypes.Add(node.zombie);
        }
        if (levelData.level == 3 && !zombieTypes.Contains("Ghost")) zombieTypes.Add("Ghost");
        if (zombieTypes.Count == 0) zombieTypes.Add("ZombieNormal");

        zombiePreviewRoot = new GameObject("Zombie Preview");
        int previewCount = Mathf.Clamp(zombieTypes.Count * 2, 5, 8);
        for (int i = 0; i < previewCount; i++)
        {
            string zombieName = zombieTypes[i % zombieTypes.Count];
            int row = i % levelData.zombieInitPosY.Count;
            Vector3 position = new Vector3(5.45f + (i / levelData.rowCount) * 0.62f,
                levelData.zombieInitPosY[row], 0f);

            GameObject prefab = null;
            foreach (GameObject candidate in manager.zombies)
                if (candidate != null && candidate.name == zombieName)
                {
                    prefab = candidate;
                    break;
                }

            GameObject preview = prefab != null
                ? Instantiate(prefab, position, Quaternion.identity, zombiePreviewRoot.transform)
                : ImportedZombieRuntime.Create(zombieName, position, zombiePreviewRoot.transform);
            if (preview == null) continue;

            preview.name = "Preview - " + zombieName;
            Zombie zombie = preview.GetComponent<Zombie>();
            if (zombie != null)
            {
                zombie.pos_row = row;
                zombie.enabled = false;
            }
            foreach (Collider2D collider in preview.GetComponentsInChildren<Collider2D>(true))
                collider.enabled = false;
            foreach (Rigidbody2D body in preview.GetComponentsInChildren<Rigidbody2D>(true))
                body.simulated = false;
            foreach (AudioSource source in preview.GetComponentsInChildren<AudioSource>(true))
            {
                source.playOnAwake = false;
                source.Stop();
            }
            foreach (Animator animator in preview.GetComponentsInChildren<Animator>(true))
            {
                foreach (AnimatorControllerParameter parameter in animator.parameters)
                    if (parameter.type == AnimatorControllerParameterType.Bool && parameter.name == "Walk")
                        animator.SetBool("Walk", true);
            }
        }
    }

    private void clearZombiePreview()
    {
        if (zombiePreviewRoot != null) Destroy(zombiePreviewRoot);
        zombiePreviewRoot = null;
    }

    public void awakeAll()
    {
        if (gameplayStarted) return;
        gameplayStarted = true;

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
        if (EndlessRun.HandleGameOver()) return;
        if (gameEnding) return;
        gameEnding = true;
        //Máy chủ báo kết quả cho máy khách trước khi hiện bảng kết thúc
        NetGameplay.NotifyGameEnd(true);
        StartCoroutine(playGameOverCamera());
    }

    private IEnumerator playGameOverCamera()
    {
        Camera mainCamera = Camera.main;
        SpriteRenderer backgroundRenderer = background != null
            ? background.GetComponent<SpriteRenderer>()
            : null;
        if (mainCamera != null && backgroundRenderer != null)
        {
            Vector3 start = mainCamera.transform.position;
            float halfCameraWidth = mainCamera.orthographicSize * mainCamera.aspect;
            float leftEdgeX = backgroundRenderer.bounds.min.x + halfCameraWidth;
            Vector3 target = new Vector3(Mathf.Min(start.x, leftEdgeX), start.y, start.z);
            float elapsed = 0f;
            while (elapsed < GameOverCameraPanDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.SmoothStep(0f, 1f,
                    Mathf.Clamp01(elapsed / GameOverCameraPanDuration));
                mainCamera.transform.position = Vector3.Lerp(start, target, progress);
                yield return null;
            }
            mainCamera.transform.position = target;
        }

        float hold = 0f;
        while (hold < GameOverHouseHold)
        {
            hold += Time.unscaledDeltaTime;
            yield return null;
        }
        if (endMenuPanel != null) endMenuPanel.GetComponent<EndMenu>()?.gameOver();
    }

    public void win()
    {
        if (gameEnding) return;
        gameEnding = true;
        NetGameplay.NotifyGameEnd(false);
        endMenuPanel.GetComponent<EndMenu>().win();
    }

    private void OnDestroy()
    {
        clearZombiePreview();
    }
}
