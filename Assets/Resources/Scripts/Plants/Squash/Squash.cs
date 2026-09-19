using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Squash : Plant
{
    public BoxCollider2D collider_Idle;   //Collider dùng để dò zombie lại gần lúc đang nhàn rỗi
    public Collider2D collider_attack;   //Collider dùng để đè bẹp zombie

    public GameObject lockedZombie;   //Zombie đã khoá mục tiêu

    public Animator animator;   //Component Animator của chính nó

    bool jumpingUp = false;   //Có đang nhảy lên không
    bool jumpingDown = false;   //Có đang rơi xuống không
    public bool idle = true;
    Vector3 speed_jumpUp;   //Tốc độ nhảy lên
    Vector3 speed_jumpDown;   //Tốc độ rơi xuống

    protected override void Awake()
    {
        //Lấy component
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if(jumpingUp == true)
        {
            transform.Translate(speed_jumpUp * Time.deltaTime);
        }
        else if(jumpingDown == true)
        {
            transform.Translate(speed_jumpDown * Time.deltaTime);
        }
    }

    public override int beAttacked(int hurt, string form)
    {
        if(idle == true)
        {
            bloodVolume -= hurt;
            if (bloodVolume <= 0)
            {
                die(form);
            }
        }
        return bloodVolume;
    }

    public void hmm()
    {
        audioSource.Play();
    }

    public void jumpUp()
    {
        Vector3 peak = lockedZombie.transform.position + new Vector3(0, 1.3f, 0);
        Vector3 destination = new Vector3(lockedZombie.transform.position.x, transform.position.y, 0);
        speed_jumpUp = (peak - transform.position) / 0.133f;   //Animation nhảy lên 0.133 giây
        speed_jumpDown = (destination - peak) / 0.117f;   //Animation rơi xuống 0.117 giây
        transform.Find("Shadow").gameObject.SetActive(false);
        transform.Find("Halo").gameObject.SetActive(false);
        GetComponent<SpriteRenderer>().sortingLayerName = "PlantBullet";
        //Vô hiệu collider của Bí Ngòi, tránh việc nhảy lên rồi lại bị ăn
        GetComponent<BoxCollider2D>().enabled = false;
        jumpingUp = true;
    }

    public void reachPeak()
    {
        jumpingUp = false;
    }

    public void jumpDown()
    {
        jumpingDown = true;
    }

    public void reachDest()
    {
        collider_attack.enabled = false;
        jumpingDown = false;
    }

    public void squashZombie()
    {
        collider_attack.enabled = true;
        audioSource.PlayOneShot(
            Resources.Load<AudioClip>("Sounds/Plants/SquashFall")
        );
    }

    public void disappear()
    {
        die("");
    }

    //Animation của Bí Ngòi không chậm lại khi bị lạnh, nếu không giữ nguyên tốc độ nhảy sẽ nhảy rất xa
    public override void cold()
    {
        if (state == PlantState.Normal)
        {
            state = PlantState.Cold;
            GetComponent<AudioSource>().PlayOneShot(Resources.Load<AudioClip>("Sounds/Plants/frozen"));
            GetComponent<SpriteRenderer>().color = new Color(0.33f, 0.54f, 1f);
            Invoke("coldHurt", 1f);
        }
    }

    protected override void intensify_specific()
    {
        Vector2 colliderIdleSize = collider_Idle.size;
        collider_Idle.size = new Vector2(colliderIdleSize.x * 2, colliderIdleSize.y);
    }

    protected override void cancelIntensify_specific()
    {
        Vector2 colliderIdleSize = collider_Idle.size;
        collider_Idle.size = new Vector2(colliderIdleSize.x / 2, colliderIdleSize.y);
    }
}
