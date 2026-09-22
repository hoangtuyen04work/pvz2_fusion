using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Ánh xạ nhân vật trong màn chiến dịch tới ba bộ sprite tại Resources/aset.
/// CampaignCharacterAnimator tự tải các frame Idle/Walking/Attack/Hurt khi chạy.
/// </summary>
public static class CampaignArt
{
    public const string ScoutPack = "Male Goblin";
    public const string GuardianPack = "Chief Goblin";
    // Thư mục chỉ có ba pack nên Hồi Phục dùng lại Female với màu nhận diện riêng.
    public const string HealerPack = "Female Goblin";

}

/// <summary>Các thông số combat khớp với nhịp frame của bộ Attack_Player.</summary>
public static class CampaignCombatTuning
{
    public const float RangedAnimationFps = 18f;
    public const float RangedReleaseDelay = 6f / RangedAnimationFps;
    public const float RangedCooldown = 14f / RangedAnimationFps;
    public const float ArrowSpeed = 12f;
    public const float ArrowLifetime = 1.55f;
    public const int ArrowDamage = 34;

    public const float MeleeAnimationFps = 14f;
    public const float MeleeHitDelay = 3f / MeleeAnimationFps;
    public const float MeleeCooldown = 0.46f;

    public const int HealThreshold = 50;
    public const int HealAmount = 30;
    public const float HealCooldown = 8f;
}

public sealed class CampaignPlayer : MonoBehaviour
{
    private CampaignGame game;
    private CampaignJoystick joystick;
    private Vector2 facing = Vector2.right;
    private Vector2 velocity;
    private float nextAction;
    private float nextDamage;
    private int meleeCombo;
    private SpriteRenderer body;
    private CampaignPlayerAnimator characterAnimator;

    public Vector2 Velocity => velocity;

    public void Initialize(CampaignGame owner)
    {
        game = owner;
        body = gameObject.AddComponent<SpriteRenderer>();
        body.sortingOrder = 20;
        characterAnimator = gameObject.AddComponent<CampaignPlayerAnimator>();
        characterAnimator.Initialize(body);
        CircleCollider2D hitbox = gameObject.AddComponent<CircleCollider2D>();
        hitbox.radius = 0.28f;
        hitbox.offset = new Vector2(0f, 0.26f);
        transform.localScale = Vector3.one * 2.25f;
    }

    public void SetJoystick(CampaignJoystick value) => joystick = value;

    private void Update()
    {
        if (game == null || !game.IsPlaying) return;
        Vector2 keyboard = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        Vector2 input = keyboard.sqrMagnitude > 0.01f ? keyboard.normalized : (joystick != null ? joystick.Value : Vector2.zero);
        characterAnimator.SetMovement(input.magnitude);
        if (input.sqrMagnitude > 0.03f) facing = input.normalized;

        float acceleration = game.CurrentMap == 1 ? 3.2f : 16f;
        velocity = Vector2.Lerp(velocity, input * 4.6f, acceleration * Time.deltaTime);
        if (game.CurrentMap == 1 && game.HazardActive) velocity *= 0.965f;
        transform.position += (Vector3)(velocity * Time.deltaTime);
        transform.position = game.ClampToArena(transform.position);

        if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.LeftControl)) Fire();
        if (Input.GetKey(KeyCode.J)) Melee();
        if (Input.GetKeyDown(KeyCode.Q)) game.TryHealPlayer(transform.position);
        // Attack_Player nhìn sang phải ở file gốc.
        if (body != null) body.flipX = facing.x < -0.05f;
    }

    public void Fire()
    {
        if (game == null || !game.IsPlaying || Time.time < nextAction) return;
        nextAction = Time.time + CampaignCombatTuning.RangedCooldown;
        StartCoroutine(FireAtAnimationFrame(facing));
    }

    public void Melee()
    {
        if (game == null || !game.IsPlaying || Time.time < nextAction) return;
        meleeCombo = meleeCombo % 3 + 1;
        nextAction = Time.time + CampaignCombatTuning.MeleeCooldown + (meleeCombo == 3 ? 0.12f : 0f);
        StartCoroutine(MeleeAtAnimationFrame(facing, meleeCombo));
    }

    private IEnumerator FireAtAnimationFrame(Vector2 shotDirection)
    {
        characterAnimator.PlayShot();
        yield return new WaitForSeconds(CampaignCombatTuning.RangedReleaseDelay);
        if (game == null || !game.IsPlaying) yield break;
        CampaignProjectile.Spawn(game, transform.position + (Vector3)(shotDirection * 0.62f), shotDirection);
        game.PlaySfx("Sounds/Plants/firepea", 0.28f);
    }

    private IEnumerator MeleeAtAnimationFrame(Vector2 attackDirection, int combo)
    {
        characterAnimator.PlayMelee(combo);
        yield return new WaitForSeconds(CampaignCombatTuning.MeleeHitDelay);
        if (game == null || !game.IsPlaying) yield break;
        int damage = combo == 3 ? 46 : 30 + combo * 4;
        CampaignMeleeHitbox.Spawn(game, transform.position, attackDirection, damage);
        game.PlaySfx("Sounds/Zombies/splat1", 0.4f);
    }

    public void TouchDamage(int amount, Vector3 source)
    {
        if (Time.time < nextDamage || !game.IsPlaying) return;
        nextDamage = Time.time + 0.85f;
        velocity = ((Vector2)(transform.position - source)).normalized * 5.5f;
        characterAnimator.PlayHurt();
        StartCoroutine(Flash());
        game.DamagePlayer(amount);
    }

    public void Die()
    {
        velocity = Vector2.zero;
        characterAnimator.PlayDeath();
    }

    private IEnumerator Flash()
    {
        if (body == null) yield break;
        for (int i = 0; i < 4; i++)
        {
            body.color = i % 2 == 0 ? new Color(1f, 0.25f, 0.2f) : Color.white;
            yield return new WaitForSeconds(0.08f);
        }
        body.color = Color.white;
    }
}

