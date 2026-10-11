using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeafKnife : StraightBullet
{
    private MiaoMiao myCreater;

    protected override void attack(Zombie target)
    {
        //Phát âm thanh
        AudioSource.PlayClipAtPoint(
            Resources.Load<AudioClip>("Sounds/Plants/KnifeKill"),
            new Vector3(0, 0, -10)
        );
        //Zombie bị tấn công
        target.beAttacked(hurt);

        //Nếu zombie chưa bị ký sinh thì báo Mèo Miu bắn hạt ký sinh
        if (target.state != ZombieState.Parasiticed && myCreater != null)
            myCreater.prepareParasitic = true;
    }

    public void initialize(MiaoMiao miao, int row)
    {
        myCreater = miao;
        this.row = row;
    }

    public void initialize(MiaoMiao miao, int row, int hurt)
    {
        myCreater = miao;
        this.row = row;
        this.hurt = hurt;
    }
}
