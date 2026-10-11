using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Presentation-only view for the three-card Endless reward decision.</summary>
public sealed class EndlessRewardOverlay : MonoBehaviour
{
    private bool resolved;
    private Action<EndlessChoice> onSelected;
    private Action onSkipped;

    public static void Show(string title, string subtitle, IList<EndlessChoice> choices,
        bool allowSkip, Action<EndlessChoice> selected, Action skipped = null)
    {
        EndlessRewardOverlay existing = FindAnyObjectByType<EndlessRewardOverlay>();
        if (existing != null) Destroy(existing.gameObject);

        if (choices == null || choices.Count == 0)
        {
            skipped?.Invoke();
            return;
        }

        var root = new GameObject("Endless Reward", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(EndlessRewardOverlay));
        EndlessRewardOverlay view = root.GetComponent<EndlessRewardOverlay>();
        view.Build(title, subtitle, choices, allowSkip, selected, skipped);
    }

    private void Build(string titleValue, string subtitleValue, IList<EndlessChoice> choices,
        bool allowSkip, Action<EndlessChoice> selected, Action skipped)
    {
        onSelected = selected;
        onSkipped = skipped;

        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1400;
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1000f, 750f);
        scaler.matchWidthOrHeight = 0.5f;
        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        Image shade = EndlessMenuOverlay.ImageObject("Shade", transform, new Color(0f, 0f, 0f, 0.82f));
        EndlessMenuOverlay.Stretch(shade.rectTransform);

        Sprite woodPanelSprite = Resources.Load<Sprite>("GameUI/dialog_main") ?? Resources.Load<Sprite>("GameUI/ui_notice_boxchat");
        Image panel = EndlessMenuOverlay.ImageObject("Wood Board", transform, Color.white);
        panel.sprite = woodPanelSprite;
        panel.type = woodPanelSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        EndlessMenuOverlay.Anchor(panel.rectTransform, 0.08f, 0.08f, 0.92f, 0.92f);
        AddSoftShadow(panel.gameObject, new Vector2(4f, -5f));

