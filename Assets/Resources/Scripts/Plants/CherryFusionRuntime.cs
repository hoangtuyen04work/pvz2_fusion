using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CherryFusionKind { CherryShooter, Cherrepeater, SplitCherry, GatlingCherry, CherryBomber, GatlingCherryBomber }

public static class CherryFusionRuntime
{
    private const string Root = "Sprites/Plants/CherryFusions/";
    private static readonly Dictionary<string, Sprite[]> animationCache = new Dictionary<string, Sprite[]>();

    public static bool TryGetFusionResult(string currentPlant, string addedPlant, out string result)
    {
        result = null;
        string current = Clean(currentPlant);
        string added = Clean(addedPlant);
        bool cherry = added.StartsWith("CherryBomb", StringComparison.OrdinalIgnoreCase);
        bool pea = added.StartsWith("PeaShooter", StringComparison.OrdinalIgnoreCase);

        if ((current.StartsWith("PeaShooter", StringComparison.OrdinalIgnoreCase) && cherry) ||
            (current.StartsWith("CherryBomb", StringComparison.OrdinalIgnoreCase) && pea)) result = "CherryShooter";
        else if (current.Equals("RepeaterPea", StringComparison.OrdinalIgnoreCase) && cherry) result = "Cherrepeater";
        else if (current.Equals("Threepeater", StringComparison.OrdinalIgnoreCase) && cherry) result = "SplitCherry";
        else if (current.Equals("CherryShooter", StringComparison.OrdinalIgnoreCase) && pea) result = "Cherrepeater";
        else if (current.Equals("Cherrepeater", StringComparison.OrdinalIgnoreCase) && pea) result = "GatlingCherry";
        else if (current.Equals("SplitCherry", StringComparison.OrdinalIgnoreCase) && pea) result = "GatlingCherry";
        else if (current.Equals("CherryShooter", StringComparison.OrdinalIgnoreCase) && cherry) result = "CherryBomber";
        else if (current.Equals("GatlingCherry", StringComparison.OrdinalIgnoreCase) && added.Equals("CherryBomber", StringComparison.OrdinalIgnoreCase)) result = "GatlingCherryBomber";
        else if (current.Equals("CherryBomber", StringComparison.OrdinalIgnoreCase) && added.Equals("GatlingCherry", StringComparison.OrdinalIgnoreCase)) result = "GatlingCherryBomber";
        return result != null;
    }

    public static bool IsFinal(string name)
    {
        string clean = Clean(name);
        if (string.IsNullOrEmpty(clean)) return false;
        return clean.Equals("GatlingCherryBomber", StringComparison.OrdinalIgnoreCase) ||
               clean.Equals("CherryBomber", StringComparison.OrdinalIgnoreCase) ||
               clean.Equals("GatlingCherry", StringComparison.OrdinalIgnoreCase);
    }

