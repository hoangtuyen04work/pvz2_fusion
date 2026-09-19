using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum HybridPlantKind
{
    PeaTorch,
    TorchSun,
    SunPea,
    SunflowerQueen
}

public readonly struct HybridPlantStats
{
    public readonly int Health;
    public readonly int ShotDamage;
    public readonly int Shots;
    public readonly int BurnDamage;
    public readonly float ShotInterval;
    public readonly float SunInterval;
    public readonly bool Shoots;
    public readonly bool CreatesSun;
    public readonly bool HasFire;

    public HybridPlantStats(int health, int shotDamage, int shots, int burnDamage,
        float shotInterval, float sunInterval, bool shoots, bool createsSun, bool hasFire)
    {
        Health = health;
        ShotDamage = shotDamage;
        Shots = shots;
        BurnDamage = burnDamage;
        ShotInterval = shotInterval;
        SunInterval = sunInterval;
        Shoots = shoots;
        CreatesSun = createsSun;
        HasFire = hasFire;
    }
}

public static class HybridPlantRuntime
{
    private const string SpriteRoot = "Sprites/Plants/Hybrids/";

    public static bool TryGetFusionResult(string currentPlant, string addedPlant, out string result)
    {
        result = null;
        if (string.IsNullOrEmpty(currentPlant) || string.IsNullOrEmpty(addedPlant)) return false;

        string current = CleanName(currentPlant);
        if (IsFinalEvolution(current)) return false;
        bool addPea = IsPea(addedPlant);
        bool addTorch = IsTorch(addedPlant);
        bool addSun = IsSun(addedPlant);

        if ((IsPea(current) && addTorch) || (IsTorch(current) && addPea)) result = "PeaTorch";
        else if ((IsTorch(current) && addSun) || (IsSun(current) && addTorch)) result = "TorchSun";
        else if ((IsSun(current) && addPea) || (IsPea(current) && addSun)) result = "SunPea";
        else if (current.Equals("PeaTorch", StringComparison.OrdinalIgnoreCase) && addSun) result = "SunflowerQueen";
        else if (current.Equals("TorchSun", StringComparison.OrdinalIgnoreCase) && addPea) result = "SunflowerQueen";
        else if (current.Equals("SunPea", StringComparison.OrdinalIgnoreCase) && addTorch) result = "SunflowerQueen";

        return result != null;
    }

    public static bool IsFinalEvolution(string plantName)
    {
        return !string.IsNullOrEmpty(plantName) &&
            CleanName(plantName).Equals("SunflowerQueen", StringComparison.OrdinalIgnoreCase);
    }

    public static GameObject Create(string key, Vector3 position, Transform parent)
    {
        if (!Enum.TryParse(key, true, out HybridPlantKind kind)) return null;

        GameObject plantObject = new GameObject(key);
        plantObject.tag = "Plant";
        plantObject.transform.SetParent(parent, false);
        plantObject.transform.position = position;

        SpriteRenderer renderer = plantObject.AddComponent<SpriteRenderer>();
        Sprite[] frames = LoadFrames(key);
        if (frames.Length == 0)
        {
            Debug.LogError("Missing hybrid animation frames for " + key, plantObject);
            UnityEngine.Object.Destroy(plantObject);
            return null;
        }
        renderer.sprite = frames[0];

        float scale = Mathf.Clamp(0.88f / renderer.sprite.bounds.size.y, 0.35f, 1f);
        plantObject.transform.localScale = new Vector3(scale, scale, 1f);

        BoxCollider2D collider = plantObject.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.62f / scale, 0.74f / scale);
        collider.offset = new Vector2(0f, 0.02f / scale);

        Rigidbody2D body = plantObject.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;

        plantObject.AddComponent<AudioSource>().playOnAwake = false;
        plantObject.AddComponent<Animator>();

        GameObject halo = new GameObject("Halo");
        halo.transform.SetParent(plantObject.transform, false);
        halo.SetActive(false);

        HybridFrameAnimator frameAnimator = plantObject.AddComponent<HybridFrameAnimator>();
        frameAnimator.Configure(renderer, frames);