        Text title = EndlessMenuOverlay.TextObject("Title", panel.transform, titleValue, 34,
            TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.38f));
        title.fontStyle = FontStyle.Bold;
        EndlessMenuOverlay.Anchor(title.rectTransform, 0.06f, 0.85f, 0.94f, 0.96f);
        AddTextOutline(title.gameObject, new Color(0.24f, 0.10f, 0.02f, 0.98f), new Vector2(1.5f, -1.5f));

        Text subtitle = EndlessMenuOverlay.TextObject("Subtitle", panel.transform, subtitleValue, 19,
            TextAnchor.MiddleCenter, new Color(0.96f, 0.94f, 0.82f));
        subtitle.fontStyle = FontStyle.Bold;
        EndlessMenuOverlay.Anchor(subtitle.rectTransform, 0.08f, 0.77f, 0.92f, 0.85f);
        AddTextOutline(subtitle.gameObject, new Color(0.18f, 0.08f, 0.02f, 0.95f), new Vector2(1.2f, -1.2f));

        int count = Mathf.Min(3, choices.Count);
        float gap = 0.025f;
        float totalWidth = 0.84f;
        float width = (totalWidth - gap * (count - 1)) / count;
        float start = 0.08f;
        for (int index = 0; index < count; index++)
        {
            EndlessChoice choice = choices[index];
            GameObject card = CreateChoiceCard(panel.transform, choice);
            float left = start + index * (width + gap);
            EndlessMenuOverlay.Anchor(card.GetComponent<RectTransform>(), left, 0.22f, left + width, 0.74f);
        }

        if (allowSkip)
        {
            Sprite skipBtnSprite = Resources.Load<Sprite>("GameUI/button2");
            GameObject skip = CreateThemedButton(panel.transform, "BỎ QUA — KHÔNG NHẬN DEBUFF",
                skipBtnSprite, new Color(1f, 0.95f, 0.85f), ResolveSkip);
            EndlessMenuOverlay.Anchor(skip.GetComponent<RectTransform>(), 0.30f, 0.07f, 0.70f, 0.17f);
        }
        else
        {
            Text hint = EndlessMenuOverlay.TextObject("Required Hint", panel.transform,
                "★ Hãy chọn 1 thẻ cường hóa để tiếp tục hành trình ★", 18, TextAnchor.MiddleCenter, new Color(1f, 0.88f, 0.45f));
            hint.fontStyle = FontStyle.Bold;
            EndlessMenuOverlay.Anchor(hint.rectTransform, 0.20f, 0.07f, 0.80f, 0.16f);
            AddTextOutline(hint.gameObject, new Color(0.20f, 0.08f, 0.02f, 0.95f), new Vector2(1.2f, -1.2f));
        }
    }

    private GameObject CreateChoiceCard(Transform parent, EndlessChoice choice)
    {
        Sprite cardSprite = Resources.Load<Sprite>("GameUI/dialog_child") ?? Resources.Load<Sprite>("GameUI/button1");
        Color cardTint = choice.Kind == EndlessChoiceKind.Buff
            ? new Color(0.92f, 1f, 0.88f, 1f)
            : new Color(1f, 0.88f, 0.92f, 1f);

        var root = new GameObject(choice.DisplayName, typeof(RectTransform), typeof(Image), typeof(Button));
        root.transform.SetParent(parent, false);
        Image image = root.GetComponent<Image>();
        image.sprite = cardSprite;
        image.type = cardSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = cardTint;
        Button button = root.GetComponent<Button>();
        button.onClick.AddListener(() => ResolveChoice(choice));
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 0.80f);
        colors.pressedColor = new Color(0.80f, 0.80f, 0.80f);
        button.colors = colors;
        AddSoftShadow(root, new Vector2(2f, -3f));

        Color titleColor = choice.Kind == EndlessChoiceKind.Buff
            ? new Color(0.40f, 1f, 0.25f)
            : new Color(1f, 0.45f, 0.40f);
        Text name = EndlessMenuOverlay.TextObject("Label", root.transform, choice.DisplayName, 23,
            TextAnchor.UpperCenter, titleColor);
        name.fontStyle = FontStyle.Bold;
        EndlessMenuOverlay.Anchor(name.rectTransform, 0.06f, 0.68f, 0.94f, 0.95f);
        name.raycastTarget = false;
        AddTextOutline(name.gameObject, new Color(0.20f, 0.08f, 0.02f, 0.98f), new Vector2(1.2f, -1.2f));

        Text description = EndlessMenuOverlay.TextObject("Description", root.transform, choice.Description, 18,
            TextAnchor.MiddleCenter, new Color(1f, 0.96f, 0.88f));
        description.fontStyle = FontStyle.Bold;
        EndlessMenuOverlay.Anchor(description.rectTransform, 0.08f, 0.20f, 0.92f, 0.68f);
        description.raycastTarget = false;
        AddTextOutline(description.gameObject, new Color(0.18f, 0.08f, 0.02f, 0.95f), new Vector2(1f, -1f));

        int currentStacks = EndlessRun.Session == null ? 0 : EndlessRun.Session.GetStacks(
            choice.Id, choice.Kind == EndlessChoiceKind.Buff);
        string kindLabel = choice.Kind == EndlessChoiceKind.Buff ? "BUFF" : "DEBUFF";
        string levelLabel = kindLabel + "  •  CẤP " + Mathf.Min(choice.MaxStacks, currentStacks + 1) + "/" + choice.MaxStacks;
        Text power = EndlessMenuOverlay.TextObject("Power", root.transform,
            levelLabel, 16,
            TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.42f));
        power.fontStyle = FontStyle.Bold;
        EndlessMenuOverlay.Anchor(power.rectTransform, 0.15f, 0.05f, 0.85f, 0.18f);
        power.raycastTarget = false;
        AddTextOutline(power.gameObject, new Color(0.22f, 0.08f, 0.02f, 0.98f), new Vector2(1.2f, -1.2f));
        return root;
    }

    private static GameObject CreateThemedButton(Transform parent, string label, Sprite sprite,
        Color textColor, UnityEngine.Events.UnityAction action)
    {
        var root = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        root.transform.SetParent(parent, false);
        Image image = root.GetComponent<Image>();
        image.sprite = sprite;
        image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = Color.white;
        Button button = root.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 0.88f);
        colors.pressedColor = new Color(0.80f, 0.80f, 0.80f);
        button.colors = colors;
        button.onClick.AddListener(action);
        AddSoftShadow(root, new Vector2(2f, -3f));

        Text text = EndlessMenuOverlay.TextObject("Label", root.transform, label, 20,
            TextAnchor.MiddleCenter, textColor);
        text.fontStyle = FontStyle.Bold;
        EndlessMenuOverlay.Stretch(text.rectTransform);
        text.raycastTarget = false;
        AddTextOutline(text.gameObject, new Color(0.20f, 0.08f, 0.02f, 0.95f), new Vector2(1.2f, -1.2f));
        return root;
    }

    private static void AddTextOutline(GameObject go, Color color, Vector2 dist)
    {
        Outline outline = go.GetComponent<Outline>();
        if (outline == null) outline = go.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = dist;
    }

    private static void AddSoftShadow(GameObject go, Vector2 dist)
    {
        Shadow shadow = go.GetComponent<Shadow>();
        if (shadow == null) shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
        shadow.effectDistance = dist;
    }

    private void ResolveChoice(EndlessChoice choice)
    {
        if (resolved) return;
        resolved = true;
        Action<EndlessChoice> callback = onSelected;
        Destroy(gameObject);
        callback?.Invoke(choice);
    }

    private void ResolveSkip()
    {
        if (resolved) return;
        resolved = true;
        Action callback = onSkipped;
        Destroy(gameObject);
        callback?.Invoke();
    }
}
