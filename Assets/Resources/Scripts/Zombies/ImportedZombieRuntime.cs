using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ImportedZombieRuntime
{
    private const string Root = "Sprites/Imported/MarbleXu/Zombies/";
    private const string JiangNanRoot = "Sprites/Imported/JiangNan/Zombies/";

    private sealed class Profile
    {
        public string walk, attack, die;
        public float speed;
        public int health, damage;

        public Profile(string w, string a, string d, float s, int hp, int hit)
        {
            walk = w; attack = a; die = d; speed = s; health = hp; damage = hit;
        }
    }

    private static readonly Dictionary<string, Profile> Profiles = new Dictionary<string, Profile>(StringComparer.OrdinalIgnoreCase)
    {
        {"PoleVaultingZombie", new Profile("PoleVaultingZombie", "PoleVaultingZombieAttack", "PoleVaultingZombieDie", .32f, 500, 25)},
        {"FootballZombie", new Profile("FootballZombie", "Attack", "Die", .38f, 1400, 35)},
        {"ScreenDoorZombie", new Profile("ScreenDoorZombie", "ScreenDoorZombieAttack", "LostHeadWalk1", .18f, 1100, 25)},
        {"BalloonZombie", new Profile("Walk", "Attack", "Die", .24f, 450, 20)},
        {"JackinTheBoxZombie", new Profile("Walk", "Attack", "Boom", .26f, 500, 30)},
        {"DancingZombie", new Profile("DancingZombie", "Attack", "Die", .22f, 500, 25)},
        {"BackupDancer", new Profile("BackupDancer", "Attack", "Die", .22f, 300, 20)},
        {"DolphinRiderZombie", new Profile("Walk1", "Attack", "Die", .36f, 500, 30)},
        {"SnorkelZombie", new Profile("Walk1", "Attack", "Die", .24f, 500, 20)},
        {"Zomboni", new Profile("0", "1", "BoomDie", .26f, 1350, 45)},
        {"Imp", new Profile("Zombie", "ZombieAttack", "ZombieDie", .34f, 270, 20)},

        // --- ZOMBIE FUSION & HYBRID BIẾN THỂ ĐỘC ĐÁO ---
        {"GatlingZombie", new Profile("FootballZombie", "Attack", "Die", .20f, 1200, 30)},
        {"ConeBucketZombie", new Profile("FootballZombie", "Attack", "Die", .18f, 1850, 30)},
        {"FireImpZombie", new Profile("Zombie", "ZombieAttack", "ZombieDie", .42f, 320, 25)}
    };

    public static bool Supports(string key) =>
        key == "FlagZombie" || key == "NewspaperZombie" || Profiles.ContainsKey(key);

    public static GameObject Create(string key, Vector3 position, Transform parent)
    {
        if (!Supports(key)) return null;

        // Đồng bộ kích thước và điểm tiếp đất (chân) của toàn bộ zombie nhập ngoài với Zombie chuẩn (ZombieNormal: cao ~1.25m, chân ở y = -0.34f)
        Vector3 spawnPos = position;
        Vector3 spawnScale = Vector3.one;
        Vector2 colliderSize = new Vector2(0.56f, 0.95f);
        Vector2 colliderOffset = new Vector2(0.05f, 0.0f);
        float shadowLocalY = -0.55f;
        float shadowScaleX = 0.40f;
        float shadowScaleY = 0.20f;

        switch (key)
        {
            case "FlagZombie":
                // Cầm cờ: sprite MinFoot = -0.276. Ở scale 1.95, chân tại local Y = -0.538. Bù +0.198 để chân về đúng vạch -0.34
                spawnScale = new Vector3(1.95f, 1.95f, 1f);
                spawnPos.y += 0.20f;
                colliderSize = new Vector2(0.58f, 0.98f);
                colliderOffset = new Vector2(0.04f, 0.0f);
                shadowLocalY = -0.276f;
                shadowScaleX = 0.40f;
                shadowScaleY = 0.18f;
                break;

            case "NewspaperZombie":
                // Đọc báo: sprite MinFoot = -0.176 (ảnh crop sát đáy). Ở scale 1.85, chân tại local Y = -0.326.
                // Để chân tiếp đất thẳng hàng vạch -0.34, bù -0.014f
                spawnScale = new Vector3(1.85f, 1.85f, 1f);
                spawnPos.y -= 0.01f;
                colliderSize = new Vector2(0.62f, 0.98f);
                colliderOffset = new Vector2(0.02f, 0.0f);
                shadowLocalY = -0.176f;
                shadowScaleX = 0.40f;
                shadowScaleY = 0.18f;
                break;

            case "PoleVaultingZombie":
                // Nhảy sào: sprite MinFoot = -0.404. Ở scale 1.90, chân tại local Y = -0.768. Bù +0.428 để chân về đúng -0.34
                spawnScale = new Vector3(1.90f, 1.90f, 1f);
                spawnPos.y += 0.43f;
                colliderSize = new Vector2(0.60f, 0.98f);
                colliderOffset = new Vector2(0.05f, 0.0f);
                shadowLocalY = -0.404f;
                shadowScaleX = 0.44f;
                shadowScaleY = 0.18f;
                break;

            case "FootballZombie":
                // Bóng bầu dục: sprite MinFoot = -0.296. Ở scale 1.90, chân tại local Y = -0.562. Bù +0.222 để chân về đúng -0.34
                spawnScale = new Vector3(1.90f, 1.90f, 1f);
                spawnPos.y += 0.22f;
                colliderSize = new Vector2(0.65f, 1.02f);
                colliderOffset = new Vector2(0.04f, 0.0f);
                shadowLocalY = -0.296f;
                shadowScaleX = 0.45f;
                shadowScaleY = 0.20f;
                break;

            case "ScreenDoorZombie":
                // Cầm cửa: sprite MinFoot = -0.278. Ở scale 1.90, chân tại local Y = -0.528. Bù +0.188 để chân về đúng -0.34
                spawnScale = new Vector3(1.90f, 1.90f, 1f);
                spawnPos.y += 0.19f;
                colliderSize = new Vector2(0.64f, 0.98f);
                colliderOffset = new Vector2(0.04f, 0.0f);
                shadowLocalY = -0.278f;
                shadowScaleX = 0.42f;
                shadowScaleY = 0.18f;
                break;

            case "BalloonZombie":
                // Bóng bay: bay lơ lửng, bóng bay cao hơn mặt đất
                spawnScale = new Vector3(1.70f, 1.70f, 1f);
                spawnPos.y += 0.45f;
                colliderSize = new Vector2(0.58f, 0.98f);
                colliderOffset = new Vector2(0.0f, 0.0f);
                shadowLocalY = -0.465f;
                shadowScaleX = 0.38f;
                shadowScaleY = 0.16f;
                break;

            case "JackinTheBoxZombie":
                // Hộp hề: sprite MinFoot = -0.326. Ở scale 1.80, chân tại local Y = -0.587. Bù +0.247 để chân về đúng -0.34
                spawnScale = new Vector3(1.80f, 1.80f, 1f);
                spawnPos.y += 0.25f;
                colliderSize = new Vector2(0.58f, 0.94f);
                colliderOffset = new Vector2(0.0f, 0.0f);
                shadowLocalY = -0.326f;
                shadowScaleX = 0.40f;
                shadowScaleY = 0.18f;
                break;

            case "DancingZombie":
                // Vũ công disco: sprite MinFoot = -0.344. Ở scale 1.85, chân tại local Y = -0.636. Bù +0.296 để chân về đúng -0.34
                spawnScale = new Vector3(1.85f, 1.85f, 1f);
                spawnPos.y += 0.30f;
                colliderSize = new Vector2(0.58f, 0.98f);
                colliderOffset = new Vector2(0.0f, 0.0f);
                shadowLocalY = -0.344f;
                shadowScaleX = 0.40f;
                shadowScaleY = 0.18f;
                break;

            case "BackupDancer":
                // Vũ công phụ họa: sprite MinFoot = -0.324. Ở scale 1.80, chân tại local Y = -0.583. Bù +0.243 để chân về đúng -0.34
                spawnScale = new Vector3(1.80f, 1.80f, 1f);
                spawnPos.y += 0.24f;
                colliderSize = new Vector2(0.55f, 0.94f);
                colliderOffset = new Vector2(0.0f, 0.0f);
                shadowLocalY = -0.324f;
                shadowScaleX = 0.38f;
                shadowScaleY = 0.16f;
                break;

            case "DolphinRiderZombie":
                // Cưỡi cá heo: sprite MinFoot = -0.372. Ở scale 1.75, đáy tại local Y = -0.651. Bù +0.311 để chân về đúng -0.34
                spawnScale = new Vector3(1.75f, 1.75f, 1f);
                spawnPos.y += 0.31f;
                colliderSize = new Vector2(0.74f, 0.98f);
                colliderOffset = new Vector2(0.0f, 0.0f);
                shadowLocalY = -0.372f;
                shadowScaleX = 0.50f;
                shadowScaleY = 0.20f;
                break;

            case "SnorkelZombie":
                // Bơi lặn: sprite MinFoot = -0.340. Ở scale 1.80, chân tại local Y = -0.612. Bù +0.272 để chân về đúng -0.34
                spawnScale = new Vector3(1.80f, 1.80f, 1f);
                spawnPos.y += 0.27f;
                colliderSize = new Vector2(0.58f, 0.95f);
                colliderOffset = new Vector2(0.0f, 0.0f);
                shadowLocalY = -0.340f;
                shadowScaleX = 0.38f;
                shadowScaleY = 0.18f;
                break;

            case "Zomboni":
                // Xe dọn băng: sprite MinFoot = -0.384. Ở scale 1.25, bánh xe tại local Y = -0.480. Bù +0.140 để bánh xe chạm vạch -0.34
                spawnScale = new Vector3(1.25f, 1.25f, 1f);
                spawnPos.y += 0.14f;
                colliderSize = new Vector2(1.15f, 0.88f);
                colliderOffset = new Vector2(0.0f, -0.05f);
                shadowLocalY = -0.384f;
                shadowScaleX = 0.85f;
                shadowScaleY = 0.25f;
                break;

            case "Imp":
                // Quỷ lùn Imp: sprite MinFoot = -0.272. Ở scale 1.15, chân tại local Y = -0.313. Bù -0.027 để chân chạm vạch -0.34
                spawnScale = new Vector3(1.15f, 1.15f, 1f);
                spawnPos.y -= 0.03f;
                colliderSize = new Vector2(0.42f, 0.58f);
                colliderOffset = new Vector2(0.0f, -0.05f);
                shadowLocalY = -0.272f;
                shadowScaleX = 0.26f;
                shadowScaleY = 0.14f;
                break;

            case "FireImpZombie":
                // Quỷ lùn lửa: sprite MinFoot = -0.272. Ở scale 1.20, chân tại local Y = -0.326. Bù -0.014 để chân chạm vạch -0.34
                spawnScale = new Vector3(1.20f, 1.20f, 1f);
                spawnPos.y -= 0.01f;
                colliderSize = new Vector2(0.44f, 0.60f);
                colliderOffset = new Vector2(0.0f, -0.05f);
                shadowLocalY = -0.272f;
                shadowScaleX = 0.28f;
                shadowScaleY = 0.14f;
                break;

            case "GatlingZombie":
            case "ConeBucketZombie":
                // Zombie fusion: sprite 100 PPU, MinFoot = -0.905. Ở scale 0.88, chân tại local Y = -0.796. Bù +0.456 để chân về đúng -0.34
                spawnScale = new Vector3(0.88f, 0.88f, 1f);
                spawnPos.y += 0.46f;
                colliderSize = new Vector2(0.55f, 0.94f);
                colliderOffset = new Vector2(0.04f, 0.0f);
                shadowLocalY = -0.905f;
                shadowScaleX = 0.42f;
                shadowScaleY = 0.20f;
                break;

            default:
                spawnScale = new Vector3(1.80f, 1.80f, 1f);
                spawnPos.y += 0.22f;
                colliderSize = new Vector2(0.58f, 0.98f);
                colliderOffset = new Vector2(0.04f, 0.0f);
                shadowLocalY = -0.300f;
                shadowScaleX = 0.40f;
                shadowScaleY = 0.18f;
                break;
        }

        var go = new GameObject(key);
        go.tag = "Zombie";
        // Gán layer Zombie (Layer 9) để mọi hệ thống quét va chạm (Physics2D.Overlap / Linecast / Tag) nhận diện tuyệt đối
        int zombieLayer = LayerMask.NameToLayer("Zombie");
        go.layer = zombieLayer >= 0 ? zombieLayer : 9;
        go.transform.SetParent(parent, false);
        go.transform.position = spawnPos;
        go.transform.localScale = spawnScale;

        var renderer = go.AddComponent<SpriteRenderer>();
        var animator = go.AddComponent<RuntimeFrameAnimator>();
        var audioSource = go.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        var body = go.AddComponent<BoxCollider2D>();
        body.isTrigger = true;
        body.size = colliderSize;
        body.offset = colliderOffset;

        // Tạo bóng (Shadow) dưới chân cho mọi zombie nhập ngoài để chân tiếp đất chuẩn xác và tự nhiên
        var shadowGo = new GameObject("Shadow");
        shadowGo.transform.SetParent(go.transform, false);
        shadowGo.transform.localPosition = new Vector3(colliderOffset.x, shadowLocalY, 0f);
        // Tỉ lệ bóng bù trừ theo scale của zombie
        shadowGo.transform.localScale = new Vector3(shadowScaleX / spawnScale.x, shadowScaleY / spawnScale.y, 1f);
        var shadowSr = shadowGo.AddComponent<SpriteRenderer>();
        shadowSr.sprite = Resources.Load<Sprite>("Sprites/Items/Shadow");
        shadowSr.color = new Color(1f, 1f, 1f, 0.75f);
        shadowSr.sortingLayerName = "Default";
        shadowSr.sortingOrder = 0;

        var rigidbody = go.AddComponent<Rigidbody2D>();
        rigidbody.gravityScale = 0f;

        var zombie = go.AddComponent<ImportedZombie>();
        string idle, attack, die;
        float speed = .18f;
        int health = 420, damage = 20;

        if (Profiles.TryGetValue(key, out Profile profile))
        {
            if (key == "FireImpZombie")
            {
                string root = JiangNanRoot + "Imp/";
                idle = root + profile.walk;
                attack = root + profile.attack;
                die = root + profile.die;
            }
            else if (key == "GatlingZombie")
            {
                idle = "Sprites/Zombies/GatlingZombie/Walk";
                attack = "Sprites/Zombies/GatlingZombie/Attack";
                die = "Sprites/Imported/MarbleXu/Zombies/FlagZombie/FlagZombieLostHead";
            }
            else if (key == "ConeBucketZombie")
            {
                idle = "Sprites/Zombies/ConeBucketZombie/Walk";
                attack = "Sprites/Zombies/ConeBucketZombie/Attack";
                die = "Sprites/Imported/MarbleXu/Zombies/FlagZombie/FlagZombieLostHead";
            }
            else
            {
                string root = JiangNanRoot + key + "/";
                idle = root + profile.walk;
                attack = root + profile.attack;
                die = root + profile.die;
            }

            speed = profile.speed;
            health = profile.health;
            damage = profile.damage;
        }
        else
        {
            idle = Root + key + "/" + key;
            attack = Root + key + "/" + key + "Attack";
            die = key == "NewspaperZombie" ? Root + key + "/NewspaperZombieDie" : Root + key + "/FlagZombieLostHead";
            speed = key == "FlagZombie" ? .28f : .18f;
            health = key == "FlagZombie" ? 270 : 420;
        }

        zombie.Configure(key, animator, renderer, idle, attack, die, speed, health, damage);
        float animFps = (key == "ConeBucketZombie" || key == "GatlingZombie") ? 14f : 12f;
        animator.Configure(renderer, idle, animFps);

        // Thiết lập trực quan đặc thù cho Zombie Fusion
        if (key == "FireImpZombie")
        {
            renderer.color = new Color(1f, 0.45f, 0.25f, 1f);
            var trail = go.AddComponent<FireImpFlameAura>();
            trail.Initialize(renderer);
        }
        else if (key == "ConeBucketZombie")
        {
            renderer.color = Color.white;
        }
        else if (key == "GatlingZombie")
        {
            renderer.color = Color.white;
        }

        return go;
    }
}

