using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlantGrid : MonoBehaviour
{
    #region Biến

    public int row;   //Ở hàng thứ mấy

    GameObject toBePlanted;   //Đối tượng To Be Planted
    GameObject selectedShovel;        //Đối tượng SelectedShovel

    SpriteRenderer spriteRenderer;  //Component SpriteRenderer của chính nó
    AudioSource audioSource;   //Component AudioSource của chính nó

    bool havePlanted = false;   //Ô này đã trồng cây chưa
    GameObject nowPlant;    //Cây đang trồng hiện tại

    #endregion

    #region Thông điệp hệ thống

    private void Awake()
    {
        //Lấy đối tượng và component
        toBePlanted = GameObject.Find("To Be Planted");
        selectedShovel = GameObject.Find("SelectedShovel");

        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
    }

    private void OnMouseEnter()
    {
        if(havePlanted == false && toBePlanted.activeSelf == true)
        {
            spriteRenderer.sprite = toBePlanted.GetComponent<SpriteRenderer>().sprite;
        }
        else if(havePlanted == true && selectedShovel.activeSelf == true)
        {
            nowPlant.GetComponent<Plant>().highlight();
        }
    }

    private void OnMouseExit()
    {
        if (havePlanted == false && toBePlanted.activeSelf == true)
        {
            spriteRenderer.sprite = null;
        }
        else if (havePlanted == true && selectedShovel.activeSelf == true)
        {
            nowPlant.GetComponent<Plant>().cancelHighlight();
        }
    }

    private void OnMouseDown()
    {
        if (havePlanted == false && toBePlanted.activeSelf == true)
        {
            plant(toBePlanted.GetComponent<ToBePlanted>().plantName);
        }
        else if (havePlanted == true && selectedShovel.activeSelf == true)
        {
            nowPlant.GetComponent<Plant>().die("shovelPlant");
        }
    }

    #endregion

    #region Hàm tự định nghĩa private

    #endregion

    #region Hàm tự định nghĩa public

    public void plant(string name)
    {
        spriteRenderer.sprite = null;   //Ẩn bóng mờ
        havePlanted = true;   //Cây đã trồng

        //Sinh ra cây
        nowPlant = Instantiate(Resources.Load<GameObject>("Prefabs/Plants/" + name),
                                transform.position + new Vector3(0, 0, 5),
                                Quaternion.Euler(0, 0, 0),
                                transform);
        nowPlant.GetComponent<Plant>().initialize(
            this,
            spriteRenderer.sortingLayerName,
            spriteRenderer.sortingOrder
        );

        //Phát âm thanh
        audioSource.clip =
            Resources.Load<AudioClip>("Sounds/UI/SeedAndShovelBank/plant");
        audioSource.Play();

        //Gửi thông điệp tới PlantingManagement để xử lý các sự kiện liên quan UI
        GameObject.Find("Planting Management").GetComponent<PlantingManagement>().plant();

    }

    //Trồng ở chế độ god mode, dùng để sinh cây tham gia hội thoại đầu màn
    public GameObject plantByGod(string name)
    {
        havePlanted = true;   //Cây đã trồng

        //Sinh ra cây
        nowPlant = Instantiate(Resources.Load<GameObject>("Prefabs/Plants/" + name),
                                          transform.position + new Vector3(0, 0, 5),
                                          Quaternion.Euler(0, 0, 0),
                                          transform);
        nowPlant.GetComponent<Plant>().initialize(
            this,
            spriteRenderer.sortingLayerName,
            spriteRenderer.sortingOrder
        );

        return nowPlant;
    }

    public void plantDie(string reason)
    {
        havePlanted = false;   //Không còn cây nữa

        AudioClip clip = null;
        if (reason != "") clip = Resources.Load<AudioClip>("Sounds/Plants/" + reason);
        if (clip != null)
        {
            //Phát âm thanh
            audioSource.clip = clip; 
            audioSource.Play();
        }
    }

    #endregion
}
