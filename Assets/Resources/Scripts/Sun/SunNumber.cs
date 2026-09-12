using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SunNumber : MonoBehaviour
{
    //Text số nắng
    Text myText;
    Text foregroundText;
    int nowSun;

    //Nhóm thẻ cây
    List<Card> cardGroup;
    bool initialized;

    private void Awake()
    {
        myText = GetComponent<Text>();
    }


    // Start is called before the first frame update
    void Start()
    {
        //Lấy số nắng
        if (!initialized)
        {
            if (!int.TryParse(myText.text, out nowSun)) nowSun = 0;
            initialized = true;
        }
        RefreshText();

        //Cập nhật trạng thái thẻ cây
        updateCard();
    }

    public void setCardGroup(List<Card> group)
    {
        cardGroup = group;
        if (myText != null) updateCard();
    }

    public void Initialize(int initialSun, List<Card> group)
    {
        if (myText == null) myText = GetComponent<Text>();
        nowSun = Mathf.Max(0, initialSun);
        initialized = true;
        cardGroup = group;
        RefreshText();
        updateCard();
    }

    public void SetForegroundText(Text value)
    {
        foregroundText = value;
        RefreshText();
    }

    public void addSun(int sunNum)
    {
        nowSun += sunNum;
        RefreshText();
        //Cập nhật trạng thái thẻ cây
        updateCard();
    }

    public void subSun(int sunNum)
    {
        if(nowSun >= sunNum)
        {
            nowSun -= sunNum;
            RefreshText();
            //Cập nhật trạng thái thẻ cây
            updateCard();
        }
    }

    private void updateCard()
    {
        if (cardGroup == null) return;
        foreach(Card i in cardGroup)
        {
            i.updateSunEnough(nowSun >= i.sunNeeded);
        }
    }
    private void RefreshText()
    {
        if (myText != null)
        {
            if (myText.font == null) myText.font = Resources.Load<Font>("Fonts/Baloo2");
            myText.horizontalOverflow = HorizontalWrapMode.Overflow;
            myText.verticalOverflow = VerticalWrapMode.Overflow;
            myText.text = nowSun.ToString();
            myText.SetAllDirty();
        }
        if (foregroundText != null)
        {
            if (foregroundText.font == null) foregroundText.font = Resources.Load<Font>("Fonts/Baloo2");
            foregroundText.horizontalOverflow = HorizontalWrapMode.Overflow;
            foregroundText.verticalOverflow = VerticalWrapMode.Overflow;
            foregroundText.text = nowSun.ToString();
            foregroundText.SetAllDirty();
        }
    }
}