public sealed class ImportedZombie : Zombie
{
    private string key, idlePath, attackPath, diePath;
    private RuntimeFrameAnimator frameAnimator;
    private SpriteRenderer spriteRenderer;
    private bool attacking;
    private float nextBite;

    // --- State cho từng loại kỹ năng ---
    // PoleVaulting
    private bool hasVaulted;
    private bool isVaulting;

    // JackinTheBox
    private bool boxExploding;
    private float boxTimer;

    // Dancing Zombie
    private float nextDanceTime;
    private bool isSummoningDancers;
    private readonly List<GameObject> activeBackupDancers = new List<GameObject>();

    // Zomboni
    private float nextSquashCheck;
    private float lastIceTrailDropX = float.MaxValue;
    private float nextZomboniSound;

    // Football Zombie
    private bool helmetLost;

    // Screen Door Zombie
    private int screenDoorHealth = 650;
    private bool screenDoorBroken;

    // Balloon Zombie
    private bool isFlying = true;

    // Gatling Zombie (Fusion)
    private float nextGatlingShot;
    private bool isShooting;

    public void Configure(string zombieKey, RuntimeFrameAnimator animator, SpriteRenderer renderer,
        string idle, string attackFrames, string deathFrames, float moveSpeed, int health, int damage)
    {
        key = zombieKey;
        frameAnimator = animator;
        spriteRenderer = renderer;
        idlePath = idle;
        attackPath = attackFrames;
        diePath = deathFrames;
        speed = moveSpeed;
        eatOffset = 0.45f;
        attackPower = damage;
        bloodVolume = health;
    }

