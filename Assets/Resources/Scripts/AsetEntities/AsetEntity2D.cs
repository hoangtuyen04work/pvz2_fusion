using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Registry nhẹ, tránh gọi FindObjectsByType mỗi frame khi có nhiều thực thể.
/// </summary>
public static class AsetEntityRegistry
{
    private static readonly List<AsetEntity2D> defenders = new List<AsetEntity2D>();
    private static readonly List<AsetEntity2D> enemies = new List<AsetEntity2D>();

    public static void Register(AsetEntity2D entity)
    {
        List<AsetEntity2D> list = entity.Faction == AsetFaction.Defender ? defenders : enemies;
        if (!list.Contains(entity)) list.Add(entity);
    }

    public static void Unregister(AsetEntity2D entity)
    {
        defenders.Remove(entity);
        enemies.Remove(entity);
    }

    public static AsetEntity2D ClosestEnemy(AsetEntity2D seeker)
    {
        List<AsetEntity2D> candidates = seeker.Faction == AsetFaction.Defender ? enemies : defenders;
        AsetEntity2D closest = null;
        float bestDistance = float.PositiveInfinity;
        for (int i = candidates.Count - 1; i >= 0; i--)
        {
            AsetEntity2D candidate = candidates[i];
            if (candidate == null) { candidates.RemoveAt(i); continue; }
            if (!candidate.IsAlive) continue;
            float distance = ((Vector2)(candidate.transform.position - seeker.transform.position)).sqrMagnitude;
            if (distance >= bestDistance) continue;
            bestDistance = distance;
            closest = candidate;
        }
        return closest;
    }
}

/// <summary>Lớp nền chung cho Defender và Enemy được tạo từ bộ ảnh trong Resources/aset.</summary>
public abstract class AsetEntity2D : MonoBehaviour
{
    private static readonly int MovingParameter = Animator.StringToHash("Moving");
    private static readonly int AttackTrigger = Animator.StringToHash("Attack");
    private static readonly int HurtTrigger = Animator.StringToHash("Hurt");
    private static readonly int DieTrigger = Animator.StringToHash("Die");

    [SerializeField] protected AsetEntityDefinition definition;
    [SerializeField] protected SpriteRenderer body;
    [SerializeField] protected Animator animator;

    private float health;
    private bool dead;

    public AsetFaction Faction => definition != null ? definition.faction : AsetFaction.Enemy;
    public float Health => health;
    public bool IsAlive => !dead && health > 0f;
    protected AsetEntityDefinition Definition => definition;

    protected virtual void Awake()
    {
        if (body == null) body = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();
        health = definition != null ? definition.maximumHealth : 100f;
    }

    protected virtual void OnEnable() => AsetEntityRegistry.Register(this);
    protected virtual void OnDisable() => AsetEntityRegistry.Unregister(this);

    public virtual void TakeDamage(float damage)
    {
        if (!IsAlive || damage <= 0f) return;
        health = Mathf.Max(0f, health - damage);
        if (health <= 0f) Die();
        else if (animator != null) animator.SetTrigger(HurtTrigger);
    }

    protected void SetMoving(bool value)
    {
        if (animator != null) animator.SetBool(MovingParameter, value);
    }

    protected void FaceDirection(float horizontal)
    {
        // Các frame Left trong gói asset nhìn sang trái; flip để đi sang phải.
        if (body != null && Mathf.Abs(horizontal) > 0.01f) body.flipX = horizontal > 0f;
    }

    protected void PlayAttack()
    {
        if (animator != null) animator.SetTrigger(AttackTrigger);
    }

    protected virtual void Die()
    {
        if (dead) return;
        dead = true;
        SetMoving(false);
        if (animator != null) animator.SetTrigger(DieTrigger);
        Collider2D hitbox = GetComponent<Collider2D>();
        if (hitbox != null) hitbox.enabled = false;
        StartCoroutine(RemoveAfterDeath());
    }

    private IEnumerator RemoveAfterDeath()
    {
        yield return new WaitForSeconds(1.1f);
        Destroy(gameObject);
    }
}
