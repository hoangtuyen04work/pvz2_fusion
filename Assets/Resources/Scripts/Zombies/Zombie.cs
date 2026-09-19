using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Zombie : MonoBehaviour
{
    public float speed;   //Tốc độ di chuyển
    public float eatOffset;   //Độ lệch vị trí ăn cây, tức là cây ở phía sau mình bao xa thì không ăn nữa
    public int pos_row;   //Đang ở hàng thứ mấy
    public ZombieState state = ZombieState.Normal;
    private Plant parasiticPlant;   //Cây đang ký sinh lên mình khi ở trạng thái bị ký sinh

    //Liên quan tới máu
    public int bloodVolume;   //Lượng máu
    protected int bloodVolumeMax;
    public int BloodVolumeMax => bloodVolumeMax; // Getter cho GameStateCollector
    protected bool alive = true;
    private bool burning;
    private float burnEndTime;
    private int burnDamagePerTick;
    private bool slowed;
    private float slowEndTime;
    private float slowMultiplier = 1f;
    private bool frozen;
    private float freezeEndTime;
    private bool hypnotized;
    private float nextHypnotizedScan;
    private float nextHypnotizedBite;
    private Zombie hypnotizedTarget;

    public bool IsHypnotized => hypnotized;

    //Liên quan tới tấn công
    public int attackPower;  //Sức tấn công
    protected Plant plant;   //Component Plant của cây đang bị tấn công

    protected Animator myAnimator;   //Component animation
    protected AudioSource audioSource;  //Component AudioSource của chính nó
    protected string audioOfBeingAttacked = "Sounds/Zombies/bodyhit";
    private int audioIndex = 1;

    static int orderOffset = 0;

    bool sleep = true;   //Có đứng yên lúc đầu không

    //Hai giá trị ngẫu nhiên do máy chủ quyết định, để hai máy sinh ra zombie giống hệt nhau
    [HideInInspector] public float netSpeedScale = 0f;   //Hệ số tăng tốc, 0 nghĩa là tự bốc ngẫu nhiên
    [HideInInspector] public float netSleepTime = -1f;   //Thời gian đứng yên lúc đầu, âm nghĩa là tự bốc

    protected virtual void Awake()
    {
        //Lấy component
        myAnimator = gameObject.GetComponent<Animator>();
        audioSource = gameObject.GetComponent<AudioSource>();
    }

    // Start is called before the first frame update
    protected virtual void Start()
    {
        //Zombie đứng yên ngẫu nhiên một lúc lúc đầu, để chúng không di chuyển đều tăm tắp
        if (sleep == true)
        {
            gameObject.SetActive(false);
            Invoke("activate", netSleepTime >= 0f ? netSleepTime : Random.Range(0.0f, 5.0f));
        }

        //Thêm mức tăng tốc độ ngẫu nhiên, chơi mạng thì lấy đúng hệ số máy chủ gửi sang
        float increase = netSpeedScale > 0f ? netSpeedScale : Random.Range(1.0f, 1.5f);
        speed *= increase;
        myAnimator.speed *= increase;

        bloodVolumeMax = bloodVolume;
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        UpdateTimedStatusEffects();

        //Máy khách không tự cho zombie đi, vị trí do NetZombieView kéo theo máy chủ
        if (!NetSession.IsAuthority) return;

        if (UpdateHypnotizedBehavior()) return;

        if (myAnimator.GetBool("Walk") == true)
        {
            transform.Translate(-speed * Time.deltaTime, 0, 0);
        }
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (hypnotized) return;
        Plant candidate = collision.GetComponent<Plant>();
        ImportedPlant importedCandidate = candidate != null ? candidate.GetComponent<ImportedPlant>() : null;
        if (collision.tag == "Plant" 
            && candidate != null
            && (importedCandidate == null || importedCandidate.CanBeEaten)
            && candidate.row == pos_row
            && collision.transform.position.x < transform.position.x + eatOffset
            && myAnimator.GetBool("Attack") == false)
        {
            myAnimator.SetBool("Walk", false);
            myAnimator.SetBool("Attack", true);

            plant = collision.GetComponent<Plant>();
        }
        else if (collision.tag == "GameOverLine")
        {
            //Chỉ máy chủ được tuyên bố thua, máy khách chờ gói tin kết thúc
            if (!NetSession.IsAuthority) return;
            GameObject.Find("Game Management").GetComponent<GameManagement>().gameOver();
        }
    }

    protected virtual void OnTriggerExit2D(Collider2D collision)
    {
        if (hypnotized) return;
        if (collision.tag == "Plant" && collision.GetComponent<Plant>().row == pos_row)
        {
            myAnimator.SetBool("Attack", false);
            myAnimator.SetBool("Walk", true);
        }
    }

    protected virtual void activate()
    {
        gameObject.SetActive(true);
    }

    public virtual void attack()
    {
        //Máy khách chỉ diễn hoạt ảnh gặm, sát thương do máy chủ tính
        if (!NetSession.IsAuthority || hypnotized) return;

        //Cây bị tấn công
        if (plant != null)
        {
            ImportedPlant imported = plant.GetComponent<ImportedPlant>();
            if (imported != null && imported.OnBitten(this)) return;
            plant.beAttacked(attackPower, "beEated");
            FireWallNutFusion fusion = plant.GetComponent<FireWallNutFusion>();
            if (fusion != null) fusion.OnBitten(this);
        }
    }

    protected virtual void die()
    {
        //Máy chủ báo cho máy khách trước khi xác biến mất
        NetGameplay.NotifyZombieDead(this, false);

        //Vô hiệu collider
        gameObject.GetComponent<Collider2D>().enabled = false;
        //Giảm một zombie trên toàn màn
        GameObject.Find("Zombie Management").GetComponent<ZombieManagement>().minusZombieNumAll();
        alive = false;
        //Ẩn đầu
        hideHead();
        //Chuyển animation
        myAnimator.SetBool("Walk", false);
        myAnimator.SetBool("Die", true);
    }

    //Vì phần đầu của mỗi zombie có thể khác nhau nên hàm này do lớp con ghi đè
    protected virtual void hideHead()
    {

    }

    //Bị tấn công
    public virtual void beAttacked(int hurt)
    {
        //Chơi mạng: máu do máy chủ giữ, máy khách nhận số máu qua gói đồng bộ
        if (!NetSession.IsAuthority) return;

        bloodVolume -= hurt;
        if (bloodVolume <= 0 && alive == true)
        {
            die();
        }
    }

    public virtual void playAudioOfBeingAttacked()
    {
        audioSource.PlayOneShot(
            Resources.Load<AudioClip>(audioOfBeingAttacked + audioIndex)
        );
        if (audioIndex == 1) audioIndex = 2;
        else audioIndex = 1;
    }

    //Bị thiêu, gọi khi trúng đòn lửa
    public virtual void beBurned()
    {
        Thaw();
        beAttacked(10);
    }

    protected void UpdateTimedStatusEffects()
    {
        if (frozen && Time.time >= freezeEndTime)
        {
            float previousMultiplier = slowMultiplier;
            slowMultiplier = 0.5f;
            if (previousMultiplier > 0f)
            {
                speed = speed / previousMultiplier * slowMultiplier;
                if (myAnimator != null) myAnimator.speed = myAnimator.speed / previousMultiplier * slowMultiplier;
            }
            frozen = false;
        }
        if (slowed && Time.time >= slowEndTime) ClearSlow();
    }

    public void applyBurn(int damagePerTick, float duration)
    {
        burnDamagePerTick = Mathf.Max(burnDamagePerTick, damagePerTick);
        burnEndTime = Mathf.Max(burnEndTime, Time.time + duration);
        if (!burning)
        {
            burning = true;
            InvokeRepeating("burnTick", 0f, 1f);
            setBurnColor(true);
        }
    }

    public void ApplySlow(float multiplier, float duration)
    {
        multiplier = Mathf.Clamp(multiplier, 0.1f, 1f);
        if (!slowed)
        {
            slowed = true;
            slowMultiplier = multiplier;
            speed *= slowMultiplier;
            if (myAnimator != null) myAnimator.speed *= slowMultiplier;
        }
        slowEndTime = Mathf.Max(slowEndTime, Time.time + duration);
        state = ZombieState.Cold;
    }

    public void ApplyFreeze(float immobilizeDuration, float chilledDuration)
    {
        if (slowed) ClearSlow();
        slowed = true;
        frozen = true;
        slowMultiplier = 0.01f;
        speed *= slowMultiplier;
        if (myAnimator != null) myAnimator.speed *= slowMultiplier;
        freezeEndTime = Time.time + Mathf.Max(0f, immobilizeDuration);
        slowEndTime = freezeEndTime + Mathf.Max(0f, chilledDuration);
        state = ZombieState.Cold;
    }

    public void Thaw()
    {
        if (slowed) ClearSlow();
        frozen = false;
    }

    private void ClearSlow()
    {
        if (!slowed) return;
        speed /= slowMultiplier;
        if (myAnimator != null) myAnimator.speed /= slowMultiplier;
        slowed = false;
        slowMultiplier = 1f;
        if (state == ZombieState.Cold) state = hypnotized ? ZombieState.Hypnotized : ZombieState.Normal;
    }

    public void Hypnotize()
    {
        if (hypnotized || !alive) return;
        hypnotized = true;
        plant = null;
        state = ZombieState.Hypnotized;
        nextHypnotizedBite = Time.time;
        Vector3 scale = transform.localScale;
        scale.x = -Mathf.Abs(scale.x);
        transform.localScale = scale;
        foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>(true))
        {
            Color color = renderer.color;
            renderer.color = new Color(0.72f, 1f, 0.72f, color.a);
        }
        OnHypnotized();
    }

    protected virtual void OnHypnotized()
    {
        SetAnimatorBoolIfPresent("Attack", false);
        SetAnimatorBoolIfPresent("Walk", true);
    }

    protected bool UpdateHypnotizedBehavior()
    {
        if (!hypnotized) return false;
        if (!NetSession.IsAuthority) return true;

        if (Time.time >= nextHypnotizedScan)
        {
            nextHypnotizedScan = Time.time + 0.15f;
            hypnotizedTarget = null;
            float nearest = float.MaxValue;
            foreach (Zombie candidate in FindObjectsByType<Zombie>())
            {
                if (candidate == this || candidate.IsHypnotized || !candidate.alive ||
                    !candidate.gameObject.activeInHierarchy || candidate.pos_row != pos_row) continue;
                float distance = candidate.transform.position.x - transform.position.x;
                if (distance >= -0.15f && distance < nearest)
                {
                    nearest = distance;
                    hypnotizedTarget = candidate;
                }
            }
        }

        if (hypnotizedTarget != null && hypnotizedTarget.alive &&
            Mathf.Abs(hypnotizedTarget.transform.position.x-transform.position.x) <= 0.62f)
        {
            SetAnimatorBoolIfPresent("Walk", false);
            SetAnimatorBoolIfPresent("Attack", true);
            if (Time.time >= nextHypnotizedBite)
            {
                hypnotizedTarget.playAudioOfBeingAttacked();
                hypnotizedTarget.beAttacked(attackPower);
                nextHypnotizedBite = Time.time + 1f;
            }
        }
        else
        {
            SetAnimatorBoolIfPresent("Attack", false);
            SetAnimatorBoolIfPresent("Walk", true);
            transform.Translate(speed * Time.deltaTime, 0f, 0f, Space.World);
        }

        if (transform.position.x > 7f)
        {
            GameObject manager = GameObject.Find("Zombie Management");
            if (manager != null) manager.GetComponent<ZombieManagement>()?.minusZombieNumAll();
            Destroy(gameObject);
        }
        return true;
    }

    private void SetAnimatorBoolIfPresent(string parameter, bool value)
    {
        if (myAnimator == null) return;
        foreach (AnimatorControllerParameter item in myAnimator.parameters)
            if (item.type == AnimatorControllerParameterType.Bool && item.name == parameter)
            {
                myAnimator.SetBool(parameter, value);
                return;
            }
    }

    private void burnTick()
    {
        if (!alive || Time.time >= burnEndTime)
        {
            burning = false;
            CancelInvoke("burnTick");
            setBurnColor(false);
            return;
        }
        beAttacked(burnDamagePerTick);
    }

    private void setBurnColor(bool value)
    {
        foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>(true))
        {
            Color color = renderer.color;
            renderer.color = value
                ? new Color(1f, 0.48f, 0.16f, color.a)
                : new Color(1f, 1f, 1f, color.a);
        }
    }

    public virtual void beSquashed()
    {
        //Máy khách chờ máy chủ báo, không tự nghiền chết zombie
        if (!NetSession.IsAuthority) return;

        bloodVolume -= 1800;
        if(bloodVolume <= 0)
        {
            NetGameplay.NotifyZombieDead(this, true);
            //Giảm một zombie trên toàn màn
            GameObject.Find("Zombie Management").GetComponent<ZombieManagement>().minusZombieNumAll();
            //Zombie biến mất
            Destroy(gameObject);
        }
    }

    //Máy chủ báo zombie này đã chết, máy khách diễn lại y hệt
    public void applyNetworkDeath(bool squashed)
    {
        if (squashed)
        {
            GameObject.Find("Zombie Management").GetComponent<ZombieManagement>().minusZombieNumAll();
            Destroy(gameObject);
            return;
        }

        if (!alive) return;
        bloodVolume = 0;
        die();
    }

    public void beParasiticed(Plant parasiticPlant)
    {
        if(state != ZombieState.Parasiticed)
        {
            SpriteRenderer[] spriteRenderers = gameObject.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (SpriteRenderer spriteRenderer in spriteRenderers)
            {
                spriteRenderer.color = new Color(0.4f, 1, 0.4f, spriteRenderer.color.a);
            }
            this.parasiticPlant = parasiticPlant;
            state = ZombieState.Parasiticed;
            InvokeRepeating("suckBlood", 0, 1f);
        }
    }

    private void suckBlood()
    {
        int hurt = (int)(bloodVolumeMax * 0.01);
        beAttacked(hurt);
        if (parasiticPlant != null) parasiticPlant.recover(hurt);
    }

    public void cancelSleep()
    {
        if(gameObject.activeSelf == false)
        {
            gameObject.SetActive(true);
        }
        sleep = false;
    }

    //Đặt hàng đang đứng, rồi dựa vào hàng đó để đặt thứ tự hiển thị
    public virtual void setPosRow(int pos)
    {
        //Đặt hàng đang đứng
        pos_row = pos;

        //Đặt sorting layer và thứ tự hiển thị
        SpriteRenderer[] spriteRenderers = gameObject.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer spriteRenderer in spriteRenderers)
        {
            if (spriteRenderer.sortingLayerName == "Default")
            {
                spriteRenderer.sortingLayerName = "Zombie-" + pos_row;
                spriteRenderer.sortingOrder += orderOffset * 20;
            }
        }
        orderOffset++;
    }

    //Phát âm thanh zombie ngã xuống
    public virtual void fallDown()
    {
        audioSource.PlayOneShot(
            Resources.Load<AudioClip>("Sounds/Zombies/zombie_falling")
        );
    }

    //Phát âm thanh zombie gặm
    public virtual void PlayEatAudio()
    {
        audioSource.PlayOneShot(
            Resources.Load<AudioClip>("Sounds/Zombies/chomp" + Random.Range(1, 3))
        );
    }

    //Xác zombie biến mất sau khi ngã xuống
    public void disappear()
    {
        Destroy(gameObject);
    }

}

public enum ZombieState { Normal, Cold, Parasiticed, Hypnotized }
