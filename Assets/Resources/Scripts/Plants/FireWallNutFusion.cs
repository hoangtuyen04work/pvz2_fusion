using UnityEngine;

// Cây Óc Chó Lửa: thành phần cây lai, kết hợp độ bền Hạt Dẻ Tường và ngọn lửa Liễu Đuốc.
public class FireWallNutFusion : MonoBehaviour
{
    public int biteBurnDamage = 35;
    public float biteBurnDuration = 4f;
    public int firePeaDamage = 30;

    private int row;
    private WarmPlantRegion warmRegion;
    private SpriteRenderer bodyRenderer;
    private Vector3 baseScale;
    private Plant plant;
    private int maximumHealth;
    private int damageArtworkState = -1;

    private void Start()
    {
        plant = GetComponent<Plant>();
        row = plant.row;
        useFusionArtwork();
        cloneTorchwoodFlame();
        createWarmRegion();
        createPeaIgnitionRegion();
    }

    private void cloneTorchwoodFlame()
    {
        GameObject flame = new GameObject("Torchwood Flame (Original)");
        flame.transform.SetParent(transform, false);
        flame.transform.localPosition = new Vector3(-0.01f, 0.18f, -0.02f);
        flame.transform.localScale = Vector3.one * 1.13f;

        SpriteRenderer renderer = flame.AddComponent<SpriteRenderer>();
        renderer.sprite = Resources.Load<Sprite>("Sprites/Plants/TorchWood/Idle/Torchwood0001");
        renderer.sortingLayerName = bodyRenderer.sortingLayerName;
        renderer.sortingOrder = bodyRenderer.sortingOrder + 1;

        Shader flameShader = Resources.Load<Shader>("Shaders/TorchwoodFlameOnly");
        if (flameShader != null)
        {
            Material material = new Material(flameShader);
            material.SetFloat("_CutoffY", 0.64f);
            renderer.material = material;
        }

        Animator animator = flame.AddComponent<Animator>();
        animator.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>(
            "Animations/Plants/TorchWood/Torchwood");
    }

    private void useFusionArtwork()
    {
        Animator animator = GetComponent<Animator>();
        if (animator != null) animator.enabled = false;
        bodyRenderer = GetComponent<SpriteRenderer>();
        Sprite fusionSprite = Resources.Load<Sprite>("Sprites/Plants/FireWallNut/FireWallNutV2");
        if (fusionSprite != null) bodyRenderer.sprite = fusionSprite;
        baseScale = transform.localScale;
    }

    private void Update()
    {
        updateDamageArtwork();
        // Nhip tho nhe, giu than va lua la mot khoi thong nhat nhu tranh mau.
        float breathe = Mathf.Sin(Time.time * 3.2f);
        transform.localScale = new Vector3(
            baseScale.x * (1f - breathe * 0.012f),
            baseScale.y * (1f + breathe * 0.018f),
            baseScale.z);
        if (bodyRenderer != null && bodyRenderer.color.r > 0.95f)
        {
            float warmth = 0.96f + (breathe + 1f) * 0.02f;
            bodyRenderer.color = new Color(1f, warmth, warmth * 0.92f, bodyRenderer.color.a);
        }
    }

    private void updateDamageArtwork()
    {
        if (plant == null || bodyRenderer == null) return;
        if (maximumHealth <= 0) maximumHealth = plant.bloodVolume;
        if (maximumHealth <= 0) return;

        float healthRatio = plant.bloodVolume / (float)maximumHealth;
        int nextState = healthRatio <= 1f / 3f ? 2 : healthRatio <= 2f / 3f ? 1 : 0;
        if (nextState == damageArtworkState) return;

        string path = nextState == 0 ? "Sprites/Plants/FireWallNut/FireWallNutV2"
            : nextState == 1 ? "Sprites/Plants/FireWallNut/FireWallNutDamaged"
            : "Sprites/Plants/FireWallNut/FireWallNutCritical";
        Sprite artwork = Resources.Load<Sprite>(path);
        if (artwork != null)
        {
            bodyRenderer.sprite = artwork;
            damageArtworkState = nextState;
        }
    }
    private void createWarmRegion()
    {
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        if (body == null) body = gameObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        GameObject region = new GameObject("WarmPlantRegion");
        region.transform.SetParent(transform, false);
        BoxCollider2D collider = region.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(2.15f, 0.72f);
        warmRegion = region.AddComponent<WarmPlantRegion>();
    }

    private void createPeaIgnitionRegion()
    {
        GameObject region = new GameObject("FirePeaIgnitionRegion");
        region.transform.SetParent(transform, false);

        BoxCollider2D collider = region.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(0.42f, 0.68f);
        collider.offset = new Vector2(0.04f, -0.1f);

        region.AddComponent<FireWallNutPeaIgniter>().initialize(row, firePeaDamage);
    }

    public void OnBitten(Zombie zombie)
    {
        zombie.applyBurn(biteBurnDamage, biteBurnDuration);
    }

    private void OnDestroy()
    {
        if (warmRegion != null) warmRegion.stopWarm();
    }
}
// Vùng đốt đạn của Cây Óc Chó Lửa.
public class FireWallNutPeaIgniter : MonoBehaviour
{
    private int row;
    private int firePeaDamage;

    public void initialize(int row, int damage)
    {
        this.row = row;
        firePeaDamage = damage;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        ImportedProjectile imported = collision.GetComponent<ImportedProjectile>();
        if (imported != null)
        {
            imported.PassThroughTorchwood(row, firePeaDamage, this);
            return;
        }
        if (!collision.CompareTag("Pea")) return;

        StraightBullet pea = collision.GetComponent<StraightBullet>();
        if (pea == null || pea.Row != row) return;

        GameObject firePea = Resources.Load<GameObject>("Prefabs/PlantBullet/FirePea");
        if (firePea == null) return;

        Instantiate(firePea, collision.transform.position, Quaternion.identity)
            .GetComponent<StraightBullet>().initialize(row, firePeaDamage);
        Destroy(collision.gameObject);
    }
}
