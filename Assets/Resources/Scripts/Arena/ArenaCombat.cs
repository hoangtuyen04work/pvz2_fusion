using System.Collections;
using UnityEngine;

/// <summary>
/// Lửa phun ra thành một nắm ba tia loe. Sát thương lẻ nhưng bắn được liên tục,
/// bù lại tầm rất ngắn nên phải áp sát mới ăn.
/// </summary>
public sealed class ArenaFlame : MonoBehaviour
{
    private const float Speed = 8.4f;
    private const int Damage = 10;
    private const float Lifetime = 0.4f;
    private const float HitRadius = 0.92f;

    private GargantuarArenaGame game;
    private Vector2 direction;
    private float bornAt;
    private bool spent;

    public static void Spawn(GargantuarArenaGame owner, Vector3 origin, Vector2 facing)
    {
        //Ba tia lệch nhau để thành hình nón
        float[] offsets = { -17f, 0f, 17f };
        foreach (float degrees in offsets)
        {
            Vector2 aim = Quaternion.Euler(0f, 0f, degrees) * facing;
            var go = new GameObject("Flame", typeof(SpriteRenderer), typeof(ArenaFlame));
            go.transform.position = origin;
            go.transform.localScale = Vector3.one * 1.5f;

            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = Resources.Load<Sprite>("Sprites/PlantBullet/FirePea/FirePea0001");
            renderer.sortingOrder = 48;
            renderer.color = new Color(1f, 0.86f, 0.5f, 0.95f);

            go.GetComponent<ArenaFlame>().Initialize(owner, aim);
        }
    }

    private void Initialize(GargantuarArenaGame owner, Vector2 aim)
    {
        game = owner;
        direction = aim.normalized;
        bornAt = Time.time;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Update()
    {
        float age = Time.time - bornAt;
        if (age >= Lifetime)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += (Vector3)(direction * Speed * Time.deltaTime);

        //Tia lửa loãng dần rồi tắt
        var renderer = GetComponent<SpriteRenderer>();
        renderer.color = new Color(1f, Mathf.Lerp(0.86f, 0.34f, age / Lifetime), 0.3f, 1f - age / Lifetime);
        transform.localScale = Vector3.one * Mathf.Lerp(1.5f, 2.6f, age / Lifetime);

        if (spent || game == null) return;
        var boss = game.Boss;
        if (boss != null && boss.CanBeHit && Vector2.Distance(transform.position, boss.HitCenter) < HitRadius)
        {
            spent = true;
            boss.TakeDamage(Damage);
        }
    }
}

/// <summary>
/// Dao lá bay xuyên: sát thương nặng, nạp lâu, và không biến mất khi trúng
/// nên xuyên tiếp về phía sau.
/// </summary>
public sealed class ArenaKnife : MonoBehaviour
{
    private const float Speed = 13.5f;
    private const int Damage = 45;
    private const float HitRadius = 0.85f;

    private GargantuarArenaGame game;
    private Vector2 direction;
    private float nextHitTime;

    public static void Spawn(GargantuarArenaGame owner, Vector3 origin, Vector2 facing)
    {
        var go = new GameObject("LeafKnife", typeof(SpriteRenderer), typeof(ArenaKnife));
        go.transform.position = origin;
        go.transform.localScale = Vector3.one * 1.35f;

        var renderer = go.GetComponent<SpriteRenderer>();
        renderer.sprite = Resources.Load<Sprite>("Sprites/PlantBullet/LeafKnife/LeafKnife");
        renderer.sortingOrder = 52;

        var knife = go.GetComponent<ArenaKnife>();
        knife.game = owner;
        knife.direction = facing.normalized;
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * Speed * Time.deltaTime);
        transform.Rotate(0f, 0f, -720f * Time.deltaTime);   //Xoay vù vù khi bay

        if (game != null && Time.time >= nextHitTime)
        {
            var boss = game.Boss;
            if (boss != null && boss.CanBeHit && Vector2.Distance(transform.position, boss.HitCenter) < HitRadius)
            {
                nextHitTime = Time.time + 0.35f;   //Chặn trúng liên tục từng khung hình
                boss.TakeDamage(Damage);
            }
        }

        Vector3 position = transform.position;
        if (position.x < GargantuarArenaGame.Left - 1.5f || position.x > GargantuarArenaGame.Right + 1.5f ||
            position.y < GargantuarArenaGame.Bottom - 1.5f || position.y > GargantuarArenaGame.Top + 1.5f)
            Destroy(gameObject);
    }
}

