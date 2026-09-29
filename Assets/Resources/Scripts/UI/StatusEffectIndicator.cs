using UnityEngine;
using System.Linq;

// In-world status VFX for zombies. This deliberately uses visuals rather than
// text labels, so the effect remains readable during active play.
public sealed class StatusEffectIndicator : MonoBehaviour
{
    private Zombie zombie;
    private SpriteRenderer frost;
    private SpriteRenderer flames;
    private Sprite[] fireFrames;
    private float frameClock;

    private void Awake()
    {
        zombie = GetComponent<Zombie>();
        if (zombie == null)
        {
            Destroy(this);
            return;
        }
        Build();
    }

    private void LateUpdate()
    {
        bool chilled = zombie.IsFrozen || zombie.IsSlowed;
        frost.enabled = chilled;
        flames.enabled = zombie.IsBurning;

        if (chilled)
        {
            float pulse = 0.82f + Mathf.Sin(Time.time * 7f) * 0.08f;
            frost.transform.localScale = Vector3.one * pulse;
            frost.color = zombie.IsFrozen
                ? new Color(0.55f, 0.9f, 1f, 0.82f)
                : new Color(0.58f, 0.8f, 1f, 0.58f);
        }

        if (zombie.IsBurning && fireFrames.Length > 0)
        {
            frameClock += Time.deltaTime * 14f;
            flames.sprite = fireFrames[Mathf.FloorToInt(frameClock) % fireFrames.Length];
        }

        SyncSorting();
    }

    private void Build()
    {
        frost = CreateEffect("Freeze VFX", "Sprites/Imported/MarbleXu/Bullets/PeaIce/PeaIce_0", new Vector3(0f, 0.02f, -0.1f));
        frost.color = new Color(0.55f, 0.9f, 1f, 0.75f);
        frost.transform.localScale = Vector3.one * 0.9f;

        flames = CreateEffect("Burn VFX", "Sprites/Items/Fire/Fire0001", new Vector3(0f, -0.08f, -0.12f));
        flames.transform.localScale = Vector3.one * 0.85f;
        fireFrames = Resources.LoadAll<Sprite>("Sprites/Items/Fire").OrderBy(sprite => ImportedPlantRuntime.NaturalIndex(sprite.name)).ToArray();
    }

    private SpriteRenderer CreateEffect(string name, string spritePath, Vector3 localPosition)
    {
        GameObject effect = new GameObject(name, typeof(SpriteRenderer));
        effect.transform.SetParent(transform, false);
        effect.transform.localPosition = localPosition;
        SpriteRenderer renderer = effect.GetComponent<SpriteRenderer>();
        renderer.sprite = Resources.Load<Sprite>(spritePath);
        renderer.enabled = false;
        return renderer;
    }

    private void SyncSorting()
    {
        SpriteRenderer baseRenderer = GetComponentsInChildren<SpriteRenderer>(true)
            .FirstOrDefault(renderer => renderer != frost && renderer != flames);
        if (baseRenderer == null) return;
        frost.sortingLayerName = baseRenderer.sortingLayerName;
        flames.sortingLayerName = baseRenderer.sortingLayerName;
        frost.sortingOrder = baseRenderer.sortingOrder + 30;
        flames.sortingOrder = baseRenderer.sortingOrder + 31;
    }
}
