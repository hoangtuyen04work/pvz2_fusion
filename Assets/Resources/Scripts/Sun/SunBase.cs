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
            sunControl.addSun(sunNumber);
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