/// <summary>
/// Khiên Đậu Tường quay quanh người chơi. Còn khiên thì mọi đòn của Gargantuar
/// đều bị chặn, nhưng khiên chỉ đứng được vài giây.
/// </summary>
public sealed class ArenaShield : MonoBehaviour
{
    private float endsAt;

    public static ArenaShield Spawn(Transform owner, float duration)
    {
        var go = new GameObject("WallNut Shield", typeof(SpriteRenderer), typeof(ArenaShield));
        go.transform.SetParent(owner, false);
        go.transform.localPosition = new Vector3(0.95f, 0.1f, 0f);
        go.transform.localScale = Vector3.one * 2.1f;

        var renderer = go.GetComponent<SpriteRenderer>();
        renderer.sprite = Resources.Load<Sprite>("Sprites/Plants/WallNut/Body/Wallnut_Body_0001");
        renderer.sortingOrder = 46;
        renderer.color = new Color(1f, 1f, 1f, 0.9f);

        var shield = go.GetComponent<ArenaShield>();
        shield.endsAt = Time.time + duration;
        return shield;
    }

    private void Update()
    {
        float left = endsAt - Time.time;
        if (left <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        //Quay quanh chủ, và nhấp nháy báo sắp hết giờ
        float angle = Time.time * 210f;
        transform.localPosition = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad) * 0.55f + 0.1f, 0f) * 1.05f;

        var renderer = GetComponent<SpriteRenderer>();
        float alpha = left < 0.9f ? Mathf.PingPong(Time.time * 8f, 0.6f) + 0.35f : 0.9f;
        renderer.color = new Color(1f, 1f, 1f, alpha);
    }
}

/// <summary>
/// Vùng cấm ở mép trái sân, coi như phần sân nhà phải giữ.
/// Gargantuar bước vào là còi báo kêu liên tục từ 3 đến 6 tiếng.
/// </summary>
public sealed class ArenaDangerZone : MonoBehaviour
{
    private const string AlarmClip = "Sounds/UI/SeedAndShovelBank/unable";

    private GargantuarArenaGame game;
    private SpriteRenderer panel;
    private AudioSource audioSource;
    private bool intruderInside;

    public float BorderX { get; private set; }

    public void Initialize(GargantuarArenaGame owner, float width)
    {
        game = owner;
        BorderX = GargantuarArenaGame.Left + width;

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        //Dải đỏ mờ đánh dấu vùng cấm
        var texture = Texture2D.whiteTexture;
        var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 1f);

        var strip = new GameObject("Danger Strip", typeof(SpriteRenderer));
        strip.transform.SetParent(transform, false);
        strip.transform.position = new Vector3(GargantuarArenaGame.Left + width * 0.5f, 0f, 0f);
        strip.transform.localScale = new Vector3(width, GargantuarArenaGame.Top - GargantuarArenaGame.Bottom + 1.6f, 1f);
        panel = strip.GetComponent<SpriteRenderer>();
        panel.sprite = sprite;
        panel.color = new Color(0.85f, 0.12f, 0.08f, 0.14f);
        panel.sortingOrder = -15;

        //Vạch ranh giới cho dễ nhìn
        var edge = new GameObject("Danger Edge", typeof(SpriteRenderer));
        edge.transform.SetParent(transform, false);
        edge.transform.position = new Vector3(BorderX, 0f, 0f);
        edge.transform.localScale = new Vector3(0.07f, GargantuarArenaGame.Top - GargantuarArenaGame.Bottom + 1.6f, 1f);
        var edgeRenderer = edge.GetComponent<SpriteRenderer>();
        edgeRenderer.sprite = sprite;
        edgeRenderer.color = new Color(0.95f, 0.25f, 0.15f, 0.55f);
        edgeRenderer.sortingOrder = -14;
    }

    private void Update()
    {
        if (game == null) return;
        var boss = game.Boss;
        bool inside = boss != null && boss.CanBeHit && boss.transform.position.x < BorderX;

        //Chỉ báo động một lần cho mỗi lần xâm nhập, không kêu lại tới khi nó ra ngoài
        if (inside && !intruderInside)
        {
            intruderInside = true;
            StartCoroutine(Alarm());
        }
        else if (!inside && intruderInside)
        {
            intruderInside = false;
        }

        //Vùng cấm sáng lên khi đang bị xâm nhập
        if (panel != null)
        {
            float alpha = intruderInside ? Mathf.PingPong(Time.time * 3.2f, 0.24f) + 0.16f : 0.14f;
            panel.color = new Color(0.85f, 0.12f, 0.08f, alpha);
        }
    }

    private IEnumerator Alarm()
    {
        //Đề bài yêu cầu kêu từ 3 đến 6 tiếng: Range với số nguyên loại trừ mốc trên
        int beeps = Random.Range(3, 7);
        for (int i = 0; i < beeps; i++)
        {
            ArenaSfx.Play(audioSource, AlarmClip, 0.85f);
            yield return new WaitForSeconds(0.38f);
        }
    }
}

