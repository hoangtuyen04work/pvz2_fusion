using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TestZombieSpawner : MonoBehaviour
{
    private ZombieManagement manager;
    private string selectedZombieName = "ZombieNormal";
    private readonly List<Image> cardBorders = new List<Image>();
    private Text selectedStatusText;

    private void Start()
    {
        manager = GetComponent<ZombieManagement>();
        BuildTestUI();
    }

    private void Update()
    {
        // Hỗ trợ phím tắt nhanh
        for (int i = 0; i < 9 && i < ZombieRoster.All.Length; i++)
        {
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i)))
            {
                SelectZombie(ZombieRoster.All[i].name);
            }
        }

        // Nhấp chuột trái hoặc phải vào bãi cỏ để thả zombie đã chọn
        if ((Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) &&
            !(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
        {
            Vector3 p = Camera.main != null ? Camera.main.ScreenToWorldPoint(Input.mousePosition) : Vector3.zero;
            foreach (Collider2D hit in Physics2D.OverlapPointAll(p))
            {
                PlantGrid grid = hit.GetComponent<PlantGrid>();
                if (grid != null)
                {
                    SpawnZombieOnRow(grid.row);
                    return;
                }
            }
        }
    }

    private void SelectZombie(string zombieName)
    {
        selectedZombieName = zombieName;
        int index = -1;
        for (int i = 0; i < ZombieRoster.All.Length; i++)
        {
            if (ZombieRoster.All[i].name == zombieName)
            {
                index = i;
                break;
            }
        }

        for (int i = 0; i < cardBorders.Count; i++)
        {
            if (cardBorders[i] != null)
            {
                cardBorders[i].color = (i == index)
                    ? new Color(1f, 0.85f, 0.2f, 1f)
                    : new Color(0.25f, 0.18f, 0.16f, 0.9f);
            }
        }

        if (selectedStatusText != null)
        {
            string label = index >= 0 ? ZombieRoster.All[index].label : zombieName;
            selectedStatusText.text = "Đã chọn: " + label.ToUpper() + " — Nhấp vào ô đất hoặc bấm nút [HÀNG X] để thả";
        }
    }

    private void SpawnZombieOnRow(int row)
    {
        if (manager == null) return;
        manager.SpawnZombieByName(selectedZombieName, row);
    }

    private void ClearAllZombies()
    {
        foreach (Zombie z in FindObjectsByType<Zombie>())
        {
            Destroy(z.gameObject);
        }
    }

    private void BuildTestUI()
    {
        if (GameObject.Find("TestZombieCanvas") != null) return;

        var canvasObject = new GameObject("TestZombieCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 180;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1000f, 750f);
        scaler.matchWidthOrHeight = 0.5f;

        // Bảng chứa dưới cùng
        var panel = new GameObject("Test Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasObject.transform, false);
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.01f, 0.01f);
        panelRect.anchorMax = new Vector2(0.99f, 0.29f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var panelBg = panel.GetComponent<Image>();
        panelBg.color = new Color(0.10f, 0.06f, 0.06f, 0.92f);
        var border = panel.AddComponent<Outline>();
        border.effectColor = new Color(0.60f, 0.25f, 0.20f, 0.8f);

        // Thanh tiêu đề + nút Hàng + Nút xóa
        var topBar = new GameObject("TopBar", typeof(RectTransform));
        topBar.transform.SetParent(panel.transform, false);
        var topRect = topBar.GetComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0.01f, 0.72f);
        topRect.anchorMax = new Vector2(0.99f, 0.98f);
        topRect.offsetMin = Vector2.zero;
        topRect.offsetMax = Vector2.zero;

        // Status Text
        var statusGO = new GameObject("StatusText", typeof(RectTransform), typeof(Text));
        statusGO.transform.SetParent(topBar.transform, false);
        var statusRect = statusGO.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0f, 0f);
        statusRect.anchorMax = new Vector2(0.52f, 1f);
        statusRect.offsetMin = Vector2.zero;
        statusRect.offsetMax = Vector2.zero;

        selectedStatusText = statusGO.GetComponent<Text>();
        selectedStatusText.font = Resources.Load<Font>("Fonts/Baloo2") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        selectedStatusText.fontSize = 15;
        selectedStatusText.alignment = TextAnchor.MiddleLeft;
        selectedStatusText.color = new Color(1f, 0.90f, 0.50f);

        // Nút Hàng 1..5
        float startX = 0.53f;
        float btnWidth = 0.07f;
        for (int r = 0; r < 5; r++)
        {
            int rowIdx = r;
            var btnGO = CreateButton(topBar.transform, "Hàng " + (r + 1), "H" + (r + 1), new Vector2(startX + r * (btnWidth + 0.005f), 0.05f), new Vector2(startX + (r + 1) * btnWidth + r * 0.005f, 0.95f), () => SpawnZombieOnRow(rowIdx));
            btnGO.GetComponent<Image>().color = new Color(0.28f, 0.45f, 0.20f, 0.95f);
        }

        // Nút Xóa Hết Zombie
        var clearBtn = CreateButton(topBar.transform, "Xóa Hết", "🧹 XÓA", new Vector2(0.91f, 0.05f), new Vector2(0.99f, 0.95f), ClearAllZombies);
        clearBtn.GetComponent<Image>().color = new Color(0.55f, 0.18f, 0.16f, 0.95f);

        // Scroll View danh sách thẻ Zombie
        var scrollView = new GameObject("ScrollView", typeof(RectTransform));
        scrollView.transform.SetParent(panel.transform, false);
        var scrollRectTransform = scrollView.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0.01f, 0.03f);
        scrollRectTransform.anchorMax = new Vector2(0.99f, 0.70f);
        scrollRectTransform.offsetMin = Vector2.zero;
        scrollRectTransform.offsetMax = Vector2.zero;

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scrollView.transform, false);
        var vpRect = viewport.GetComponent<RectTransform>();
        vpRect.anchorMin = Vector2.zero;
        vpRect.anchorMax = Vector2.one;
        vpRect.offsetMin = Vector2.zero;
        vpRect.offsetMax = Vector2.zero;
        viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.2f);

        var content = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        var contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 0f);
        contentRect.anchorMax = new Vector2(0f, 1f);
        contentRect.pivot = new Vector2(0f, 0.5f);

        var layout = content.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(6, 6, 4, 4);
        layout.spacing = 6f;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        content.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scroll = scrollView.AddComponent<ScrollRect>();
        scroll.viewport = vpRect;
        scroll.content = contentRect;
        scroll.horizontal = true;
        scroll.vertical = false;
        scroll.scrollSensitivity = 25f;

        cardBorders.Clear();

        // Tạo các thẻ Zombie
        foreach (var entry in ZombieRoster.All)
        {
            var card = new GameObject("Card_" + entry.name, typeof(RectTransform), typeof(Image), typeof(Button));
            card.transform.SetParent(content.transform, false);
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(90f, 0f);

            var cardImg = card.GetComponent<Image>();
            cardImg.type = Image.Type.Sliced;
            cardImg.color = new Color(0.25f, 0.18f, 0.16f, 0.9f);
            cardBorders.Add(cardImg);

            // Icon
            var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGO.transform.SetParent(card.transform, false);
            var iconRect = iconGO.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.08f, 0.28f);
            iconRect.anchorMax = new Vector2(0.92f, 0.96f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            var iconImg = iconGO.GetComponent<Image>();
            iconImg.sprite = LoadZombieIcon(entry.name);
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            // Name
            var nameGO = new GameObject("Name", typeof(RectTransform), typeof(Text));
            nameGO.transform.SetParent(card.transform, false);
            var nameRect = nameGO.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.02f, 0.02f);
            nameRect.anchorMax = new Vector2(0.98f, 0.26f);
            nameRect.offsetMin = Vector2.zero;
            nameRect.offsetMax = Vector2.zero;

            var nameText = nameGO.GetComponent<Text>();
            nameText.font = Resources.Load<Font>("Fonts/Baloo2") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            nameText.text = entry.label;
            nameText.fontSize = 12;
            nameText.alignment = TextAnchor.MiddleCenter;
            nameText.color = Color.white;
            nameText.raycastTarget = false;

            string targetName = entry.name;
            card.GetComponent<Button>().onClick.AddListener(() => SelectZombie(targetName));
        }

        SelectZombie("ZombieNormal");
    }

    private GameObject CreateButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var img = go.GetComponent<Image>();
        img.color = new Color(0.35f, 0.25f, 0.20f, 0.95f);

        var btn = go.GetComponent<Button>();
        btn.onClick.AddListener(onClick);

        var textGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textGO.transform.SetParent(go.transform, false);
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var txt = textGO.GetComponent<Text>();
        txt.font = Resources.Load<Font>("Fonts/Baloo2") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.text = label;
        txt.fontSize = 13;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.raycastTarget = false;

        return go;
    }

    private static Sprite LoadZombieIcon(string zombieName)
    {
        return ZombieIconHelper.GetIcon(zombieName);
    }
}
