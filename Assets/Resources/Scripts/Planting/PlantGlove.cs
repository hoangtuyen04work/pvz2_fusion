using UnityEngine;
using UnityEngine.UI;

// Handles the local-only "lift and move" interaction. A plant remains owned by
// its source grid until a valid target is clicked, so a cancelled move can never
// lose the plant.
public sealed class PlantGlove : MonoBehaviour
{
    public static PlantGlove Active { get; private set; }

    private GameObject cursor;
    private GameObject carryAnchor;
    private Image buttonImage;
    private PlantGrid sourceGrid;
    private GameObject carriedPlant;
    private bool enabledForMove;

    public bool IsEnabledForMove => enabledForMove;

    public static Sprite LoadIcon()
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>("Sprites/UI/Tools/PlantGlove");
        Sprite largest = null;
        float largestArea = 0f;
        foreach (Sprite sprite in sprites)
        {
            float area = sprite.rect.width * sprite.rect.height;
            if (area > largestArea)
            {
                largestArea = area;
                largest = sprite;
            }
        }
        return largest;
    }

    public void Configure(Image button)
    {
        buttonImage = button;
        CreateCursorIfNeeded();
        RefreshButton();
    }

    public void SelectGlove()
    {
        // Plant movement is deliberately disabled online until it has a network
        // message/authority path. This prevents desynchronising a multiplayer game.
        if (NetSession.IsOnline) return;

        if (enabledForMove)
        {
            CancelMove();
            return;
        }

        GameObject planting = GameObject.Find("Planting Management");
        PlantingManagement plantingManagement = planting != null ? planting.GetComponent<PlantingManagement>() : null;
        if (plantingManagement != null) plantingManagement.clearSelectedPlant();

        GameObject shovel = GameObject.Find("SelectedShovel");
        if (shovel != null) shovel.SetActive(false);

        enabledForMove = true;
        Active = this;
        CreateCursorIfNeeded();
        carryAnchor.SetActive(true);
        RefreshButton();
    }

    private void Update()
    {
        if (!enabledForMove) return;
        if (Camera.main != null && carryAnchor != null)
        {
            Vector3 position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            position.z = 0f;
            carryAnchor.transform.position = position;
        }

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Mouse1))
            CancelMove();
    }

    public bool HandleGridClick(PlantGrid target)
    {
        if (!enabledForMove || target == null) return false;

        if (carriedPlant == null)
        {
            if (!target.TryPickUpForGlove(out carriedPlant)) return true;
            sourceGrid = target;
            BeginCarry(carriedPlant);
            return true;
        }

        if (target == sourceGrid) return true;

        if (target.TryReceiveMovedPlant(carriedPlant))
        {
            sourceGrid.ReleasePlantForGlove(carriedPlant);
            EndCarry(carriedPlant);
            FinishMove();
            return true;
        }

        if (target.TryFuseGlovePlant(carriedPlant.name))
        {
            sourceGrid.ReleasePlantForGlove(carriedPlant);
            Plant plant = carriedPlant.GetComponent<Plant>();
            if (plant != null) plant.removeForFusion();
            else Destroy(carriedPlant);
            FinishMove();
        }
        return true;
    }

    public void CancelMove()
    {
        if (carriedPlant != null && sourceGrid != null)
        {
            carriedPlant.transform.SetParent(sourceGrid.transform, true);
            carriedPlant.transform.position = sourceGrid.transform.position + new Vector3(0f, 0f, 5f);
            EndCarry(carriedPlant);
        }
        FinishMove();
    }

    private void FinishMove()
    {
        carriedPlant = null;
        sourceGrid = null;
        enabledForMove = false;
        if (carryAnchor != null) carryAnchor.SetActive(false);
        if (Active == this) Active = null;
        RefreshButton();
    }

    private void BeginCarry(GameObject plant)
    {
        if (plant == null) return;
        CreateCursorIfNeeded();
        plant.transform.SetParent(carryAnchor.transform, true);
        plant.transform.localPosition = Vector3.zero;
        foreach (Collider2D collider in plant.GetComponentsInChildren<Collider2D>())
            collider.enabled = false;
    }

    private static void EndCarry(GameObject plant)
    {
        if (plant == null) return;
        foreach (Collider2D collider in plant.GetComponentsInChildren<Collider2D>())
            collider.enabled = true;
    }

    private void CreateCursorIfNeeded()
    {
        if (cursor != null) return;
        Sprite sprite = LoadIcon();
        carryAnchor = new GameObject("PlantGlove Carry Anchor");
        cursor = new GameObject("SelectedGlove", typeof(SpriteRenderer));
        cursor.transform.SetParent(carryAnchor.transform, false);
        SpriteRenderer renderer = cursor.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = "UI";
        renderer.sortingOrder = 200;
        cursor.transform.localScale = Vector3.one * 0.12f;
        cursor.transform.localPosition = new Vector3(0.28f, 0.30f, 0f);
        carryAnchor.SetActive(false);
    }

    private void RefreshButton()
    {
        if (buttonImage != null)
            buttonImage.color = enabledForMove ? new Color(0.55f, 0.85f, 1f, 1f) : Color.white;
    }

    private void OnDestroy()
    {
        if (carryAnchor != null) Destroy(carryAnchor);
        if (Active == this) Active = null;
    }
}