        HybridPlant plant = plantObject.AddComponent<HybridPlant>();
        plant.Configure(kind, frameAnimator);
        return plantObject;
    }

    public static Sprite Preview(string key)
    {
        Sprite[] frames = LoadFrames(key);
        return frames.Length > 0 ? frames[0] : null;
    }

    public static HybridPlantStats Stats(HybridPlantKind kind)
    {
        switch (kind)
        {
            case HybridPlantKind.PeaTorch:
                return new HybridPlantStats(450, 30, 1, 14, 1.55f, 0f, true, false, true);
            case HybridPlantKind.TorchSun:
                return new HybridPlantStats(500, 0, 0, 14, 0f, 18f, false, true, true);
            case HybridPlantKind.SunPea:
                return new HybridPlantStats(350, 20, 1, 0, 1.7f, 24f, true, true, false);
            default:
                return new HybridPlantStats(900, 60, 3, 24, 1.35f, 12f, true, true, true);
        }
    }

    private static Sprite[] LoadFrames(string key)
    {
        return Resources.LoadAll<Sprite>(SpriteRoot + key + "/Frames")
            .OrderBy(sprite => ImportedPlantRuntime.NaturalIndex(sprite.name))
            .ToArray();
    }

    private static string CleanName(string value) => value.Replace("(Clone)", string.Empty).Trim();
    private static bool IsPea(string value) => CleanName(value).StartsWith("PeaShooter", StringComparison.OrdinalIgnoreCase);
    private static bool IsTorch(string value) => CleanName(value).StartsWith("TorchWood", StringComparison.OrdinalIgnoreCase);
    private static bool IsSun(string value) => CleanName(value).StartsWith("SunFlower", StringComparison.OrdinalIgnoreCase);
}

public sealed class HybridFrameAnimator : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private Sprite[] frames = Array.Empty<Sprite>();
    private float elapsed;
    private float actionStartedAt;
    private float actionUntil;
    private float speedMultiplier = 1f;

    public void Configure(SpriteRenderer target, Sprite[] animationFrames)
    {
        spriteRenderer = target;
        frames = animationFrames ?? Array.Empty<Sprite>();
        if (frames.Length > 0) spriteRenderer.sprite = frames[0];
    }

    public void PlayAction(float duration = 0.48f)
    {
        actionStartedAt = Time.time;
        actionUntil = Time.time + Mathf.Max(0.1f, duration / speedMultiplier);
        elapsed = 0f;
        if (spriteRenderer != null && frames.Length >= 8) spriteRenderer.sprite = frames[4];
    }

    public void SetSpeed(float multiplier) => speedMultiplier = Mathf.Max(0.1f, multiplier);

    private void Update()
    {
        if (spriteRenderer == null || frames.Length == 0) return;
        bool action = Time.time < actionUntil && frames.Length >= 8;
        int start = action ? 4 : 0;
        int count = Mathf.Min(4, frames.Length - start);
        if (action)
        {
            float duration = Mathf.Max(0.1f, actionUntil - actionStartedAt);
            float progress = Mathf.Clamp01((Time.time - actionStartedAt) / duration);
            spriteRenderer.sprite = frames[start + Mathf.Min(count - 1, Mathf.FloorToInt(progress * count))];
            return;
        }

        elapsed += Time.deltaTime;
        spriteRenderer.sprite = frames[Mathf.FloorToInt(elapsed * 6f * speedMultiplier) % count];
    }
}

public sealed class HybridPlant : Plant
{
    private const float LawnLeftEdge = -5.3f;
    private const float LawnRightEdge = 5.3f;

    private HybridPlantKind kind;
    private HybridPlantStats stats;
    private HybridFrameAnimator frameAnimator;
    private WarmPlantRegion warmRegion;
    private BoxCollider2D attackRegion;
    private Transform sunManagement;
    private GameObject sunPrefab;
    private GameObject peaPrefab;
    private GameObject firePeaPrefab;
    private ContactFilter2D zombieFilter;
    private readonly List<Collider2D> targetColliders = new List<Collider2D>();
    private readonly HashSet<Zombie> targetZombies = new HashSet<Zombie>();
    private float nextShot;
    private float nextSun;
    private float nextBurn;
    private float rateMultiplier = 1f;

    public void Configure(HybridPlantKind plantKind, HybridFrameAnimator animator)
    {
        kind = plantKind;
        stats = HybridPlantRuntime.Stats(kind);
        frameAnimator = animator;
        bloodVolume = stats.Health;
    }

