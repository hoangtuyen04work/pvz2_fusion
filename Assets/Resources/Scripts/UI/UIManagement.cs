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
        GameObject sunObject = GameObject.Find("Sun Text");
        Text sunText = sunObject.GetComponent<Text>();
        PlaceSunCounterAboveSeedBank(sunObject);
        sunObject.SetActive(true);
        sunText.enabled = true;
        if (sunText.font == null) sunText.font = Resources.Load<Font>("Fonts/Baloo2");
        sunText.color = new Color(0.18f, 0.09f, 0.02f, 1f);
        sunText.fontStyle = FontStyle.Bold;
        sunText.fontSize = 20;
        sunText.alignment = TextAnchor.MiddleCenter;
        sunText.horizontalOverflow = HorizontalWrapMode.Overflow;
        sunText.verticalOverflow = VerticalWrapMode.Overflow;
        sunText.raycastTarget = false;
        Outline sunOutline = sunObject.GetComponent<Outline>();
        if (sunOutline == null) sunOutline = sunObject.AddComponent<Outline>();
        sunOutline.effectColor = new Color(1f, 0.91f, 0.50f, 0.9f);
        sunOutline.effectDistance = new Vector2(1f, -1f);

        //Tải tên màn chơi
        levelNameText.text = GameManagement.levelData.levelName;

        //Tải nhóm thẻ cây và đặt kích thước, vị trí cho UI liên quan
        List<string> plantCards = GameManagement.levelData.plantCards;
        List<Card> cards = new List<Card>();
        for (int i = cardGroup.transform.childCount - 1; i >= 0; i--)
            Destroy(cardGroup.transform.GetChild(i).gameObject);

        HorizontalLayoutGroup layout = cardGroup.GetComponent<HorizontalLayoutGroup>();
        if (layout == null) layout = cardGroup.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 2f;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = layout.childControlHeight = false;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        foreach (string plant in plantCards)
        {
            if (!PlantLoadoutCatalog.TryGet(plant, out var entry))
            {
                Debug.LogError("No seed packet definition found for plant: " + plant, this);
                continue;
            }
            Card card = SeedPacketFactory.CreateGameplayCard(entry, cardGroup.transform);
            if (GameManagement.levelData.isTestMode) { card.sunNeeded = 0; card.coolingTime = 0f; }
            cards.Add(card);
        }
        SunNumber sunNumber = sunObject.GetComponent<SunNumber>();
        sunNumber.SetForegroundText(null);
        sunNumber.Initialize(GameManagement.levelData.initialSun, cards);
        float cardGroupWidth = cards.Count == 0 ? 0f : cards.Count * SeedPacketFactory.GameplaySize.x + (cards.Count - 1) * layout.spacing;
        cardGroup.GetComponent<RectTransform>()
            .SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, cardGroupWidth);
        cardGroup.GetComponent<RectTransform>()
            .SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, SeedPacketFactory.GameplaySize.y);
        seedBank.GetComponent<RectTransform>()
            .SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, cardGroupWidth + 78);
        shovelBank.GetComponent<RectTransform>()
            .SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, cardGroupWidth + 108, 60);
        // Render the total after the dynamically-created cards so it cannot be hidden.
        sunObject.transform.SetAsLastSibling();
    }

    private void PlaceSunCounterAboveSeedBank(GameObject sunObject)
    {
        Transform overlayParent = seedBank.transform.parent;
        Transform obsoleteCounter = overlayParent.Find("Sun Counter Foreground");
        if (obsoleteCounter != null)
        {
            obsoleteCounter.gameObject.SetActive(false);
            Destroy(obsoleteCounter.gameObject);
        }

        RectTransform bankRect = seedBank.GetComponent<RectTransform>();
        RectTransform rect = sunObject.GetComponent<RectTransform>();
        rect.SetParent(overlayParent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = bankRect.anchoredPosition + new Vector2(34f, -62.5f);
        rect.sizeDelta = new Vector2(62f, 23f);
        rect.SetAsLastSibling();

        Text text = sunObject.GetComponent<Text>();
        if (text.font == null) text.font = Resources.Load<Font>("Fonts/Baloo2");
        text.maskable = false;
        text.canvasRenderer.SetAlpha(1f);
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.SetAllDirty();
    }

    public void appear()
    {
        //Nhóm thẻ cây vốn để inactive, tránh việc thẻ hồi chiêu trong lúc đang chạy cốt truyện
        cardGroup.SetActive(true);

        topMotionPanel.GetComponent<MotionPanel>().startMove();
        bottomMotionPanel.GetComponent<MotionPanel>().startMove();
    }
}