    protected override void Start()
    {
        bloodVolumeMax = bloodVolume;
        nextBite = Time.time + 1f;

        if (key == "JackinTheBoxZombie")
        {
            // Hộp hề nổ ngẫu nhiên từ 10 đến 18 giây, hoặc khi chạm cụm cây
            boxTimer = Time.time + UnityEngine.Random.Range(10f, 18f);
        }
        else if (key == "DancingZombie")
        {
            nextDanceTime = Time.time + UnityEngine.Random.Range(4f, 7f);
        }
        else if (key == "GatlingZombie")
        {
            nextGatlingShot = Time.time + 2.5f;
        }
    }

    protected override void Update()
    {
        UpdateTimedStatusEffects();
        if (!alive) return;
        if (UpdateHypnotizedBehavior()) return;

        // Xử lý logic đặc thù riêng cho từng loại Zombie
        switch (key)
        {
            case "PoleVaultingZombie":
                UpdatePoleVaulting();
                break;
            case "JackinTheBoxZombie":
                UpdateJackinTheBox();
                break;
            case "DancingZombie":
                UpdateDancingZombie();
                break;
            case "Zomboni":
                UpdateZomboni();
                break;
            case "GatlingZombie":
                UpdateGatlingZombie();
                break;
        }

        if (isVaulting || boxExploding || isSummoningDancers) return;

        if (!attacking)
        {
            // Nếu đang trong chu kỳ nã đạn Gatling (isShooting) thì đứng vững để bắn, không bị trượt khi nạp đạn
            if (!isShooting)
            {
                transform.Translate(-speed * Time.deltaTime, 0f, 0f);
            }
        }
        else if (plant == null || plant.bloodVolume <= 0)
        {
            StopAttacking();
        }
        else if (Time.time >= nextBite)
        {
            PlayEatAudio();
            base.attack();
            if (plant == null || plant.bloodVolume <= 0)
            {
                StopAttacking();
            }
            nextBite = Time.time + 0.85f;
        }
    }

