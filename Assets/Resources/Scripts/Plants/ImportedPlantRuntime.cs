using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public enum ImportedPlantKind { Shooter, Sun, Bomb, Mine, Chomper, Spikeweed, Hypno }

public sealed class ImportedPlantDefinition
{
    public string key, framePath, attackPath, secondaryPath, cardPath;
    public ImportedPlantKind kind;
    public int cost, health, damage, shots;
    public float cooldown, interval, range, initialDelay;
    public int[] rows;
}

public static class ImportedPlantRuntime
{
    private const string Root = "Sprites/Imported/MarbleXu/";
    private static readonly Dictionary<string, ImportedPlantDefinition> Definitions = BuildDefinitions();

    public static bool Supports(string key) => Definitions.ContainsKey(key);

    public static bool TryGetDefinition(string key, out ImportedPlantDefinition definition)
    {
        return Definitions.TryGetValue(key, out definition);
    }

    public static Sprite Preview(string key)
    {
        if (!Definitions.TryGetValue(key, out var d)) return null;
        var sprites = Resources.LoadAll<Sprite>(d.framePath);
        return sprites.OrderBy(s => NaturalIndex(s.name)).FirstOrDefault();
    }

    public static Sprite CardPreview(string key)
    {
        return Definitions.TryGetValue(key, out var definition)
            ? Resources.Load<Sprite>(definition.cardPath)
            : null;
    }

    public static float VisualScale(string key)
    {
        Sprite sprite = Preview(key);
        if (sprite == null || sprite.bounds.size.y <= 0f) return 1f;
        // Legacy plants use roughly 200px canvases at 250 PPU (~0.8 world units).
        return Mathf.Clamp(0.78f / sprite.bounds.size.y, 1f, 3.2f);
    }

    public static GameObject CreatePlant(string key, Vector3 position, Transform parent)
    {
        if (!Definitions.TryGetValue(key, out var d)) return null;
        var go = new GameObject(key);
        go.tag = "Plant";
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = Preview(key);
        float visualScale = VisualScale(key);
        go.transform.localScale = new Vector3(visualScale, visualScale, 1f);
        var collider = go.AddComponent<BoxCollider2D>();
        // Compensate for the visual scale so planting/collision footprints stay unchanged.
        collider.size = new Vector2(0.62f / visualScale, 0.76f / visualScale);
        go.AddComponent<AudioSource>();
        go.AddComponent<Animator>();
        var halo = new GameObject("Halo");
        halo.transform.SetParent(go.transform, false);
        halo.SetActive(false);
        var animator = go.AddComponent<RuntimeFrameAnimator>();
        animator.Configure(renderer, d.framePath, 12f);
        var plant = go.AddComponent<ImportedPlant>();
        plant.Configure(d, animator);
        return go;
    }

    public static Card CreateCard(string key, Transform parent)
    {
        return Definitions.ContainsKey(key) && PlantLoadoutCatalog.TryGet(key, out var entry)
            ? SeedPacketFactory.CreateGameplayCard(entry, parent)
            : null;
    }

    public static int NaturalIndex(string name)
    {
        int end = name.Length - 1;
        while (end >= 0 && char.IsDigit(name[end])) end--;
        return int.TryParse(name.Substring(end + 1), out int value) ? value : 0;
    }

