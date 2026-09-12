using UnityEngine;

public static class ImportedZombieRuntime
{
    private const string Root = "Sprites/Imported/MarbleXu/Zombies/";

    public static bool Supports(string key) => key == "FlagZombie" || key == "NewspaperZombie";

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
        string idle = Root + key + "/" + key;
        string attack = Root + key + "/" + key + "Attack";
        string die = key == "NewspaperZombie" ? Root + key + "/NewspaperZombieDie" : Root + key + "/FlagZombieLostHead";
        zombie.Configure(key, animator, idle, attack, die);
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

    public void Configure(string zombieKey, RuntimeFrameAnimator animator, string idle, string attackFrames, string deathFrames)
    {
        key = zombieKey; frameAnimator = animator; idlePath = idle; attackPath = attackFrames; diePath = deathFrames;
        speed = key == "FlagZombie" ? 0.28f : 0.18f;
        eatOffset = 0.45f;
        attackPower = 20;
        bloodVolume = key == "FlagZombie" ? 270 : 420;
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
        Plant hit = collision.GetComponent<Plant>();
        if (hit != null && hit.row == pos_row && collision.transform.position.x < transform.position.x + eatOffset)
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
        if (collision.GetComponent<Plant>() == plant) StopAttacking();
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
        frameAnimator.SetFrames(diePath);
        Destroy(gameObject, 1f);
    }
}
