using System;
using System.Collections.Generic;
using UnityEngine;

public enum EndlessChoiceKind
{
    Buff,
    TradeoffDebuff,
    StageDebuff,
    CycleDebuff
}

public enum EndlessEliteType
{
    None,
    Berserker,
    Armored,
    Healer,
    Commander,
    Splitter,
    SunThief
}

[Serializable]
public sealed class EndlessModifierStack
{
    public string id;
    public int stacks;
}

[Serializable]
public sealed class EndlessSessionState
{
    public int schemaVersion = 2;
    public int seed;
    public int completedStages;
    public int score;
    public int kills;
    public int bossesDefeated;
    public int pendingReward;
    public float durationSeconds;
    public string activeCycleDebuffId;
    public int activeCycleDebuffCycle;
    public string lastCycleDebuffId;
    public int activeCycleDebuffLevel;
    public string activeStageDebuffId;
    public List<EndlessModifierStack> buffs = new List<EndlessModifierStack>();
    public List<EndlessModifierStack> tradeoffDebuffs = new List<EndlessModifierStack>();
    public List<string> selectedPlants = new List<string>();

    public int CurrentStage => completedStages + 1;
    public int CurrentCycle => EndlessRules.CycleForStage(CurrentStage);

    public int GetStacks(string id, bool buff)
    {
        List<EndlessModifierStack> source = buff ? buffs : tradeoffDebuffs;
        for (int index = 0; index < source.Count; index++)
            if (string.Equals(source[index].id, id, StringComparison.Ordinal))
                return source[index].stacks;
        return 0;
    }

    public bool AddStack(EndlessChoice choice)
    {
        if (choice == null) return false;
        List<EndlessModifierStack> target = choice.Kind == EndlessChoiceKind.Buff
            ? buffs : tradeoffDebuffs;
        for (int index = 0; index < target.Count; index++)
        {
            if (!string.Equals(target[index].id, choice.Id, StringComparison.Ordinal)) continue;
            if (target[index].stacks >= choice.MaxStacks) return false;
            target[index].stacks++;
            return true;
        }
        target.Add(new EndlessModifierStack { id = choice.Id, stacks = 1 });
        return true;
    }
}

public sealed class EndlessChoice
{
    public readonly string Id;
    public readonly string DisplayName;
    public readonly string Description;
    public readonly EndlessChoiceKind Kind;
    public readonly int Power;
    public readonly int MaxStacks;

    public EndlessChoice(string id, string displayName, string description,
        EndlessChoiceKind kind, int power, int maxStacks = 3)
    {
        Id = id;
        DisplayName = displayName;
        Description = description;
        Kind = kind;
        Power = power;
        MaxStacks = Mathf.Max(1, maxStacks);
    }
}

public struct EndlessModifierSnapshot
{
    public float PlantHealth;
    public float CardCooldown;
    public float CardCost;
    public float SunValue;
    public float ZombieHealth;
    public float ZombieSpeed;
    public float ZombieAttack;
    public float EliteChanceBonus;
    public float ScoreMultiplier;
}

public static class EndlessRules
{
    public const int StagesPerCycle = 5;

    public static int CycleForStage(int stage)
    {
        return Mathf.Max(1, (Mathf.Max(1, stage) - 1) / StagesPerCycle + 1);
    }

    public static int PositionInCycle(int stage)
    {
        return (Mathf.Max(1, stage) - 1) % StagesPerCycle + 1;
    }

    public static bool IsBossStage(int stage)
    {
        return PositionInCycle(stage) == StagesPerCycle;
    }

    public static int CycleDebuffLevel(int cycle)
    {
        return Mathf.Clamp(1 + (Mathf.Max(1, cycle) - 1) / 2, 1, 3);
    }

    public static int StageBudget(int stage, bool boss)
    {
        float budget = 5f + 1.8f * stage + 0.04f * stage * stage;
        if (boss) budget *= 0.75f;
        return Mathf.Clamp(Mathf.RoundToInt(budget), 6, 125);
    }

    public static float BaseEliteChance(int cycle)
    {
        if (cycle < 2) return 0f;
        return Mathf.Min(0.30f, 0.04f + (cycle - 2) * 0.025f);
    }
}

