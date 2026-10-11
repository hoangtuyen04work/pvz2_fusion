using System.Linq;
using UnityEngine;

// Dynamic fusion result: WallNut + IceShroom. The source art contains five
// left-to-right health states, so it does not rely on an Animator controller.
public sealed class IceWallNut : MonoBehaviour
{
    private const string SpritePath = "Sprites/Plants/IceWallNut/IceWallNutStates";
    private const int StateCount = 5;
    private SpriteRenderer baseRenderer;
    private SpriteRenderer artworkRenderer;
    private Sprite[] states;
    private Plant plant;
    private int lastHealth = -1;

    public static GameObject Create(Vector3 position, Transform parent)
    {
        GameObject wallNutPrefab = Resources.Load<GameObject>("Prefabs/Plants/WallNut");
        if (wallNutPrefab == null)
        {
            Debug.LogError("IceWallNut requires the WallNut prefab.");
            return null;
        }

        // Match the proven FireWallNut fusion path: retain the complete legacy
        // prefab (layer, renderer material, collider, animator and Plant state)
        // and add only the fusion-specific behaviour.
        GameObject result = Object.Instantiate(wallNutPrefab, position, Quaternion.identity, parent);
        result.name = "IceWallNut";
        result.AddComponent<IceWallNut>();
        return result;
    }

    public static void CreateFusionBurst(Vector3 position)
    {
        GameObject burst = new GameObject("IceWallNut Fusion Burst", typeof(ParticleSystem));
        burst.transform.position = position;
        ParticleSystem particles = burst.GetComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.startColor = new Color(0.45f, 0.88f, 1f, 0.9f);
        main.startSize = 0.11f;
        main.startLifetime = 0.45f;
        main.maxParticles = 32;
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 24) });
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.35f;
        Object.Destroy(burst, 1f);
    }

    private void Awake()
    {
        baseRenderer = GetComponent<SpriteRenderer>();
        plant = GetComponent<Plant>();

        // Never replace the proven WallNut renderer. It remains as a visible,
        // icy fallback while the fusion artwork is drawn by an isolated child
        // renderer. This also prevents WallNut's Animator from overwriting the
        // fusion sprite or a bad imported sprite from hiding the whole plant.
        baseRenderer.enabled = true;
        baseRenderer.color = new Color(0.48f, 0.82f, 1f, 1f);
        GameObject artwork = new GameObject("Ice Armor Artwork");
        artwork.layer = gameObject.layer;
        artwork.transform.SetParent(transform, false);
        artworkRenderer = artwork.AddComponent<SpriteRenderer>();

        states = Resources.LoadAll<Sprite>(SpritePath)
            .OrderBy(sprite => sprite.rect.x)
            .ToArray();
        if (states.Length == 0)
            states = CreateStatesFromTexture();

        if (states.Length > 0)
        {
            baseRenderer.enabled = false;
            artworkRenderer.enabled = true;
            artworkRenderer.sprite = states[0];
            artworkRenderer.color = Color.white;
            FitArtworkToPlant();
        }
        else
        {
            baseRenderer.enabled = true;
            artworkRenderer.enabled = false;
            Debug.LogWarning("IceWallNut states were not loaded; using the visible WallNut fallback.", this);
        }

        SyncArtworkRenderer();
        UpdateDamageSprite();
    }

    private void FitArtworkToPlant()
    {
        if (baseRenderer.sprite == null || artworkRenderer.sprite == null) return;
        float artworkHeight = artworkRenderer.sprite.bounds.size.y;
        if (artworkHeight <= 0f) return;
        const float targetHeight = 0.92f;
        float scale = targetHeight / artworkHeight;
        artworkRenderer.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private void SyncArtworkRenderer()
    {
        if (baseRenderer == null || artworkRenderer == null) return;
        artworkRenderer.sortingLayerID = baseRenderer.sortingLayerID;
        artworkRenderer.sortingOrder = baseRenderer.sortingOrder + 1;
        artworkRenderer.flipX = baseRenderer.flipX;
        artworkRenderer.flipY = baseRenderer.flipY;
    }

    private static Sprite[] CreateStatesFromTexture()
    {
        Texture2D texture = Resources.Load<Texture2D>(SpritePath);
        if (texture == null) return new Sprite[0];

        // The source sheet is 2172x724. Use normalized measurements so this
        // fallback also works when Unity scales the texture on import.
        const float sourceWidth = 2172f;
        const float sourceHeight = 724f;
        float stateWidth = texture.width * (400f / sourceWidth);
        float stateHeight = texture.height * (580f / sourceHeight);
        float gap = texture.width * (20f / sourceWidth);
        float bottom = texture.height * (80f / sourceHeight);
        Sprite[] result = new Sprite[StateCount];

        for (int index = 0; index < StateCount; index++)
        {
            Rect rect = new Rect(index * (stateWidth + gap), bottom, stateWidth, stateHeight);
            result[index] = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 500f);
            result[index].name = "IceWallNutState" + index;
        }

        Debug.LogWarning("IceWallNut sub-sprites were not imported; using runtime slices from the texture.");
        return result;
    }

    private void Start()
    {
        UpdateDamageSprite();
    }

    private void LateUpdate()
    {
        // initialize() assigns the grid's final sorting values after Awake.
        SyncArtworkRenderer();
        if (plant != null && plant.bloodVolume != lastHealth) UpdateDamageSprite();
    }

    private void UpdateDamageSprite()
    {
        if (plant == null || states == null || states.Length == 0 || plant.BloodVolumeMax <= 0) return;
        lastHealth = plant.bloodVolume;
        float health = Mathf.Clamp01((float)plant.bloodVolume / plant.BloodVolumeMax);
        int index = health > 0.75f ? 0 : health > 0.50f ? 1 : health > 0.25f ? 2 : health > 0f ? 3 : 4;
        artworkRenderer.sprite = states[Mathf.Min(index, states.Length - 1)];
    }

    // Called by Zombie.attack after every bite. A bitten zombie is chilled and
    // keeps its standard slow visual/state handling from Zombie.ApplySlow.
    public void OnBitten(Zombie zombie)
    {
        if (zombie != null) zombie.ApplySlow(0.5f, 3.5f);
    }
}
