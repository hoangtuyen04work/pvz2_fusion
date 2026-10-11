using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shovel : MonoBehaviour
{
    public GameObject shovelUI;   //UI cái xẻng, điều khiển ẩn hiện

    Vector3 mouseWorldPos;   //Vị trí chuột

    // Start is called before the first frame update
    void Start()
    {
        gameObject.SetActive(false);   //Chính nó không hiển thị
    }

    // Update is called once per frame
    void Update()
    {
        //Xẻng luôn đi theo chuột
        mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        transform.position = mouseWorldPos;

        //Bấm chuột trái, chính nó ẩn đi, UI hiện ra
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            shovelUI.SetActive(true);
            gameObject.SetActive(false);
        }
    }

    public void clickShovel()
    {
        //UI cái xẻng bị ẩn
        shovelUI.SetActive(false);

        //Chính nó hiện ra, đi theo chuột
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        transform.position = mouseWorldPos;
        gameObject.SetActive(true);

        //Phát âm thanh
        GetComponent<AudioSource>().Play();
    }
}
