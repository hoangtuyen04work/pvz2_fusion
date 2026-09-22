using UnityEngine;

/// <summary>Phe địch: tìm Defender gần nhất, tiến đến mục tiêu rồi tấn công theo nhịp.</summary>
public sealed class AsetEnemy : AsetEntity2D
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
        if (target == null || !target.IsAlive) { SetMoving(false); return; }

        Vector2 delta = target.transform.position - transform.position;
        FaceDirection(delta.x);
        if (delta.sqrMagnitude > Definition.attackRange * Definition.attackRange)
        {
            transform.position += (Vector3)(delta.normalized * Definition.movementSpeed * Time.deltaTime);
            SetMoving(true);
            return;
        }

        SetMoving(false);
        if (Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + Definition.attackInterval;
        PlayAttack();
        target.TakeDamage(Definition.attackDamage);
    }
}
