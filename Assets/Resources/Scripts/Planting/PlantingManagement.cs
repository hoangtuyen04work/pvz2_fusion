using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlantingManagement : MonoBehaviour
{
    #region Biến

    //Liên quan tới cây đang chờ trồng
    public GameObject toBePlanted_Object;
    ToBePlanted toBePlanted_Script;

    Card nowCard;  //Card của thẻ cây đang được chọn

    #endregion

    #region Thông điệp hệ thống

    // Start is called before the first frame update
    void Start()
    {
        //Lấy game object và component
        toBePlanted_Script = toBePlanted_Object.GetComponent<ToBePlanted>();
    }

    #endregion

    #region Hàm tự định nghĩa private



    #endregion

    #region Hàm tự định nghĩa public

    //Bấm nút để chọn cây
    public void clickPlant(string plant, Card card)
    {
        nowCard = card;
        toBePlanted_Script.showPlantPreview(plant);
    }

    //Trồng cây
    public void plant()
    {
        GameObject.Find("Sun Text").GetComponent<SunNumber>().subSun(nowCard.sunNeeded);
        nowCard.cooling();
    }

    #endregion
}