    private static Dictionary<string, ImportedPlantDefinition> BuildDefinitions()
    {
        var map = new Dictionary<string, ImportedPlantDefinition>(StringComparer.OrdinalIgnoreCase);
        Add(map, "RepeaterPea", ImportedPlantKind.Shooter, 200, 300, 20, 2, 7.5f, 1.45f, 99f, "RepeaterPea", "RepeaterPea");
        Add(map, "SnowPea", ImportedPlantKind.Shooter, 175, 300, 20, 1, 7.5f, 1.45f, 99f, "SnowPea", "SnowPea");
        Add(map, "Threepeater", ImportedPlantKind.Shooter, 325, 300, 20, 1, 7.5f, 1.45f, 99f, "Threepeater", "Threepeater", new[]{-1,0,1});
        Add(map, "CherryBomb", ImportedPlantKind.Bomb, 150, 300, 1800, 1, 50f, 0.75f, 1.65f, "CherryBomb", "CherryBomb");
        Add(map, "PotatoMine", ImportedPlantKind.Mine, 25, 300, 1800, 1, 30f, 14f, 0.55f, "PotatoMine/PotatoMineInit", "PotatoMine/PotatoMineExplode");
        // Chomper's interval is its long digestion cooldown, not its first-bite delay.
        Add(map, "Chomper", ImportedPlantKind.Chomper, 150, 300, 1800, 1, 7.5f, 42f, 1.55f, "Chomper/Chomper", "Chomper/ChomperAttack", secondary: "Chomper/ChomperDigest", initialDelay: 0.2f);
        Add(map, "PuffShroom", ImportedPlantKind.Shooter, 0, 300, 20, 1, 7.5f, 1.45f, 3.2f, "PuffShroom/PuffShroom", "PuffShroom/PuffShroom");
        Add(map, "SunShroom", ImportedPlantKind.Sun, 25, 300, 0, 1, 7.5f, 24f, 0f, "SunShroom/SunShroom", "SunShroom/SunShroom", secondary: "SunShroom/SunShroomBig", initialDelay: 7f);
        Add(map, "ScaredyShroom", ImportedPlantKind.Shooter, 25, 300, 20, 1, 7.5f, 1.45f, 99f, "ScaredyShroom/ScaredyShroom", "ScaredyShroom/ScaredyShroom", secondary: "ScaredyShroom/ScaredyShroomCry");
        Add(map, "HypnoShroom", ImportedPlantKind.Hypno, 75, 300, 1800, 1, 30f, 0f, 0f, "HypnoShroom/HypnoShroom", "HypnoShroom/HypnoShroom");
        Add(map, "IceShroom", ImportedPlantKind.Bomb, 75, 300, 20, 1, 50f, 1f, 99f, "IceShroom/IceShroom", "IceShroom/IceShroomSnow");
        Add(map, "Jalapeno", ImportedPlantKind.Bomb, 125, 300, 1800, 1, 50f, 0.8f, -1f, "Jalapeno/Jalapeno", "Jalapeno/JalapenoExplode");
        Add(map, "Spikeweed", ImportedPlantKind.Spikeweed, 100, 300, 20, 1, 7.5f, 1f, 0.8f, "Spikeweed/Spikeweed", "Spikeweed/Spikeweed");
        return map;
    }

    private static void Add(Dictionary<string, ImportedPlantDefinition> map, string key, ImportedPlantKind kind, int cost, int hp, int damage, int shots, float cooldown, float interval, float range, string frames, string attack, int[] rows = null, string secondary = null, float initialDelay = -1f)
    {
        map[key] = new ImportedPlantDefinition
        {
            key=key, kind=kind, cost=cost, health=hp, damage=damage, shots=shots,
            cooldown=cooldown, interval=interval, range=range, initialDelay=initialDelay < 0f ? interval : initialDelay,
            framePath=Root+"Plants/"+frames, attackPath=Root+"Plants/"+attack,
            secondaryPath=string.IsNullOrEmpty(secondary) ? null : Root+"Plants/"+secondary,
            cardPath=Root+"Cards/card_"+CardFile(key), rows=rows ?? new[]{0}
        };
    }

    private static string CardFile(string key)
    {
        switch (key) {
            case "RepeaterPea": return "repeaterpea";
            case "Threepeater": return "threepeashooter";
            default: return key.ToLowerInvariant();
        }
    }
}

