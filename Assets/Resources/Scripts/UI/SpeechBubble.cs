using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpeechBubble : MonoBehaviour
{
    bool waitingShow = false;
    string text = "";

    // Update is called once per frame
    void Update()
    {
        //Bấm chuột trái thì biến mất
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            gameObject.SetActive(false);
            if(waitingShow) showDialog(text);
        }
    }

    public void showDialog(string content)
    {
        if(gameObject.activeSelf)
        {
            waitingShow = true;
            text = content;
        }
        else
        {
            waitingShow = false;
            transform.Find("Dialog-Text").GetComponent<Text>().text = content;
            gameObject.SetActive(true);
        }
    }
}
