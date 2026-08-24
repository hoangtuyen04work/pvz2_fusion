using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EndMenu : MonoBehaviour
{
    public Text dialogText;   //Component Text của đối tượng con DialogText, dùng để cập nhật font
    public AudioSource backgroundAudio;   //Component phát nhạc nền

    public void gameOver()
    {
        //Hiển thị giao diện
        Time.timeScale = 0;
        dialogText.text = "Zombie đã ăn mất não bạn";
        dialogText.color = new Color(0.06f, 0.79f, 0.11f);
        gameObject.SetActive(true);

        //Phát âm thanh
        backgroundAudio.Stop();
        GetComponent<AudioSource>().clip =
            Resources.Load<AudioClip>("Sounds/UI/loseMusic");
        GetComponent<AudioSource>().Play();
    }

    public void win()
    {
        Invoke("win_real", 5f);
    }

    private void win_real()
    {
        //Hiển thị giao diện
        Time.timeScale = 0;
        dialogText.text = "Bạn đã đẩy lùi được lũ zombie";
        dialogText.color = new Color(0.89f, 0.76f, 0.37f);
        gameObject.SetActive(true);

        //Phát âm thanh
        backgroundAudio.Stop();
        GetComponent<AudioSource>().clip =
            Resources.Load<AudioClip>("Sounds/UI/winMusic");
        GetComponent<AudioSource>().Play();
    }

    public void exitGame()
    {
        Application.Quit();
    }
}