public sealed class RuntimeFrameAnimator : MonoBehaviour
{
    private SpriteRenderer renderer;
    private Sprite[] frames = Array.Empty<Sprite>();
    private float fps = 12f, elapsed;
    private bool loop = true;
    private Action onComplete;
    public void Configure(SpriteRenderer target, string path, float rate) { renderer=target; fps=rate; SetFrames(path); }
    public void SetFrames(string path) { LoadFrames(path, true, null); }
    public void PlayOnce(string path, Action completed = null) { LoadFrames(path, false, completed); }
    private void LoadFrames(string path, bool shouldLoop, Action completed)
    {
        frames=Resources.LoadAll<Sprite>(path).OrderBy(s=>ImportedPlantRuntime.NaturalIndex(s.name)).ToArray();
        elapsed=0f; loop=shouldLoop; onComplete=completed;
        if(frames.Length>0 && renderer!=null) renderer.sprite=frames[0];
    }
    private void Update()
    {
        if(frames.Length==0 || renderer==null) return;
        elapsed += Time.deltaTime;
        int index=Mathf.FloorToInt(elapsed*fps);
        if(loop) { renderer.sprite=frames[index%frames.Length]; return; }
        if(index<frames.Length) { renderer.sprite=frames[index]; return; }
        renderer.sprite=frames[frames.Length-1];
        Action completed=onComplete; onComplete=null; frames=Array.Empty<Sprite>();
        completed?.Invoke();
    }
}

public sealed class ImportedPlant : Plant
{
    private const float LawnRightEdge = 5.3f;
    private ImportedPlantDefinition definition;
    private RuntimeFrameAnimator frameAnimator;
    private float nextAction;
    private bool armed, grown, scared, resolvingSingleUse, chomping;

    public bool CanBeEaten => definition==null || definition.kind!=ImportedPlantKind.Spikeweed;

    public void Configure(ImportedPlantDefinition value, RuntimeFrameAnimator animator)
    {
        definition=value; frameAnimator=animator; bloodVolume=value.health;
    }

    protected override void Start()
    {
        base.Start();
        nextAction = Time.time + definition.initialDelay;
        if(definition.kind==ImportedPlantKind.Bomb) Invoke(nameof(TriggerBomb), definition.interval);
        if(definition.kind==ImportedPlantKind.Mine) Invoke(nameof(ArmMine), definition.interval);
        if(definition.key=="SunShroom") Invoke(nameof(GrowSunShroom), 120f);
    }

    private void Update()
    {
        if(definition==null || resolvingSingleUse || Time.time<nextAction) return;
        switch(definition.kind)
        {
            case ImportedPlantKind.Shooter: ShootIfPossible(); break;
            case ImportedPlantKind.Sun: CreateSun(); break;
            case ImportedPlantKind.Chomper: ChompIfPossible(); break;
            case ImportedPlantKind.Spikeweed: DamageNearby(); break;
            case ImportedPlantKind.Mine: if(armed) TriggerMine(); break;
        }
    }

    private List<Zombie> Targets(int targetRow, float range)
    {
        return FindObjectsByType<Zombie>()
            .Where(z=>!z.IsHypnotized && z.gameObject.activeInHierarchy && z.enabled && z.bloodVolume>0 &&
                z.pos_row==targetRow && z.transform.position.x>=transform.position.x-0.25f &&
                z.transform.position.x<=LawnRightEdge && z.transform.position.x-transform.position.x<=range)
            .OrderBy(z=>z.transform.position.x).ToList();
    }

    public bool HasTargetInLane(int targetRow, float range)
    {
        return Targets(targetRow, range).Count > 0;
    }

    private void ShootIfPossible()
    {
        if(definition.key=="ScaredyShroom" && IsZombieNearScaredyShroom())
        {
            if(!scared && !string.IsNullOrEmpty(definition.secondaryPath)) frameAnimator.SetFrames(definition.secondaryPath);
            scared=true; nextAction=Time.time+0.25f; return;
        }
        if(scared) { scared=false; frameAnimator.SetFrames(definition.framePath); }
        bool fired=false;
        int rowCount=GameManagement.levelData!=null ? GameManagement.levelData.rowCount : 5;
        bool threepeaterTriggered=definition.key=="Threepeater" && definition.rows.Any(offset =>
        {
            int candidate=row+offset;
            return candidate>=0 && candidate<rowCount && Targets(candidate,definition.range).Count>0;
        });
        foreach(int offset in definition.rows)
        {
            int targetRow=row+offset;
            if(targetRow<0 || targetRow>=rowCount) continue;
            if(!threepeaterTriggered && Targets(targetRow,definition.range).Count==0) continue;
            for(int shot=0;shot<definition.shots;shot++) SpawnProjectile(targetRow, shot*0.16f);
            fired=true;
        }
        if(fired)
            frameAnimator.PlayOnce(definition.attackPath, ()=>frameAnimator.SetFrames(definition.framePath));
        nextAction=Time.time+(fired?definition.interval:0.25f);
    }