public enum ArenaPickupKind
{
    Sun,        //X: thưởng
    FireTrap,   //Y: bẫy
    Crate       //Z: hộp quà ngẫu nhiên
}

/// <summary>
/// Vật phẩm nằm trên sân. Người chơi chạm vào thì nhận hiệu ứng tương ứng.
/// </summary>
public sealed class ArenaPickup : MonoBehaviour
{
    private const float PickRadius = 0.92f;
    private const float Lifetime = 13f;

    private GargantuarArenaGame game;
    private ArenaPickupKind kind;
    private AudioSource audioSource;
    private float bornAt;
    private float baseY;
    private bool taken;

    public static void Spawn(GargantuarArenaGame owner, ArenaPickupKind kind, Vector3 position)
    {
        var go = new GameObject("Pickup " + kind, typeof(SpriteRenderer), typeof(ArenaPickup), typeof(AudioSource));
        go.transform.position = position;

        var pickup = go.GetComponent<ArenaPickup>();
        pickup.game = owner;
        pickup.kind = kind;
        pickup.Dress();
    }

    private void Dress()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        bornAt = Time.time;
        baseY = transform.position.y;

        var renderer = GetComponent<SpriteRenderer>();
        renderer.sortingOrder = 35;

        switch (kind)
        {
            case ArenaPickupKind.Sun:
                renderer.sprite = Resources.Load<Sprite>("Sprites/Sun/Sun/Sun0001");
                transform.localScale = Vector3.one * 0.72f;
                break;
            case ArenaPickupKind.FireTrap:
                renderer.sprite = Resources.Load<Sprite>("Sprites/Items/Fire/Fire0001");
                renderer.color = new Color(1f, 0.72f, 0.42f, 0.96f);
                transform.localScale = Vector3.one * 1.5f;
                break;
            case ArenaPickupKind.Crate:
                renderer.sprite = ArenaIcons.Crate();
                transform.localScale = Vector3.one * 0.95f;
                break;
        }
    }

    private void Update()
    {
        if (taken) return;

        float age = Time.time - bornAt;
        if (age >= Lifetime)
        {
            Destroy(gameObject);
            return;
        }

        //Nhấp nhô cho dễ thấy, và mờ dần khi gần hết hạn
        transform.position = new Vector3(transform.position.x, baseY + Mathf.Sin(Time.time * 2.6f) * 0.12f, 0f);
        var renderer = GetComponent<SpriteRenderer>();
        Color color = renderer.color;
        color.a = age > Lifetime - 2.5f ? Mathf.PingPong(Time.time * 6f, 0.7f) + 0.3f : 1f;
        renderer.color = color;

        if (game == null) return;
        var player = game.Player;
        if (player == null) return;
        if (Vector2.Distance(transform.position, player.transform.position) > PickRadius) return;

        taken = true;
        Collect(player);
    }

    private void Collect(ArenaPeashooter player)
    {
        Vector3 at = transform.position;

        switch (kind)
        {
            case ArenaPickupKind.Sun:
                //Hiệu ứng 1 và 2: cộng vàng, cộng điểm
                game.AddGold(50);
                game.AddScore(25);
                ArenaSfx.Play(audioSource, "Sounds/Plants/sunCollected", 0.85f);
                ArenaFloatingText.Show(at, "+50 VÀNG  +25 ĐIỂM", new Color(1f, 0.88f, 0.3f));
                break;

            case ArenaPickupKind.FireTrap:
                //Hiệu ứng 3 và 4: ăn sát thương, và bị chậm chân một lúc
                ArenaSfx.Play(audioSource, "Sounds/Zombies/bodyhit2", 0.8f);
                player.ApplySlow(0.45f, 3f);
                ArenaFloatingText.Show(at, "BẪY LỬA!  CHẬM CHÂN", new Color(1f, 0.42f, 0.24f));
                game.TrapHit(at);
                break;

            case ArenaPickupKind.Crate:
                ArenaSfx.Play(audioSource, "Sounds/UI/SeedAndShovelBank/seedLift", 0.85f);
                OpenCrate(player, at);
                break;
        }

        //Giữ lại đối tượng một nhịp để tiếng kêu phát xong
        GetComponent<SpriteRenderer>().enabled = false;
        Destroy(gameObject, 1.2f);
    }

    private void OpenCrate(ArenaPeashooter player, Vector3 at)
    {
        switch (Random.Range(0, 3))
        {
            case 0:
                //Hiệu ứng 5: thêm một lớp giáp
                game.AddArmor(1);
                ArenaFloatingText.Show(at, "+1 GIÁP", new Color(0.62f, 0.86f, 1f));
                break;
            case 1:
                //Hiệu ứng 6: thêm một mạng
                game.AddLife(1);
                ArenaFloatingText.Show(at, "+1 MẠNG", new Color(1f, 0.48f, 0.6f));
                break;
            default:
                //Hiệu ứng 7: hồi sạch thời gian chờ của mọi kỹ năng
                player.ResetCooldowns();
                ArenaFloatingText.Show(at, "KỸ NĂNG HỒI SẠCH", new Color(0.78f, 1f, 0.42f));
                break;
        }
    }
}

