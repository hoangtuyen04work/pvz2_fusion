using System.Collections.Generic;
using UnityEngine;

/// <summary>Runtime abilities shared by Elite zombies. Creation and score ownership stay in EndlessGameController.</summary>
public sealed class EndlessEliteRuntime : MonoBehaviour
{
    private EndlessGameController owner;
    private Zombie zombie;
    private EndlessEliteType type;
    private int berserkTier;
    private bool killed;
    private float nextAuraAt;
    private float nextTheftAt;
    private int lastBossPhase;
    private TextMesh badge;
    private SunNumber sunBank;
    private SpriteRenderer[] visualRenderers;
    private Color[] originalColors;
    private readonly List<Zombie> laneBuffer = new List<Zombie>(16);

    public EndlessEliteType Type => type;

    public void Initialize(EndlessGameController controller, Zombie target, EndlessEliteType eliteType)
    {
        owner = controller;
        zombie = target;
        type = eliteType;
        if (zombie == null || type == EndlessEliteType.None) return;

        if (type == EndlessEliteType.Armored) zombie.bloodVolume = Mathf.RoundToInt(zombie.bloodVolume * 1.25f);
        else if (type == EndlessEliteType.Healer) zombie.bloodVolume = Mathf.RoundToInt(zombie.bloodVolume * 1.15f);
        else if (type == EndlessEliteType.Commander) zombie.attackPower = Mathf.RoundToInt(zombie.attackPower * 1.20f);
        else if (type == EndlessEliteType.Splitter) zombie.bloodVolume = Mathf.RoundToInt(zombie.bloodVolume * 1.10f);
        else if (type == EndlessEliteType.SunThief) sunBank = FindAnyObjectByType<SunNumber>();

        ApplyVisualIdentity();
        CreateBadge(ShortLabel(type));
    }

    public int ModifyIncomingDamage(int damage)
    {
        if (type != EndlessEliteType.Armored) return damage;
        return Mathf.Max(1, Mathf.RoundToInt(damage * 0.60f));
    }

    public void NotifyKilled()
    {
        if (killed) return;
        killed = true;
        if (type == EndlessEliteType.Splitter && owner != null && zombie != null)
            owner.SpawnSplitterChildren(zombie.pos_row, transform.position);
    }

    private void Update()
    {
        if (zombie == null || !zombie.IsAlive) return;

        if (type == EndlessEliteType.Berserker && zombie.BloodVolumeMax > 0)
        {
            float ratio = zombie.bloodVolume / (float)zombie.BloodVolumeMax;
            int targetTier = ratio <= 0.15f ? 3 : ratio <= 0.40f ? 2 : ratio <= 0.70f ? 1 : 0;
            bool tierChanged = false;
            while (berserkTier < targetTier)
            {
                berserkTier++;
                zombie.ApplyEndlessSpeedFactor(1.15f);
                zombie.attackPower = Mathf.Max(1, Mathf.RoundToInt(zombie.attackPower * 1.10f));
                tierChanged = true;
            }
            while (berserkTier > targetTier)
            {
                berserkTier--;
                zombie.ApplyEndlessSpeedFactor(1f / 1.15f);
                zombie.attackPower = Mathf.Max(1, Mathf.RoundToInt(zombie.attackPower / 1.10f));
                tierChanged = true;
            }
            if (tierChanged)
            {
                if (berserkTier > 0) Tint(new Color(1f, 0.30f, 0.18f));
                else ApplyVisualIdentity();
            }
        }

        if (type == EndlessEliteType.Healer && Time.time >= nextAuraAt)
        {
            nextAuraAt = Time.time + 2.5f;
            HealNearby();
        }
        else if (type == EndlessEliteType.Commander && Time.time >= nextAuraAt)
        {
            nextAuraAt = Time.time + 1f;
            CommandLane();
        }
        else if (type == EndlessEliteType.SunThief && Time.time >= nextTheftAt)
        {
            nextTheftAt = Time.time + 6f;
            StealSun();
        }
    }

    public void ConfigureAsBoss(int cycle)
    {
        type = EndlessEliteType.None;
        transform.localScale *= 1.30f;
        zombie.bloodVolume = Mathf.RoundToInt(zombie.bloodVolume * (4f + cycle * 0.45f));
        zombie.attackPower = Mathf.RoundToInt(zombie.attackPower * 1.5f);
        Tint(new Color(0.72f, 0.45f, 0.88f));
        CreateBadge("BOSS");
        lastBossPhase = 0;
        enabled = true;
    }

    public void UpdateBossPhases(int cycle)
    {
        if (zombie == null || !zombie.IsAlive || zombie.BloodVolumeMax <= 0) return;
        float ratio = zombie.bloodVolume / (float)zombie.BloodVolumeMax;
        int phase = ratio <= 0.35f ? 2 : ratio <= 0.70f ? 1 : 0;
        if (phase <= lastBossPhase) return;
        lastBossPhase = phase;
        zombie.speed *= 1.15f;
        zombie.attackPower = Mathf.RoundToInt(zombie.attackPower * 1.15f);
        if (owner != null) owner.SpawnBossEscort(zombie.pos_row, cycle, phase + 1);
    }

