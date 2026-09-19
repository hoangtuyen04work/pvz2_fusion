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

    // Getter cho GameStateCollector
    public bool HavePlanted => havePlanted;
    public GameObject NowPlant => nowPlant;

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
        if (tryPlaceSelectedPlant()) return;

        if (havePlanted == true && selectedShovel.activeSelf == true)
        {
            digOut();
        }
    }
    #endregion

    private bool canFuse(string selectedPlant)
    {
        if (string.IsNullOrEmpty(selectedPlant) || nowPlant == null ||
            nowPlant.GetComponent<Plant>() == null ||
            nowPlant.GetComponent<FireWallNutFusion>() != null ||
            nowPlant.GetComponent<SunNut>() != null) return false;
        if (HybridPlantRuntime.IsFinalEvolution(nowPlant.name)) return false;
        if (HybridPlantRuntime.TryGetFusionResult(nowPlant.name, selectedPlant, out _)) return true;
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

        if (HybridPlantRuntime.TryGetFusionResult(nowPlant.name, selectedPlant, out string hybridResult))
            return fuseHybrid(hybridResult);

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
        return true;
    }

    private bool fuseHybrid(string resultPlant)
    {
        GameObject fusedPlant = HybridPlantRuntime.Create(
            resultPlant,
            transform.position + new Vector3(0, 0, 5),
            transform);
        Plant fusedPlantComponent = fusedPlant != null ? fusedPlant.GetComponent<Plant>() : null;
        if (fusedPlantComponent == null)
        {
            Debug.LogError("Unable to create hybrid plant: " + resultPlant, this);
            if (fusedPlant != null) Destroy(fusedPlant);
            return false;
        }

        GameObject oldPlant = nowPlant;
        fusedPlant.name = resultPlant;
        fusedPlantComponent.initialize(this, spriteRenderer.sortingLayerName, spriteRenderer.sortingOrder);

        fusionHighlighted = false;
        nowPlant = fusedPlant;
        oldPlant.GetComponent<Plant>().removeForFusion();

        audioSource.clip = Resources.Load<AudioClip>("Sounds/UI/SeedAndShovelBank/plant");
        if (audioSource.clip != null) audioSource.Play();
        return true;
    }
    #region Hàm tự định nghĩa private

    #endregion

    #region Hàm tự định nghĩa public

    public bool tryPlaceSelectedPlant()
    {
        ensurePlantingManagement();
        if (plantingManagement == null || !plantingManagement.hasSelectedPlant()) return false;

        string selectedPlant = plantingManagement.getSelectedPlantName();

        //Chơi mạng thì thao tác phải đi qua máy chủ, không được tự trồng tại chỗ
        if (NetSession.IsOnline)
        {
            if (!NetSession.ControlsPlants) return false;
            if (!canAccept(selectedPlant)) return false;
            if (!NetGameplay.RequestPlace(gameObject.name, selectedPlant)) return false;
            plantingManagement.clearSelectedPlant();
            return true;
        }

        if (!placePlant(selectedPlant)) return false;
        plantingManagement.clearSelectedPlant();
        chargeLocal();
        return true;
    }

    private void ensurePlantingManagement()
    {
        if (plantingManagement != null) return;
        GameObject managerObject = GameObject.Find("Planting Management");
        plantingManagement = managerObject != null ? managerObject.GetComponent<PlantingManagement>() : null;
    }

    //Ô này có nhận được cây đang chọn không: hoặc còn trống, hoặc ghép được với cây đang có
    private bool canAccept(string plantName)
    {
        return !havePlanted || canFuse(plantName);
    }

    //Đặt cây xuống ô, không đụng gì tới nắng và hồi chiêu. Trả về true nếu đặt được.
    public bool placePlant(string plantName)
    {
        if (!havePlanted)
        {
            plant(plantName);
            return true;
        }
        if (canFuse(plantName))
        {
            if (!fuse(plantName)) return false;
            return true;
        }
        return false;
    }

    //Trừ nắng và cho thẻ vào hồi chiêu ở chế độ chơi đơn
    private void chargeLocal()
    {
        ensurePlantingManagement();
        if (plantingManagement != null) plantingManagement.plant();
    }

    //Bấm xẻng lên ô này
    private void digOut()
    {
        if (NetSession.IsOnline && !NetSession.ControlsPlants) return;
        //Máy khách chỉ gửi yêu cầu, máy chủ mới thật sự đào
        if (NetGameplay.RequestShovel(gameObject.name)) return;
        removePlantByShovel();
    }

    //Đào cây khỏi ô, dùng cho cả thao tác tại chỗ lẫn yêu cầu từ máy khách
    public void removePlantByShovel()
    {
        if (!havePlanted || nowPlant == null) return;
        nowPlant.GetComponent<Plant>().die("shovelPlant");
    }

    //Máy chủ báo cây trên ô này đã biến mất
    public void removePlantFromNetwork(string reason)
    {
        if (!havePlanted || nowPlant == null) return;
        nowPlant.GetComponent<Plant>().die(reason);
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

        //Máy chủ báo cho máy khách biết cây trên ô này đã mất
        NetGameplay.NotifyPlantRemoved(this, reason);

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