    protected override void Start()
    {
        base.Start();
        nextShot = Time.time + 0.8f;
        nextSun = Time.time + 5f;
        nextBurn = Time.time + 1f;
        CreateAttackRegion();
        if (stats.Shoots)
        {
            peaPrefab = Resources.Load<GameObject>("Prefabs/PlantBullet/PeaBullet");
            firePeaPrefab = Resources.Load<GameObject>("Prefabs/PlantBullet/FirePea");
            if (kind == HybridPlantKind.SunPea && peaPrefab == null)
                Debug.LogError("SunPea requires Prefabs/PlantBullet/PeaBullet.", this);
            if ((kind == HybridPlantKind.PeaTorch || kind == HybridPlantKind.SunflowerQueen) && firePeaPrefab == null)
                Debug.LogError(kind + " requires Prefabs/PlantBullet/FirePea.", this);
        }
        if (stats.HasFire)
        {
            CreateWarmRegion();
            CreatePeaIgnitionRegion();
        }
        if (stats.CreatesSun)
        {
            GameObject manager = GameObject.Find("Sun Management");
            sunManagement = manager != null ? manager.transform : null;
            sunPrefab = Resources.Load<GameObject>("Prefabs/Sun/FlowerSun");
            if (sunManagement == null || sunPrefab == null)
                Debug.LogError(kind + " requires Sun Management and Prefabs/Sun/FlowerSun.", this);
        }
    }

    private void Update()
    {
        if (stats.Shoots && Time.time >= nextShot) TryShoot();
        if (stats.CreatesSun && Time.time >= nextSun) CreateSun();
        if (stats.HasFire && Time.time >= nextBurn) BurnNearby();
    }

    private void TryShoot()
    {
        RefreshTargets();
        if (kind == HybridPlantKind.SunflowerQueen)
        {
            if (!ShootQueenVolley())
            {
                nextShot = Time.time + 0.2f;
                return;
            }

            frameAnimator.PlayAction(0.8f);
            nextShot = Time.time + stats.ShotInterval / rateMultiplier;
            return;
        }

        Zombie target = null;
        foreach (Zombie candidate in targetZombies)
        {
            if (candidate.pos_row != row || candidate.transform.position.x < transform.position.x - 0.2f ||
                candidate.transform.position.x > LawnRightEdge) continue;
            if (IsPreferredTarget(candidate, target)) target = candidate;
        }

        if (target == null)
        {
            nextShot = Time.time + 0.2f;
            return;
        }

        GameObject projectilePrefab = kind == HybridPlantKind.PeaTorch ? firePeaPrefab : peaPrefab;
        SpawnProjectile(projectilePrefab, row, transform.position.y + 0.14f, stats.ShotDamage, 0f);

        frameAnimator.PlayAction();
        nextShot = Time.time + stats.ShotInterval / rateMultiplier;
    }

    private bool ShootQueenVolley()
    {
        if (firePeaPrefab == null || targetZombies.Count == 0) return false;

        Zombie target = null;
        foreach (Zombie zombie in targetZombies)
        {
            if (!IsValidForwardTarget(zombie)) continue;
            if (IsPreferredTarget(zombie, target)) target = zombie;
        }
        if (target == null) return false;

        for (int shot = 0; shot < stats.Shots; shot++)
            SpawnQueenHomingProjectile(target, shot);
        return true;
    }

    internal Zombie AcquireQueenHomingTarget()
    {
        RefreshTargets();
        Zombie target = null;
        foreach (Zombie candidate in targetZombies)
        {
            if (!IsValidForwardTarget(candidate)) continue;
            if (IsPreferredTarget(candidate, target)) target = candidate;
        }
        return target;
    }

    private void SpawnQueenHomingProjectile(Zombie target, int shotIndex)
    {
        Vector3 muzzle = transform.position + new Vector3(0.3f, 0.22f, 0f);
        GameObject projectile = Instantiate(firePeaPrefab, muzzle, Quaternion.identity);

        StraightBullet straightBullet = projectile.GetComponent<StraightBullet>();
        if (straightBullet != null) straightBullet.enabled = false;

        Collider2D projectileCollider = projectile.GetComponent<Collider2D>();
        if (projectileCollider != null) projectileCollider.enabled = false;

        Transform shadow = projectile.transform.Find("Shadow");
        if (shadow != null) shadow.gameObject.SetActive(false);

        float launchY = shotIndex == 0 ? 0.85f : shotIndex == 1 ? 0.35f : -0.12f;
        QueenHomingFirePea homing = projectile.AddComponent<QueenHomingFirePea>();
        homing.Initialize(this, target, stats.ShotDamage, new Vector2(1f, launchY));
    }

    private bool IsValidForwardTarget(Zombie zombie)
    {
        return zombie != null && zombie.bloodVolume > 0 &&
            zombie.transform.position.x >= LawnLeftEdge &&
            zombie.transform.position.x <= LawnRightEdge;
    }