    #region Kỹ năng: Pole Vaulting Zombie (Nhảy sào)
    private void UpdatePoleVaulting()
    {
        if (hasVaulted || isVaulting || IsHypnotized) return;

        // Quét tìm xem phía trước có cây không (trong phạm vi 0.65 unit)
        Plant nearPlant = FindNearestPlantInFront(0.65f);
        if (nearPlant != null)
        {
            StartCoroutine(ExecutePoleVault(nearPlant));
        }
    }

    private IEnumerator ExecutePoleVault(Plant targetPlant)
    {
        isVaulting = true;
        hasVaulted = true;
        StopAttacking();

        PlayLocalSound("Sounds/Zombies/ImportedOriginal/polevault");

        // Diễn hoạt ảnh lấy đà nhảy
        frameAnimator.SetFrames("Sprites/Imported/JiangNan/Zombies/PoleVaultingZombie/PoleVaultingZombieJump");
        float jumpDuration = 0.65f;
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        // Điểm hạ cánh: vượt qua phía sau cây
        float landX = targetPlant != null ? targetPlant.transform.position.x - 0.75f : startPos.x - 1.4f;
        Vector3 endPos = new Vector3(landX, startPos.y, startPos.z);

        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / jumpDuration;
            float arcY = Mathf.Sin(t * Mathf.PI) * 0.75f;
            transform.position = new Vector3(Mathf.Lerp(startPos.x, endPos.x, t), startPos.y + arcY, startPos.z);
            yield return null;
        }

        transform.position = endPos;

