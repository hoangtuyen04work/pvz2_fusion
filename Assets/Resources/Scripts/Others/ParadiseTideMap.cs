using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Điều phối chu kỳ thủy triều của Đảo Thiên Đường. Luật chơi chỉ tác động
/// lên ba cột ven biển; sáu cột còn lại luôn là vùng an toàn.
/// </summary>
public sealed class ParadiseTideMap : MonoBehaviour
{
    private enum TidePhase { Low, Warning, High }

    private sealed class TideCell
    {
        public PlantGrid Grid;
        public int Column;
        public SpriteRenderer Overlay;
    }

    private const int Columns = 9;
    private const int Rows = 5;
    private const int CoastStartColumn = 6;
    private const int StormStartColumn = 5;
    private const float LowTideDuration = 16f;
    private const float WarningDuration = 5f;
    private const float HighTideDuration = 10f;
    private const float GameplayTick = 0.75f;
    private const int TideDamage = 4;
    private const int PlantingReward = 25;
    private const int SurvivalReward = 50;
    private const float OverlayFramesPerSecond = 8f;
    private const float OverlaySizeBoost = 1.02f;
    private const string AssetRoot = "Sprites/Map8Fx/";
    private static readonly Vector2 OverlayCenterOffset = new Vector2(-0.035f, -0.025f);
    private static readonly Color[] PhaseColors =
    {
        new Color(1f, 0.78f, 0.16f),
        new Color(1f, 0.48f, 0.14f),
        new Color(0.20f, 0.82f, 1f)
    };

    // Vùng alpha thật của 5 icon trong texture 384x384, có thêm khoảng thở 12 px.
    // Dùng rect riêng để UI fit artwork thay vì fit cả vùng trong suốt của file nguồn.
    private static readonly Rect[] PhaseIconCropRects =
    {
        new Rect(98f, 103f, 186f, 189f),
        new Rect(101f, 101f, 185f, 193f),
        new Rect(100f, 100f, 185f, 181f),
        new Rect(95f, 100f, 187f, 201f),
        new Rect(95f, 96f, 192f, 198f)
    };

    private readonly PlantGrid[,] grids = new PlantGrid[Columns, Rows];
    private readonly List<TideCell> tideCells = new List<TideCell>();
    private readonly HashSet<PlantGrid> goldenCells = new HashSet<PlantGrid>();
    private readonly HashSet<PlantGrid> rewardedGoldenCells = new HashSet<PlantGrid>();

    private Transform zombieRoot;
    private SunNumber sunNumber;
    private Sprite[] goldenSandFrames;
    private Sprite[] tideWarningFrames;
    private Sprite[] shallowWaterFrames;
    private Sprite[] stormWaterFrames;
    private Sprite[] repelWaveFrames;
    private Sprite[] waterSplashFrames;
    private Sprite[] phaseIcons;
    private readonly List<Sprite> runtimeSprites = new List<Sprite>();
    private readonly Dictionary<Zombie, float> nextSplashAt = new Dictionary<Zombie, float>();
    private Vector2 gridCellSize = new Vector2(0.94f, 0.88f);
    private Texture2D circleTexture;
    private Sprite circleSprite;
    private Text phaseText;
    private Text bonusText;
    private Image phaseIconBackground;
    private Image phaseIcon;
    private readonly Image[] phaseDots = new Image[3];

    private TidePhase phase;
    private bool initialized;
    private bool gameplayActive;
    private int cycle;
    private float phaseEndsAt;
    private float nextGameplayTick;
    private float nextRewardScan;
    private float bonusVisibleUntil;
    private string bonusLabel = string.Empty;
    private int displayedSeconds = -1;
    private bool bonusWasVisible;

    public void BeginGameplay()
    {
        EnsureInitialized();
        if (!initialized || gameplayActive) return;

        gameplayActive = true;
        cycle = 0;
        BeginLowTide();
    }

    private void Start()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (initialized) return;

        GameObject plantingRoot = GameObject.Find("Planting Management");
        if (plantingRoot == null)
        {
            Debug.LogError("Paradise Tide requires Planting Management.", this);
            return;
        }

        foreach (PlantGrid grid in plantingRoot.GetComponentsInChildren<PlantGrid>(true))
        {
            if (!TryReadGridPosition(grid.gameObject.name, out int column, out int row)) continue;
            if (column < 0 || column >= Columns || row < 0 || row >= Rows) continue;
            grids[column, row] = grid;
        }