public static class EndlessContentCatalog
{
    public static readonly EndlessChoice[] Buffs =
    {
        new EndlessChoice("deep_roots", "Rễ cây vững chắc", "Cây nhận thêm 20% máu tối đa.", EndlessChoiceKind.Buff, 2, 4),
        new EndlessChoice("quick_seeds", "Hạt giống lanh lẹ", "Thẻ cây hồi nhanh hơn 12%.", EndlessChoiceKind.Buff, 2, 4),
        new EndlessChoice("cheap_seeds", "Gieo trồng tiết kiệm", "Giá cây giảm 8%.", EndlessChoiceKind.Buff, 2, 4),
        new EndlessChoice("bright_sun", "Nắng rực rỡ", "Mỗi mặt trời có giá trị cao hơn 15%.", EndlessChoiceKind.Buff, 2, 4),
        new EndlessChoice("elite_bounty", "Săn tiền thưởng", "Điểm hạ mọi zombie tăng 12%.", EndlessChoiceKind.Buff, 2, 4),
        new EndlessChoice("garden_recovery", "Khu vườn hồi sinh", "Hồi 25% máu cho toàn bộ cây hiện có.", EndlessChoiceKind.Buff, 3, 3),
        new EndlessChoice("strong_start", "Khởi đầu sung túc", "Nhận thêm 100 nắng khi bắt đầu sân mới.", EndlessChoiceKind.Buff, 2, 3),
        new EndlessChoice("boss_hunter", "Khắc tinh khổng lồ", "Boss mất thêm 15% máu tối đa khi xuất hiện.", EndlessChoiceKind.Buff, 3, 3)
    };

    public static readonly EndlessChoice[] TradeoffDebuffs =
    {
        new EndlessChoice("fragile_plants", "Thân cây mong manh", "Máu tối đa của cây giảm 12%.", EndlessChoiceKind.TradeoffDebuff, 2, 4),
        new EndlessChoice("slow_seeds", "Hạt giống uể oải", "Thời gian hồi thẻ tăng 14%.", EndlessChoiceKind.TradeoffDebuff, 2, 4),
        new EndlessChoice("costly_seeds", "Đất khó trồng", "Giá cây tăng 10%.", EndlessChoiceKind.TradeoffDebuff, 2, 4),
        new EndlessChoice("weak_sun", "Nắng nhạt", "Giá trị mặt trời giảm 15%.", EndlessChoiceKind.TradeoffDebuff, 2, 4),
        new EndlessChoice("zombie_vitality", "Zombie dai sức", "Zombie nhận thêm 12% máu.", EndlessChoiceKind.TradeoffDebuff, 2, 4),
        new EndlessChoice("zombie_haste", "Bước chân dồn dập", "Zombie di chuyển nhanh hơn 10%.", EndlessChoiceKind.TradeoffDebuff, 2, 4),
        new EndlessChoice("zombie_fury", "Hàm răng cuồng nộ", "Zombie gây thêm 15% sát thương.", EndlessChoiceKind.TradeoffDebuff, 2, 4),
        new EndlessChoice("elite_surge", "Dấu ấn Elite", "Elite xuất hiện thường xuyên hơn.", EndlessChoiceKind.TradeoffDebuff, 3, 3)
    };

    public static readonly EndlessChoice[] StageDebuffs =
    {
        new EndlessChoice("stage_rush", "Màn: Bầy đàn tốc hành", "Zombie màn này nhanh hơn 15%.", EndlessChoiceKind.StageDebuff, 2, 1),
        new EndlessChoice("stage_tough", "Màn: Da dày", "Zombie màn này có thêm 15% máu.", EndlessChoiceKind.StageDebuff, 2, 1),
        new EndlessChoice("stage_fury", "Màn: Cắn xé", "Zombie màn này gây thêm 18% sát thương.", EndlessChoiceKind.StageDebuff, 2, 1),
        new EndlessChoice("stage_elite", "Màn: Đội quân tinh nhuệ", "Tỷ lệ xuất hiện Elite tăng mạnh.", EndlessChoiceKind.StageDebuff, 3, 1),
        new EndlessChoice("stage_wither", "Màn: Vườn cây suy yếu", "Cây có ít hơn 10% máu trong màn này.", EndlessChoiceKind.StageDebuff, 2, 1),
        new EndlessChoice("stage_dim_sun", "Màn: Mây che nắng", "Mặt trời trong màn này mất 20% giá trị.", EndlessChoiceKind.StageDebuff, 2, 1)
    };