        // Tiếp đất với hoạt họa Jump2
        frameAnimator.PlayOnce("Sprites/Imported/JiangNan/Zombies/PoleVaultingZombie/PoleVaultingZombieJump2", () =>
        {
            // Chuyển sang đi bộ bình thường và giảm tốc độ
            idlePath = "Sprites/Imported/JiangNan/Zombies/PoleVaultingZombie/PoleVaultingZombieWalk";
            attackPath = "Sprites/Imported/JiangNan/Zombies/PoleVaultingZombie/PoleVaultingZombieAttack";
            speed = 0.18f;
            frameAnimator.SetFrames(idlePath);
            isVaulting = false;
        });
    }
    #endregion

    #region Kỹ năng: Jack-in-the-Box Zombie (Hộp hề nổ)
    private void UpdateJackinTheBox()
    {
        if (boxExploding || !alive) return;

        bool hasCluster = HasNearbyPlantCluster();
        if (Time.time >= boxTimer || (hasCluster && Time.time >= boxTimer - 4f))
        {
            StartCoroutine(ExecuteJackinTheBoxExplosion());
        }
    }

    private bool HasNearbyPlantCluster()
    {
        var plants = FindObjectsByType<Plant>();
        int count = 0;
        foreach (var p in plants)
        {
            if (p != null && Mathf.Abs(p.row - pos_row) <= 1 && Mathf.Abs(p.transform.position.x - transform.position.x) <= 1.4f)
            {
                count++;
                if (count >= 2) return true;
            }
        }
        return false;
    }

    private IEnumerator ExecuteJackinTheBoxExplosion()
    {
        boxExploding = true;
        speed = 0f;
        PlayLocalSound("Sounds/Zombies/ImportedOriginal/jackinthebox");

        // Quay mở nắp hộp nhạc
        frameAnimator.SetFrames("Sprites/Imported/JiangNan/Zombies/JackinTheBoxZombie/OpenBox");
        yield return new WaitForSeconds(1.35f);

        if (!alive) yield break;

        PlayLocalSound("Sounds/Zombies/ImportedOriginal/jack_surprise");

        // Hoạt ảnh nổ Boom
        frameAnimator.PlayOnce("Sprites/Imported/JiangNan/Zombies/JackinTheBoxZombie/Boom", null);

        // Gây 1800 sát thương diện rộng (3x3 ô) lên tất cả các cây xung quanh
        var plants = FindObjectsByType<Plant>();
        foreach (var p in plants)
        {
            if (p != null && Mathf.Abs(p.row - pos_row) <= 1 && Mathf.Abs(p.transform.position.x - transform.position.x) <= 1.6f)
            {
                p.beAttacked(1800, "explosion");
            }
        }

        // Tự hủy
        alive = false;
        var collider = GetComponent<Collider2D>();
        if (collider != null) collider.enabled = false;
        var manager = GameObject.Find("Zombie Management");
        if (manager != null) manager.GetComponent<ZombieManagement>().minusZombieNumAll();

        yield return new WaitForSeconds(0.45f);
        Destroy(gameObject);
    }
    #endregion

    #region Kỹ năng: Dancing Zombie (Vũ công triệu hồi)
    private void UpdateDancingZombie()
    {
        if (isSummoningDancers || !alive || IsHypnotized) return;

        // Dọn dẹp danh sách vũ công đã chết
        activeBackupDancers.RemoveAll(d => d == null);

        if (Time.time >= nextDanceTime && activeBackupDancers.Count == 0)
        {
            StartCoroutine(SummonBackupDancers());
            nextDanceTime = Time.time + 12f;
        }
    }

    private IEnumerator SummonBackupDancers()
    {
        isSummoningDancers = true;
        StopAttacking();
        PlayLocalSound("Sounds/Zombies/ImportedOriginal/dancer");

        // Chuỗi động tác vẫy tay triệu hồi
        frameAnimator.SetFrames("Sprites/Imported/JiangNan/Zombies/DancingZombie/Summon1");
        yield return new WaitForSeconds(0.6f);
        frameAnimator.SetFrames("Sprites/Imported/JiangNan/Zombies/DancingZombie/Summon2");
        yield return new WaitForSeconds(0.6f);
        frameAnimator.SetFrames("Sprites/Imported/JiangNan/Zombies/DancingZombie/Summon3");
        yield return new WaitForSeconds(0.4f);

        if (!alive) yield break;

        // Tạo 4 vũ công ở 4 vị trí: trước, sau, hàng trên, hàng dưới
        int maxRow = GameManagement.levelData != null ? GameManagement.levelData.landRowCount : 5;
        var offsets = new List<Vector2Int>
        {
            new Vector2Int(0, 1),   // Hàng trên
            new Vector2Int(0, -1),  // Hàng dưới
            new Vector2Int(1, 0),   // Phía trước
            new Vector2Int(-1, 0)   // Phía sau
        };

        foreach (var off in offsets)
        {
            int targetRow = pos_row + off.y;
            if (targetRow < 0 || targetRow >= maxRow) continue;

            float yPos = GameManagement.levelData != null && targetRow < GameManagement.levelData.zombieInitPosY.Count
                ? GameManagement.levelData.zombieInitPosY[targetRow]
                : transform.position.y + off.y * 1.4f;

            Vector3 spawnPos = new Vector3(transform.position.x + off.x * 1.15f, yPos, transform.position.z);
            GameObject dancer = ImportedZombieRuntime.Create("BackupDancer", spawnPos, transform.parent);
            if (dancer != null)
            {
                Zombie dz = dancer.GetComponent<Zombie>();
                if (dz != null) dz.setPosRow(targetRow);

                // Hiệu ứng đất Mound trồi lên từ bãi cỏ
                StartCoroutine(PlayBackupDancerMoundEffect(dancer));
                activeBackupDancers.Add(dancer);
                GameObject.Find("Zombie Management")?.GetComponent<ZombieManagement>()?.addZombieNumAll();
            }
        }

        yield return new WaitForSeconds(0.5f);
        frameAnimator.SetFrames(idlePath);
        isSummoningDancers = false;
    }

    private IEnumerator PlayBackupDancerMoundEffect(GameObject dancer)
    {
        var dancerAnim = dancer.GetComponent<RuntimeFrameAnimator>();
        if (dancerAnim != null)
        {
            dancerAnim.SetFrames("Sprites/Imported/JiangNan/Zombies/BackupDancer/Mound");
            yield return new WaitForSeconds(0.9f);
            if (dancer != null && dancerAnim != null)
            {
                dancerAnim.SetFrames("Sprites/Imported/JiangNan/Zombies/BackupDancer/BackupDancer");
            }
        }
    }
    #endregion

    #region Kỹ năng: Zomboni (Xe dọn băng nghiền nát cây & Tạo đường băng)
    private void UpdateZomboni()
    {
        if (!alive || IsHypnotized) return;

        // Âm thanh máy chạy rền rĩ định kỳ
        if (Time.time >= nextZomboniSound)
        {
            nextZomboniSound = Time.time + 3.2f;
            PlayLocalSound("Sounds/Zombies/ImportedOriginal/zamboni");
        }

        // 1. Quét nghiền nát cây trên đường đi (hoặc nổ lốp xe nếu cán phải Spikeweed / Chông gai)
        if (Time.time >= nextSquashCheck)
        {
            nextSquashCheck = Time.time + 0.12f;
            var plants = FindObjectsByType<Plant>();
            foreach (var p in plants)
            {
                if (p != null && p.row == pos_row && Mathf.Abs(p.transform.position.x - transform.position.x) <= 0.75f)
                {
                    ImportedPlant imp = p.GetComponent<ImportedPlant>();
                    bool isSpikeweed = (imp != null && imp.Definition != null && imp.Definition.key == "Spikeweed") ||
                                      p.gameObject.name.IndexOf("Spikeweed", StringComparison.OrdinalIgnoreCase) >= 0;

                    if (isSpikeweed)
                    {
                        // Chông gai hy sinh đâm nổ lốp xe Zomboni lập tức!
                        p.die("squashed");
                        PopZomboniTire();
                        return;
                    }
                    else
                    {
                        p.die("squashed");
                        PlayLocalSound("Sounds/Zombies/ImportedOriginal/zamboni");
                    }
                }
            }
        }

        // 2. Tạo vệt băng (Ice Road) phía sau xe mỗi khi di chuyển được khoảng 0.75 unit
        float currentX = transform.position.x;
        if (lastIceTrailDropX == float.MaxValue || lastIceTrailDropX - currentX >= 0.75f)
        {
            // Bánh sau xe rải băng nằm ở x lùi lại sau thân xe (+0.45f)
            float trailX = currentX + 0.45f;
            ZomboniIceRoad.DropIcePatch(pos_row, trailX, transform.position.y);
            lastIceTrailDropX = currentX;
        }
    }

    private void PopZomboniTire()
    {
        if (!alive) return;
        PlayLocalSound("Sounds/Plants/frozen");
        PlayLocalSound("Sounds/Zombies/ImportedOriginal/jack_surprise");

        // Gây sát thương chí mạng phá hủy xe Zomboni
        beAttacked(bloodVolume + 100);
    }
    #endregion

    #region Kỹ năng: Gatling Zombie (Zombie Fusion súng đậu)
    private void UpdateGatlingZombie()
    {
        if (!alive || Time.time < nextGatlingShot || IsHypnotized || isShooting) return;

        // Chỉ khai hỏa khi phía trước trên cùng hàng có cây trồng của người chơi
        Plant targetPlant = FindNearestPlantInFront(8.5f);
        if (targetPlant == null && !attacking) return;

        // Bắn loạt 4 viên đạn đậu liên thanh cực mạnh (Gatling burst)
        nextGatlingShot = Time.time + 3.4f;
        StartCoroutine(FireGatlingPeas());
    }

    private IEnumerator FireGatlingPeas()
    {
        isShooting = true;

        // Khi chuẩn bị nã đạn: chuyển animation sang nòng súng nạp đạn và giật bắn (Shoot)
        if (!attacking && frameAnimator != null)
        {
            frameAnimator.SetFrames("Sprites/Zombies/GatlingZombie/Shoot");
        }

        // Delay 0.12s để mô phỏng động tác lên đạn / giương súng
        yield return new WaitForSeconds(0.12f);

        for (int i = 0; i < 4; i++)
        {
            if (!alive)
            {
                isShooting = false;
                yield break;
            }

            // Vị trí nòng súng Gatling: tính ngang tầm với độ cao ngực/đầu zombie và ngang tầm thân cây
            Vector3 muzzle = transform.position + new Vector3(-0.38f, 0.05f, 0f);
            ZombieHostilePea.Create(pos_row, muzzle, 25);
            PlayLocalSound("Sounds/Plants/firepea");
            yield return new WaitForSeconds(0.16f);
        }

        // Chờ thêm 0.15s cho hết chu kỳ giật nòng súng trước khi quay lại bước đi
        yield return new WaitForSeconds(0.15f);

        isShooting = false;

        // Sau khi bắn xong: quay lại trạng thái di chuyển Walk hoặc gặm nhấm Attack
        if (alive && frameAnimator != null)
        {
            frameAnimator.SetFrames(attacking ? attackPath : idlePath);
        }
    }
    #endregion

    #region Va chạm & Nhận sát thương
    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsHypnotized) return;

        Plant hit = collision.GetComponent<Plant>();
        ImportedPlant imported = hit != null ? hit.GetComponent<ImportedPlant>() : null;

        // Nếu là Zomboni thì nghiền nát chứ không dừng lại ăn (hoặc nổ lốp nếu gặp Spikeweed)
        if (key == "Zomboni" && hit != null && hit.row == pos_row)
        {
            ImportedPlant imp = hit.GetComponent<ImportedPlant>();
            bool isSpikeweed = (imp != null && imp.Definition != null && imp.Definition.key == "Spikeweed") ||
                              hit.gameObject.name.IndexOf("Spikeweed", StringComparison.OrdinalIgnoreCase) >= 0;
            if (isSpikeweed)
            {
                hit.die("squashed");
                PopZomboniTire();
            }
            else
            {
                hit.die("squashed");
                PlayLocalSound("Sounds/Zombies/ImportedOriginal/zamboni");
            }
            return;
        }

        // Balloon Zombie bay qua cây mà không ăn cho đến khi bị bắn hạ
        if (key == "BalloonZombie" && isFlying && hit != null)
        {
            return;
        }

        if (hit != null && (imported == null || imported.CanBeEaten) && hit.row == pos_row && collision.transform.position.x < transform.position.x + eatOffset)
        {
            plant = hit;
            attacking = true;
            nextBite = Time.time;
            frameAnimator.SetFrames(attackPath);
        }
        else if (collision.CompareTag("GameOverLine"))
        {
            GameObject.Find("Game Management")?.GetComponent<GameManagement>()?.gameOver();
        }
    }

    protected override void OnTriggerExit2D(Collider2D collision)
    {
        if (IsHypnotized) return;
        if (collision.GetComponent<Plant>() == plant) StopAttacking();
    }

    protected override void OnHypnotized()
    {
        StopAttacking();
    }

    private void StopAttacking()
    {
        plant = null;
        attacking = false;
        frameAnimator.SetFrames(idlePath);
    }

    public override void beAttacked(int hurt)
    {
        // Screen Door Zombie: Cửa lưới chặn hoàn toàn đạn phía trước cho đến khi cửa hỏng
        if (key == "ScreenDoorZombie" && !screenDoorBroken)
        {
            PlayLocalSound("Sounds/Zombies/ImportedOriginal/shieldhit");
            screenDoorHealth -= hurt;
            if (screenDoorHealth <= 0)
            {
                screenDoorBroken = true;
                idlePath = "Sprites/Imported/JiangNan/Zombies/ScreenDoorZombie/HeadWalk1";
                attackPath = "Sprites/Imported/JiangNan/Zombies/ScreenDoorZombie/HeadAttack1";
                frameAnimator.SetFrames(attacking ? attackPath : idlePath);
            }
            return;
        }

        // Balloon Zombie: Khi trúng đòn đầu tiên thì bóng bay vỡ, rơi xuống đất đi bộ bình thường
        if (key == "BalloonZombie" && isFlying)
        {
            isFlying = false;
            PlayLocalSound("Sounds/Zombies/ImportedOriginal/balloon_pop");
            frameAnimator.PlayOnce("Sprites/Imported/JiangNan/Zombies/BalloonZombie/Drop", () =>
            {
                idlePath = "Sprites/Imported/JiangNan/Zombies/BalloonZombie/Walk2";
                attackPath = "Sprites/Imported/JiangNan/Zombies/BalloonZombie/Attack2";
                frameAnimator.SetFrames(attacking ? attackPath : idlePath);
            });
        }

        base.beAttacked(hurt);

        // Football Zombie rụng mũ bảo hiểm khi dưới 450 HP
        if (key == "FootballZombie" && !helmetLost && alive && bloodVolume <= 450)
        {
            helmetLost = true;
            idlePath = "Sprites/Imported/JiangNan/Zombies/FootballZombie/OrnLost";
            attackPath = "Sprites/Imported/JiangNan/Zombies/FootballZombie/OrnLostAttack";
            frameAnimator.SetFrames(attacking ? attackPath : idlePath);
        }

        // ConeBucket Zombie: Rụng mũ bảo hộ khi còn 900 HP
        if (key == "ConeBucketZombie" && alive && bloodVolume <= 900)
        {
            if (spriteRenderer != null) spriteRenderer.color = Color.white;
        }

        // Báo nổi giận
        if (key == "NewspaperZombie" && alive && bloodVolume <= 200)
        {
            speed *= 1.8f;
            idlePath = "Sprites/Imported/MarbleXu/Zombies/NewspaperZombie/NewspaperZombieNoPaper";
            attackPath = "Sprites/Imported/MarbleXu/Zombies/NewspaperZombie/NewspaperZombieNoPaperAttack";
            frameAnimator.SetFrames(attacking ? attackPath : idlePath);
        }
    }

    protected override void die()
    {
        if (!alive) return;
        alive = false;

        var collider = GetComponent<Collider2D>();
        if (collider != null) collider.enabled = false;

        var manager = GameObject.Find("Zombie Management");
        if (manager != null) manager.GetComponent<ZombieManagement>().minusZombieNumAll();

        // Fire Imp Zombie khi chết bùng cháy thiêu đốt ô đất
        if (key == "FireImpZombie")
        {
            PlayLocalSound("Sounds/Plants/cherrybomb");
            var plants = FindObjectsByType<Plant>();
            foreach (var p in plants)
            {
                if (p != null && p.row == pos_row && Mathf.Abs(p.transform.position.x - transform.position.x) <= 0.65f)
                {
                    p.beAttacked(150, "burn");
                }
            }
        }

        // Zomboni hiệu ứng nổ khói to
        if (key == "Zomboni")
        {
            frameAnimator.PlayOnce("Sprites/Imported/JiangNan/Zombies/Zomboni/5", () => Destroy(gameObject));
            Destroy(gameObject, 3f);
            return;
        }

        frameAnimator.PlayOnce(diePath, () => Destroy(gameObject));
        Destroy(gameObject, 3.5f);
    }
    #endregion

    #region Helper Methods
    private Plant FindNearestPlantInFront(float maxDistance)
    {
        Plant[] plants = FindObjectsByType<Plant>();
        Plant nearest = null;
        float minDist = maxDistance;

        foreach (Plant p in plants)
        {
            if (p != null && p.row == pos_row && p.transform.position.x < transform.position.x &&
                (transform.position.x - p.transform.position.x) <= minDist)
            {
                minDist = transform.position.x - p.transform.position.x;
                nearest = p;
            }
        }
        return nearest;
    }

    private void PlayLocalSound(string path)
    {
        var clip = Resources.Load<AudioClip>(path);
        if (clip != null)
        {
            var audio = GetComponent<AudioSource>();
            if (audio != null) audio.PlayOneShot(clip, 0.8f);
            else AudioSource.PlayClipAtPoint(clip, transform.position);
        }
    }
    #endregion
}

