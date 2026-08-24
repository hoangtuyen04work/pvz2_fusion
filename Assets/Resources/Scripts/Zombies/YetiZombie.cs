using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class YetiZombie : Zombie
{
    protected override void activate()
    {
        base.activate();
        audioSource.clip = Resources.Load<AudioClip>("Sounds/Zombies/yetiroar");
        audioSource.Play();
    }

    public override void beBurned()
    {
        beAttacked(20);
    }

    protected override void hideHead()
    {
        transform.Find("head").gameObject.SetActive(false);
        transform.Find("jaw").gameObject.SetActive(false);
    }

    //Phát âm thanh zombie ngã xuống
    public override void fallDown()
    {
        audioSource.clip = Resources.Load<AudioClip>("Sounds/Zombies/yetifall");
        audioSource.Play();
    }

    //Phát âm thanh zombie gặm
    public override void PlayEatAudio()
    {
        audioSource.clip = Resources.Load<AudioClip>("Sounds/Zombies/chomp_yeti");
        audioSource.Play();
    }
}
