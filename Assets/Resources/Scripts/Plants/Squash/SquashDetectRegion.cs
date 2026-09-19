using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SquashDetectRegion : MonoBehaviour
{
    public Squash squash;   //Bí Ngòi

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Zombie")
        {
            Zombie zombie=collision.GetComponent<Zombie>();
            if(zombie==null || zombie.IsHypnotized) return;
            if (squash.collider_Idle.enabled == true)
            {
                //Khoá mục tiêu zombie
                squash.lockedZombie = collision.gameObject;

                //Xác định zombie ở bên trái hay bên phải
                if (squash.lockedZombie.transform.position.x > transform.position.x)
                    squash.animator.SetBool("LookRight", true);
                else squash.animator.SetBool("LookLeft", true);

                squash.collider_Idle.enabled = false;   //Vô hiệu collider dò tìm
                squash.idle = false;
            }
            else if (squash.collider_attack.enabled == true)
            {
                if (zombie.pos_row == squash.row)
                {
                    zombie.beSquashed();
                }
            }
        }
    }
}
