using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Card : MonoBehaviour
{
    //Ảnh hồi chiêu
    public GameObject upperImageObj;
    public Image lowerImage;
    public GameObject lowerImageObj;

    public Button myButton;   //Component Button của chính nó

    //Thời gian và trạng thái hồi chiêu
    public float coolingTime;
    float timer;
    bool coolingState = true;

    //Trạng thái nắng có đủ hay không
    bool sunEnough;

    //Liên quan tới trồng cây
    PlantingManagement planting;
    public string plantName;
    public int sunNeeded;

    // Start is called before the first frame update
    void Start()
    {
        //Component này phải do đối tượng quản lý tải, nên lấy trong Start
        planting = GameObject.Find("Planting Management").GetComponent<PlantingManagement>();

        if (coolingTime > 10f) cooling();
        else endCooling();
    }

    // Update is called once per frame
    void Update()
    {
        if(coolingState == true)
        {
            timer += Time.deltaTime;
            if (timer / coolingTime < 1)
                lowerImage.rectTransform.localScale = new Vector3(1, 1 - timer / coolingTime, 1);
            else endCooling();
        }
    }

    public void cooling()
    {
        coolingState = true;
        timer = 0;
        lowerImage.fillAmount = 1;
        upperImageObj.SetActive(true);
        lowerImageObj.SetActive(true);
        myButton.enabled = false;
    }

    private void endCooling()
    {
        coolingState = false;
        lowerImageObj.SetActive(false);
        if (sunEnough)
        {
            upperImageObj.SetActive(false);
            myButton.enabled = true;
        }
    }

    public void updateSunEnough(bool state)
    {
        if(state == true)
        {
            sunEnough = true;
            if (coolingState == false)
            {
                upperImageObj.SetActive(false);
                myButton.enabled = true;
            }
        }
        else
        {
            sunEnough = false;
            upperImageObj.SetActive(true);
            myButton.enabled = false;
        }
    }    

    public void click()
    {
        //Phát âm thanh
        gameObject.GetComponent<AudioSource>().Play();

        //Chuyển cho quản lý trồng cây
        planting.clickPlant(plantName, gameObject.GetComponent<Card>());
    }
}