    public static readonly EndlessChoice[] CycleDebuffs =
    {
        new EndlessChoice("cycle_famine", "Chu kỳ: Nạn đói mặt trời", "Giá trị mặt trời bị giảm trong cả 5 màn.", EndlessChoiceKind.CycleDebuff, 3, 1),
        new EndlessChoice("cycle_wither", "Chu kỳ: Cây suy yếu", "Máu tối đa của cây bị giảm trong cả 5 màn.", EndlessChoiceKind.CycleDebuff, 3, 1),
        new EndlessChoice("cycle_haste", "Chu kỳ: Cuồng nộ", "Zombie di chuyển nhanh hơn trong cả 5 màn.", EndlessChoiceKind.CycleDebuff, 3, 1),
        new EndlessChoice("cycle_pressure", "Chu kỳ: Áp đảo", "Zombie có thêm máu và sức tấn công trong cả 5 màn.", EndlessChoiceKind.CycleDebuff, 3, 1),
        new EndlessChoice("cycle_elite", "Chu kỳ: Elite trỗi dậy", "Elite xuất hiện thường xuyên hơn trong cả 5 màn.", EndlessChoiceKind.CycleDebuff, 3, 1)
    };

    public static EndlessChoice Find(string id)
    {
        EndlessChoice choice = FindIn(Buffs, id);
        if (choice != null) return choice;
        choice = FindIn(TradeoffDebuffs, id);
        if (choice != null) return choice;
        choice = FindIn(StageDebuffs, id);
        return choice ?? FindIn(CycleDebuffs, id);
    }

    public static List<EndlessChoice> CreateOffer(EndlessChoiceKind kind, EndlessSessionState state,
        int stage, int count, int salt)
    {
        EndlessChoice[] source = kind == EndlessChoiceKind.Buff ? Buffs :
            kind == EndlessChoiceKind.TradeoffDebuff ? TradeoffDebuffs :
            kind == EndlessChoiceKind.StageDebuff ? StageDebuffs : CycleDebuffs;
        var candidates = new List<EndlessChoice>();
        for (int index = 0; index < source.Length; index++)
        {
            EndlessChoice item = source[index];
            if ((kind == EndlessChoiceKind.Buff || kind == EndlessChoiceKind.TradeoffDebuff) &&
                state.GetStacks(item.Id, kind == EndlessChoiceKind.Buff) >= item.MaxStacks)
                continue;
            if (kind == EndlessChoiceKind.CycleDebuff &&
                (item.Id == state.activeCycleDebuffId || item.Id == state.lastCycleDebuffId))
                continue;
            candidates.Add(item);
        }

        var random = new System.Random(unchecked(state.seed ^ stage * 73856093 ^ salt * 19349663));
        for (int index = candidates.Count - 1; index > 0; index--)
        {
            int other = random.Next(index + 1);
            EndlessChoice temporary = candidates[index];
            candidates[index] = candidates[other];
            candidates[other] = temporary;
        }
        if (candidates.Count > count) candidates.RemoveRange(count, candidates.Count - count);
        return candidates;
    }

