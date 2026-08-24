using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StraightBullet : MonoBehaviour
{
    public float speed = 4;   //Tốc độ đạn
    public int hurt;  //Sát thương đạn
    public Sprite boomSprite;

    protected bool boomState = false;  //Đạn đã nổ chưa
    protected int row;

    // Update is called once per frame
    void Update()
    {
        if (boomState == false)  //Chưa nổ thì bay tiếp về phía trước
        {
            transform.Translate(speed * Time.deltaTime, 0, 0);
        }
    }

    protected virtual void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Zombie" && boomState == false && row == collision.GetComponent<Zombie>().pos_row)
        {
            boom();
            attack(collision.GetComponent<Zombie>());
        }
        else if (collision.tag == "BulletDisappearLine")
        {
            Destroy(gameObject);
        }
    }

    protected virtual void boom()
    {
        boomState = true;

        //Đổi sang ảnh nổ
        gameObject.GetComponent<SpriteRenderer>().sprite = boomSprite;
        Invoke("disappear", 0.1f);
    }

    protected virtual void attack(Zombie target)
    {
        //Phát âm thanh
        target.playAudioOfBeingAttacked();
        //Zombie bị tấn công
        target.beAttacked(hurt);
    }

    protected void disappear()
    {
        Destroy(gameObject);
    }

    public void initialize(int row)
    {
        this.row = row;
    }

    public void initialize(int row, int hurt)
    {
        this.row = row;
        this.hurt = hurt;
    }
}
