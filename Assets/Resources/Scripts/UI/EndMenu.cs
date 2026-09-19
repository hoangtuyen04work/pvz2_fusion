using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EndMenu : MonoBehaviour
{
    public Text dialogText;   //Component Text của đối tượng con DialogText, dùng để cập nhật font
    public AudioSource backgroundAudio;   //Component phát nhạc nền

    //Zombie chạm vạch: phe cây thua, phe zombie trong chế độ đối kháng thì thắng
    public void gameOver()
    {
        bool localWins = NetSession.ControlsZombies;
        show(localWins, localWins
            ? "Zombie của bạn đã ăn được não!"
            : "Zombie đã ăn mất não bạn");
    }

    public void win()
    {
        Invoke("win_real", 5f);
    }

    private void win_real()
    {
        bool localWins = !NetSession.ControlsZombies;
        show(localWins, localWins
            ? "Bạn đã đẩy lùi được lũ zombie"
            : "Hàng cây đã cầm cự tới cùng, bạn thua");
    }

    private void show(bool localWins, string message)
    {
        //Hiển thị giao diện
        Time.timeScale = 0;
        dialogText.text = message;
        dialogText.color = localWins
            ? new Color(0.89f, 0.76f, 0.37f)
            : new Color(0.06f, 0.79f, 0.11f);
        gameObject.SetActive(true);

        //Phát âm thanh
        backgroundAudio.Stop();
        GetComponent<AudioSource>().clip =
            Resources.Load<AudioClip>(localWins ? "Sounds/UI/winMusic" : "Sounds/UI/loseMusic");
        GetComponent<AudioSource>().Play();
    }

    public void exitGame()
    {
        Application.Quit();
    }
}
