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
    string selectedPlantName;

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
        selectedPlantName = plant;
        toBePlanted_Script.showPlantPreview(plant);
    }

    public bool hasSelectedPlant()
    {
        return !string.IsNullOrEmpty(selectedPlantName);
    }

    public string getSelectedPlantName()
    {
        return selectedPlantName;
    }

    public void clearSelectedPlant()
    {
        selectedPlantName = null;
        if (toBePlanted_Object != null) toBePlanted_Object.SetActive(false);
    }
    //Trồng cây
    public void plant()
    {
        if (GameManagement.levelData.isTestMode) return;
        GameObject.Find("Sun Text").GetComponent<SunNumber>().subSun(nowCard.sunNeeded);
        nowCard.cooling();
    }

    #endregion
}
