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
        Instantiate(
            skysunPrefab, 
            new Vector3(Random.Range(leftEdge, rightEdge), posY, 0), 
            Quaternion.Euler(0, 0, 0), 
            transform
        );

        Invoke(createFunc, Random.Range(minInterval, maxInterval));
    }

    private void createMoon()
    {
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
        //Phe zombie trong chế độ đối kháng không nhặt được nắng
        if (NetSession.IsOnline && !NetSession.ControlsPlants) return;

        Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D[] allSun = Physics2D.OverlapPointAll(mouseWorldPos, LayerMask.GetMask("Sun"));
        if (allSun.Length > 0)
        {
            //Chơi mạng thì máy chủ mới quyết định mặt trời có được nhặt hay không
            NetGameplay.RequestSunPickup(
                allSun[allSun.Length - 1].gameObject.GetComponent<SunBase>());
        }
    }
}