public sealed class CampaignProjectile : MonoBehaviour
{
    private CampaignGame game;
    private Vector2 direction;
    private float expires;
    private bool consumed;
    private Rigidbody2D physicsBody;

    public static void Spawn(CampaignGame owner, Vector3 position, Vector2 direction)
    {
        var go = new GameObject("Player Arrow", typeof(SpriteRenderer), typeof(CircleCollider2D),
            typeof(Rigidbody2D), typeof(CampaignProjectile));
        go.transform.position = position;
        go.transform.localScale = Vector3.one * 1.15f;
        go.transform.right = direction;
        SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
        renderer.sprite = Resources.Load<Sprite>("aset/Attack_Player/Arrow");
        renderer.color = Color.white;
        renderer.sortingOrder = 30;
        CircleCollider2D collider = go.GetComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.16f;
        Rigidbody2D rigidbody = go.GetComponent<Rigidbody2D>();
        rigidbody.bodyType = RigidbodyType2D.Kinematic;
        rigidbody.gravityScale = 0f;
        rigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        go.GetComponent<CampaignProjectile>().Initialize(owner, direction);
    }

    private void Initialize(CampaignGame owner, Vector2 value)
    {
        game = owner;
        direction = value.normalized;
        physicsBody = GetComponent<Rigidbody2D>();
        expires = Time.time + CampaignCombatTuning.ArrowLifetime;
    }

    private void Update()
    {
        if (game == null || !game.IsPlaying) return;
        if (Time.time >= expires) { Destroy(gameObject); return; }
    }

    private void FixedUpdate()
    {
        if (game == null || !game.IsPlaying || physicsBody == null) return;
        physicsBody.MovePosition(physicsBody.position + direction * CampaignCombatTuning.ArrowSpeed * Time.fixedDeltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed || game == null || !game.IsPlaying) return;
        CampaignNpc target = other.GetComponent<CampaignNpc>();
        if (target == null || !target.Alive) return;
        consumed = true;
        game.RouteProjectileHit(target, CampaignCombatTuning.ArrowDamage);
        Destroy(gameObject);
    }
}

/// <summary>Hitbox tồn tại ngắn đúng tại frame chém gây sát thương.</summary>
public sealed class CampaignMeleeHitbox : MonoBehaviour
{
    private CampaignGame game;
    private int damage;
    private float expires;
    private readonly HashSet<CampaignNpc> hitTargets = new HashSet<CampaignNpc>();

    public static void Spawn(CampaignGame owner, Vector3 origin, Vector2 direction, int damage)
    {
        var go = new GameObject("Player Melee Hitbox", typeof(CircleCollider2D), typeof(Rigidbody2D), typeof(CampaignMeleeHitbox));
        go.transform.position = origin + (Vector3)(direction.normalized * 0.72f);
        CircleCollider2D collider = go.GetComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.62f;
        Rigidbody2D rigidbody = go.GetComponent<Rigidbody2D>();
        rigidbody.bodyType = RigidbodyType2D.Kinematic;
        rigidbody.gravityScale = 0f;
        CampaignMeleeHitbox hitbox = go.GetComponent<CampaignMeleeHitbox>();
        hitbox.game = owner;
        hitbox.damage = damage;
        hitbox.expires = Time.time + 0.12f;
    }

    private void Update()
    {
        if (Time.time >= expires) Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        CampaignNpc target = other.GetComponent<CampaignNpc>();
        if (target == null || !target.Alive || !hitTargets.Add(target)) return;
        game.RouteSkillHit(target, damage);
    }
}

