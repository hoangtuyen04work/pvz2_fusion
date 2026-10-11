using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SunManagement : MonoBehaviour
{
    //Prefab mặt trời trên trời
    public GameObject skysunPrefab;
    public GameObject fullMoonPrefab;
    public GameObject crescentMoonPrefab;

    string createFunc;   //Tên hàm tạo nắng, chia làm ban ngày và ban đêm

    //Đếm giờ mặt trời rơi
    float minInterval = 10f, maxInterval = 20f;
    bool spawningPaused;
    //Vị trí ban đầu của mặt trời
    float posY =  3.4f;
    //Giới hạn trục x của vị trí mặt trời rơi
    const float leftEdge = -4.4f;
    const float rightEdge = 2.8f;

    public void setDropInterval(float minimum, float maximum)
    {
        minInterval = Mathf.Max(0.15f, minimum);
        maxInterval = Mathf.Max(minInterval, maximum);
    }

    public void SetSpawningPaused(bool paused)
    {
        spawningPaused = paused;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (GameManagement.levelData.isDay)
            createFunc = "createSun";
        else createFunc = "createMoon";

        //Máy khách không tự sinh mặt trời, nó nhận lệnh sinh từ máy chủ
        if (!NetSession.IsAuthority) return;

        Invoke(createFunc, Random.Range(minInterval, maxInterval));
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse0)) clickSun();
    }

    private void createSun()
    {
        if (!spawningPaused)
        {
            Instantiate(
                skysunPrefab,
                new Vector3(Random.Range(leftEdge, rightEdge), posY, 0),
                Quaternion.Euler(0, 0, 0),
                transform
            );
        }

        Invoke(createFunc, Random.Range(minInterval, maxInterval));
    }

    private void createMoon()
    {
        if (spawningPaused)
        {
            Invoke(createFunc, Random.Range(minInterval, maxInterval));
            return;
        }

        GameObject randPrefab;
        if (Random.Range(0.0f, 10.0f) > 3.0f) randPrefab = fullMoonPrefab;
        else randPrefab = crescentMoonPrefab;

        Instantiate(
            randPrefab,
            new Vector3(Random.Range(leftEdge, rightEdge), posY, 0),
            Quaternion.Euler(0, 0, 0),
            transform
        );

        Invoke(createFunc, Random.Range(minInterval, maxInterval));
    }

    private void clickSun()
    {
        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D[] allSun = Physics2D.OverlapPointAll(mouseWorldPos, LayerMask.GetMask("Sun"));
        if (allSun.Length > 0)
        {
            SunBase sun = allSun[allSun.Length - 1].gameObject.GetComponent<SunBase>();
            if (sun == null) return;

            // Kiểm tra quyền nhặt trong chế độ chơi mạng:
            // Phe cây nhặt được mọi loại nắng/mặt trăng.
            // Phe zombie trong đối kháng chỉ được nhặt Mặt Trăng (FullMoon / CrescentMoon).
            if (NetSession.IsOnline)
            {
                bool isMoon = sun.name.Contains("Moon") || sun.gameObject.name.Contains("Moon");
                if (NetSession.ControlsZombies && !isMoon) return;
                if (!NetSession.ControlsPlants && !NetSession.ControlsZombies) return;

                NetGameplay.RequestSunPickup(sun);
            }
            else
            {
                sun.bePickedUp();
            }
        }
    }
}
