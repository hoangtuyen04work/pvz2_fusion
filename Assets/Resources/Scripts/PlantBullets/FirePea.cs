using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirePea : StraightBulletAnimationSwitch
{

    protected override void attack(Zombie target)
    {
        //Zombie hứng đòn tấn công
        target.beAttacked(hurt);
        target.beBurned();
    }
}