    private static bool IsPreferredTarget(Zombie candidate, Zombie current)
    {
        if (current == null) return true;
        float deltaX = candidate.transform.position.x - current.transform.position.x;
        return deltaX < -0.001f || (Mathf.Abs(deltaX) <= 0.001f && candidate.pos_row < current.pos_row);
    }

    private void SpawnProjectile(GameObject projectilePrefab, int targetRow, float worldY, int damage, float trailOffset)
    {
        if (projectilePrefab == null) return;
        GameObject projectile = Instantiate(
            projectilePrefab,
            new Vector3(transform.position.x + 0.38f - trailOffset, worldY, transform.position.z),
            Quaternion.identity);
        StraightBullet bullet = projectile.GetComponent<StraightBullet>();
        if (bullet != null) bullet.initialize(targetRow, damage);
        else Destroy(projectile);
    }

    private void CreateSun()
    {
        frameAnimator.PlayAction(0.7f);
        if (NetSession.IsAuthority && sunPrefab != null && sunManagement != null)
            Instantiate(sunPrefab, transform.position + new Vector3(0f, 0.16f, 0f), Quaternion.identity, sunManagement);
        nextSun = Time.time + stats.SunInterval / rateMultiplier;
    }

    private void BurnNearby()
    {
        RefreshTargets();
        foreach (Zombie zombie in targetZombies)
        {
            if (zombie == null || zombie.pos_row != row || zombie.bloodVolume <= 0) continue;
            if (Mathf.Abs(zombie.transform.position.x - transform.position.x) <= 1.05f)
                zombie.applyBurn(stats.BurnDamage, 1.2f);
        }
        nextBurn = Time.time + 1f;
    }

    private void CreateAttackRegion()
    {
        float scale = Mathf.Max(0.01f, transform.localScale.x);
        float worldWidth = Mathf.Max(0.6f, LawnRightEdge - transform.position.x + 0.3f);
        float worldCenterX = transform.position.x + worldWidth * 0.5f - 0.15f;
        float worldCenterY = transform.position.y;
        float worldHeight = 0.62f;
        if (kind == HybridPlantKind.SunflowerQueen)
        {
            worldWidth = LawnRightEdge - LawnLeftEdge;
            worldCenterX = (LawnLeftEdge + LawnRightEdge) * 0.5f;
            if (GameManagement.levelData != null && GameManagement.levelData.zombieInitPosY != null &&
                GameManagement.levelData.zombieInitPosY.Count > 0)
            {
                float minY = GameManagement.levelData.zombieInitPosY[0];
                float maxY = minY;
                foreach (float laneY in GameManagement.levelData.zombieInitPosY)
                {
                    minY = Mathf.Min(minY, laneY);
                    maxY = Mathf.Max(maxY, laneY);
                }
                worldCenterY = (minY + maxY) * 0.5f;
                worldHeight = maxY - minY + 0.9f;
            }
        }

        GameObject region = new GameObject("HybridAttackRegion");
        region.transform.SetParent(transform, false);
        region.transform.localPosition = new Vector3(
            (worldCenterX - transform.position.x) / scale,
            (worldCenterY - transform.position.y) / scale,
            0f);
        attackRegion = region.AddComponent<BoxCollider2D>();
        attackRegion.isTrigger = true;
        attackRegion.size = new Vector2(worldWidth / scale, worldHeight / scale);

        zombieFilter = ContactFilter2D.noFilter;
        zombieFilter.SetLayerMask(LayerMask.GetMask("Zombie"));
        zombieFilter.useTriggers = true;
    }

    private void RefreshTargets()
    {
        targetColliders.Clear();
        targetZombies.Clear();
        if (attackRegion == null) return;

        attackRegion.Overlap(zombieFilter, targetColliders);
        foreach (Collider2D targetCollider in targetColliders)
        {
            if (targetCollider == null) continue;
            Zombie zombie = targetCollider.GetComponent<Zombie>();
            if (zombie == null) zombie = targetCollider.GetComponentInParent<Zombie>();
            if (zombie != null && !zombie.IsHypnotized && zombie.enabled && zombie.gameObject.activeInHierarchy && zombie.bloodVolume > 0)
                targetZombies.Add(zombie);
        }
    }