    public static GameObject Create(string key, Vector3 position, Transform parent)
    {
        if (!Enum.TryParse(key, true, out CherryFusionKind kind)) return null;
        Sprite sprite = LoadSprite(key);
        if (sprite == null) { Debug.LogError("Missing Cherry fusion artwork: " + key); return null; }

        GameObject go = new GameObject(key) { tag = "Plant" };
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        float scale = Mathf.Clamp(0.95f / Mathf.Max(0.01f, sprite.bounds.size.y), 0.12f, 1.2f);
        go.transform.localScale = new Vector3(scale, scale, 1f);
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.68f / scale, 0.76f / scale);
        Rigidbody2D body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        go.AddComponent<AudioSource>().playOnAwake = false;
        go.AddComponent<Animator>();
        GameObject halo = new GameObject("Halo");
        halo.transform.SetParent(go.transform, false);
        halo.SetActive(false);
        CherryFusionPlant plant = go.AddComponent<CherryFusionPlant>();
        plant.Configure(kind);
        return go;
    }

    public static Sprite LoadSprite(string key)
    {
        Sprite sprite = Resources.Load<Sprite>(Root + key);
        if (sprite != null) return sprite;
        Texture2D texture = Resources.Load<Texture2D>(Root + key);
        return texture == null ? null : Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .08f), 100f);
    }

    public static Sprite[] LoadAnimation(string key)
    {
        if (animationCache.TryGetValue(key, out Sprite[] cached)) return cached;
        Texture2D sheet = Resources.Load<Texture2D>(Root + "Animation/" + key + "Sheet");
        if (sheet == null) return animationCache[key] = Array.Empty<Sprite>();

        int frameWidth = sheet.width / 4;
        int frameHeight = sheet.height / 2;
        Sprite[] frames = new Sprite[8];
        for (int row = 0; row < 2; row++)
        for (int column = 0; column < 4; column++)
        {
            // Authored sheet: idle on the top row, attack on the bottom row.
            int sourceY = row == 0 ? frameHeight : 0;
            frames[row * 4 + column] = Sprite.Create(sheet,
                new Rect(column * frameWidth, sourceY, frameWidth, frameHeight),
                new Vector2(.5f, .08f), 100f, 0, SpriteMeshType.FullRect);
            frames[row * 4 + column].name = key + (row == 0 ? "_Idle_" : "_Attack_") + column;
        }
        return animationCache[key] = frames;
    }

    private static string Clean(string value) => string.IsNullOrEmpty(value) ? string.Empty : value.Replace("(Clone)", string.Empty).Trim();
}

public sealed class CherryFusionPlant : Plant
{
    private CherryFusionKind kind;
    private float nextShot;
    private float rate = 1f;
    private SpriteRenderer spriteRenderer;
    private Sprite[] animationFrames = Array.Empty<Sprite>();
    private float animationStartedAt;
    private float attackUntil;

    public void Configure(CherryFusionKind value)
    {
        kind = value;
        bloodVolume = kind == CherryFusionKind.GatlingCherryBomber ? 600 : 300;
        spriteRenderer = GetComponent<SpriteRenderer>();
        animationFrames = CherryFusionRuntime.LoadAnimation(kind.ToString());
        animationStartedAt = Time.time;
        if (animationFrames.Length == 8) spriteRenderer.sprite = animationFrames[0];
    }

    protected override void Start() { base.Start(); nextShot = Time.time + .75f; }

    private void Update()
    {
        UpdateAnimation();
        if (Time.time < nextShot || !HasTarget()) return;
        StartCoroutine(FireVolley());
        nextShot = Time.time + 1.5f / rate;
    }

    private void UpdateAnimation()
    {
        if (animationFrames.Length != 8 || spriteRenderer == null) return;
        bool attacking = Time.time < attackUntil;
        float fps = attacking ? 16f : 5f;
        int frame = Mathf.FloorToInt((Time.time - animationStartedAt) * fps) % 4;
        spriteRenderer.sprite = animationFrames[(attacking ? 4 : 0) + frame];
    }

    private void PlayAttackAnimation()
    {
        animationStartedAt = Time.time;
        attackUntil = Time.time + .25f / rate;
    }

    private bool HasTarget()
    {
        foreach (Zombie zombie in FindObjectsByType<Zombie>())
            if (zombie != null && !zombie.IsHypnotized && zombie.bloodVolume > 0 && zombie.pos_row == row && zombie.transform.position.x > transform.position.x - .15f)
                return true;
        return false;
    }

    private IEnumerator FireVolley()
    {
        int shots = kind == CherryFusionKind.Cherrepeater ? 2 : (kind == CherryFusionKind.GatlingCherry || kind == CherryFusionKind.GatlingCherryBomber ? 4 : 1);
        if (kind == CherryFusionKind.SplitCherry)
        {
            PlayAttackAnimation();
            Spawn(1, 0f);
            Spawn(-1, .08f);
            Spawn(-1, -.08f);
            yield break;
        }
        for (int i = 0; i < shots; i++)
        {
            PlayAttackAnimation();
            Spawn(1, (i - (shots - 1) * .5f) * .035f);
            if (i + 1 < shots) yield return new WaitForSeconds(.11f / rate);
        }
    }

