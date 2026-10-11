using System;
using UnityEngine;
using UnityEngine.UI;

public static class AdventureDialogUI
{
    public static void AddCloseButton(GameObject popup, Action closeAction)
    {
        if (popup == null || popup.transform.Find("Close Tutorial Popup") != null) return;

        GameObject close = new GameObject("Close Tutorial Popup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        close.transform.SetParent(popup.transform, false);
        RectTransform rect = close.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-18f, -18f);
        rect.sizeDelta = new Vector2(52f, 52f);

        Image image = close.GetComponent<Image>();
        image.sprite = Resources.Load<Sprite>("GameUI/cancel");
        image.preserveAspect = true;
        image.color = image.sprite != null ? Color.white : new Color(0.45f, 0.12f, 0.06f, 0.96f);

        Button button = close.GetComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        button.onClick.AddListener(() => closeAction?.Invoke());

        if (image.sprite == null)
        {
            Text label = new GameObject("X", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(close.transform, false);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            label.font = Resources.Load<Font>("Fonts/Baloo2");
            label.fontSize = 28;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
        }

        close.transform.SetAsLastSibling();
    }

    public static GameObject ShowLevelInformation(Transform parent, string title, string description, Action closeAction)
    {
        GameObject shade = new GameObject("Level Information", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        shade.transform.SetParent(parent, false);
        RectTransform shadeRect = shade.GetComponent<RectTransform>();
        shadeRect.anchorMin = Vector2.zero;
        shadeRect.anchorMax = Vector2.one;
        shadeRect.offsetMin = shadeRect.offsetMax = Vector2.zero;
        shade.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.76f);

        GameObject panel = new GameObject("Information Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(shade.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(720f, 390f);
        Image panelImage = panel.GetComponent<Image>();
        panelImage.sprite = Resources.Load<Sprite>("GameUI/dialog_main");
        panelImage.type = panelImage.sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        panelImage.color = panelImage.sprite != null ? Color.white : new Color(0.32f, 0.20f, 0.08f, 1f);

        CreateText(panel.transform, "Title", title, 30, new Vector2(0.08f, 0.74f), new Vector2(0.92f, 0.91f), FontStyle.Bold);
        CreateText(panel.transform, "Description", description, 21, new Vector2(0.10f, 0.17f), new Vector2(0.90f, 0.72f), FontStyle.Normal);
        AddCloseButton(panel, closeAction);
        return shade;
    }

    private static void CreateText(Transform parent, string name, string value, int size, Vector2 min, Vector2 max, FontStyle style)
    {
        Text text = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text)).GetComponent<Text>();
        text.transform.SetParent(parent, false);
        text.rectTransform.anchorMin = min;
        text.rectTransform.anchorMax = max;
        text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
        text.font = Resources.Load<Font>("Fonts/Baloo2");
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.22f, 0.08f, 0.02f, 1f);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.text = value;
        text.raycastTarget = false;
    }
}
