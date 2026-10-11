using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SunBase : MonoBehaviour
{
    public int sunNumber;   //Trị giá bao nhiêu nắng

    protected bool dropState;  //Nắng có đang rơi không
    bool pickState;  //Nắng đã được nhặt chưa
    Vector3 finalPos = new Vector3(-4.47f, 2.61f, 0f);  //Điểm cuối của animation nhặt nắng
    float timer = 0, disappearTime = 15.0f;   //Bộ đếm giờ, bao lâu thì nắng biến mất
    SpriteRenderer mySpriteRenderer;   //Dùng để nắng mờ dần rồi biến mất

    //Dùng để cộng thêm nắng
    SunNumber sunControl;

    //Phần dành cho chơi mạng: id do máy chủ cấp, và giá trị ngẫu nhiên do máy chủ quyết định
    [HideInInspector] public int netId;
    [HideInInspector] public float netParam;
    [HideInInspector] public bool netParamSet;

    // Start is called before the first frame update
    protected virtual void Start()
    {
        EndlessModifierSystem.ApplyToSun(this);
        dropState = true;
        pickState = false;
        mySpriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        sunControl = GameObject.Find("Sun Text").GetComponent<SunNumber>();
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        if (dropState == true) drop();
        if (pickState == true) collect();
        if (pickState == false && timer > disappearTime) disappear();
    }

    public abstract void drop();

    //Bốc một số ngẫu nhiên, nhưng nếu máy chủ đã gửi sẵn giá trị thì dùng đúng giá trị đó,
    //nhờ vậy mặt trời ở hai máy rơi giống hệt nhau.
    protected float netRandom(float min, float max)
    {
        if (netParamSet) return netParam;

        netParam = Random.Range(min, max);
        netParamSet = true;
        return netParam;
    }

    private Vector3 targetPos = new Vector3(-4.47f, 2.61f, 0f);
    public bool pickedByZombie;

    public void bePickedUp()
    {
        bePickedUp(NetSession.IsOnline && NetSession.ControlsZombies);
    }

    public void bePickedUp(bool byZombie)
    {
        dropState = false;
        pickState = true;
        pickedByZombie = byZombie;

        // Nếu người nhặt là Zombie, bay về phía góc dưới bên trái (khay não Zombie)
        if (byZombie)
        {
            targetPos = new Vector3(-4.2f, -2.8f, 0f);
        }
        else
        {
            targetPos = finalPos;
        }

        //Phát âm thanh
        GetComponent<AudioSource>().Play();
    }

    private void collect()
    {
        if (Vector3.Distance(transform.position, targetPos) > 0.1f)  //Chưa tới điểm cuối thì di chuyển về phía điểm cuối
        {
            transform.Translate((targetPos - transform.position) * 4 * Time.deltaTime);
        }
        else   //Tới điểm cuối, cộng số nắng hoặc não, huỷ GameObject này
        {
            //Chỉ máy chủ cộng nắng nếu không phải nhặt bởi zombie, máy khách nhận tổng số nắng qua gói tin
            if (NetSession.IsAuthority && !pickedByZombie)
            {
                if (sunControl != null) sunControl.addSun(sunNumber);
            }
            Destroy(gameObject);
        }
    }

    private void disappear()
    {
        float alpha = 1 - (timer - disappearTime) * 3;
        if (alpha > 0) mySpriteRenderer.color = new Color(255, 255, 255, alpha);
        else Destroy(gameObject);
    }
}
