using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlowerSun : SunBase
{
    protected Vector3 highestPoint, lowestPoint;

    //0: đi lên, 1: đi xuống, 2: dừng
    int moveState = 0;

    protected override void Start()
    {
        base.Start();

        float xOffset = netRandom(-0.4f, 0.4f);
        highestPoint = transform.position +
                        new Vector3(xOffset, 0.5f, 0);
        lowestPoint = transform.position +
                        new Vector3(xOffset, -0.5f, 0);

        //Ghi danh với bộ đồng bộ sau khi đã có đủ thông tin ngẫu nhiên
        NetGameplay.RegisterSun(this);
    }

    public override void drop()
    {
        if(moveState == 0)
        {
            transform.Translate((highestPoint - transform.position) * 2 * Time.deltaTime);
            if (Vector3.Distance(transform.position, highestPoint) < 0.05f) moveState = 1;
        }
        else if(moveState == 1)
        {
            transform.Translate((lowestPoint - transform.position) * 2 * Time.deltaTime);
            if (Vector3.Distance(transform.position, lowestPoint) < 0.05f) moveState = 2;
        }
        else
        {
            dropState = false;
        }
    }
}