    private bool IsZombieNearScaredyShroom()
    {
        return FindObjectsByType<Zombie>().Any(z=>!z.IsHypnotized && z.gameObject.activeInHierarchy &&
            z.enabled && z.bloodVolume>0 && Mathf.Abs(z.pos_row-row)<=1 &&
            Mathf.Abs(z.transform.position.x-transform.position.x)<=1.25f);
    }

    private void SpawnProjectile(int targetRow, float delay)
    {
        Vector3 muzzle=transform.position+new Vector3(0.42f,0.15f,0f);
        // Threepeater fires from the plant, but each projectile must immediately
        // occupy its own lane instead of travelling on top of the centre shot.
        if(GameManagement.levelData!=null && GameManagement.levelData.zombieInitPosY!=null &&
            targetRow>=0 && targetRow<GameManagement.levelData.zombieInitPosY.Count)
            muzzle.y=GameManagement.levelData.zombieInitPosY[targetRow]+0.15f;
        ImportedProjectile.Create(
            definition.key,
            muzzle,
            targetRow,
            definition.damage,
            delay);
        PlaySfx(definition.key=="SnowPea" ? "Sounds/Plants/frozen" : "Sounds/Plants/firepea", 0.35f);
    }

    private void CreateSun()
    {
        var prefab=Resources.Load<GameObject>("Prefabs/Sun/FlowerSun");
        var manager=GameObject.Find("Sun Management");
        if(prefab!=null && manager!=null)
        {
            GameObject sun=Instantiate(prefab,transform.position,Quaternion.identity,manager.transform);
            SunBase value=sun.GetComponent<SunBase>();
            if(value!=null) value.sunNumber=grown ? 25 : 15;
        }
        frameAnimator.PlayOnce(definition.attackPath, ()=>frameAnimator.SetFrames(definition.framePath));
        PlaySfx("Sounds/Plants/sunCollected", 0.3f);
        nextAction=Time.time+definition.interval;
    }

    private void GrowSunShroom()
    {
        if(definition==null || definition.key!="SunShroom" || grown) return;
        grown=true;
        if(!string.IsNullOrEmpty(definition.secondaryPath)) frameAnimator.SetFrames(definition.secondaryPath);
    }

    private void ChompIfPossible()
    {
        var target=Targets(row,definition.range).FirstOrDefault();
        if(target==null || chomping) { nextAction=Time.time+0.2f; return; }
        chomping=true;
        // Do not switch to the digest loop mid-animation: it hides the bite.
        frameAnimator.PlayOnce(definition.attackPath, ()=>ResolveChomp(target));
        PlaySfx("Sounds/Zombies/chomp1", 0.8f);
        nextAction=Time.time+definition.interval;
        Invoke(nameof(FinishDigest), definition.interval);
    }

    private void ResolveChomp(Zombie target)
    {
        if(target!=null && target.gameObject.activeInHierarchy && target.bloodVolume>0 &&
            target.pos_row==row)
        {
            target.playAudioOfBeingAttacked();
            target.beAttacked(definition.damage);
            BeginDigest();
        }
        else frameAnimator.SetFrames(definition.framePath);
    }

    private void BeginDigest()
    {
        if(definition!=null && !string.IsNullOrEmpty(definition.secondaryPath)) frameAnimator.SetFrames(definition.secondaryPath);
    }

    private void DamageNearby()
    {
        float halfTileRange=Mathf.Max(0.45f,definition.range*0.6f);
        foreach(var zombie in FindObjectsByType<Zombie>())
            if(!zombie.IsHypnotized && zombie.gameObject.activeInHierarchy && zombie.enabled &&
                zombie.bloodVolume>0 && zombie.pos_row==row &&
                Mathf.Abs(zombie.transform.position.x-transform.position.x)<=halfTileRange)
                zombie.beAttacked(definition.damage);
        frameAnimator.PlayOnce(definition.attackPath, ()=>frameAnimator.SetFrames(definition.framePath));
        PlaySfx("Sounds/Plants/firepea", 0.25f);
        nextAction=Time.time+definition.interval;
    }