/// <summary>
/// Hiệu ứng hào quang lửa rực cháy quanh người FireImpZombie.
/// </summary>
public sealed class FireImpFlameAura : MonoBehaviour
{
    private SpriteRenderer bodyRenderer;
    private float pulse;

    public void Initialize(SpriteRenderer renderer)
    {
        bodyRenderer = renderer;
    }

    private void Update()
    {
        if (bodyRenderer == null) return;
        pulse += Time.deltaTime * 8f;
        float flash = 0.85f + Mathf.Sin(pulse) * 0.15f;
        bodyRenderer.color = new Color(1f, 0.40f * flash, 0.20f, 1f);
    }
}

/// <summary>
/// Đạn đậu bay ngược về bên trái do GatlingZombie bắn ra nhằm phá hủy hàng rào thực vật.
/// </summary>
public sealed class ZombieHostilePea : MonoBehaviour
{
    private int row;
    private int damage;
    private const float Speed = 5.2f;
    private bool hasHit = false;

    public static ZombieHostilePea Create(int laneRow, Vector3 startPos, int peaDamage)
    {
        var go = new GameObject("HostilePea");
        go.transform.position = startPos;
        go.transform.localScale = Vector3.one * 0.95f;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Resources.Load<Sprite>("Sprites/PlantBullet/PeaBullet/PeaBullet");
        sr.color = new Color(0.95f, 0.35f, 0.95f, 1f); // Màu tím năng lượng bóng tối của Zombie
        sr.sortingLayerName = "PlantBullet";
        sr.sortingOrder = 50;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.28f;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        var pea = go.AddComponent<ZombieHostilePea>();
        pea.row = laneRow;
        pea.damage = peaDamage;
        Destroy(go, 5f);
        return pea;
    }

