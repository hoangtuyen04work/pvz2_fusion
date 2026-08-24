using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SunFlower : Plant
{
    public GameObject flowersunPrefab;   //Prefab mặt trời của Hướng Dương

    Transform sunManagement;   //Component Transform của đối tượng quản lý mặt trời, là cha của mọi mặt trời
    float createSunSpeed = 24f;

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();

        sunManagement = GameObject.Find("Sun Management").GetComponent<Transform>();

        Invoke("createSun", 5);
    }

    private void createSun()
    {
        //Phát âm thanh
        audioSource.Play();

        //Sinh ra mặt trời
        Instantiate(flowersunPrefab, transform.position, Quaternion.Euler(0, 0, 0), sunManagement);

        Invoke("createSun", createSunSpeed);
    }

    public override void cold()
    {
        base.cold();
        createSunSpeed = 48f;
    }

    public override void warm()
    {
        base.warm();
        createSunSpeed = 24f;
    }

    public override void normal()
    {
        base.normal();
        createSunSpeed = 24f;
    }

    protected override void intensify_specific()
    {
        GetComponent<Animator>().speed = 1.5f;
        createSunSpeed = 16f;
    }

    protected override void cancelIntensify_specific()
    {
        GetComponent<Animator>().speed = 1f;
        createSunSpeed = 24f;
    }
}