    private void ArmMine() { armed=true; frameAnimator.SetFrames("Sprites/Imported/MarbleXu/Plants/PotatoMine/PotatoMine"); nextAction=Time.time; }
    private void TriggerMine()
    {
        List<Zombie> targets=Targets(row,definition.range);
        if(targets.Count==0) { nextAction=Time.time+0.1f; return; }
        resolvingSingleUse=true;
        foreach(Zombie target in targets) target.beAttacked(definition.damage);
        frameAnimator.SetFrames(definition.attackPath);
        ImportedPlantVfx.CreateBurst(transform.position, GetComponent<SpriteRenderer>().sprite, new Color(1f,0.72f,0.25f));
        PlaySfx("Sounds/Plants/SquashFall", 0.7f);
        Invoke(nameof(FinishBomb),0.35f);
    }

    private void TriggerBomb()
    {
        if(definition.key=="Jalapeno")
        {
            // The damage already targets every zombie in this row; the VFX must
            // match it and remain within the playable lawn edges.
            ImportedPlantVfx.CreateFireLane(transform.position.y, -5.3f, 5.3f);
            PlaySfx("Sounds/Plants/fire", 0.85f);
        }
        else if(definition.key=="CherryBomb")
        {
            ImportedPlantVfx.CreateCherryExplosion(transform.position);
            PlaySfx("Sounds/Plants/SquashFall", 0.9f);
        }
        else if(definition.key=="IceShroom")
        {
            ImportedPlantVfx.CreateBurst(transform.position, GetComponent<SpriteRenderer>().sprite, new Color(0.58f,0.9f,1f));
            PlaySfx("Sounds/Plants/frozen", 0.85f);
        }
        var all=FindObjectsByType<Zombie>();
        foreach(var zombie in all)
        {
            if(zombie.IsHypnotized || !zombie.gameObject.activeInHierarchy || !zombie.enabled || zombie.bloodVolume<=0) continue;
            bool hit=definition.key=="IceShroom" || (definition.key=="Jalapeno" ? zombie.pos_row==row : Vector2.Distance(zombie.transform.position,transform.position)<=definition.range);
            if(!hit) continue;
            zombie.beAttacked(definition.damage);
            if(definition.key=="IceShroom") zombie.ApplyFreeze(3.25f,16f);
            if(definition.key=="Jalapeno") zombie.Thaw();
        }
        resolvingSingleUse=true;
        frameAnimator.SetFrames(definition.attackPath);
        Invoke(nameof(FinishBomb),definition.key=="IceShroom" ? 0.35f : 0.65f);
    }
    private void FinishBomb(){die("");}
    private void ResetIdleFrames(){ if(definition!=null) frameAnimator.SetFrames(definition.framePath); }
    private void FinishDigest(){ ResetIdleFrames(); chomping=false; }

    private void PlaySfx(string path, float volume)
    {
        AudioClip clip=Resources.Load<AudioClip>(path);
        if(audioSource!=null && clip!=null) audioSource.PlayOneShot(clip, volume);
    }

    public bool OnBitten(Zombie attacker)
    {
        if(definition==null || definition.kind!=ImportedPlantKind.Hypno) return false;
        attacker.Hypnotize();
        frameAnimator.PlayOnce(definition.attackPath);
        PlaySfx("Sounds/Plants/frozen", 0.55f);
        Invoke(nameof(FinishBomb), 0.35f);
        resolvingSingleUse=true;
        return true;
    }

    protected override void beforeDie()
    {
        CancelInvoke();
    }
}

public sealed class ImportedProjectile : MonoBehaviour
{
    private const float Speed = 4f;
    private const float MushroomVisualScale = 1.55f;
    private readonly HashSet<UnityEngine.Object> processedIgniters = new HashSet<UnityEngine.Object>();
    private int row, damage;
    private bool slow, fire, canIgnite;
    private float startAt;
    private SpriteRenderer spriteRenderer;

