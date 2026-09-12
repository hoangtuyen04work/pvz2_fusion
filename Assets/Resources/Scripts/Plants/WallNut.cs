using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Cây Hạt Dẻ Tường.
public class WallNut : Plant
{
    float crackedPoint1, crackedPoint2;

    protected override void Start()
    {
        base.Start();

        crackedPoint1 = bloodVolume * 2 / 3;
        crackedPoint2 = bloodVolume / 3;
    }
    public override int beAttacked(int hurt, string form)
    {
        //Nếu đang ở trạng thái tăng cường thì phòng thủ tăng lên
        if (intensified) hurt = (int)(hurt * 0.75);

        bloodVolume -= hurt;
        if (bloodVolume <= 0)
        {
            die(form);
        }
        else if (bloodVolume  <= crackedPoint2)
        {
            GetComponent<Animator>().SetBool("Cracked2", true);
        }
        else if (bloodVolume <= crackedPoint1)
        {
            GetComponent<Animator>().SetBool("Cracked1", true);
        }
        return bloodVolume;
    }
}
