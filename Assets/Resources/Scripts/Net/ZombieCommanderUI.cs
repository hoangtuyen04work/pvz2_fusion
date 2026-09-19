using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Thanh điều khiển của phe zombie trong chế độ đối kháng.
/// Chọn một thẻ zombie rồi bấm vào hàng trên bãi cỏ để thả quân, mỗi lần thả tốn não.
/// </summary>
public class ZombieCommanderUI : MonoBehaviour
{
    private static ZombieCommanderUI instance;
    public static ZombieCommanderUI Instance { get { return instance; } }

    private class CardView
    {
        public ZombieRoster.Entry entry;
        public Button button;
        public Image frame;
        public Image cooldownFill;
        public Text costText;
        public float readyTime;
    }

    private readonly List<CardView> cards = new List<CardView>();

    private Font font;
    private Sprite buttonSprite;

    private Text brainText;
    private Text hintText;
    private Text followText;
    private RectTransform followRect;
    private Canvas canvas;

    private int selected = -1;
    private float hintUntil;

    private void Awake()
    {
        instance = this;
        font = Resources.Load<Font>("Fonts/Baloo2");
        buttonSprite = Resources.Load<Sprite>("Sprites/UI/Menu/button_normal");
        Build();
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    private void Update()
    {
        UpdateCards();
        UpdateFollowLabel();

        if (hintText != null && hintUntil > 0f && Time.unscaledTime > hintUntil)
        {
            hintText.text = DefaultHint();
            hintUntil = 0f;
        }

        if (selected < 0) return;
        if (!Input.GetMouseButtonDown(0)) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        DropSelected();
    }

    #region Thả zombie

    private void DropSelected()
    {
        if (Camera.main == null) return;

        Vector3 world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        int row = RowFromWorld(world);

        if (row < 0)
        {
            ShowHint("Hãy bấm vào một hàng trên bãi cỏ.");
            return;
        }

        CardView card = cards[selected];
        if (Time.time < card.readyTime)
        {
            ShowHint(card.entry.label + " còn đang hồi chiêu.");
            return;
        }

        if (NetGameplay.Instance != null && NetGameplay.Instance.Brains < card.entry.cost)
        {
            ShowHint("Không đủ não để thả " + card.entry.label + ".");
            return;
        }

        NetGameplay.RequestZombieDrop(card.entry.name, row);
        ShowHint("Đã ra lệnh thả " + card.entry.label + " ở hàng " + (row + 1) + ".");
    }

    /// <summary>Tìm hàng gần nhất với điểm vừa bấm, trả về -1 nếu bấm ra ngoài bãi cỏ.</summary>
    private int RowFromWorld(Vector3 world)
    {
        if (GameManagement.levelData == null) return -1;
        List<float> rows = GameManagement.levelData.zombieInitPosY;
        if (rows == null || rows.Count == 0) return -1;

        int best = -1;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < rows.Count; i++)
        {
            float distance = Mathf.Abs(world.y - rows[i]);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = i;
            }
        }