/// <summary>Lớp gốc NPC: cảm nhận người chơi, di chuyển, tấn công và nhận sát thương.</summary>
public abstract class CampaignNpc : MonoBehaviour
{
    protected CampaignGame game;
    protected CampaignPlayer player;
    protected SpriteRenderer body;
    protected float health;
    protected float maximumHealth;
    protected float speed;
    protected float nextDecision;
    protected Vector2 desired;
    protected CampaignCharacterAnimator characterAnimator;
    private float nextContact;

    public float HealthRatio => maximumHealth <= 0 ? 0 : health / maximumHealth;
    public bool Alive => health > 0;

    public virtual void Initialize(CampaignGame owner, CampaignPlayer target, float hp, float moveSpeed,
        Color tint, string assetPack)
    {
        game = owner;
        player = target;
        health = maximumHealth = hp;
        speed = moveSpeed;
        body = gameObject.AddComponent<SpriteRenderer>();
        body.color = tint;
        body.sortingOrder = 15;
        characterAnimator = gameObject.AddComponent<CampaignCharacterAnimator>();
        characterAnimator.Initialize(body, assetPack);
        CircleCollider2D hitbox = gameObject.AddComponent<CircleCollider2D>();
        hitbox.radius = 0.32f;
        hitbox.offset = new Vector2(0f, 0.18f);
        transform.localScale = Vector3.one * 1.75f;
        CreateBadge();
    }

    protected abstract Vector2 ChooseMovement();

    protected virtual void Update()
    {
        if (game == null || !game.IsPlaying || player == null) return;
        if (Time.time >= nextDecision)
        {
            nextDecision = Time.time + Random.Range(0.12f, 0.24f);
            desired = ChooseMovement();
        }
        transform.position += (Vector3)(desired.normalized * speed * Time.deltaTime);
        characterAnimator.SetMoving(desired.sqrMagnitude > 0.02f);
        transform.position = game.ClampToArena(transform.position);
        body.flipX = desired.x > 0f;

        if (Vector2.Distance(transform.position, player.transform.position) < 0.82f && Time.time >= nextContact)
        {
            nextContact = Time.time + 0.75f;
            characterAnimator.PlayAttack();
            player.TouchDamage(ContactDamage, transform.position);
        }
    }

    protected virtual int ContactDamage => 10;

    public virtual void TakeDamage(float amount)
    {
        if (!Alive) return;
        health -= amount;
        if (health <= 0)
        {
            health = 0;
            game.NpcDefeated(this);
            Destroy(gameObject);
        }
        else
        {
            characterAnimator.PlayHurt();
            StartCoroutine(HitFlash());
        }
    }

    public void Heal(float amount)
    {
        if (!Alive) return;
        health = Mathf.Min(maximumHealth, health + amount);
        StartCoroutine(HealFlash());
    }

    private IEnumerator HitFlash()
    {
        Color original = body.color;
        body.color = Color.white;
        yield return new WaitForSeconds(0.08f);
        if (body != null) body.color = original;
    }

    private IEnumerator HealFlash()
    {
        Color original = body.color;
        body.color = new Color(0.35f, 1f, 0.45f);
        yield return new WaitForSeconds(0.16f);
        if (body != null) body.color = original;
    }

    private void CreateBadge()
    {
        var badge = new GameObject("AI Badge", typeof(TextMesh));
        badge.transform.SetParent(transform, false);
        badge.transform.localPosition = new Vector3(0f, 0.75f, 0f);
        TextMesh text = badge.GetComponent<TextMesh>();
        text.text = Badge;
        text.font = CampaignUI.Font;
        text.fontSize = 42;
        text.characterSize = 0.055f;
        text.anchor = TextAnchor.MiddleCenter;
        text.color = Color.white;
        MeshRenderer meshRenderer = badge.GetComponent<MeshRenderer>();
        if (CampaignUI.Font != null) meshRenderer.material = CampaignUI.Font.material;
        meshRenderer.sortingOrder = 25;
    }

    protected abstract string Badge { get; }
}

/// <summary>NPC 1: dự đoán điểm đến từ vận tốc hiện tại thay vì chỉ chạy theo vị trí cũ.</summary>
public sealed class ScoutNpc : CampaignNpc
{
    protected override string Badge => "TRINH SÁT";
    protected override Vector2 ChooseMovement()
    {
        Vector2 predicted = (Vector2)player.transform.position + player.Velocity * 0.42f;
        Vector2 pursuit = predicted - (Vector2)transform.position;
        Vector2 strafe = new Vector2(-pursuit.y, pursuit.x).normalized * Mathf.Sin(Time.time * 3.3f) * 0.35f;
        return pursuit.normalized + strafe;
    }
}