    public int Row => row;
    public int Damage => damage;
    public bool AppliesSlow => slow;
    public bool IsFire => fire;
    public bool CanIgnite => canIgnite;

    public static ImportedProjectile Create(string sourceKey, Vector3 position, int targetRow, int hurt, float delay)
    {
        GameObject bullet=new GameObject(sourceKey+" Projectile");
        bullet.transform.position=position;

        SpriteRenderer renderer=bullet.AddComponent<SpriteRenderer>();
        bool mushroom=sourceKey=="PuffShroom" || sourceKey=="ScaredyShroom";
        bool snowPea=sourceKey=="SnowPea";
        string spritePath=snowPea
            ? "Sprites/Imported/MarbleXu/Bullets/PeaIce/PeaIce_0"
            : mushroom
            ? "Sprites/Imported/MarbleXu/Bullets/BulletMushRoom/BulletMushRoom_0"
            : "Sprites/PlantBullet/PeaBullet/PeaBullet";
        renderer.sprite=Resources.Load<Sprite>(spritePath);
        renderer.sortingLayerName="PlantBullet";
        float snowPeaScale=1f;
        if(snowPea)
        {
            Sprite normalPea=Resources.Load<Sprite>("Sprites/PlantBullet/PeaBullet/PeaBullet");
            if(normalPea!=null && renderer.sprite!=null && renderer.sprite.bounds.size.x>0f && renderer.sprite.bounds.size.y>0f)
                snowPeaScale=Mathf.Max(
                    normalPea.bounds.size.x/renderer.sprite.bounds.size.x,
                    normalPea.bounds.size.y/renderer.sprite.bounds.size.y);
        }
        bullet.transform.localScale=mushroom
            ? new Vector3(MushroomVisualScale,MushroomVisualScale,1f)
            : snowPea ? Vector3.one*snowPeaScale : Vector3.one;

        CircleCollider2D collider=bullet.AddComponent<CircleCollider2D>();
        collider.isTrigger=true;
        collider.radius=0.10f;
        Rigidbody2D body=bullet.AddComponent<Rigidbody2D>();
        body.bodyType=RigidbodyType2D.Kinematic;
        body.gravityScale=0f;
        body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;

        ImportedProjectile projectile=bullet.AddComponent<ImportedProjectile>();
        projectile.Configure(targetRow,hurt,sourceKey=="SnowPea",!mushroom,delay,renderer);
        if(sourceKey=="SnowPea") renderer.color=Color.white;
        if(projectile.canIgnite) bullet.tag="Pea";
        return projectile;
    }

    private void Configure(int targetRow,int hurt,bool appliesSlow,bool isPea,float delay,SpriteRenderer renderer)
    {
        row=targetRow; damage=hurt; slow=appliesSlow; canIgnite=isPea;
        startAt=Time.time+delay; spriteRenderer=renderer;
    }

    public GameObject PassThroughTorchwood(int torchwoodRow, int fireDamage, UnityEngine.Object igniter)
    {
        if(!canIgnite || row!=torchwoodRow || igniter==null || !processedIgniters.Add(igniter)) return null;
        if(slow)
        {
            slow=false;
            spriteRenderer.sprite=Resources.Load<Sprite>("Sprites/PlantBullet/PeaBullet/PeaBullet");
            spriteRenderer.color=Color.white;
            return null;
        }

        GameObject firePeaPrefab=Resources.Load<GameObject>("Prefabs/PlantBullet/FirePea");
        if(firePeaPrefab==null)
        {
            Debug.LogError("Missing Prefabs/PlantBullet/FirePea; cannot ignite projectile.", this);
            return null;
        }

        fire=true;
        damage=Mathf.Max(damage,fireDamage);
        canIgnite=false;
        gameObject.tag="Untagged";
        Collider2D projectileCollider=GetComponent<Collider2D>();
        if(projectileCollider!=null) projectileCollider.enabled=false;
        GameObject replacement=Instantiate(firePeaPrefab,transform.position,Quaternion.identity);
        StraightBullet replacementBullet=replacement.GetComponent<StraightBullet>();
        if(replacementBullet==null)
        {
            Debug.LogError("FirePea prefab is missing StraightBullet.", replacement);
            if(Application.isPlaying) Destroy(replacement); else DestroyImmediate(replacement);
            return null;
        }
        replacementBullet.initialize(row,damage);
        enabled=false;
        if(Application.isPlaying) Destroy(gameObject); else DestroyImmediate(gameObject);
        return replacement;
    }

