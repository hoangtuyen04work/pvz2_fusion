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
    PlantingManagement plantingManagement;

    SpriteRenderer spriteRenderer;  //Component SpriteRenderer của chính nó
    AudioSource audioSource;   //Component AudioSource của chính nó

    bool havePlanted = false;   //Ô này đã trồng cây chưa
    GameObject nowPlant;    //Cây đang trồng hiện tại
    bool fusionHighlighted;

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
        else if (havePlanted && toBePlanted.activeSelf && canFuse(toBePlanted.GetComponent<ToBePlanted>().plantName))
        {
            nowPlant.GetComponent<Plant>().highlight();
            fusionHighlighted = true;
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
        else if (havePlanted && fusionHighlighted)
        {
            nowPlant.GetComponent<Plant>().cancelHighlight();
            fusionHighlighted = false;
        }
    }

    private void OnMouseDown()
    {
        if (tryPlaceSelectedPlant())
        {
            toBePlanted.SetActive(false);
            return;
        }

        if (havePlanted && selectedShovel.activeSelf)
        {
            nowPlant.GetComponent<Plant>().die("shovelPlant");
        }
    }
    #endregion

    private bool canFuse(string selectedPlant)
    {
        if (string.IsNullOrEmpty(selectedPlant) || nowPlant == null ||
            nowPlant.GetComponent<Plant>() == null ||
            nowPlant.GetComponent<FireWallNutFusion>() != null ||
            nowPlant.GetComponent<SunNut>() != null) return false;
        bool wallNutOnGrid = nowPlant.name.StartsWith("WallNut", StringComparison.OrdinalIgnoreCase);
        bool torchWoodOnGrid = nowPlant.name.StartsWith("Torchwood", StringComparison.OrdinalIgnoreCase);
        bool sunFlowerOnGrid = nowPlant.name.StartsWith("SunFlower", StringComparison.OrdinalIgnoreCase);
        return (wallNutOnGrid && (selectedPlant.Equals("TorchWood", StringComparison.OrdinalIgnoreCase) || selectedPlant.Equals("SunFlower", StringComparison.OrdinalIgnoreCase)))
            || (torchWoodOnGrid && selectedPlant.Equals("WallNut", StringComparison.OrdinalIgnoreCase))
            || (sunFlowerOnGrid && selectedPlant.Equals("WallNut", StringComparison.OrdinalIgnoreCase));
    }

    private bool fuse(string selectedPlant)
    {
        if (!canFuse(selectedPlant)) return false;

        bool createsFireWallNut = selectedPlant.Equals("TorchWood", StringComparison.OrdinalIgnoreCase)
            || nowPlant.name.StartsWith("Torchwood", StringComparison.OrdinalIgnoreCase);
        string resultPlant = createsFireWallNut ? "WallNut" : "SunNut";
        GameObject resultPrefab = Resources.Load<GameObject>("Prefabs/Plants/" + resultPlant);
        if (resultPrefab == null)
        {
            Debug.LogError("Missing fusion plant prefab: " + resultPlant, this);
            return false;
        }

        GameObject fusedPlant = Instantiate(
            resultPrefab,
            transform.position + new Vector3(0, 0, 5),
            Quaternion.identity,
            transform);
        Plant fusedPlantComponent = fusedPlant.GetComponent<Plant>();
        if (fusedPlantComponent == null)
        {
            Debug.LogError("Fusion prefab is missing Plant component: " + resultPlant, resultPrefab);
            Destroy(fusedPlant);
            return false;
        }

        GameObject oldPlant = nowPlant;
        fusedPlant.name = createsFireWallNut ? "FireWallNut" : "SunNut";
        if (createsFireWallNut) fusedPlant.AddComponent<FireWallNutFusion>();
        fusedPlantComponent.initialize(this, spriteRenderer.sortingLayerName, spriteRenderer.sortingOrder);

        fusionHighlighted = false;
        nowPlant = fusedPlant;
        oldPlant.GetComponent<Plant>().removeForFusion();

        audioSource.clip = Resources.Load<AudioClip>("Sounds/UI/SeedAndShovelBank/plant");
        if (audioSource.clip != null) audioSource.Play();
        plantingManagement.plant();
        return true;
    }
    #region Hàm tự định nghĩa private

    #endregion

    #region Hàm tự định nghĩa public

    public bool tryPlaceSelectedPlant()
    {
        if (plantingManagement == null)
        {
            GameObject managerObject = GameObject.Find("Planting Management");
            plantingManagement = managerObject != null ? managerObject.GetComponent<PlantingManagement>() : null;
        }
        if (plantingManagement == null || !plantingManagement.hasSelectedPlant()) return false;

        string selectedPlant = plantingManagement.getSelectedPlantName();
        if (!havePlanted)
        {
            plant(selectedPlant);
            plantingManagement.clearSelectedPlant();
            return true;
        }
        if (canFuse(selectedPlant))
        {
            if (fuse(selectedPlant))
            {
                plantingManagement.clearSelectedPlant();
                return true;
            }
        }
        return false;
    }
    public void plant(string name)
    {
        spriteRenderer.sprite = null;   //Ẩn bóng mờ

        //Sinh ra cây
        GameObject prefab = Resources.Load<GameObject>("Prefabs/Plants/" + name);
        nowPlant = prefab != null
            ? Instantiate(prefab, transform.position + new Vector3(0, 0, 5), Quaternion.identity, transform)
            : ImportedPlantRuntime.CreatePlant(name, transform.position + new Vector3(0, 0, 5), transform);
        Plant component = nowPlant != null ? nowPlant.GetComponent<Plant>() : null;
        if (component == null)
        {
            Debug.LogError("No playable prefab or imported definition found for plant: " + name, this);
            nowPlant = null;
            return;
        }
        havePlanted = true;   //Cây đã trồng
        component.initialize(
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
        //Sinh ra cây
        GameObject prefab = Resources.Load<GameObject>("Prefabs/Plants/" + name);
        nowPlant = prefab != null
            ? Instantiate(prefab, transform.position + new Vector3(0, 0, 5), Quaternion.identity, transform)
            : ImportedPlantRuntime.CreatePlant(name, transform.position + new Vector3(0, 0, 5), transform);
        Plant component = nowPlant != null ? nowPlant.GetComponent<Plant>() : null;
        if (component == null) return null;
        havePlanted = true;
        component.initialize(
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