    private void HealNearby()
    {
        if (owner == null) return;
        owner.CollectLivingZombiesInRow(zombie.pos_row, laneBuffer);
        for (int index = 0; index < laneBuffer.Count; index++)
        {
            Zombie target = laneBuffer[index];
            if (target == zombie) continue;
            EndlessEliteRuntime targetElite = target.GetComponent<EndlessEliteRuntime>();
            if (targetElite != null && targetElite.Type == EndlessEliteType.Healer) continue;
            if (Mathf.Abs(target.transform.position.x - transform.position.x) > 2.2f) continue;
            target.Heal(Mathf.Max(5, Mathf.RoundToInt(target.BloodVolumeMax * 0.04f)));
        }
    }

    private void CommandLane()
    {
        if (owner == null) return;
        owner.CollectLivingZombiesInRow(zombie.pos_row, laneBuffer);
        for (int index = 0; index < laneBuffer.Count; index++)
        {
            Zombie target = laneBuffer[index];
            if (target == zombie) continue;
            EndlessCommanderAura aura = target.GetComponent<EndlessCommanderAura>();
            if (aura == null) aura = target.gameObject.AddComponent<EndlessCommanderAura>();
            aura.Refresh(target, 1.4f);
        }
    }

    private void StealSun()
    {
        if (sunBank != null && sunBank.Current > 0) sunBank.subSun(Mathf.Min(15, sunBank.Current));
    }

    private void ApplyVisualIdentity()
    {
        Color color = type == EndlessEliteType.Berserker ? new Color(1f, 0.48f, 0.30f) :
            type == EndlessEliteType.Armored ? new Color(0.58f, 0.65f, 0.72f) :
            type == EndlessEliteType.Healer ? new Color(0.42f, 1f, 0.50f) :
            type == EndlessEliteType.Commander ? new Color(1f, 0.82f, 0.25f) :
            type == EndlessEliteType.Splitter ? new Color(0.82f, 0.45f, 1f) :
            new Color(0.42f, 0.92f, 1f);
        Tint(color);
    }

    private void CreateBadge(string label)
    {
        if (string.IsNullOrEmpty(label)) return;
        if (badge == null)
        {
            var root = new GameObject("Elite Badge");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            root.transform.localScale = Vector3.one * 0.12f;
            badge = root.AddComponent<TextMesh>();
            badge.anchor = TextAnchor.MiddleCenter;
            badge.alignment = TextAlignment.Center;
            badge.fontSize = 32;
            badge.characterSize = 0.35f;
            badge.color = new Color(1f, 0.92f, 0.35f);
            Font font = Resources.Load<Font>("Fonts/Baloo2");
            if (font != null)
            {
                badge.font = font;
                badge.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            }
            SpriteRenderer source = GetComponentInChildren<SpriteRenderer>();
            MeshRenderer renderer = badge.GetComponent<MeshRenderer>();
            if (source != null && renderer != null)
            {
                renderer.sortingLayerID = source.sortingLayerID;
                renderer.sortingOrder = source.sortingOrder + 100;
            }
        }
        badge.text = label;
    }

    private static string ShortLabel(EndlessEliteType value)
    {
        if (value == EndlessEliteType.Berserker) return "CUỒNG";
        if (value == EndlessEliteType.Armored) return "GIÁP";
        if (value == EndlessEliteType.Healer) return "HỒI";
        if (value == EndlessEliteType.Commander) return "LỆNH";
        if (value == EndlessEliteType.Splitter) return "TÁCH";
        if (value == EndlessEliteType.SunThief) return "TRỘM";
        return string.Empty;
    }

    private void Tint(Color tint)
    {
        EnsureVisualCache();
        for (int index = 0; index < visualRenderers.Length; index++)
        {
            if (visualRenderers[index] == null) continue;
            Color original = originalColors[index];
            visualRenderers[index].color = new Color(
                Mathf.Lerp(original.r, tint.r, 0.38f),
                Mathf.Lerp(original.g, tint.g, 0.38f),
                Mathf.Lerp(original.b, tint.b, 0.38f), original.a);
        }
    }

    private void EnsureVisualCache()
    {
        if (visualRenderers != null) return;
        visualRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        originalColors = new Color[visualRenderers.Length];
        for (int index = 0; index < visualRenderers.Length; index++)
            originalColors[index] = visualRenderers[index] != null ? visualRenderers[index].color : Color.white;
    }
}

public sealed class EndlessCommanderAura : MonoBehaviour
{
    private Zombie zombie;
    private float expiresAt;
    private bool attackApplied;
    private bool speedApplied;
    private const float SpeedFactor = 1.12f;
    private const float AttackFactor = 1.15f;

    public void Refresh(Zombie target, float duration)
    {
        if (zombie == null)
        {
            zombie = target;
            zombie.attackPower = Mathf.Max(1, Mathf.RoundToInt(zombie.attackPower * AttackFactor));
            attackApplied = true;
            zombie.ApplyEndlessSpeedFactor(SpeedFactor);
            speedApplied = true;
        }
        expiresAt = Mathf.Max(expiresAt, Time.time + duration);
    }

    private void Update()
    {
        if (Time.time < expiresAt) return;
        RemoveAura();
        Destroy(this);
    }

    private void OnDestroy()
    {
        RemoveAura();
    }

    private void RemoveAura()
    {
        if (zombie == null) return;
        if (attackApplied)
            zombie.attackPower = Mathf.Max(1, Mathf.RoundToInt(zombie.attackPower / AttackFactor));
        if (speedApplied) zombie.ApplyEndlessSpeedFactor(1f / SpeedFactor);
        attackApplied = false;
        speedApplied = false;
    }
}
