using UnityEngine;

/// <summary>Phe ta: đứng giữ vị trí, tự tìm kẻ địch gần nhất và phản công.</summary>
public sealed class AsetDefender : AsetEntity2D
{
    private float nextAttackTime;
    private float nextScanTime;
    private AsetEntity2D target;

    private void Update()
    {
        if (!IsAlive || Definition == null) return;
        if (Time.time >= nextScanTime)
        {
            nextScanTime = Time.time + 0.2f;
            target = AsetEntityRegistry.ClosestEnemy(this);
        }
        if (target == null || !target.IsAlive) return;
        Vector2 delta = target.transform.position - transform.position;
        FaceDirection(delta.x);
        if (delta.sqrMagnitude > Definition.attackRange * Definition.attackRange || Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + Definition.attackInterval;
        PlayAttack();
        target.TakeDamage(Definition.attackDamage);
    }
}
