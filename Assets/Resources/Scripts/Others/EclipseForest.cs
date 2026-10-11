using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Cứ mỗi chu kỳ, hai hàng bị bóng nhật thực phủ: ngừng nắng trời và zombie trong
// hai hàng đó chạy nhanh hơn. Hàng nguy hiểm được hiển thị trực tiếp trên mặt sân.
public sealed class EclipseForest : MonoBehaviour
{
    private const float FirstEclipseDelay = 24f;
    private const float EclipseDuration = 10f;
    private const float CalmDuration = 20f;
    private const float ShadowSpeedMultiplier = 1.2f;

    private readonly List<SpriteRenderer> rowShadows = new List<SpriteRenderer>();
    private readonly Dictionary<Zombie, float> boostedZombies = new Dictionary<Zombie, float>();
    private SunManagement sunManagement;
    private Text statusText;
    private int cycle;
    private int firstShadowRow = -1;
    private int secondShadowRow = -1;
    private bool eclipsed;
    private float phaseEndsAt;
    private float nextZombieScan;

    private void Start()
    {
        sunManagement = Object.FindAnyObjectByType<SunManagement>();
        BuildRowShadows();
        BuildStatusUI();
        phaseEndsAt = Time.time + FirstEclipseDelay;
        SetStatus("ÁNH SÁNG XUYÊN RỪNG", phaseEndsAt - Time.time);
    }

    private void Update()
    {
        if (Time.time >= phaseEndsAt)
        {
            if (eclipsed) EndEclipse();
            else BeginEclipse();
        }

        if (eclipsed && Time.time >= nextZombieScan)
        {
            nextZombieScan = Time.time + 0.35f;
            BoostZombiesInShadowRows();
        }

        AnimateShadows();
        SetStatus(eclipsed
            ? "NHẬT THỰ SÂU  •  HÀNG " + (firstShadowRow + 1) + " & " + (secondShadowRow + 1)
            : "ÁNH SÁNG XUYÊN RỪNG", phaseEndsAt - Time.time);
    }

    private void BeginEclipse()
    {
        eclipsed = true;
        cycle++;
        firstShadowRow = cycle * 2 % 5;
        secondShadowRow = (firstShadowRow + 2) % 5;
        phaseEndsAt = Time.time + EclipseDuration;
        if (sunManagement != null) sunManagement.SetSpawningPaused(true);

        for (int row = 0; row < rowShadows.Count; row++)
            rowShadows[row].gameObject.SetActive(row == firstShadowRow || row == secondShadowRow);
    }

    private void EndEclipse()
    {
        eclipsed = false;
        phaseEndsAt = Time.time + CalmDuration;
        if (sunManagement != null) sunManagement.SetSpawningPaused(false);
        RestoreZombieSpeeds();
        foreach (SpriteRenderer shadow in rowShadows) shadow.gameObject.SetActive(false);
    }

    private void BoostZombiesInShadowRows()
    {
        if (!NetSession.IsAuthority) return;

        foreach (Zombie zombie in Object.FindObjectsByType<Zombie>())
        {
            if (zombie == null || boostedZombies.ContainsKey(zombie)) continue;
            if (zombie.pos_row != firstShadowRow && zombie.pos_row != secondShadowRow) continue;
            boostedZombies.Add(zombie, zombie.speed);
            zombie.speed *= ShadowSpeedMultiplier;
        }
    }

    private void RestoreZombieSpeeds()
    {
        foreach (KeyValuePair<Zombie, float> pair in boostedZombies)
            if (pair.Key != null) pair.Key.speed = pair.Value;
        boostedZombies.Clear();
    }

    private void BuildRowShadows()
    {
        if (GameManagement.levelData == null || GameManagement.levelData.plantGridPosY == null) return;

        for (int row = 0; row < GameManagement.levelData.plantGridPosY.Count; row++)
        {
            var stripe = new GameObject("Bóng Nhật Thực - Hàng " + (row + 1), typeof(SpriteRenderer));
            stripe.transform.position = new Vector3(1.22f, GameManagement.levelData.plantGridPosY[row], 0f);
            stripe.transform.localScale = new Vector3(7.95f, 0.84f, 1f);
            SpriteRenderer renderer = stripe.GetComponent<SpriteRenderer>();
            renderer.sprite = Sprite.Create(Texture2D.whiteTexture,
                new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                new Vector2(0.5f, 0.5f), 1f);
            renderer.color = new Color(0.18f, 0.08f, 0.34f, 0.12f);
            renderer.sortingOrder = 0;
            stripe.SetActive(false);
            rowShadows.Add(renderer);
        }
    }

    private void AnimateShadows()
    {
        if (!eclipsed) return;
        float alpha = 0.10f + Mathf.PingPong(Time.time * 0.12f, 0.07f);
        foreach (SpriteRenderer shadow in rowShadows)
            if (shadow.gameObject.activeSelf)
                shadow.color = new Color(0.18f, 0.08f, 0.34f, alpha);
    }

    private void BuildStatusUI()
    {
        var canvasObject = new GameObject("Eclipse HUD", typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 220;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1000f, 750f);

        var panel = new GameObject("Eclipse Status", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.34f, 0.875f);
        panelRect.anchorMax = new Vector2(0.66f, 0.94f);
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.035f, 0.025f, 0.075f, 0.82f);

        var textObject = new GameObject("Status Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(panel.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 2f);
        textRect.offsetMax = new Vector2(-8f, -2f);
        statusText = textObject.GetComponent<Text>();
        statusText.font = Resources.Load<Font>("Fonts/Baloo2") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statusText.fontSize = 20;
        statusText.resizeTextForBestFit = true;
        statusText.alignment = TextAnchor.MiddleCenter;
        statusText.color = new Color(1f, 0.86f, 0.42f);
    }

    private void SetStatus(string title, float seconds)
    {
        if (statusText != null)
            statusText.text = title + "  •  " + Mathf.Max(0, Mathf.CeilToInt(seconds)) + "s";
    }

    private void OnDestroy()
    {
        if (sunManagement != null) sunManagement.SetSpawningPaused(false);
        RestoreZombieSpeeds();
    }
}