    public static EndlessModifierSnapshot BuildSnapshot(EndlessSessionState state)
    {
        var result = new EndlessModifierSnapshot
        {
            PlantHealth = 1f,
            CardCooldown = 1f,
            CardCost = 1f,
            SunValue = 1f,
            ZombieHealth = 1f,
            ZombieSpeed = 1f,
            ZombieAttack = 1f,
            EliteChanceBonus = 0f,
            ScoreMultiplier = 1f
        };

        ApplyStacks(state.buffs, true, ref result);
        ApplyStacks(state.tradeoffDebuffs, false, ref result);
        ApplyTemporary(state.activeStageDebuffId, 1, ref result);
        ApplyTemporary(state.activeCycleDebuffId, Mathf.Clamp(state.activeCycleDebuffLevel, 1, 3), ref result);

        result.PlantHealth = Mathf.Clamp(result.PlantHealth, 0.45f, 3f);
        result.CardCooldown = Mathf.Clamp(result.CardCooldown, 0.35f, 2.5f);
        result.CardCost = Mathf.Clamp(result.CardCost, 0.5f, 2.5f);
        result.SunValue = Mathf.Clamp(result.SunValue, 0.4f, 3f);
        result.ZombieHealth = Mathf.Clamp(result.ZombieHealth, 0.5f, 5f);
        result.ZombieSpeed = Mathf.Clamp(result.ZombieSpeed, 0.6f, 2.2f);
        result.ZombieAttack = Mathf.Clamp(result.ZombieAttack, 0.5f, 4f);
        result.EliteChanceBonus = Mathf.Clamp(result.EliteChanceBonus, 0f, 0.45f);
        return result;
    }

    private static void ApplyStacks(List<EndlessModifierStack> stacks, bool buff, ref EndlessModifierSnapshot result)
    {
        if (stacks == null) return;
        for (int index = 0; index < stacks.Count; index++)
        {
            int count = Mathf.Max(0, stacks[index].stacks);
            string id = stacks[index].id;
            if (buff)
            {
                if (id == "deep_roots") result.PlantHealth *= Mathf.Pow(1.20f, count);
                else if (id == "quick_seeds") result.CardCooldown *= Mathf.Pow(0.88f, count);
                else if (id == "cheap_seeds") result.CardCost *= Mathf.Pow(0.92f, count);
                else if (id == "bright_sun") result.SunValue *= Mathf.Pow(1.15f, count);
                else if (id == "elite_bounty") result.ScoreMultiplier += 0.12f * count;
            }
            else
            {
                if (id == "fragile_plants") result.PlantHealth *= Mathf.Pow(0.88f, count);
                else if (id == "slow_seeds") result.CardCooldown *= Mathf.Pow(1.14f, count);
                else if (id == "costly_seeds") result.CardCost *= Mathf.Pow(1.10f, count);
                else if (id == "weak_sun") result.SunValue *= Mathf.Pow(0.85f, count);
                else if (id == "zombie_vitality") result.ZombieHealth *= Mathf.Pow(1.12f, count);
                else if (id == "zombie_haste") result.ZombieSpeed *= Mathf.Pow(1.10f, count);
                else if (id == "zombie_fury") result.ZombieAttack *= Mathf.Pow(1.15f, count);
                else if (id == "elite_surge") result.EliteChanceBonus += 0.08f * count;
            }
        }
    }

    private static void ApplyTemporary(string id, int level, ref EndlessModifierSnapshot result)
    {
        if (string.IsNullOrEmpty(id)) return;
        float strength = 1f + (level - 1) * 0.65f;
        if (id == "stage_rush") result.ZombieSpeed *= 1.15f;
        else if (id == "stage_tough") result.ZombieHealth *= 1.15f;
        else if (id == "stage_fury") result.ZombieAttack *= 1.18f;
        else if (id == "stage_elite") result.EliteChanceBonus += 0.15f;
        else if (id == "stage_wither") result.PlantHealth *= 0.90f;
        else if (id == "stage_dim_sun") result.SunValue *= 0.80f;
        else if (id == "cycle_famine") result.SunValue *= 1f - 0.10f * strength;
        else if (id == "cycle_wither") result.PlantHealth *= 1f - 0.08f * strength;
        else if (id == "cycle_haste") result.ZombieSpeed *= 1f + 0.10f * strength;
        else if (id == "cycle_pressure")
        {
            result.ZombieHealth *= 1f + 0.08f * strength;
            result.ZombieAttack *= 1f + 0.06f * strength;
        }
        else if (id == "cycle_elite") result.EliteChanceBonus += 0.06f * strength;
    }

    private static EndlessChoice FindIn(EndlessChoice[] source, string id)
    {
        for (int index = 0; index < source.Length; index++)
            if (string.Equals(source[index].Id, id, StringComparison.Ordinal)) return source[index];
        return null;
    }
}