    private void Spawn(int direction, float yOffset)
    {
        bool explosive = kind == CherryFusionKind.CherryBomber || kind == CherryFusionKind.GatlingCherryBomber;
        GameObject projectile = new GameObject("CherryProjectile");
        projectile.transform.position = transform.position + new Vector3(direction * .36f, .24f + yOffset, 0f);
        SpriteRenderer renderer = projectile.AddComponent<SpriteRenderer>();
        renderer.sprite = CherryFusionRuntime.LoadSprite("CherryProjectile");
        // Bomber projectiles keep the exact same sprite/scale, but use a
        // deeper ripe-cherry red so they are distinct from regular shots.
        renderer.color = explosive ? new Color(.72f, .32f, .32f, 1f) : Color.white;
        renderer.sortingLayerName = "PlantBullet";
        renderer.sortingOrder = 4;
        if (renderer.sprite != null)
        {
            float s = .27f / Mathf.Max(.01f, renderer.sprite.bounds.size.y);
            projectile.transform.localScale = new Vector3(direction * s, s, 1f);
        }
        CherryFusionProjectile bullet = projectile.AddComponent<CherryFusionProjectile>();
        bullet.Initialize(row, direction, explosive ? 300 : 40, explosive, direction < 0);
    }

    protected override void intensify_specific() { rate = 1.5f; GetComponent<Animator>().speed = 1.5f; }
    protected override void cancelIntensify_specific() { rate = 1f; GetComponent<Animator>().speed = 1f; }
}

public sealed class CherryFusionProjectile : MonoBehaviour
{
    private int row, direction, damage;
    private bool explosive, canBounce;
    private readonly HashSet<Zombie> hit = new HashSet<Zombie>();

    public void Initialize(int targetRow, int travelDirection, int shotDamage, bool isExplosive, bool bounces)
    { row = targetRow; direction = travelDirection; damage = shotDamage; explosive = isExplosive; canBounce = bounces; }

    private void Update()
    {
        transform.position += Vector3.right * direction * 3.2f * Time.deltaTime;
        if (canBounce && direction < 0 && transform.position.x <= -5.35f)
        {
            direction = 1; canBounce = false;
            Vector3 s = transform.localScale; s.x = Mathf.Abs(s.x); transform.localScale = s;
        }
        Zombie target = null;
        float best = .27f;
        foreach (Zombie zombie in FindObjectsByType<Zombie>())
        {
            if (zombie == null || zombie.IsHypnotized || zombie.bloodVolume <= 0 || zombie.pos_row != row || hit.Contains(zombie)) continue;
            float distance = Mathf.Abs(zombie.transform.position.x - transform.position.x);
            if (distance < best) { best = distance; target = zombie; }
        }
        if (target != null) Impact(target);
        else if (transform.position.x > 5.8f || transform.position.x < -5.8f) Destroy(gameObject);
    }

    private void Impact(Zombie target)
    {
        if (explosive)
        {
            ImportedPlantVfx.CreateCherryExplosion(transform.position);
            AudioClip explosionSound = Resources.Load<AudioClip>("Sounds/Plants/cherrybomb");
            if (explosionSound != null) AudioSource.PlayClipAtPoint(explosionSound, transform.position);
            foreach (Zombie zombie in FindObjectsByType<Zombie>())
                if (zombie != null && !zombie.IsHypnotized && zombie.bloodVolume > 0 && Vector2.Distance(zombie.transform.position, transform.position) <= 1.25f)
                { zombie.playAudioOfBeingAttacked(); zombie.beAttacked(damage); }
        }
        else { target.playAudioOfBeingAttacked(); target.beAttacked(damage); }
        Destroy(gameObject);
    }
}