    private void Update()
    {
        if (hasHit) return;

        // Đạn bay thẳng sang trái
        transform.Translate(-Speed * Time.deltaTime, 0f, 0f);

        // 1. Quét va chạm bằng Physics2D Overlap
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.45f);
        foreach (var hit in hits)
        {
            if (hit == null) continue;
            Plant p = hit.GetComponent<Plant>();
            if (p != null && (p.row == row || Mathf.Abs(p.transform.position.y - transform.position.y) < 0.65f))
            {
                OnHitPlant(p);
                return;
            }
        }

        // 2. Quét dự phòng trực tiếp danh sách cây trên cùng làn
        Plant[] allPlants = FindObjectsByType<Plant>();
        foreach (var p in allPlants)
        {
            if (p == null || p.bloodVolume <= 0) continue;
            bool sameLane = (p.row == row) || (Mathf.Abs(p.transform.position.y - transform.position.y) < 0.65f);
            if (sameLane)
            {
                // Nếu đạn đã bay đến hoặc vượt qua cây (khoảng cách x nhỏ và y gần)
                float dx = transform.position.x - p.transform.position.x;
                float dy = Mathf.Abs(transform.position.y - p.transform.position.y);
                if (Mathf.Abs(dx) <= 0.48f && dy <= 0.65f)
                {
                    OnHitPlant(p);
                    return;
                }
            }
        }

        // Nếu bay qua khỏi góc trái màn hình thì biến mất
        if (transform.position.x < -7.5f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHit || collision == null) return;
        Plant p = collision.GetComponent<Plant>();
        if (p != null)
        {
            OnHitPlant(p);
        }
    }

    private void OnHitPlant(Plant target)
    {
        if (hasHit || target == null) return;
        hasHit = true;

        // Gây sát thương lên cây
        target.beAttacked(damage, "bullet");

        // Đặt vị trí hiệu ứng nổ vỡ đạn chính xác ngay tại vị trí cây trồng đang đứng
        Vector3 hitPosition = target.transform.position;
        // Bù thêm độ cao thân cây (khoảng +0.15f theo trục Y và mép phải cây) để hiệu ứng nở đạn tự nhiên ngay trước mặt/trên thân cây
        hitPosition.y += 0.15f;
        hitPosition.x += 0.15f;
        transform.position = hitPosition;

        // Phát âm thanh va chạm
        var clip = Resources.Load<AudioClip>("Sounds/Plants/splat");
        if (clip != null) AudioSource.PlayClipAtPoint(clip, hitPosition);

        // Hiển thị hiệu ứng vỡ đạn PeaBulletHit
        var hitSprite = Resources.Load<Sprite>("Sprites/PlantBullet/PeaBullet/PeaBulletHit");
        if (hitSprite != null)
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = hitSprite;
                sr.color = new Color(1f, 0.4f, 0.9f, 1f);
                transform.localScale = Vector3.one * 1.25f;
            }
            Destroy(gameObject, 0.14f);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}

/// <summary>
/// Quản lý vệt băng do Zomboni (xe dọn băng) rải ra trên sân cỏ.
/// Mỗi mẩu vệt băng tồn tại trong đúng 15 giây, ngăn chặn người chơi đặt cây mới vào ô đó.
/// Khi hết thời gian hoặc bị Ớt Jalapeno thiêu đốt, vệt băng sẽ tan dần và mở khóa lại ô đất.
/// </summary>
public sealed class ZomboniIceRoad : MonoBehaviour
{
    private static readonly List<ZomboniIceRoad> ActiveIcePatches = new List<ZomboniIceRoad>();
    private static Sprite cachedIceRoadSprite;

    public int Row { get; private set; }
    public float WorldX { get; private set; }
    private PlantGrid boundGrid;
    private float expireTime;
    private SpriteRenderer spriteRenderer;
    private bool isMelting;

    public static void DropIcePatch(int row, float worldX, float worldY)
    {
        // Giới hạn phạm vi sân cỏ (-5.2f đến 5.2f)
        if (worldX < -5.3f || worldX > 5.5f) return;

        // Tìm PlantGrid gần nhất trên cùng hàng
        PlantGrid nearestGrid = null;
        float minDist = float.MaxValue;
        var allGrids = UnityEngine.Object.FindObjectsByType<PlantGrid>();
        foreach (var g in allGrids)
        {
            if (g != null && g.row == row)
            {
                float dist = Mathf.Abs(g.transform.position.x - worldX);
                if (dist < minDist && dist <= 0.85f)
                {
                    minDist = dist;
                    nearestGrid = g;
                }
            }
        }

        // Tạo đối tượng Ice Road Patch
        var go = new GameObject("Zomboni_IceRoad_Patch");
        // Đặt cao độ vệt băng nằm ngay trên mặt cỏ ở chân hàng (y tương đương mặt ô đất)
        float patchY = nearestGrid != null ? nearestGrid.transform.position.y - 0.12f : worldY - 0.28f;
        go.transform.position = new Vector3(worldX, patchY, 0f);
        var ice = go.AddComponent<ZomboniIceRoad>();
        ice.Initialize(row, worldX, nearestGrid);
    }

