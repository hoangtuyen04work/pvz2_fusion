using System;
using System.Collections.Generic;
using UnityEngine;

public static class ImportedZombieRuntime
{
    private const string Root = "Sprites/Imported/MarbleXu/Zombies/";
    private const string JiangNanRoot = "Sprites/Imported/JiangNan/Zombies/";
    private sealed class Profile
    {
        public string walk, attack, die; public float speed; public int health, damage;
        public Profile(string w,string a,string d,float s,int hp,int hit){walk=w;attack=a;die=d;speed=s;health=hp;damage=hit;}
    }
    private static readonly Dictionary<string,Profile> Profiles = new Dictionary<string,Profile>(StringComparer.OrdinalIgnoreCase)
    {
        {"PoleVaultingZombie",new Profile("PoleVaultingZombie","PoleVaultingZombieAttack","PoleVaultingZombieDie",.31f,500,25)},
        {"FootballZombie",new Profile("FootballZombie","Attack","Die",.40f,1400,35)},
        {"ScreenDoorZombie",new Profile("ScreenDoorZombie","ScreenDoorZombieAttack","LostHeadWalk1",.18f,1100,25)},
        {"BalloonZombie",new Profile("Walk","Attack","Die",.24f,450,20)},
        {"JackinTheBoxZombie",new Profile("Walk","Attack","Die",.25f,500,30)},
        {"DancingZombie",new Profile("DancingZombie","Attack","Die",.22f,500,25)},
        {"BackupDancer",new Profile("BackupDancer","Attack","Die",.22f,300,20)},
        {"DolphinRiderZombie",new Profile("Walk1","Attack","Die",.36f,500,30)},
        {"SnorkelZombie",new Profile("Walk1","Attack","Die",.24f,500,20)},
        {"Zomboni",new Profile("0","1","BoomDie",.27f,1350,40)},
        {"Imp",new Profile("Zombie","ZombieAttack","ZombieDie",.34f,270,20)}
    };

    public static bool Supports(string key) => key == "FlagZombie" || key == "NewspaperZombie" || Profiles.ContainsKey(key);

    public static GameObject Create(string key, Vector3 position, Transform parent)
    {
        if (!Supports(key)) return null;
        var go = new GameObject(key);
        go.tag = "Zombie";
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        var renderer = go.AddComponent<SpriteRenderer>();
        var animator = go.AddComponent<RuntimeFrameAnimator>();
        go.AddComponent<AudioSource>();
        var body = go.AddComponent<BoxCollider2D>();
        body.isTrigger = true;
        body.size = new Vector2(0.62f, 1.05f);
        var rigidbody = go.AddComponent<Rigidbody2D>();
        rigidbody.bodyType = RigidbodyType2D.Kinematic;
        rigidbody.gravityScale = 0f;
        var zombie = go.AddComponent<ImportedZombie>();
        string idle, attack, die; float speed=.18f; int health=420, damage=20;
        if (Profiles.TryGetValue(key,out Profile profile))
        {
            string root=JiangNanRoot+key+"/";
            idle=root+profile.walk; attack=root+profile.attack; die=root+profile.die;
            speed=profile.speed; health=profile.health; damage=profile.damage;
        }
        else
        {
            idle = Root + key + "/" + key;
            attack = Root + key + "/" + key + "Attack";
            die = key == "NewspaperZombie" ? Root + key + "/NewspaperZombieDie" : Root + key + "/FlagZombieLostHead";
            speed=key=="FlagZombie"?.28f:.18f; health=key=="FlagZombie"?270:420;
        }
        zombie.Configure(key, animator, idle, attack, die, speed, health, damage);
        animator.Configure(renderer, idle, 12f);
        return go;
    }
}

public sealed class ImportedZombie : Zombie
{
    private string key, idlePath, attackPath, diePath;
    private RuntimeFrameAnimator frameAnimator;
    private bool attacking;
    private float nextBite;
    private bool enraged;

    public void Configure(string zombieKey, RuntimeFrameAnimator animator, string idle, string attackFrames, string deathFrames, float moveSpeed, int health, int damage)
    {
        key = zombieKey; frameAnimator = animator; idlePath = idle; attackPath = attackFrames; diePath = deathFrames;
        speed = moveSpeed;
        eatOffset = 0.45f;
        attackPower = damage;
        bloodVolume = health;
    }

    protected override void Start()
    {
        bloodVolumeMax = bloodVolume;
        nextBite = Time.time + 1f;
    }

    protected override void Update()
    {
        UpdateTimedStatusEffects();
        if (!alive) return;
        if (UpdateHypnotizedBehavior()) return;
        if (!attacking)
            transform.Translate(-speed * Time.deltaTime, 0f, 0f);
        else if (plant == null)
            StopAttacking();
        else if (Time.time >= nextBite)
        {
            base.attack();
            nextBite = Time.time + 1f;
        }
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsHypnotized) return;
        Plant hit = collision.GetComponent<Plant>();
        ImportedPlant imported = hit != null ? hit.GetComponent<ImportedPlant>() : null;
        if (hit != null && (imported == null || imported.CanBeEaten) && hit.row == pos_row && collision.transform.position.x < transform.position.x + eatOffset)
        {
            plant = hit;
            attacking = true;
            nextBite = Time.time;
            frameAnimator.SetFrames(attackPath);
        }
        else if (collision.CompareTag("GameOverLine"))
        {
            GameObject.Find("Game Management")?.GetComponent<GameManagement>()?.gameOver();
        }
    }

    protected override void OnTriggerExit2D(Collider2D collision)
    {
        if (IsHypnotized) return;
        if (collision.GetComponent<Plant>() == plant) StopAttacking();
    }

    protected override void OnHypnotized()
    {
        StopAttacking();
    }

    private void StopAttacking()
    {
        plant = null; attacking = false; frameAnimator.SetFrames(idlePath);
    }

    public override void beAttacked(int hurt)
    {
        base.beAttacked(hurt);
        if (key == "NewspaperZombie" && !enraged && alive && bloodVolume <= 200)
        {
            enraged = true;
            speed *= 1.8f;
            idlePath = "Sprites/Imported/MarbleXu/Zombies/NewspaperZombie/NewspaperZombieNoPaper";
            attackPath = "Sprites/Imported/MarbleXu/Zombies/NewspaperZombie/NewspaperZombieNoPaperAttack";
            frameAnimator.SetFrames(attacking ? attackPath : idlePath);
        }
    }

    protected override void die()
    {
        if (!alive) return;
        alive = false;
        var collider = GetComponent<Collider2D>();
        if (collider != null) collider.enabled = false;
        var manager = GameObject.Find("Zombie Management");
        if (manager != null) manager.GetComponent<ZombieManagement>().minusZombieNumAll();
        frameAnimator.PlayOnce(diePath,()=>Destroy(gameObject));
        Destroy(gameObject, 4f);
    }
}
