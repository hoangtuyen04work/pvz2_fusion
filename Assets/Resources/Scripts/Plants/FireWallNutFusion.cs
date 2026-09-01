using UnityEngine;

// Thanh phan fusion: hinh anh thong nhat cua Oc Cho Lua va suc manh TorchWood.
public class FireWallNutFusion : MonoBehaviour
{
    public int biteBurnDamage = 35;
    public float biteBurnDuration = 4f;
    public int firePeaDamage = 30;

    private int row;
    private WarmPlantRegion warmRegion;
    private SpriteRenderer bodyRenderer;
    private Vector3 baseScale;

    private void Start()
    {
        row = GetComponent<Plant>().row;
        useFusionArtwork();
        cloneTorchwoodFlame();
        createWarmRegion();
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
        Sprite fusionSprite = Resources.Load<Sprite>("Sprites/Plants/FireWallNut/FireWallNut");
        if (fusionSprite != null) bodyRenderer.sprite = fusionSprite;
        baseScale = transform.localScale;
    }

    private void Update()
    {
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

    public void OnBitten(Zombie zombie)
    {
        zombie.applyBurn(biteBurnDamage, biteBurnDuration);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Pea")) return;
        GameObject firePea = Resources.Load<GameObject>("Prefabs/PlantBullet/FirePea");
        if (firePea == null) return;
        Instantiate(firePea, collision.transform.position, Quaternion.identity)
            .GetComponent<StraightBullet>().initialize(row, firePeaDamage);
        Destroy(collision.gameObject);
    }

    private void OnDestroy()
    {
        if (warmRegion != null) warmRegion.stopWarm();
    }
}
