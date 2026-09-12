using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SunNumber : MonoBehaviour
{
    //Text số nắng
    Text myText;
    int nowSun;
    public int NowSun => nowSun; // Getter cho GameStateCollector

    //Nhóm thẻ cây
    List<Card> cardGroup;


    // Start is called before the first frame update
    void Start()
    {
        //Lấy số nắng
        myText = gameObject.GetComponent<Text>();
        string sunStr = myText.text;
        nowSun = int.Parse(sunStr);

        //Cập nhật trạng thái thẻ cây
        updateCard();
    }

    public void setCardGroup(List<Card> group)
    {
        cardGroup = group;
    }

    public void addSun(int sunNum)
    {
        nowSun += sunNum;
        myText.text = nowSun.ToString();
        //Cập nhật trạng thái thẻ cây
        updateCard();
    }

    public void subSun(int sunNum)
    {
        if(nowSun >= sunNum)
        {
            nowSun -= sunNum;
            myText.text = nowSun.ToString();
            //Cập nhật trạng thái thẻ cây
            updateCard();
        }
    }

    private void updateCard()
    {
        foreach(Card i in cardGroup)
        {
            i.updateSunEnough(nowSun >= i.sunNeeded);
        }
    }
}
