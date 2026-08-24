using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PeaShooterSingle : Plant
{
    public GameObject pea;  //Prefab đạn

    public void fireEvent()
    {
        //Sinh ra hạt đậu
        Instantiate(pea,
                    transform.position + new Vector3(0.4f, 0.14f, 0),
                    Quaternion.Euler(0, 0, 0))
            .GetComponent<StraightBullet>().initialize(row);

        //Phát âm thanh
        audioSource.Play();
    }

}