        for (int column = 0; column < Columns; column++)
            for (int row = 0; row < Rows; row++)
                if (grids[column, row] == null)
                {
                    Debug.LogError("Paradise Tide is missing grid " + column + "-" + row + ".", this);
                    return;
                }

        GameObject zombieObject = GameObject.Find("Zombie Management");
        zombieRoot = zombieObject != null ? zombieObject.transform : null;
        GameObject sunObject = GameObject.Find("Sun Text");
        sunNumber = sunObject != null ? sunObject.GetComponent<SunNumber>() : null;

        gridCellSize = MeasureGridCellSize();
        LoadMap8Sprites();
        BuildTideOverlays();
        BuildHud();
        SetPlantingBlocked(Columns);
        initialized = true;
    }

    private static bool TryReadGridPosition(string gridName, out int column, out int row)
    {
        column = row = -1;
        string[] parts = gridName.Split('-');
        return parts.Length == 3 && int.TryParse(parts[1], out column) && int.TryParse(parts[2], out row);
    }

    private void Update()
    {
        if (!gameplayActive || !initialized) return;

        if (Time.time >= phaseEndsAt)
        {
            if (phase == TidePhase.Low) BeginWarning();
            else if (phase == TidePhase.Warning) BeginHighTide();
            else BeginLowTide();
        }

        if (phase == TidePhase.Low && Time.time >= nextRewardScan)
        {
            nextRewardScan = Time.time + 0.25f;
            RewardNewGoldenPlants();
        }

        if (phase == TidePhase.High && Time.time >= nextGameplayTick)
        {
            nextGameplayTick = Time.time + GameplayTick;
            ApplyHighTideEffects();
        }

        AnimateOverlays();
        UpdateHud(false);
    }

    private void BeginLowTide()
    {
        phase = TidePhase.Low;
        cycle++;
        phaseEndsAt = Time.time + LowTideDuration;
        nextRewardScan = Time.time;
        SetPlantingBlocked(Columns);
        ChooseGoldenCells();
        RefreshOverlayVisibility();
        UpdateHud(true);
    }

    private void BeginWarning()
    {
        phase = TidePhase.Warning;
        phaseEndsAt = Time.time + WarningDuration;
        RefreshOverlayVisibility();
        UpdateHud(true);
    }

    private void BeginHighTide()
    {
        RewardGoldenSurvivors();
        phase = TidePhase.High;
        phaseEndsAt = Time.time + HighTideDuration;
        nextGameplayTick = Time.time;
        nextSplashAt.Clear();
        SetPlantingBlocked(FloodStartColumn);
        PushCoastalZombies();
        goldenCells.Clear();
        rewardedGoldenCells.Clear();
        RefreshOverlayVisibility();
        UpdateHud(true);
    }

    private int FloodStartColumn => cycle % 3 == 0 ? StormStartColumn : CoastStartColumn;

    private void ChooseGoldenCells()
    {
        goldenCells.Clear();
        rewardedGoldenCells.Clear();

        while (goldenCells.Count < 2)
        {
            int column = UnityEngine.Random.Range(CoastStartColumn, Columns);
            int row = UnityEngine.Random.Range(0, Rows);
            goldenCells.Add(grids[column, row]);
        }
    }

    private void RewardNewGoldenPlants()
    {
        if (!NetSession.IsAuthority || sunNumber == null) return;

        foreach (PlantGrid grid in goldenCells)
        {
            if (grid == null || grid.NowPlant == null || rewardedGoldenCells.Contains(grid)) continue;
            rewardedGoldenCells.Add(grid);
            sunNumber.addSun(PlantingReward);
            ShowBonus("+" + PlantingReward);
        }
    }

    private void RewardGoldenSurvivors()
    {
        if (!NetSession.IsAuthority || sunNumber == null) return;

        int survivors = 0;
        foreach (PlantGrid grid in rewardedGoldenCells)
            if (grid != null && grid.NowPlant != null)
                survivors++;

        if (survivors <= 0) return;
        int reward = survivors * SurvivalReward;
        sunNumber.addSun(reward);
        ShowBonus("+" + reward);
    }

    private void SetPlantingBlocked(int firstBlockedColumn)
    {
        for (int column = 0; column < Columns; column++)
            for (int row = 0; row < Rows; row++)
                if (grids[column, row] != null)
                    grids[column, row].SetPlantingBlocked(column >= firstBlockedColumn);
    }

    private void ApplyHighTideEffects()
    {
        if (!NetSession.IsAuthority) return;

        for (int column = FloodStartColumn; column < Columns; column++)
            for (int row = 0; row < Rows; row++)
            {
                GameObject plantObject = grids[column, row].NowPlant;
                Plant plant = plantObject != null ? plantObject.GetComponent<Plant>() : null;
                if (plant != null) plant.beAttacked(TideDamage, string.Empty);
            }

        if (zombieRoot == null) return;
        float waterEdge = grids[FloodStartColumn, 0].transform.position.x - 0.5f;
        for (int index = 0; index < zombieRoot.childCount; index++)
        {
            Transform child = zombieRoot.GetChild(index);
            if (!child.gameObject.activeInHierarchy || child.position.x < waterEdge) continue;
            Zombie zombie = child.GetComponent<Zombie>();
            if (zombie != null && !zombie.IsHypnotized)
            {
                zombie.ApplySlow(0.65f, GameplayTick + 0.4f);
                if (!nextSplashAt.TryGetValue(zombie, out float allowedAt) || Time.time >= allowedAt)
                {
                    nextSplashAt[zombie] = Time.time + 1.5f;
                    SpawnOneShot(waterSplashFrames, GetZombieGroundPoint(child),
                        new Vector2(0.78f, 0.68f), child);
                }
            }
        }
    }

    private void PushCoastalZombies()
    {
        if (!NetSession.IsAuthority || zombieRoot == null) return;

        float waterEdge = grids[FloodStartColumn, 0].transform.position.x - 0.5f;
        for (int index = 0; index < zombieRoot.childCount; index++)
        {
            Transform child = zombieRoot.GetChild(index);
            Zombie zombie = child.GetComponent<Zombie>();
            if (zombie != null && !zombie.IsHypnotized && child.position.x >= waterEdge)
            {
                Vector3 burstPosition = GetZombieGroundPoint(child) + Vector3.left * 0.18f;
                child.position += Vector3.right * 0.65f;
                SpawnOneShot(repelWaveFrames, burstPosition, new Vector2(1.15f, 0.95f), child);
            }
        }
    }

    private void LoadMap8Sprites()
    {
        goldenSandFrames = LoadSequence("golden_sand", "golden_sand", 8);
        tideWarningFrames = LoadSequence("tide_warning", "tide_warning", 8);
        shallowWaterFrames = LoadSequence("shallow_water", "shallow_water", 8);
        stormWaterFrames = LoadSequence("storm_water", "storm_water", 8);
        repelWaveFrames = LoadSequence("repel_wave", "repel_wave", 8);
        waterSplashFrames = LoadSequence("water_splash", "water_splash", 8);
        phaseIcons = LoadHudPhaseIcons();
    }

    private Sprite[] LoadSequence(string folder, string prefix, int count)
    {
        var frames = new Sprite[count];
        int missingCount = 0;
        for (int index = 0; index < count; index++)
        {
            string path = AssetRoot + folder + "/" + prefix + "-" + (index + 1);
            Sprite sprite = Resources.Load<Sprite>(path);
            if (sprite == null)
            {
                // Cho phép dùng cả asset Texture2D nếu cấu hình import bị thay đổi sau này.
                Texture2D texture = Resources.Load<Texture2D>(path);
                if (texture != null)
                {
                    sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                    sprite.name = prefix + "-" + (index + 1);
                    runtimeSprites.Add(sprite);
                }
            }

            if (sprite == null)
            {
                missingCount++;
                continue;
            }

            frames[index] = sprite;
        }

        if (missingCount > 0)
            Debug.LogError("Map 8 is missing " + missingCount + "/" + count
                + " sprites in Resources/" + AssetRoot + folder + ".", this);
        return frames;
    }

    private Sprite[] LoadHudPhaseIcons()
    {
        Sprite[] sourceIcons = LoadSequence("phase_icon", "phase_icon", 5);
        var croppedIcons = new Sprite[sourceIcons.Length];
        for (int index = 0; index < sourceIcons.Length; index++)
        {
            Sprite source = sourceIcons[index];
            if (source == null) continue;

            Texture2D texture = source.texture;
            Rect referenceRect = PhaseIconCropRects[index];
            float scaleX = texture.width / 384f;
            float scaleY = texture.height / 384f;
            Rect cropRect = new Rect(referenceRect.x * scaleX, referenceRect.y * scaleY,
                referenceRect.width * scaleX, referenceRect.height * scaleY);
            cropRect.x = Mathf.Clamp(cropRect.x, 0f, texture.width - 1f);
            cropRect.y = Mathf.Clamp(cropRect.y, 0f, texture.height - 1f);
            cropRect.width = Mathf.Min(cropRect.width, texture.width - cropRect.x);
            cropRect.height = Mathf.Min(cropRect.height, texture.height - cropRect.y);

            Sprite cropped = Sprite.Create(texture, cropRect, new Vector2(0.5f, 0.5f),
                100f, 0, SpriteMeshType.FullRect);
            cropped.name = source.name + "-hud-cropped";
            runtimeSprites.Add(cropped);
            croppedIcons[index] = cropped;
        }
        return croppedIcons;
    }

    private void BuildTideOverlays()
    {
        for (int column = StormStartColumn; column < Columns; column++)
            for (int row = 0; row < Rows; row++)
            {
                PlantGrid grid = grids[column, row];
                var overlayObject = new GameObject("Tide Overlay " + column + "-" + row, typeof(SpriteRenderer));
                overlayObject.transform.SetParent(transform, false);
                overlayObject.transform.position = grid.transform.position
                    + new Vector3(OverlayCenterOffset.x, OverlayCenterOffset.y, 1f);
                SpriteRenderer renderer = overlayObject.GetComponent<SpriteRenderer>();
                renderer.sprite = FirstAvailable(goldenSandFrames);
                renderer.sortingLayerName = "Default";
                renderer.sortingOrder = 1;
                renderer.color = Color.clear;
                FitRenderer(renderer, GetOverlayWorldSize());
                tideCells.Add(new TideCell { Grid = grid, Column = column, Overlay = renderer });
            }
    }

    private Vector2 MeasureGridCellSize()
    {
        float totalWidth = 0f;
        int widthSamples = 0;
        for (int column = 1; column < Columns; column++)
            for (int row = 0; row < Rows; row++)
            {
                totalWidth += Mathf.Abs(grids[column, row].transform.position.x
                    - grids[column - 1, row].transform.position.x);
                widthSamples++;
            }

        float totalHeight = 0f;
        int heightSamples = 0;
        for (int column = 0; column < Columns; column++)
            for (int row = 1; row < Rows; row++)
            {
                totalHeight += Mathf.Abs(grids[column, row].transform.position.y
                    - grids[column, row - 1].transform.position.y);
                heightSamples++;
            }

        return new Vector2(
            widthSamples > 0 ? totalWidth / widthSamples : 0.94f,
            heightSamples > 0 ? totalHeight / heightSamples : 0.88f);
    }

    private Vector2 GetOverlayWorldSize()
    {
        // Bù phần padding trong suốt khác nhau của từng bộ frame. Phần alpha nhìn thấy
        // sau khi bù và tăng 2% sẽ chạm nhẹ nhau, tạo thành vùng thủy triều liền mạch.
        Vector2 size;
        if (phase == TidePhase.Low)
            size = new Vector2(gridCellSize.x * 1.18f, gridCellSize.y * 1.31f);
        else if (phase == TidePhase.Warning)
            size = new Vector2(gridCellSize.x * 1.17f, gridCellSize.y * 1.20f);
        else if (cycle % 3 == 0)
            size = new Vector2(gridCellSize.x * 1.06f, gridCellSize.y * 1.10f);
        else
            size = new Vector2(gridCellSize.x * 1.15f, gridCellSize.y * 1.20f);
        return size * OverlaySizeBoost;
    }

    private void RefreshOverlayVisibility()
    {
        foreach (TideCell cell in tideCells)
        {
            bool visible = phase == TidePhase.Low
                ? goldenCells.Contains(cell.Grid)
                : cell.Column >= FloodStartColumn;
            cell.Overlay.gameObject.SetActive(visible);
        }
    }

    private void AnimateOverlays()
    {
        int frameIndex = Mathf.FloorToInt(Time.time * OverlayFramesPerSecond) % 8;
        Sprite[] activeFrames = phase == TidePhase.Low
            ? goldenSandFrames
            : phase == TidePhase.Warning
                ? tideWarningFrames
                : cycle % 3 == 0 ? stormWaterFrames : shallowWaterFrames;
        float alpha = phase == TidePhase.High ? 0.72f : 1f;
        foreach (TideCell cell in tideCells)
        {
            if (!cell.Overlay.gameObject.activeSelf) continue;
            Sprite nextSprite = activeFrames != null && activeFrames.Length > 0
                ? activeFrames[frameIndex % activeFrames.Length]
                : null;
            if (nextSprite != null && cell.Overlay.sprite != nextSprite)
            {
                cell.Overlay.sprite = nextSprite;
                FitRenderer(cell.Overlay, GetOverlayWorldSize());
            }
            cell.Overlay.color = new Color(1f, 1f, 1f, alpha);
        }
    }

    private static Sprite FirstAvailable(Sprite[] frames)
    {
        if (frames == null) return null;
        foreach (Sprite frame in frames)
            if (frame != null) return frame;
        return null;
    }

    private static void FitRenderer(SpriteRenderer renderer, Vector2 worldSize)
    {
        if (renderer == null || renderer.sprite == null) return;
        Vector2 spriteSize = renderer.sprite.bounds.size;
        renderer.transform.localScale = new Vector3(
            spriteSize.x > 0f ? worldSize.x / spriteSize.x : 1f,
            spriteSize.y > 0f ? worldSize.y / spriteSize.y : 1f,
            1f);
    }

    private void SpawnOneShot(Sprite[] frames, Vector3 position, Vector2 worldSize, Transform sortingReference)
    {
        if (FirstAvailable(frames) == null) return;
        StartCoroutine(PlayOneShot(frames, position, worldSize, sortingReference));
    }

    private IEnumerator PlayOneShot(Sprite[] frames, Vector3 position, Vector2 worldSize, Transform sortingReference)
    {
        var effectObject = new GameObject("Map 8 Water Effect", typeof(SpriteRenderer));
        effectObject.transform.SetParent(transform, true);
        effectObject.transform.position = position;
        SpriteRenderer effectRenderer = effectObject.GetComponent<SpriteRenderer>();
        SpriteRenderer referenceRenderer = FindPrimaryRenderer(sortingReference);
        if (referenceRenderer != null)
        {
            effectRenderer.sortingLayerID = referenceRenderer.sortingLayerID;
            int lowestOrder = referenceRenderer.sortingOrder;
            foreach (SpriteRenderer renderer in sortingReference.GetComponentsInChildren<SpriteRenderer>(true))
                if (renderer.sortingLayerID == referenceRenderer.sortingLayerID)
                    lowestOrder = Mathf.Min(lowestOrder, renderer.sortingOrder);
            effectRenderer.sortingOrder = lowestOrder - 1;
        }
        else
        {
            effectRenderer.sortingLayerName = "Default";
            effectRenderer.sortingOrder = 12;
        }

        foreach (Sprite frame in frames)
        {
            if (frame != null)
            {
                effectRenderer.sprite = frame;
                effectObject.transform.position = position;
                FitRenderer(effectRenderer, worldSize);
                // Tính lại từ điểm chân cho từng frame để ảnh có bounds khác nhau không làm hiệu ứng nảy.
                Vector3 alignedPosition = position;
                alignedPosition.y += position.y - effectRenderer.bounds.min.y - 0.015f;
                effectObject.transform.position = alignedPosition;
            }
            yield return new WaitForSeconds(1f / OverlayFramesPerSecond);
        }
        Destroy(effectObject);
    }

    private static SpriteRenderer FindPrimaryRenderer(Transform target)
    {
        if (target == null) return null;
        SpriteRenderer rootRenderer = target.GetComponent<SpriteRenderer>();
        if (rootRenderer != null && rootRenderer.enabled) return rootRenderer;
        foreach (SpriteRenderer renderer in target.GetComponentsInChildren<SpriteRenderer>(true))
            if (renderer.enabled && renderer.gameObject.activeInHierarchy)
                return renderer;
        return null;
    }

    private static Vector3 GetZombieGroundPoint(Transform zombieTransform)
    {
        if (zombieTransform == null) return Vector3.zero;

        bool hasBounds = false;
        Bounds combinedBounds = default;
        foreach (SpriteRenderer renderer in zombieTransform.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (!renderer.enabled || !renderer.gameObject.activeInHierarchy || renderer.sprite == null) continue;
            if (!hasBounds)
            {
                combinedBounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                combinedBounds.Encapsulate(renderer.bounds);
            }
        }

        if (!hasBounds) return zombieTransform.position;
        return new Vector3(combinedBounds.center.x, combinedBounds.min.y,
            zombieTransform.position.z);
    }

    private void BuildHud()
    {
        var canvasObject = new GameObject("Paradise Tide HUD", typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 220;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800f, 600f);
        scaler.matchWidthOrHeight = 0.5f;

        var panel = new GameObject("Tide Status", typeof(RectTransform), typeof(RawImage));
        panel.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        // Đặt dưới seed bank để HUD lớn hơn nhưng không che thẻ bài hoặc nút pause.
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-78f, -13f);
        panelRect.sizeDelta = new Vector2(290f, 48f);
        RawImage panelImage = panel.GetComponent<RawImage>();
        Sprite hudSprite = Resources.Load<Sprite>("Sprites/BackGround/hub");
        if (hudSprite == null)
            Debug.LogError("Missing Paradise Tide HUD sprite: Sprites/BackGround/hub", this);
        panelImage.texture = hudSprite != null ? hudSprite.texture : Texture2D.whiteTexture;
        // hub.png có vùng trong suốt lớn; chỉ hiển thị khung thật mà không sửa ảnh nguồn.
        panelImage.uvRect = new Rect(0.037317f, 0.344262f, 0.939486f, 0.385876f);
        panelImage.color = hudSprite != null ? Color.white : new Color(0.24f, 0.16f, 0.07f, 0.94f);
        panelImage.raycastTarget = false;

        CreateCircleSprite();

        var iconBackgroundObject = new GameObject("Phase Circle", typeof(RectTransform), typeof(Image));
        iconBackgroundObject.transform.SetParent(panel.transform, false);
        RectTransform iconBackgroundRect = iconBackgroundObject.GetComponent<RectTransform>();
        iconBackgroundRect.anchorMin = iconBackgroundRect.anchorMax = new Vector2(0.145f, 0.5f);
        iconBackgroundRect.pivot = new Vector2(0.5f, 0.5f);
        iconBackgroundRect.anchoredPosition = Vector2.zero;
        iconBackgroundRect.sizeDelta = new Vector2(40f, 40f);
        phaseIconBackground = iconBackgroundObject.GetComponent<Image>();
        phaseIconBackground.sprite = circleSprite;
        phaseIconBackground.preserveAspect = true;
        phaseIconBackground.raycastTarget = false;

        var iconObject = new GameObject("Phase Icon", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(iconBackgroundObject.transform, false);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        SetAnchors(iconRect, 0.12f, 0.12f, 0.88f, 0.88f);
        phaseIcon = iconObject.GetComponent<Image>();
        phaseIcon.sprite = phaseIcons != null && phaseIcons.Length > 0 ? phaseIcons[0] : circleSprite;
        phaseIcon.preserveAspect = true;
        phaseIcon.raycastTarget = false;

        var textObject = new GameObject("Phase", typeof(RectTransform), typeof(Text), typeof(Outline));
        textObject.transform.SetParent(panel.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        SetAnchors(textRect, 0.228f, 0.06f, 0.615f, 0.94f);
        phaseText = textObject.GetComponent<Text>();
        phaseText.font = Resources.Load<Font>("Fonts/Baloo2") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        phaseText.fontSize = 18;
        phaseText.fontStyle = FontStyle.Bold;
        phaseText.resizeTextForBestFit = true;
        phaseText.resizeTextMinSize = 11;
        phaseText.alignment = TextAnchor.MiddleCenter;
        phaseText.color = Color.white;
        phaseText.raycastTarget = false;
        Outline outline = textObject.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
        outline.effectDistance = new Vector2(1f, -1f);

        var bonusObject = new GameObject("Tide Bonus", typeof(RectTransform), typeof(Text), typeof(Outline));
        bonusObject.transform.SetParent(panel.transform, false);
        RectTransform bonusRect = bonusObject.GetComponent<RectTransform>();
        SetAnchors(bonusRect, 0.615f, 0.06f, 0.775f, 0.94f);
        bonusText = bonusObject.GetComponent<Text>();
        bonusText.font = phaseText.font;
        bonusText.fontSize = 16;
        bonusText.fontStyle = FontStyle.Bold;
        bonusText.resizeTextForBestFit = true;
        bonusText.resizeTextMinSize = 10;
        bonusText.alignment = TextAnchor.MiddleCenter;
        bonusText.color = new Color(0.25f, 0.12f, 0.02f);
        bonusText.raycastTarget = false;
        Outline bonusOutline = bonusObject.GetComponent<Outline>();
        bonusOutline.effectColor = new Color(1f, 0.88f, 0.35f, 0.75f);
        bonusOutline.effectDistance = new Vector2(1f, -1f);

        for (int index = 0; index < phaseDots.Length; index++)
        {
            var dotObject = new GameObject("Phase Dot " + index, typeof(RectTransform), typeof(Image));
            dotObject.transform.SetParent(panel.transform, false);
            RectTransform dotRect = dotObject.GetComponent<RectTransform>();
            float x = 0.806f + index * 0.068f;
            dotRect.anchorMin = dotRect.anchorMax = new Vector2(x, 0.46f);
            dotRect.pivot = new Vector2(0.5f, 0.5f);
            dotRect.anchoredPosition = Vector2.zero;
            dotRect.sizeDelta = new Vector2(18f, 18f);
            phaseDots[index] = dotObject.GetComponent<Image>();
            phaseDots[index].sprite = circleSprite;
            phaseDots[index].preserveAspect = true;
            phaseDots[index].raycastTarget = false;
        }
    }

    private void CreateCircleSprite()
    {
        const int size = 32;
        circleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        circleTexture.name = "Paradise HUD Circle";
        circleTexture.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.46f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(radius - distance + 1f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
            }
        circleTexture.SetPixels(pixels);
        circleTexture.Apply(false, true);
        circleSprite = Sprite.Create(circleTexture, new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f), size);
    }

    private void UpdateHud(bool force)
    {
        if (phaseText == null) return;
        int seconds = Mathf.Max(0, Mathf.CeilToInt(phaseEndsAt - Time.time));
        bool bonusVisible = Time.time < bonusVisibleUntil;
        if (!force && seconds == displayedSeconds && bonusVisible == bonusWasVisible) return;
        displayedSeconds = seconds;
        bonusWasVisible = bonusVisible;

        string label;
        Color color;
        int iconIndex;
        if (phase == TidePhase.Low)
        {
            label = "NƯỚC RÒNG";
            color = PhaseColors[0];
            iconIndex = 0;
        }
        else if (phase == TidePhase.Warning)
        {
            label = cycle % 3 == 0 ? "SÓNG LỚN" : "SÓNG ĐẾN";
            color = PhaseColors[1];
            iconIndex = 1;
        }
        else
        {
            label = cycle % 3 == 0 ? "TRIỀU LỚN" : "TRIỀU CAO";
            color = PhaseColors[2];
            iconIndex = cycle % 3 == 0 ? 3 : 2;
        }

        phaseText.text = label + "  " + seconds + "s";
        bonusText.text = bonusVisible ? bonusLabel : string.Empty;
        if (phaseIconBackground != null)
        {
            Color backgroundColor = bonusVisible ? new Color(0.52f, 1f, 0.36f) : color;
            backgroundColor.a = 0.92f;
            phaseIconBackground.color = backgroundColor;
        }
        if (phaseIcons != null && phaseIcons.Length == 5 && phaseIcons[iconIndex] != null)
        {
            phaseIcon.sprite = bonusVisible && phaseIcons[4] != null ? phaseIcons[4] : phaseIcons[iconIndex];
            phaseIcon.color = Color.white;
        }
        else
        {
            phaseIcon.sprite = circleSprite;
            phaseIcon.color = color;
        }
        for (int index = 0; index < phaseDots.Length; index++)
        {
            Color dotColor = PhaseColors[index];
            dotColor.a = index == (int)phase ? 1f : 0.28f;
            phaseDots[index].color = dotColor;
        }
    }

    private void ShowBonus(string value)
    {
        bonusLabel = value;
        bonusVisibleUntil = Time.time + 1.5f;
        UpdateHud(true);
    }

    private static void SetAnchors(RectTransform rect, float xMin, float yMin, float xMax, float yMax)
    {
        rect.anchorMin = new Vector2(xMin, yMin);
        rect.anchorMax = new Vector2(xMax, yMax);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private void OnDestroy()
    {
        if (initialized) SetPlantingBlocked(Columns);
        StopAllCoroutines();
        foreach (Sprite sprite in runtimeSprites)
            if (sprite != null) Destroy(sprite);
        if (circleSprite != null) Destroy(circleSprite);
        if (circleTexture != null) Destroy(circleTexture);
    }
}
