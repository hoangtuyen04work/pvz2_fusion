using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ghost : Zombie
{

    protected override void Start()
    {
        base.Start();

        //Xuất hiện ở vị trí ngẫu nhiên trong nửa sau bãi cỏ
        transform.localPosition = new Vector3( Random.Range(1.0f, 4.0f), transform.localPosition.y, 0);

        bloodVolume = Random.Range(60, 200);
    }

    //Ghi đè thành rỗng
    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "GameOverLine")
        {
            die();
        }
    }

    //Ghi đè thành rỗng
    protected override void OnTriggerExit2D(Collider2D collision)
    {

    }

    protected override void die()
    {
        //Vô hiệu collider
        gameObject.GetComponent<Collider2D>().enabled = false;
        //Chuyển animation
        myAnimator.SetBool("Walk", false);
        myAnimator.SetBool("Die", true);
    }

    //Bị tấn công
    public override void beAttacked(int hurt)
    {
        bloodVolume -= hurt;
        if (bloodVolume <= 0)
        {
            die();
        }
    }

    public override void beSquashed()
    {
        die();
    }
}
