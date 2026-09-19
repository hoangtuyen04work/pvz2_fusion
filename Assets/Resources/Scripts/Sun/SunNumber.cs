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
    public int NowSun => nowSun; // Getter cho GameStateCollector

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

    //Số nắng hiện có, để bộ đồng bộ kiểm tra xem có đủ trồng cây không
    public int Current { get { return nowSun; } }

    //Dãy thẻ cây của màn này
    public List<Card> Cards { get { return cardGroup; } }

    public void addSun(int sunNum)
    {
        //Máy khách không tự cộng nắng, nó nhận tổng số nắng từ máy chủ
        if (!NetSession.IsAuthority) return;

        nowSun += sunNum;
        applySun();
        NetGameplay.NotifySunTotal(nowSun);
    }

    public void subSun(int sunNum)
    {
        if (!NetSession.IsAuthority) return;

        subSunAuthoritative(sunNum);
        NetGameplay.NotifySunTotal(nowSun);
    }

    //Trừ nắng mà không tự phát tin, dùng khi bộ đồng bộ muốn gộp chung một lần gửi
    public void subSunAuthoritative(int sunNum)
    {
        if (nowSun >= sunNum)
        {
            nowSun -= sunNum;
            applySun();
        }
    }

    //Máy chủ báo tổng số nắng mới
    public void setSun(int value)
    {
        nowSun = value;
        applySun();
    }

    //Cập nhật chữ số nắng và trạng thái thẻ cây
    private void applySun()
    {
        RefreshText();
        //Cập nhật trạng thái thẻ cây
        updateCard();
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
        if (myText == null) myText = GetComponent<Text>();
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
