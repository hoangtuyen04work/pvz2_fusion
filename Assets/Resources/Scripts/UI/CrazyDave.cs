using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrazyDave : MonoBehaviour
{
    GameObject speechBubble;   //Đối tượng con - khung thoại của Dave

    string dialogToBeShowed;   //Câu thoại chờ nói
    AudioSource audioSource;   //Component AudioSource của chính nó

    private void Awake()
    {
        //Lấy đối tượng và component
        speechBubble = transform.Find("SpeechBubble").gameObject;
        speechBubble.SetActive(false);
        audioSource = gameObject.GetComponent<AudioSource>();
    }

    public void talk(string content)
    {
        GetComponent<Animator>().SetBool("talk", true);
        dialogToBeShowed = content;
    }

    public void smallTalk(string content)
    {
        GetComponent<Animator>().SetBool("smallTalk", true);
        dialogToBeShowed = content;
    }

    //Sự kiện gọi ở đầu animation talk
    public void showSpeechBubble_talk()
    {
        GetComponent<Animator>().SetBool("talk", false);   //Đặt cờ thành false, tức chỉ nói một lần
        speechBubble.GetComponent<SpeechBubble>().showDialog(dialogToBeShowed);   //Hiển thị hội thoại
        //Phát âm thanh
        audioSource.clip =
            Resources.Load<AudioClip>("Sounds/CrazyDave/CrazyDave_Talk" + Random.Range(1, 4));
        audioSource.Play();
    }

    //Sự kiện gọi ở đầu animation smallTalk
    public void showSpeechBubble_smallTalk()
    {
        GetComponent<Animator>().SetBool("smallTalk", false);   //Đặt cờ thành false, tức chỉ nói một lần
        speechBubble.GetComponent<SpeechBubble>().showDialog(dialogToBeShowed);   //Hiển thị hội thoại
        //Phát âm thanh
        audioSource.clip =
            Resources.Load<AudioClip>("Sounds/CrazyDave/CrazyDave_Short" + Random.Range(1, 4));
        audioSource.Play();
    }

    public void leave()
    {
        GetComponent<Animator>().SetBool("leave", true);
    }

    public void haveGone()
    {
        gameObject.SetActive(false);
    }
}
