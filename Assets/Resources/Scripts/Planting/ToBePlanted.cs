using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToBePlanted : MonoBehaviour
{
    #region Biến

    public string plantName;   //Tên cây đang được chọn

    SpriteRenderer spriteRenderer;   //Component SpriteRenderer của chính nó

    Vector3 mouseWorldPos;  //Vị trí chuột

    #endregion

    #region Thông điệp hệ thống

    // Start is called before the first frame update
    void Start()
    {
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        //Lấy vị trí chuột hiện tại
        mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        //Cây chờ trồng di chuyển theo chuột
        transform.position = mouseWorldPos;

    }

    #endregion

    #region Hàm tự định nghĩa private

    #endregion

    #region Hàm tự định nghĩa public

    public void showPlantPreview(string name)
    {
        plantName = name;
        spriteRenderer.sprite = Resources.Load<Sprite>("Sprites/Plants/" + plantName);
        if (spriteRenderer.sprite == null)
        {
            spriteRenderer.sprite = ImportedPlantRuntime.Preview(plantName);
            float scale = ImportedPlantRuntime.VisualScale(plantName);
            transform.localScale = new Vector3(scale, scale, 1f);
        }
        else transform.localScale = Vector3.one;

        //Lấy vị trí chuột hiện tại
        mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        transform.position = mouseWorldPos;
        gameObject.SetActive(true);
    }

    #endregion
}
