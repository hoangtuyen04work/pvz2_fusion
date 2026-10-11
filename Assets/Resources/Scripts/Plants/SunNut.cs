using UnityEngine;

// Cây Óc Chó Mặt Trời: phòng thủ, tạo nắng chậm và rơi Mini Sun khi chịu sát thương.
public class SunNut : Plant
{
    public GameObject flowersunPrefab;

    private const float SunInterval = 36f;
    private const int MiniSunDamageStep = 100;

    private Transform sunManagement;
    private SpriteRenderer spriteRenderer;
    private int maximumHealth;
    private int nextMiniSunHealth;

    protected override void Awake()
    {
        base.Awake();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // SunNut uses its own damage-state sprites. The SunFlower animator in the
        // prefab would otherwise overwrite them every frame.
        Animator animator = GetComponent<Animator>();
        if (animator != null) animator.enabled = false;
    }

    protected override void Start()
    {
        base.Start();
        maximumHealth = bloodVolume;
        nextMiniSunHealth = maximumHealth - MiniSunDamageStep;

        GameObject sunManagementObject = GameObject.Find("Sun Management");
        sunManagement = sunManagementObject != null ? sunManagementObject.transform : null;
        if (sunManagement == null)
        {
            Debug.LogWarning("SunNut could not find Sun Management; sun production is disabled.", this);
        }

        updateAppearance();
        InvokeRepeating(nameof(createSun), 7f, SunInterval);
    }

    public override int beAttacked(int hurt, string form)
    {
        if (intensified) hurt = (int)(hurt * 0.75f);

        int health = base.beAttacked(hurt, form);
        while (health > 0 && health <= nextMiniSunHealth)
        {
            createMiniSun();
            nextMiniSunHealth -= MiniSunDamageStep;
        }

        updateAppearance();
        return health;
    }

    private void createSun()
    {
        if (flowersunPrefab == null || sunManagement == null) return;

        Instantiate(flowersunPrefab, transform.position, Quaternion.identity, sunManagement);
        if (audioSource != null) audioSource.Play();
    }

    private void createMiniSun()
    {
        if (flowersunPrefab == null || sunManagement == null) return;

        GameObject miniSun = Instantiate(flowersunPrefab, transform.position, Quaternion.identity, sunManagement);
        SunBase sun = miniSun.GetComponent<SunBase>();
        if (sun != null) sun.sunNumber = 15;
    }

    private void updateAppearance()
    {
        if (spriteRenderer == null || maximumHealth <= 0 || bloodVolume <= 0) return;

        float healthRatio = bloodVolume / (float)maximumHealth;
        string state = healthRatio <= 1f / 3f
            ? "SunNut/States/SunNut2"
            : healthRatio <= 2f / 3f
                ? "SunNut/States/SunNut1"
                : "SunNut/States/SunNut0";
        Sprite sprite = loadLargestSprite("Sprites/Plants/" + state);
        if (sprite != null)
        {
            spriteRenderer.sprite = sprite;
        }
        else
        {
            Debug.LogError("SunNut could not load appearance sprite: " + state, this);
        }
    }

    private static Sprite loadLargestSprite(string resourcePath)
    {
        Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
        Sprite largestSprite = null;
        float largestArea = 0f;

        foreach (Sprite candidate in sprites)
        {
            float area = candidate.rect.width * candidate.rect.height;
            if (area > largestArea)
            {
                largestArea = area;
                largestSprite = candidate;
            }
        }

        return largestSprite;
    }

    protected override void beforeDie()
    {
        CancelInvoke();
    }
}