/// <summary>NPC 2: giữ đội hình trước đồng minh và có thể đỡ đạn cho NPC gần nó.</summary>
public sealed class GuardianNpc : CampaignNpc
{
    protected override string Badge => "HỘ VỆ";
    public bool ShieldReady => Alive;
    protected override int ContactDamage => 14;

    protected override Vector2 ChooseMovement()
    {
        CampaignNpc weak = game.FindWeakestNpc(this);
        if (weak != null)
        {
            Vector2 threat = ((Vector2)player.transform.position - (Vector2)weak.transform.position).normalized;
            Vector2 guardPoint = (Vector2)weak.transform.position + threat * 1.1f;
            if (Vector2.Distance(transform.position, guardPoint) > 0.5f)
                return guardPoint - (Vector2)transform.position;
        }
        return (Vector2)player.transform.position - (Vector2)transform.position;
    }

    public void BlockDamage(float amount)
    {
        TakeDamage(amount * 0.55f);
        CampaignFloatingText.Show(transform.position, "ĐỠ ĐÒN", new Color(0.4f, 0.8f, 1f));
    }
}

/// <summary>NPC 3: tìm đồng minh yếu để hồi máu và chủ động chạy khỏi người chơi khi bị áp sát.</summary>
public sealed class HealerNpc : CampaignNpc
{
    private float nextHeal;
    protected override string Badge => "HỒI PHỤC";
    protected override int ContactDamage => 6;

    protected override Vector2 ChooseMovement()
    {
        float playerDistance = Vector2.Distance(transform.position, player.transform.position);
        if (playerDistance < 3f)
            return (Vector2)transform.position - (Vector2)player.transform.position;

        CampaignNpc weak = game.FindWeakestNpc(this);
        if (weak != null && weak.HealthRatio < 0.92f)
            return (Vector2)weak.transform.position - (Vector2)transform.position;
        return ((Vector2)player.transform.position - (Vector2)transform.position).normalized * 0.25f;
    }

    protected override void Update()
    {
        base.Update();
        if (game == null || !game.IsPlaying || Time.time < nextHeal) return;
        CampaignNpc weak = game.FindWeakestNpc(this);
        if (weak == null || weak.HealthRatio >= 0.94f || Vector2.Distance(transform.position, weak.transform.position) > 2.5f) return;
        nextHeal = Time.time + 2.8f;
        weak.Heal(18f);
        CampaignFloatingText.Show(weak.transform.position, "+ HỒI MÁU", new Color(0.35f, 1f, 0.45f));
    }
}

public sealed class CampaignJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    private RectTransform knob;
    public Vector2 Value { get; private set; }
    public void SetKnob(RectTransform value) => knob = value;
    public void OnPointerDown(PointerEventData eventData) => OnDrag(eventData);
    public void OnDrag(PointerEventData eventData)
    {
        RectTransform rect = transform as RectTransform;
        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out local)) return;
        float radius = Mathf.Min(rect.rect.width, rect.rect.height) * 0.36f;
        Value = Vector2.ClampMagnitude(local / Mathf.Max(1f, radius), 1f);
        if (knob != null) knob.anchoredPosition = Value * radius;
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        Value = Vector2.zero;
        if (knob != null) knob.anchoredPosition = Vector2.zero;
    }
}

public sealed class CampaignFloatingText : MonoBehaviour
{
    private TextMesh mesh;
    private float born;
    private Color color;
    public static void Show(Vector3 at, string value, Color tint)
    {
        var go = new GameObject("Campaign Floating Text", typeof(TextMesh), typeof(CampaignFloatingText));
        go.transform.position = at + Vector3.up * 0.8f;
        CampaignFloatingText item = go.GetComponent<CampaignFloatingText>();
        item.mesh = go.GetComponent<TextMesh>();
        item.mesh.font = CampaignUI.Font;
        item.mesh.text = value;
        item.mesh.fontSize = 50;
        item.mesh.characterSize = 0.055f;
        item.mesh.anchor = TextAnchor.MiddleCenter;
        item.mesh.color = tint;
        item.color = tint;
        item.born = Time.time;
        MeshRenderer meshRenderer = go.GetComponent<MeshRenderer>();
        if (CampaignUI.Font != null) meshRenderer.material = CampaignUI.Font.material;
        meshRenderer.sortingOrder = 90;
    }
    private void Update()
    {
        float t = (Time.time - born) / 1.1f;
        if (t >= 1f) { Destroy(gameObject); return; }
        transform.position += Vector3.up * Time.deltaTime;
        mesh.color = new Color(color.r, color.g, color.b, 1f - t);
    }
}
