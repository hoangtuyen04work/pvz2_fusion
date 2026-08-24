using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;

public class ChineseZombie : Zombie
{
    public bool isCaptain = false;   //Có phải đội trưởng không
    public ChineseZombie prior;   //Zombie phía trước trong đội
    public ChineseZombie next;    //Zombie phía sau trong đội

    bool isAttacking = false;   //Có đang ăn không, dùng để đổi animation khi lá bùa rơi

    //Ghi đè hàm Start, không cộng thêm tốc độ ngẫu nhiên
    protected override void Start()
    {
        //Có xác suất phát cuồng sau một khoảng thời gian
        if(Random.Range(0.0f, 10.0f) < 4.0f)
        {
            Invoke("paperDisappear", Random.Range(15.0f, 30.0f));
        }
    }

    protected override void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Plant" && collision.GetComponent<Plant>().row == pos_row)
        {
            myAnimator.SetBool("Walk", false);
            myAnimator.SetBool("Attack", true);
            //Không phải đội trưởng thì thăng lên làm đội trưởng
            if(!isCaptain)
            {
                isCaptain = true;
                prior.next = null;
                prior = null;
            }
            //Đội trưởng bắt đầu ăn, cả đội dừng tiến
            stopFollower();
            plant = collision.GetComponent<Plant>();
        }
        else if (collision.tag == "GameOverLine")
        {
            GameObject.Find("Game Management").GetComponent<GameManagement>().gameOver();
        }
    }

    protected override void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "Plant" && collision.GetComponent<Plant>().row == pos_row)
        {
            myAnimator.SetBool("Attack", false);
            myAnimator.SetBool("Walk", true);
            //Đội trưởng bắt đầu tiến, cả đội cùng tiến
            if (isCaptain) startFollower();
        }
    }

    private void paperDisappear()
    {
        //Chuyển animation
        if(myAnimator.GetBool("Attack"))
        {
            isAttacking = true;
            myAnimator.SetBool("Mad", true);
        }
        else
        {
            myAnimator.SetBool("Walk", false);
            myAnimator.SetBool("Mad", true);
        }
    }

    private void mad()
    {
        //Lá bùa biến mất
        transform.Find("paper").gameObject.SetActive(false);

        //Đổi hình dạng đầu
        transform.Find("head").GetComponent<SpriteResolver>()
            .SetCategoryAndLabel("Head", "Mad");

        audioSource.PlayOneShot(
            Resources.Load<AudioClip>("Sounds/Zombies/zombie_angry")
        );

        //Đổi animation trở lại
        if (!isAttacking)
        {
            myAnimator.SetBool("Walk", true);
        }
        myAnimator.SetBool("Mad", false);

        //Tăng tốc di chuyển
        float randAmp = Random.Range(1.5f, 2.0f);  //Mức tăng ngẫu nhiên
        speed *= randAmp;
        myAnimator.speed *= randAmp;

        attackPower *= 2;

        //Kẻ phát cuồng tự tách thành đội riêng
        isCaptain = true;
        startFollower();
        if (prior != null) prior.next = null;
        if (next != null) next.isCaptain = true;
        prior = null;
        next = null;
    }

    private void stopFollower()
    {
        for (ChineseZombie nowCZ = this; nowCZ.next != null; nowCZ = nowCZ.next)
        {
            nowCZ.next.stopWalk();
        }
    }

    private void startFollower()
    {
        for (ChineseZombie nowCZ = this; nowCZ.next != null; nowCZ = nowCZ.next)
        {
            nowCZ.next.startWalk();
        }
    }

    protected override void die()
    {
        //Đội trưởng chết, zombie phía sau thăng lên làm đội trưởng
        if(next != null)
        {
            next.isCaptain = true;
            next.prior = null;
        }

        base.die();
    }

    public void playSkipAudio()
    {
        audioSource.Play();
    }

    public void stopWalk()
    {
        myAnimator.SetBool("Walk", false);
    }

    public void startWalk()
    {
        myAnimator.SetBool("Walk", true);
    }
}