    private void Update()
    {
        if(Time.time<startAt) return;
        transform.Translate(Speed*Time.deltaTime,0f,0f);
        Zombie target=FindObjectsByType<Zombie>()
            .Where(z=>!z.IsHypnotized && z.pos_row==row && z.gameObject.activeInHierarchy && z.enabled && z.bloodVolume>0 &&
                Vector2.Distance(transform.position,z.transform.position)<=0.28f)
            .OrderBy(z=>Mathf.Abs(z.transform.position.x-transform.position.x))
            .FirstOrDefault();
        if(target!=null)
        {
            target.playAudioOfBeingAttacked();
            target.beAttacked(damage);
            if(slow) target.ApplySlow(0.5f,10f);
            if(fire) target.beBurned();
            Destroy(gameObject);
            return;
        }
        if(transform.position.x>7f) Destroy(gameObject);
    }
}

// Lightweight VFX generated from existing sprites. They are deliberately
// independent of the plant object because one-shot plants destroy themselves.
public static class ImportedPlantVfx
{
    public static void CreateCherryExplosion(Vector3 position)
    {
        const string sourceRoot="Sprites/Effects/CherryExplosion/";
        Sprite cloudSprite=Resources.Load<Sprite>(sourceRoot+"ExplosionCloud");
        Sprite powieSprite=Resources.Load<Sprite>(sourceRoot+"ExplosionPowie");
        if(cloudSprite==null || powieSprite==null)
        {
            Debug.LogWarning("Cherry Bomb explosion sprites are missing.");
            return;
        }

        // Port of the original Powie.xml effect: a brief POWIE card over two
        // rings of orange/yellow explosion-cloud particles.
        CreateBurst(position+new Vector3(0f,0.08f,0f),powieSprite,Color.white,1.08f,0.62f,121);

        GameObject explosion=new GameObject("Cherry Bomb Explosion VFX",typeof(ParticleSystem));
        explosion.transform.position=position+new Vector3(0f,0.08f,-0.3f);
        ParticleSystem particles=explosion.GetComponent<ParticleSystem>();
        particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main=particles.main;
        main.loop=false;
        main.duration=0.12f;
        main.startLifetime=new ParticleSystem.MinMaxCurve(0.4f,0.62f);
        main.startSpeed=new ParticleSystem.MinMaxCurve(0.65f,2.5f);
        main.startSize=new ParticleSystem.MinMaxCurve(0.42f,0.82f);
        main.startRotation=new ParticleSystem.MinMaxCurve(0f,Mathf.PI*2f);
        main.startColor=new ParticleSystem.MinMaxGradient(
            new Color(1f,0.45f,0f,1f),new Color(1f,0.92f,0.22f,1f));
        main.gravityModifier=0f;
        main.simulationSpace=ParticleSystemSimulationSpace.World;
        main.maxParticles=32;
        main.stopAction=ParticleSystemStopAction.Destroy;

        ParticleSystem.EmissionModule emission=particles.emission;
        emission.rateOverTime=0f;
        emission.SetBursts(new[]{new ParticleSystem.Burst(0f,28)});

        ParticleSystem.ShapeModule shape=particles.shape;
        shape.enabled=true;
        shape.shapeType=ParticleSystemShapeType.Circle;
        shape.radius=0.24f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime=particles.colorOverLifetime;
        colorOverLifetime.enabled=true;
        Gradient fade=new Gradient();
        fade.SetKeys(
            new[]{new GradientColorKey(new Color(1f,0.92f,0.22f),0f),new GradientColorKey(new Color(1f,0.35f,0f),1f)},
            new[]{new GradientAlphaKey(1f,0f),new GradientAlphaKey(0f,1f)});
        colorOverLifetime.color=fade;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime=particles.sizeOverLifetime;
        sizeOverLifetime.enabled=true;
        sizeOverLifetime.size=new ParticleSystem.MinMaxCurve(1f,AnimationCurve.EaseInOut(0f,0.35f,1f,1f));

        ParticleSystemRenderer particleRenderer=explosion.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sortingLayerName="PlantBullet";
        particleRenderer.sortingOrder=120;
        Shader spriteShader=Shader.Find("Sprites/Default");
        if(spriteShader!=null)
        {
            Material particleMaterial=new Material(spriteShader);
            particleMaterial.mainTexture=cloudSprite.texture;
            particleRenderer.material=particleMaterial;
        }

        particles.Play();
    }