    private static Sprite GetOrCreateIceRoadSprite()
    {
        if (cachedIceRoadSprite != null) return cachedIceRoadSprite;

        // Ưu tiên tạo sprite bề mặt băng đặc trưng chuẩn PvZ (kích thước dải băng rộng phủ trọn bề mặt ô cỏ)
        const int width = 128;
        const int height = 64;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = "Zomboni_IceRoad_Texture";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color baseIceColor = new Color(0.72f, 0.90f, 1.0f, 0.88f);       // Màu lam băng trong suốt sáng
        Color frostCoreColor = new Color(0.92f, 0.98f, 1.0f, 0.95f);     // Lõi trắng tuyết đông cứng
        Color edgeColor = new Color(0.60f, 0.82f, 0.98f, 0.0f);          // Rìa tan vào mặt cỏ

        for (int y = 0; y < height; y++)
        {
            float ny = (float)y / (height - 1); // 0 -> 1
            // Khoảng cách theo chiều dọc tới tâm (0 ở tâm, 1 ở mép trên/dưới)
            float distY = Mathf.Abs(ny - 0.5f) * 2f;

            for (int x = 0; x < width; x++)
            {
                float nx = (float)x / (width - 1);
                // Khoảng cách theo chiều ngang tới mép
                float distX = Mathf.Abs(nx - 0.5f) * 2f;

                // Độ mờ dần ở 4 mép dải băng
                float alphaEdgeX = Mathf.Clamp01((1f - distX) * 5f);
                float alphaEdgeY = Mathf.Clamp01((1f - distY) * 3.5f);
                float edgeFactor = alphaEdgeX * alphaEdgeY;

                // Vân băng lấp lánh ngẫu nhiên giả lập tinh thể băng tuyết
                float sparkle = Mathf.Sin(x * 0.45f) * Mathf.Cos(y * 0.55f) * 0.08f
                              + Mathf.Sin(x * 1.1f + y * 0.8f) * 0.05f;

                // Trộn giữa màu lõi tuyết và viền băng
                Color blend = Color.Lerp(frostCoreColor, baseIceColor, distY * 0.85f);
                blend.r = Mathf.Clamp01(blend.r + sparkle);
                blend.g = Mathf.Clamp01(blend.g + sparkle);
                blend.b = Mathf.Clamp01(blend.b + sparkle);
                blend.a = Mathf.Lerp(0.85f, 0.40f, distY) * edgeFactor;

                // Vết nứt băng nhẹ ở giữa tạo cảm giác dải băng chịu tải xe nặng
                if (Mathf.Abs(ny - 0.5f + Mathf.Sin(x * 0.2f) * 0.1f) < 0.04f && nx > 0.15f && nx < 0.85f)
                {
                    blend = Color.Lerp(blend, Color.white, 0.65f);
                    blend.a = Mathf.Max(blend.a, 0.92f);
                }

                texture.SetPixel(x, y, blend);
            }
        }
        texture.Apply();

        cachedIceRoadSprite = Sprite.Create(
            texture,
            new Rect(0, 0, width, height),
            new Vector2(0.5f, 0.5f),
            64f // Pixel per unit: 128 px / 64 = 2.0 unit chiều ngang
        );
        return cachedIceRoadSprite;
    }

    private void Initialize(int row, float worldX, PlantGrid grid)
    {
        Row = row;
        WorldX = worldX;
        boundGrid = grid;
        expireTime = Time.time + 15f; // Đúng 15 giây theo yêu cầu thiết kế

        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetOrCreateIceRoadSprite();

        // Đặt sorting layer thẳng theo hàng để nằm chuẩn trên mặt sân cỏ
        // TagManager: Plant-5..0 -> Zombie-5..0.
        // Gán sorting layer "Plant-" + row với sortingOrder = 0 để nằm trên nền đất nhưng dưới thân cây (sortingOrder cây >= 1)
        if (row >= 0 && row <= 5)
        {
            spriteRenderer.sortingLayerName = "Plant-" + row;
            spriteRenderer.sortingOrder = 0;
        }
        else
        {
            spriteRenderer.sortingLayerName = "Default";
            spriteRenderer.sortingOrder = 5;
        }

        // Tỉ lệ hiển thị dải băng nối liền trên ô cỏ: rộng 1.45f, cao 0.95f
        transform.localScale = new Vector3(1.45f, 0.95f, 1f);

        // Khóa không cho đặt cây vào ô này trong suốt 15 giây
        if (boundGrid != null)
        {
            boundGrid.SetPlantingBlocked(true);
        }

        ActiveIcePatches.Add(this);
    }

    private void Update()
    {
        if (isMelting) return;

        // Duy trì khóa ô liên tục
        if (boundGrid != null)
        {
            boundGrid.SetPlantingBlocked(true);
        }

        // Khi hết 15 giây, bắt đầu quá trình tan chảy
        if (Time.time >= expireTime)
        {
            StartCoroutine(MeltRoutine());
        }
    }

    public void MeltInstantly()
    {
        if (isMelting) return;
        StartCoroutine(MeltRoutine());
    }

    private System.Collections.IEnumerator MeltRoutine()
    {
        isMelting = true;
        ActiveIcePatches.Remove(this);

        // Mở khóa lại ô đất sau khi tan băng
        if (boundGrid != null)
        {
            // Kiểm tra xem còn mẩu vệt băng nào khác đè lên ô này không
            bool anotherIceOverGrid = false;
            foreach (var other in ActiveIcePatches)
            {
                if (other != null && other != this && other.boundGrid == boundGrid)
                {
                    anotherIceOverGrid = true;
                    break;
                }
            }
            if (!anotherIceOverGrid)
            {
                boundGrid.SetPlantingBlocked(false);
            }
        }

        // Hiệu ứng tan mờ dần và co ngót dải băng trong 1 giây
        float duration = 1.0f;
        float elapsed = 0f;
        Color initialColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        Vector3 initialScale = transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(initialColor.r, initialColor.g, initialColor.b, Mathf.Lerp(initialColor.a, 0f, t));
            }
            transform.localScale = Vector3.Lerp(initialScale, new Vector3(initialScale.x * 0.5f, initialScale.y * 0.1f, 1f), t);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        ActiveIcePatches.Remove(this);
        if (boundGrid != null && !isMelting)
        {
            boundGrid.SetPlantingBlocked(false);
        }
    }

    /// <summary>
    /// Làm tan chảy toàn bộ đường băng trên làn (khi Ớt Jalapeno kích nổ thiêu đốt).
    /// </summary>
    public static void ThawLane(int row)
    {
        var toThaw = new List<ZomboniIceRoad>();
        foreach (var patch in ActiveIcePatches)
        {
            if (patch != null && patch.Row == row)
            {
                toThaw.Add(patch);
            }
        }

        foreach (var patch in toThaw)
        {
            patch.MeltInstantly();
        }
    }
}


