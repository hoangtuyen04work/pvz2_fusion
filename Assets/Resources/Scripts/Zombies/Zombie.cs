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
    private bool alive = true;
    private bool burning;
    private float burnEndTime;
    private int burnDamagePerTick;

    //Liên quan tới tấn công
    public int attackPower;  //Sức tấn công
    protected Plant plant;   //Component Plant của cây đang bị tấn công

    protected Animator myAnimator;   //Component animation
    protected AudioSource audioSource;  //Component AudioSource của chính nó
    protected string audioOfBeingAttacked = "Sounds/Zombies/bodyhit";
    private int audioIndex = 1;

    static int orderOffset = 0;

    bool sleep = true;   //Có đứng yên lúc đầu không

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
            Invoke("activate", Random.Range(0.0f, 5.0f));
        }

        //Thêm mức tăng tốc độ ngẫu nhiên
        float increase = Random.Range(1.0f, 1.5f);
        speed *= increase;
        myAnimator.speed *= increase;

        bloodVolumeMax = bloodVolume;
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        if (myAnimator.GetBool("Walk") == true)
        {
            transform.Translate(-speed * Time.deltaTime, 0, 0);
        }
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Plant" 
            && collision.GetComponent<Plant>().row == pos_row 
            && collision.transform.position.x < transform.position.x + eatOffset
            && myAnimator.GetBool("Attack") == false)
        {
            myAnimator.SetBool("Walk", false);
            myAnimator.SetBool("Attack", true);

            plant = collision.GetComponent<Plant>();
        }
        else if (collision.tag == "GameOverLine")
        {
            GameObject.Find("Game Management").GetComponent<GameManagement>().gameOver();
        }
    }

    protected virtual void OnTriggerExit2D(Collider2D collision)
    {
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
        //Cây bị tấn công
        if (plant != null)
        {
            plant.beAttacked(attackPower, "beEated");
            FireWallNutFusion fusion = plant.GetComponent<FireWallNutFusion>();
            if (fusion != null) fusion.OnBitten(this);
        }
    }

    protected virtual void die()
    {
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
        beAttacked(10);
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
        bloodVolume -= 1800;
        if(bloodVolume <= 0)
        {
            //Giảm một zombie trên toàn màn
            GameObject.Find("Zombie Management").GetComponent<ZombieManagement>().minusZombieNumAll();
            //Zombie biến mất
            Destroy(gameObject);
        }
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

public enum ZombieState { Normal, Cold, Parasiticed }