/// <summary>
/// Rải vật phẩm X, Y, Z lên sân theo nhịp, tránh rơi ngay vào chỗ người chơi đang đứng.
/// </summary>
public sealed class ArenaPickupSpawner : MonoBehaviour
{
    private const int MaximumOnField = 5;
    private const float Margin = 1.7f;

    private GargantuarArenaGame game;
    private float nextSpawnTime;

    public void Initialize(GargantuarArenaGame owner)
    {
        game = owner;
        nextSpawnTime = Time.time + 1.4f;
    }

    private void Update()
    {
        if (game == null || Time.time < nextSpawnTime) return;
        nextSpawnTime = Time.time + Random.Range(2.4f, 4.2f);

        if (Object.FindObjectsByType<ArenaPickup>(FindObjectsSortMode.None).Length >= MaximumOnField) return;

        Vector3 position = PickSpot();
        if (position.x < GargantuarArenaGame.Left) return;   //Không tìm được chỗ trống thì bỏ lượt

        //Bẫy thưa hơn phần thưởng để người chơi không bị dồn
        int roll = Random.Range(0, 100);
        ArenaPickupKind kind = roll < 45 ? ArenaPickupKind.Sun
            : roll < 75 ? ArenaPickupKind.FireTrap
            : ArenaPickupKind.Crate;

        ArenaPickup.Spawn(game, kind, position);
    }

    private Vector3 PickSpot()
    {
        var player = game.Player;
        for (int attempt = 0; attempt < 12; attempt++)
        {
            var candidate = new Vector3(
                Random.Range(GargantuarArenaGame.Left + Margin, GargantuarArenaGame.Right - Margin),
                Random.Range(GargantuarArenaGame.Bottom + Margin, GargantuarArenaGame.Top - Margin),
                0f);

            if (player != null && Vector2.Distance(candidate, player.transform.position) < 1.9f) continue;
            return candidate;
        }
        return new Vector3(GargantuarArenaGame.Left - 99f, 0f, 0f);
    }
}

/// <summary>
/// Dòng chữ bay lên rồi tan, dùng để người chơi thấy rõ mỗi hiệu ứng vừa xảy ra.
/// </summary>
public sealed class ArenaFloatingText : MonoBehaviour
{
    private const float Duration = 1.15f;

    private TextMesh mesh;
    private float bornAt;
    private Color baseColor;

    public static void Show(Vector3 position, string message, Color color)
    {
        var go = new GameObject("Floating Text", typeof(TextMesh), typeof(ArenaFloatingText));
        go.transform.position = position + new Vector3(0f, 0.55f, 0f);
        go.GetComponent<ArenaFloatingText>().Dress(message, color);
    }

    private void Dress(string message, Color color)
    {
        bornAt = Time.time;
        baseColor = color;

        mesh = GetComponent<TextMesh>();
        var font = Resources.Load<Font>("Fonts/Baloo2");
        if (font != null)
        {
            mesh.font = font;
            GetComponent<MeshRenderer>().material = font.material;
        }
        mesh.text = message;
        mesh.fontSize = 58;
        mesh.fontStyle = FontStyle.Bold;
        mesh.characterSize = 0.055f;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.color = color;
        GetComponent<MeshRenderer>().sortingOrder = 80;
    }

    private void Update()
    {
        float t = (Time.time - bornAt) / Duration;
        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += new Vector3(0f, 1.15f * Time.deltaTime, 0f);
        if (mesh != null) mesh.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f - t * t);
    }
}