        return bestDistance <= 0.8f ? best : -1;
    }

    /// <summary>Máy chủ trả lời yêu cầu thả quân.</summary>
    public void OnCommandResult(string zombieName, bool accepted, string reason)
    {
        foreach (CardView card in cards)
        {
            if (card.entry.name != zombieName) continue;
            if (accepted) card.readyTime = Time.time + card.entry.cooldown;
            break;
        }

        if (!accepted) ShowHint(string.IsNullOrEmpty(reason) ? "Không thả được." : reason);
    }

    #endregion

    #region Cập nhật hiển thị

    private void UpdateCards()
    {
        int brains = NetGameplay.Instance != null ? NetGameplay.Instance.Brains : 0;

        if (brainText != null)
            brainText.text = "NÃO  " + brains + " / " + NetGameplay.BrainMax;

        for (int i = 0; i < cards.Count; i++)
        {
            CardView card = cards[i];

            float remaining = card.readyTime - Time.time;
            bool cooling = remaining > 0f;
            bool affordable = brains >= card.entry.cost;

            if (card.cooldownFill != null)
                card.cooldownFill.fillAmount = cooling ? Mathf.Clamp01(remaining / card.entry.cooldown) : 0f;

            if (card.costText != null)
                card.costText.color = affordable
                    ? new Color(0.95f, 0.90f, 0.45f)
                    : new Color(0.85f, 0.35f, 0.30f);

            if (card.frame != null)
            {
                if (i == selected) card.frame.color = new Color(0.95f, 0.45f, 0.35f);
                else if (cooling || !affordable) card.frame.color = new Color(0.42f, 0.42f, 0.40f);
                else card.frame.color = new Color(0.78f, 0.76f, 0.70f);
            }
        }
    }

    private void UpdateFollowLabel()
    {
        if (followRect == null || canvas == null) return;

        bool active = selected >= 0;
        if (followRect.gameObject.activeSelf != active) followRect.gameObject.SetActive(active);
        if (!active) return;

        followText.text = cards[selected].entry.label;

        Vector2 local;
        RectTransform canvasRect = canvas.transform as RectTransform;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, Input.mousePosition, null, out local))
        {
            followRect.anchoredPosition = local + new Vector2(0f, 34f);
        }
    }

    private void SelectCard(int index)
    {
        selected = selected == index ? -1 : index;
        ShowHint(selected < 0
            ? DefaultHint()
            : "Đã chọn " + cards[index].entry.label + ", bấm vào hàng muốn thả.");
    }

    private void ShowHint(string message)
    {
        if (hintText == null) return;
        hintText.text = message;
        hintUntil = Time.unscaledTime + 2.5f;
    }

    private string DefaultHint()
    {
        return "Chọn một thẻ zombie rồi bấm vào bãi cỏ để thả quân.";
    }

    #endregion

    #region Dựng giao diện

    private void Build()
    {
        GameObject canvasObject = new GameObject("ZombieCommanderCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 320;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1000f, 750f);
        scaler.matchWidthOrHeight = 0.5f;

        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        //Thanh nền phía dưới màn hình
        GameObject bar = new GameObject("Thanh chỉ huy", typeof(RectTransform), typeof(Image));
        bar.transform.SetParent(canvasObject.transform, false);
        SetAnchors(bar.GetComponent<RectTransform>(), 0.015f, 0.005f, 0.985f, 0.155f);
        bar.GetComponent<Image>().color = new Color(0.10f, 0.05f, 0.06f, 0.92f);

        brainText = CreateText("Não", bar.transform, "NÃO", 22,
            TextAnchor.MiddleCenter, new Color(0.95f, 0.55f, 0.62f));
        SetAnchors(brainText.rectTransform, 0.006f, 0.10f, 0.115f, 0.90f);

        BuildCards(bar.transform);

        hintText = CreateText("Gợi ý", canvasObject.transform, DefaultHint(), 20,
            TextAnchor.MiddleCenter, new Color(0.95f, 0.85f, 0.70f));
        SetAnchors(hintText.rectTransform, 0.10f, 0.160f, 0.90f, 0.205f);

        //Nhãn bám theo con trỏ khi đã chọn zombie
        followText = CreateText("Đang chọn", canvasObject.transform, "", 20,
            TextAnchor.MiddleCenter, new Color(1f, 0.75f, 0.45f));
        followRect = followText.rectTransform;
        followRect.anchorMin = new Vector2(0.5f, 0.5f);
        followRect.anchorMax = new Vector2(0.5f, 0.5f);
        followRect.sizeDelta = new Vector2(220f, 34f);
        followText.raycastTarget = false;
        followRect.gameObject.SetActive(false);
    }

    private void BuildCards(Transform parent)
    {
        ZombieManagement management = null;
        GameObject holder = GameObject.Find("Zombie Management");
        if (holder != null) management = holder.GetComponent<ZombieManagement>();

        List<ZombieRoster.Entry> usable = new List<ZombieRoster.Entry>();
        foreach (ZombieRoster.Entry entry in ZombieRoster.All)
        {
            if (IsAvailable(management, entry.name)) usable.Add(entry);
        }

        if (usable.Count == 0) return;

        const float left = 0.125f;
        const float right = 0.995f;
        float width = (right - left) / usable.Count;

        for (int i = 0; i < usable.Count; i++)
        {
            float xMin = left + width * i;
            float xMax = xMin + width * 0.94f;
            cards.Add(BuildCard(parent, usable[i], xMin, xMax, i));
        }
    }

    private CardView BuildCard(Transform parent, ZombieRoster.Entry entry,
        float xMin, float xMax, int index)
    {
        GameObject go = new GameObject("Thẻ " + entry.name,
            typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        SetAnchors(go.GetComponent<RectTransform>(), xMin, 0.08f, xMax, 0.92f);

        Image frame = go.GetComponent<Image>();
        frame.sprite = buttonSprite;
        frame.type = Image.Type.Sliced;
        frame.color = new Color(0.78f, 0.76f, 0.70f);

        Button button = go.GetComponent<Button>();
        button.transition = Selectable.Transition.None;
        int captured = index;
        button.onClick.AddListener(delegate { SelectCard(captured); });

        Text label = CreateText("Tên", go.transform, entry.label, 17,
            TextAnchor.LowerCenter, Color.white);
        SetAnchors(label.rectTransform, 0.03f, 0.42f, 0.97f, 0.97f);
        label.raycastTarget = false;

        Text cost = CreateText("Giá", go.transform, entry.cost.ToString(), 20,
            TextAnchor.UpperCenter, new Color(0.95f, 0.90f, 0.45f));
        SetAnchors(cost.rectTransform, 0.03f, 0.05f, 0.97f, 0.44f);
        cost.raycastTarget = false;

        //Lớp phủ hồi chiêu, rút dần từ trên xuống
        GameObject overlay = new GameObject("Hồi chiêu", typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(go.transform, false);
        SetAnchors(overlay.GetComponent<RectTransform>(), 0f, 0f, 1f, 1f);

        Image fill = overlay.GetComponent<Image>();
        fill.color = new Color(0f, 0f, 0f, 0.62f);
        fill.raycastTarget = false;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Vertical;
        fill.fillOrigin = (int)Image.OriginVertical.Top;
        fill.fillAmount = 0f;

        CardView view = new CardView();
        view.entry = entry;
        view.button = button;
        view.frame = frame;
        view.cooldownFill = fill;
        view.costText = cost;
        view.readyTime = 0f;
        return view;
    }

    private bool IsAvailable(ZombieManagement management, string zombieName)
    {
        if (management == null || management.zombies == null) return false;
        foreach (GameObject prefab in management.zombies)
        {
            if (prefab != null && prefab.name == zombieName) return true;
        }
        return false;
    }

    private Text CreateText(string name, Transform parent, string value, int size,
        TextAnchor alignment, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        Text text = go.GetComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 10;
        text.resizeTextMaxSize = size;
        text.alignment = alignment;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static void SetAnchors(RectTransform rect, float xMin, float yMin, float xMax, float yMax)
    {
        rect.anchorMin = new Vector2(xMin, yMin);
        rect.anchorMax = new Vector2(xMax, yMax);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    #endregion
}
