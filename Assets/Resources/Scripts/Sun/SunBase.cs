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

    public void bePickedUp()
    {
        dropState = false;
        pickState = true;

        //Phát âm thanh
        GetComponent<AudioSource>().Play();
    }

    private void collect()
    {
        if (Vector3.Distance(transform.position, finalPos) > 0.1f)  //Chưa tới điểm cuối thì di chuyển về phía điểm cuối
        {
            transform.Translate((finalPos - transform.position) * 4 * Time.deltaTime);
        }
        else   //Tới điểm cuối, cộng số nắng, huỷ GameObject này
        {
            //Chỉ máy chủ cộng nắng, máy khách nhận tổng số nắng qua gói tin
            if (NetSession.IsAuthority) sunControl.addSun(sunNumber);
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
