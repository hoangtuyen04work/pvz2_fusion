using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Unity adapter for applying the current Endless rule snapshot to runtime entities.
/// Domain values are calculated by EndlessContentCatalog; this class only touches scene objects.
/// </summary>
public static class EndlessModifierSystem
{
    public static EndlessModifierSnapshot Current
    {
        get
        {
            if (!EndlessRun.Active || EndlessRun.Session == null)
                return NeutralSnapshot();
            return EndlessContentCatalog.BuildSnapshot(EndlessRun.Session);
        }
    }

    public static void ApplyToPlant(Plant plant)
    {
        if (!EndlessRun.Active || plant == null) return;
        EndlessPlantModifierRuntime runtime = plant.GetComponent<EndlessPlantModifierRuntime>();
        if (runtime == null) runtime = plant.gameObject.AddComponent<EndlessPlantModifierRuntime>();
        runtime.Apply(plant, Current.PlantHealth);
    }

    public static void ApplyToExistingPlants()
    {
        if (!EndlessRun.Active) return;
        Plant[] plants = Object.FindObjectsByType<Plant>();
        for (int index = 0; index < plants.Length; index++) ApplyToPlant(plants[index]);
    }

    public static void RecoverExistingPlants(float fraction)
    {
        if (!EndlessRun.Active) return;
        Plant[] plants = Object.FindObjectsByType<Plant>();
        for (int index = 0; index < plants.Length; index++)
            if (plants[index] != null)
                plants[index].recover(Mathf.RoundToInt(plants[index].BloodVolumeMax * Mathf.Clamp01(fraction)));
    }

    public static void ApplyToCard(Card card)
    {
        if (!EndlessRun.Active || card == null) return;
        EndlessCardModifierRuntime runtime = card.GetComponent<EndlessCardModifierRuntime>();
        if (runtime == null) runtime = card.gameObject.AddComponent<EndlessCardModifierRuntime>();
        runtime.Apply(card, Current);
    }

    public static void ApplyToExistingCards()
    {
        if (!EndlessRun.Active) return;
        Card[] cards = Object.FindObjectsByType<Card>();
        for (int index = 0; index < cards.Length; index++) ApplyToCard(cards[index]);
        SunNumber sun = Object.FindAnyObjectByType<SunNumber>();
        if (sun != null) sun.RefreshCardAvailability();
    }

    public static void ApplyToSun(SunBase sun)
    {
        if (!EndlessRun.Active || sun == null) return;
        EndlessSunModifierRuntime runtime = sun.GetComponent<EndlessSunModifierRuntime>();
        if (runtime == null) runtime = sun.gameObject.AddComponent<EndlessSunModifierRuntime>();
        runtime.Apply(sun, Current.SunValue);
    }

    public static void ApplyToExistingSuns()
    {
        if (!EndlessRun.Active) return;
        SunBase[] suns = Object.FindObjectsByType<SunBase>();
        for (int index = 0; index < suns.Length; index++) ApplyToSun(suns[index]);
    }

    public static void ApplyToZombie(Zombie zombie, float stageSpeedScale, bool boss)
    {
        if (!EndlessRun.Active || zombie == null) return;
        EndlessModifierSnapshot snapshot = Current;
        float bossHealthReduction = boss ? 1f - 0.15f * EndlessRun.GetBuffStacks("boss_hunter") : 1f;
        bossHealthReduction = Mathf.Clamp(bossHealthReduction, 0.55f, 1f);
        zombie.bloodVolume = Mathf.Max(1, Mathf.RoundToInt(zombie.bloodVolume * snapshot.ZombieHealth * bossHealthReduction));
        zombie.attackPower = Mathf.Max(1, Mathf.RoundToInt(zombie.attackPower * snapshot.ZombieAttack));
        zombie.netSpeedScale = Mathf.Max(0.1f, stageSpeedScale * snapshot.ZombieSpeed);
    }

    public static EndlessModifierSnapshot NeutralSnapshot()
    {
        return new EndlessModifierSnapshot
        {
            PlantHealth = 1f,
            CardCooldown = 1f,
            CardCost = 1f,
            SunValue = 1f,
            ZombieHealth = 1f,
            ZombieSpeed = 1f,
            ZombieAttack = 1f,
            ScoreMultiplier = 1f
        };
    }
}

public sealed class EndlessSunModifierRuntime : MonoBehaviour
{
    private bool initialized;
    private int baseValue;

    public void Apply(SunBase sun, float multiplier)
    {
        if (sun == null) return;
        if (!initialized)
        {
            baseValue = sun.sunNumber;
            initialized = true;
        }
        sun.sunNumber = Mathf.Max(1, Mathf.RoundToInt(baseValue * multiplier));
    }
}

public sealed class EndlessPlantModifierRuntime : MonoBehaviour
{
    private float appliedHealthMultiplier = 1f;

    public void Apply(Plant plant, float requestedMultiplier)
    {
        if (plant == null) return;
        requestedMultiplier = Mathf.Clamp(requestedMultiplier, 0.45f, 3f);
        if (Mathf.Approximately(appliedHealthMultiplier, requestedMultiplier)) return;
        float relative = requestedMultiplier / Mathf.Max(0.01f, appliedHealthMultiplier);
        plant.ScaleHealthForEndless(relative);
        appliedHealthMultiplier = requestedMultiplier;
    }
}

public sealed class EndlessCardModifierRuntime : MonoBehaviour
{
    private bool initialized;
    private float baseCooldown;
    private int baseCost;

    public void Apply(Card card, EndlessModifierSnapshot snapshot)
    {
        if (!initialized)
        {
            baseCooldown = card.coolingTime;
            baseCost = card.sunNeeded;
            initialized = true;
        }
        card.coolingTime = Mathf.Max(0.5f, baseCooldown * snapshot.CardCooldown);
        card.sunNeeded = Mathf.Max(0, Mathf.RoundToInt(baseCost * snapshot.CardCost / 5f) * 5);
        Transform costTransform = card.transform.Find("Cost Background/Sun Cost");
        Text costText = costTransform != null ? costTransform.GetComponent<Text>() : null;
        if (costText != null) costText.text = card.sunNeeded.ToString();
    }
}
