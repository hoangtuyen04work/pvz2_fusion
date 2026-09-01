using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Card : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
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

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (myButton == null || !myButton.enabled) return;
        click();
    }

    public void OnDrag(PointerEventData eventData)
    {
        // ToBePlanted tu di theo chuot; interface nay giu drag hoat dong tren UI.
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        GameObject preview = GameObject.Find("To Be Planted");
        if (preview == null || !preview.activeSelf || Camera.main == null) return;

        Vector3 world = Camera.main.ScreenToWorldPoint(eventData.position);
        foreach (Collider2D hit in Physics2D.OverlapPointAll(new Vector2(world.x, world.y)))
        {
            PlantGrid grid = hit.GetComponent<PlantGrid>();
            if (grid != null && grid.tryPlaceSelectedPlant()) break;
        }
        preview.SetActive(false);
    }
}
