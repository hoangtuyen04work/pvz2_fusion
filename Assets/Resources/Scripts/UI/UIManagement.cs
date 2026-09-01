using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManagement : MonoBehaviour
{
    public GameObject topMotionPanel;
    public GameObject bottomMotionPanel;
    public GameObject seedBank;
    public GameObject shovelBank;
    public Text levelNameText;

    public GameObject cardGroup;   //Nhóm thẻ cây

    // Start is called before the first frame update
    public void initUI()
    {
        //Thiết lập trước Start của SunNumber để màn đặc biệt có thể đổi lượng nắng đầu.
        GameObject.Find("Sun Text").GetComponent<Text>().text =
            GameManagement.levelData.initialSun.ToString();

        //Tải tên màn chơi
        levelNameText.text = GameManagement.levelData.levelName;

        //Tải nhóm thẻ cây và đặt kích thước, vị trí cho UI liên quan
        List<string> plantCards = GameManagement.levelData.plantCards;
        List<Card> cards = new List<Card>();
        foreach (string plant in plantCards)
        {
            cards.Add((
                    Instantiate(
                        Resources.Load<Object>("Prefabs/UI/Card/" + plant + "Card"),
                        cardGroup.transform
                    ) as GameObject
                ).GetComponent<Card>());
        }
        GameObject.Find("Sun Text").GetComponent<SunNumber>().setCardGroup(cards);
        float cardGroupWidth = plantCards.Count * 43 - 1;
        cardGroup.GetComponent<RectTransform>()
            .SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, cardGroupWidth);
        seedBank.GetComponent<RectTransform>()
            .SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, cardGroupWidth + 78);
        shovelBank.GetComponent<RectTransform>()
            .SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, cardGroupWidth + 108, 60);
    }

    public void appear()
    {
        //Nhóm thẻ cây vốn để inactive, tránh việc thẻ hồi chiêu trong lúc đang chạy cốt truyện
        cardGroup.SetActive(true);

        topMotionPanel.GetComponent<MotionPanel>().startMove();
        bottomMotionPanel.GetComponent<MotionPanel>().startMove();
    }
}
