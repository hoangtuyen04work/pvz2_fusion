using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoneZombie : Zombie
{
    int lifeNumber = 3;   //Còn lại mấy mạng

    protected override void Start()
    {
        base.Start();
    }

    public override void beAttacked(int hurt)
    {
        bloodVolume -= hurt;
        if (bloodVolume <= 0)
        {
            split();
        }
    }

    private void split()
    {
        //Vô hiệu collider
        gameObject.GetComponent<Collider2D>().enabled = false;
        //Giảm mạng
        lifeNumber--;
        if (lifeNumber <= 0)
        {
            //Giảm một zombie trên toàn màn
            GameObject.Find("Zombie Management").GetComponent<ZombieManagement>().minusZombieNumAll();
            //Zombie biến mất
            Invoke("disappear", 2f);
        }
        //Chuyển animation
        myAnimator.SetBool("Walk", false);
        myAnimator.SetBool("Die", true);
        //Hồi sinh sau một khoảng thời gian ngẫu nhiên
        Invoke("revive", Random.Range(20.0f, 30.0f));
    }

    private void revive()
    {
        //Chuyển animation
        myAnimator.SetBool("Die", false);
        myAnimator.SetBool("Walk", true);
        //Hồi máu
        bloodVolume = bloodVolumeMax;
        //Kích hoạt collider
        gameObject.GetComponent<Collider2D>().enabled = true;
    }
}