    private void CreateWarmRegion()
    {
        GameObject region = new GameObject("WarmPlantRegion");
        region.transform.SetParent(transform, false);
        BoxCollider2D area = region.AddComponent<BoxCollider2D>();
        area.isTrigger = true;
        float inverseScale = 1f / Mathf.Max(0.01f, transform.localScale.x);
        area.size = new Vector2(2.15f * inverseScale, 0.72f * inverseScale);
        warmRegion = region.AddComponent<WarmPlantRegion>();
    }

    private void CreatePeaIgnitionRegion()
    {
        GameObject region = new GameObject("FirePeaIgnitionRegion");
        region.transform.SetParent(transform, false);

        float inverseScale = 1f / Mathf.Max(0.01f, transform.localScale.x);
        BoxCollider2D collider = region.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(0.42f * inverseScale, 0.68f * inverseScale);
        collider.offset = new Vector2(0.04f * inverseScale, -0.1f * inverseScale);

        region.AddComponent<FireWallNutPeaIgniter>().initialize(row, Mathf.Max(30, stats.ShotDamage));
    }

    public override void cold()
    {
        base.cold();
        rateMultiplier = 0.5f;
        frameAnimator.SetSpeed(0.5f);
    }

    public override void warm()
    {
        base.warm();
        rateMultiplier = 1f;
        frameAnimator.SetSpeed(1f);
    }

    public override void normal()
    {
        base.normal();
        rateMultiplier = 1f;
        frameAnimator.SetSpeed(1f);
    }

    protected override void intensify_specific()
    {
        base.intensify_specific();
        rateMultiplier = 1.5f;
        frameAnimator.SetSpeed(1.5f);
    }

    protected override void cancelIntensify_specific()
    {
        base.cancelIntensify_specific();
        rateMultiplier = 1f;
        frameAnimator.SetSpeed(1f);
    }

    protected override void beforeDie()
    {
        if (warmRegion != null) warmRegion.stopWarm();
    }
}

public sealed class QueenHomingFirePea : MonoBehaviour
{
    private const float Lifetime = 7f;
    private const float HitDistance = 0.24f;
    private const float InitialSpeed = 3.2f;
    private const float HomingSpeed = 5.8f;

    private HybridPlant owner;
    private Zombie target;
    private Animator projectileAnimator;
    private AudioSource projectileAudio;
    private Vector2 direction;
    private int damage;
    private float age;
    private bool impacted;

    public void Initialize(HybridPlant projectileOwner, Zombie initialTarget, int shotDamage, Vector2 launchDirection)
    {
        owner = projectileOwner;
        target = initialTarget;
        damage = shotDamage;
        direction = launchDirection.sqrMagnitude > 0.001f ? launchDirection.normalized : Vector2.right;
        projectileAnimator = GetComponent<Animator>();
        projectileAudio = GetComponent<AudioSource>();
        FaceDirection();
    }

    private void Update()
    {
        if (impacted) return;

        age += Time.deltaTime;
        if (age >= Lifetime)
        {
            Destroy(gameObject);
            return;
        }

        if (!IsValidTarget(target))
        {
            target = owner != null ? owner.AcquireQueenHomingTarget() : null;
            if (target == null)
            {
                Destroy(gameObject);
                return;
            }
        }

        Vector2 targetPoint = (Vector2)target.transform.position + new Vector2(0f, 0.12f);
        Vector2 toTarget = targetPoint - (Vector2)transform.position;
        if (toTarget.sqrMagnitude <= HitDistance * HitDistance)
        {
            ImpactTarget();
            return;
        }

        float homingStrength = age < 0.16f ? 1.25f : 8.5f;
        float blend = 1f - Mathf.Exp(-homingStrength * Time.deltaTime);
        direction = Vector2.Lerp(direction, toTarget.normalized, blend).normalized;
        float speed = Mathf.Lerp(InitialSpeed, HomingSpeed, Mathf.Clamp01(age / 0.35f));
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
        FaceDirection();
    }

    private static bool IsValidTarget(Zombie candidate)
    {
        return candidate != null && candidate.isActiveAndEnabled && candidate.bloodVolume > 0;
    }

    private void FaceDirection()
    {
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void ImpactTarget()
    {
        impacted = true;
        if (IsValidTarget(target))
        {
            target.playAudioOfBeingAttacked();
            target.beAttacked(damage);
            target.beBurned();
        }

        if (projectileAnimator != null) projectileAnimator.SetBool("Boom", true);
        if (projectileAudio != null) projectileAudio.Play();
        Invoke(nameof(disappear), 0.55f);
    }

    public void disappear()
    {
        if (gameObject != null) Destroy(gameObject);
    }
}