    public static void CreateBurst(Vector3 position, Sprite sprite, Color tint)
    {
        CreateBurst(position,sprite,tint,1.8f,0.58f,0);
    }

    private static void CreateBurst(Vector3 position, Sprite sprite, Color tint, float maxScale, float lifetime, int sortingOrder)
    {
        if(sprite==null) return;
        GameObject burst=new GameObject("Plant Explosion VFX", typeof(SpriteRenderer), typeof(ImportedPlantBurstVfx));
        burst.transform.position=position+new Vector3(0f,0.06f,-0.2f);
        SpriteRenderer renderer=burst.GetComponent<SpriteRenderer>();
        renderer.sprite=sprite;
        renderer.color=tint;
        renderer.sortingLayerName="PlantBullet";
        renderer.sortingOrder=sortingOrder;
        burst.GetComponent<ImportedPlantBurstVfx>().Configure(renderer,maxScale,lifetime);
    }

    public static void CreateFireLane(float y, float leftEdge, float rightEdge)
    {
        Sprite[] frames=Resources.LoadAll<Sprite>("Sprites/Items/Fire").OrderBy(sprite=>ImportedPlantRuntime.NaturalIndex(sprite.name)).ToArray();
        if(frames.Length==0) return;
        const float spacing=0.72f;
        for(float x=leftEdge+0.15f; x<=rightEdge-0.15f; x+=spacing)
        {
            GameObject flame=new GameObject("Jalapeno Lane Fire VFX", typeof(SpriteRenderer), typeof(ImportedPlantFlameVfx));
            flame.transform.position=new Vector3(Mathf.Clamp(x,leftEdge,rightEdge),y-0.15f,-0.2f);
            SpriteRenderer renderer=flame.GetComponent<SpriteRenderer>();
            renderer.sprite=frames[0]; renderer.sortingLayerName="PlantBullet";
            flame.transform.localScale=Vector3.one*0.72f;
            flame.GetComponent<ImportedPlantFlameVfx>().Configure(renderer,frames,0.72f);
        }
    }
}

public sealed class ImportedPlantBurstVfx : MonoBehaviour
{
    private SpriteRenderer renderer; private float scale, lifetime, age;
    public void Configure(SpriteRenderer value,float maxScale,float seconds){renderer=value;scale=maxScale;lifetime=seconds;}
    private void Update(){age+=Time.deltaTime; float t=Mathf.Clamp01(age/lifetime); transform.localScale=Vector3.one*Mathf.Lerp(0.45f,scale,t); renderer.color=new Color(renderer.color.r,renderer.color.g,renderer.color.b,1f-t); if(t>=1f) Destroy(gameObject);}
}

public sealed class ImportedPlantFlameVfx : MonoBehaviour
{
    private SpriteRenderer renderer; private Sprite[] frames; private float lifetime, age;
    public void Configure(SpriteRenderer value,Sprite[] valueFrames,float seconds){renderer=value;frames=valueFrames;lifetime=seconds;}
    private void Update(){age+=Time.deltaTime; renderer.sprite=frames[Mathf.FloorToInt(age*16f)%frames.Length]; if(age>=lifetime) Destroy(gameObject);}
}
