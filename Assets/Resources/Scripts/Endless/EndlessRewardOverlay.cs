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

        Image shade = EndlessMenuOverlay.ImageObject("Shade", transform, new Color(0f, 0f, 0f, 0.80f));
        EndlessMenuOverlay.Stretch(shade.rectTransform);
        Image panel = EndlessMenuOverlay.ImageObject("Soil Sign", transform, new Color(0.22f, 0.13f, 0.045f, 0.99f));
        EndlessMenuOverlay.Anchor(panel.rectTransform, 0.07f, 0.10f, 0.93f, 0.90f);

        Text title = EndlessMenuOverlay.TextObject("Title", panel.transform, titleValue, 38,
            TextAnchor.MiddleCenter, new Color(1f, 0.86f, 0.30f));
        EndlessMenuOverlay.Anchor(title.rectTransform, 0.06f, 0.84f, 0.94f, 0.96f);
        Text subtitle = EndlessMenuOverlay.TextObject("Subtitle", panel.transform, subtitleValue, 20,
            TextAnchor.MiddleCenter, new Color(0.94f, 0.92f, 0.75f));
        EndlessMenuOverlay.Anchor(subtitle.rectTransform, 0.08f, 0.75f, 0.92f, 0.84f);

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
            EndlessMenuOverlay.Anchor(card.GetComponent<RectTransform>(), left, 0.22f, left + width, 0.72f);
        }

        if (allowSkip)
        {
            GameObject skip = CreateButton(panel.transform, "BỎ QUA — KHÔNG NHẬN DEBUFF",
                new Color(0.30f, 0.38f, 0.18f), ResolveSkip);
            EndlessMenuOverlay.Anchor(skip.GetComponent<RectTransform>(), 0.31f, 0.06f, 0.69f, 0.16f);
        }
        else
        {
            Text hint = EndlessMenuOverlay.TextObject("Required Hint", panel.transform,
                "Hãy chọn một thẻ để tiếp tục", 19, TextAnchor.MiddleCenter, new Color(0.88f, 0.80f, 0.62f));
            EndlessMenuOverlay.Anchor(hint.rectTransform, 0.25f, 0.06f, 0.75f, 0.16f);
        }
    }

    private GameObject CreateChoiceCard(Transform parent, EndlessChoice choice)
    {
        Color cardColor = choice.Kind == EndlessChoiceKind.Buff
            ? new Color(0.22f, 0.48f, 0.12f, 1f)
            : new Color(0.40f, 0.25f, 0.42f, 1f);
        GameObject root = CreateButton(parent, choice.DisplayName, cardColor, () => ResolveChoice(choice));
        Text name = root.transform.Find("Label").GetComponent<Text>();
        name.text = choice.DisplayName;
        name.fontSize = 25;
        name.alignment = TextAnchor.UpperCenter;
        EndlessMenuOverlay.Anchor(name.rectTransform, 0.06f, 0.65f, 0.94f, 0.94f);

        Text description = EndlessMenuOverlay.TextObject("Description", root.transform, choice.Description, 20,
            TextAnchor.MiddleCenter, Color.white);
        EndlessMenuOverlay.Anchor(description.rectTransform, 0.08f, 0.18f, 0.92f, 0.68f);
        description.raycastTarget = false;
        int currentStacks = EndlessRun.Session == null ? 0 : EndlessRun.Session.GetStacks(
            choice.Id, choice.Kind == EndlessChoiceKind.Buff);
        string kindLabel = choice.Kind == EndlessChoiceKind.Buff ? "BUFF" : "DEBUFF";
        string levelLabel = kindLabel + "  •  CẤP " + Mathf.Min(choice.MaxStacks, currentStacks + 1) + "/" + choice.MaxStacks;
        Text power = EndlessMenuOverlay.TextObject("Power", root.transform,
            levelLabel, 17,
            TextAnchor.MiddleCenter, new Color(1f, 0.88f, 0.38f));
        EndlessMenuOverlay.Anchor(power.rectTransform, 0.25f, 0.04f, 0.75f, 0.17f);
        power.raycastTarget = false;
        return root;
    }

    private static GameObject CreateButton(Transform parent, string label, Color normal,
        UnityEngine.Events.UnityAction action)
    {
        var root = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        root.transform.SetParent(parent, false);
        Image image = root.GetComponent<Image>();
        image.color = normal;
        Button button = root.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = normal;
        colors.highlightedColor = Color.Lerp(normal, Color.white, 0.16f);
        colors.pressedColor = Color.Lerp(normal, Color.black, 0.20f);
        colors.disabledColor = new Color(normal.r, normal.g, normal.b, 0.45f);
        button.colors = colors;
        button.onClick.AddListener(action);
        Text text = EndlessMenuOverlay.TextObject("Label", root.transform, label, 23,
            TextAnchor.MiddleCenter, Color.white);
        EndlessMenuOverlay.Stretch(text.rectTransform);
        text.raycastTarget = false;
        return root;
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
