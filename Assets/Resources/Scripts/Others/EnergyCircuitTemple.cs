using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Cơ chế riêng của Màn 9. Mỗi mạch gồm bốn nút; phủ cây lên đủ bốn nút trước
// khi hết giờ sẽ phóng EMP, còn thất bại khiến cây trên nút bị quá tải.
public sealed class EnergyCircuitTemple : MonoBehaviour
{
    private enum CircuitPhase { Waiting, Active, Result }

    private sealed class CircuitNode
    {
        public PlantGrid Grid;
        public SpriteRenderer Ring;
    }

    private static readonly Vector2Int[][] Patterns =
    {
        new[] { new Vector2Int(1, 0), new Vector2Int(3, 1), new Vector2Int(5, 3), new Vector2Int(7, 4) },
        new[] { new Vector2Int(2, 4), new Vector2Int(3, 2), new Vector2Int(5, 0), new Vector2Int(7, 1) },
        new[] { new Vector2Int(1, 3), new Vector2Int(4, 4), new Vector2Int(5, 1), new Vector2Int(8, 2) },
        new[] { new Vector2Int(2, 1), new Vector2Int(4, 0), new Vector2Int(6, 2), new Vector2Int(7, 4) },
        new[] { new Vector2Int(1, 2), new Vector2Int(3, 4), new Vector2Int(6, 3), new Vector2Int(8, 0) }
    };

    private const float FirstCircuitDelay = 20f;
    private const float CircuitDuration = 26f;
    private const float RestDuration = 10f;
    private const float ResultDuration = 2.2f;
    private const float CompletionConfirmDuration = 0.5f;
    private const int ExpectedGridCount = 45;
    private const int SuccessReward = 75;
    private const int PulseDamage = 140;
    private const int OverloadDamage = 80;
    private const float EffectFramesPerSecond = 12f;
    private const string ArtRoot = "Sprites/Map9_Art/";
    private static readonly Vector2 NodeFootOffset = new Vector2(-0.05f, -0.31f);

    private readonly Dictionary<Vector2Int, PlantGrid> gridLookup = new Dictionary<Vector2Int, PlantGrid>();
    private readonly List<CircuitNode> activeNodes = new List<CircuitNode>();
    private readonly List<LineRenderer> links = new List<LineRenderer>();
    private Sprite nodeDormantSprite;
    private Sprite nodeTargetSprite;
    private Sprite nodeChargedSprite;
    private Sprite nodeOverloadSprite;
    private Sprite energyLinkSprite;
    private Sprite[] successFrames;
    private Sprite[] overloadFrames;
    private Sprite zombieLightningSprite;
    private Sprite zombieFrozenAuraSprite;
    private Sprite zombieSlowAuraSprite;
    private Sprite plantOverloadSprite;
    private Sprite hudPanelSprite;
    private Sprite hudTimelineSprite;
    private Sprite hudWaitingSprite;
    private Sprite hudActiveSprite;
    private Sprite hudSuccessSprite;
    private Sprite hudFailureSprite;
    private Text titleText;
    private Text detailText;
    private Image timelineFill;
    private Image statusIcon;
    private SunNumber sunNumber;
    private bool gameplayActive;
    private bool initialized;
    private CircuitPhase phase = CircuitPhase.Waiting;
    private float nextCircuitAt;
    private float circuitEndsAt;
    private float resultEndsAt;
    private int patternIndex = -1;
    private int lastFilled = -1;
    private int lastSecond = -1;
    private float allNodesFilledAt = -1f;

    public void BeginGameplay()
    {
        gameplayActive = true;
        phase = CircuitPhase.Waiting;
        nextCircuitAt = Time.time + FirstCircuitDelay;
        SetHud("MẠCH ĐANG KHỞI ĐỘNG", "Quan sát các nút năng lượng...", 0f, hudWaitingSprite);
    }

    private void Start()
    {
        IndexGrids();
        bool artReady = LoadArt();
        BuildHud();
        GameObject sunObject = GameObject.Find("Sun Text");
        sunNumber = sunObject != null ? sunObject.GetComponent<SunNumber>() : null;
        initialized = gridLookup.Count == ExpectedGridCount && ValidatePatterns() && artReady
            && titleText != null && detailText != null && timelineFill != null && statusIcon != null;
        if (!initialized)
            Debug.LogError("Map 9 circuit initialization failed: indexed " + gridLookup.Count
                + "/" + ExpectedGridCount + " planting grids.", this);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        else
            Debug.Log("Map 9 circuit ready: 45 grids, " + Patterns.Length
                + " valid patterns.", this);
#endif
    }

    private void Update()
    {
        if (!gameplayActive || !initialized) return;
        RefreshTimeline();

        if (phase == CircuitPhase.Result)
        {
            if (Time.time >= resultEndsAt)
            {
                ClearCircuitVisuals();
                phase = CircuitPhase.Waiting;
                nextCircuitAt = Time.time + RestDuration;
                lastSecond = -1;
            }
            return;
        }

        if (phase == CircuitPhase.Waiting)
        {
            int seconds = Mathf.Max(0, Mathf.CeilToInt(nextCircuitAt - Time.time));
            if (seconds != lastSecond)
            {
                lastSecond = seconds;
                SetHud("MẠCH KẾ TIẾP", "Sẵn sàng sau " + seconds + " giây", 0f,
                    hudWaitingSprite);
            }
            if (Time.time >= nextCircuitAt) StartCircuit();
            return;
        }

        int filled = CountFilledNodes();
        int remaining = Mathf.Max(0, Mathf.CeilToInt(circuitEndsAt - Time.time));
        RefreshNodeColors();
        if (filled != lastFilled || remaining != lastSecond)
        {
            lastFilled = filled;
            lastSecond = remaining;
            SetHud("KÍCH HOẠT MẠCH: " + filled + "/" + activeNodes.Count,
                "Trồng đủ nút • còn " + remaining + " giây", CurrentTimelineProgress(),
                hudActiveSprite);
        }

        bool allNodesFilled = activeNodes.Count == Patterns[patternIndex].Length
            && filled == activeNodes.Count;
        if (allNodesFilled)
        {
            if (allNodesFilledAt < 0f)
            {
                allNodesFilledAt = Time.time;
                SetHud("MẠCH ĐÃ KHÉP!", "Đang ổn định năng lượng...", 1f, hudSuccessSprite);
            }
            else if (Time.time >= allNodesFilledAt + CompletionConfirmDuration)
            {
                CompleteCircuit();
            }
        }
        else
        {
            allNodesFilledAt = -1f;
        }

        if (phase == CircuitPhase.Active && Time.time >= circuitEndsAt) FailCircuit();
    }

    private void IndexGrids()
    {
        gridLookup.Clear();
        GameObject root = GameObject.Find("Planting Management");
        if (root == null) return;
        foreach (PlantGrid grid in root.GetComponentsInChildren<PlantGrid>(true))
        {
            string[] parts = grid.name.Split('-');
            if (parts.Length != 3 || !int.TryParse(parts[1], out int column)
                || !int.TryParse(parts[2], out int row)) continue;
            gridLookup[new Vector2Int(column, row)] = grid;
        }
    }

    private static bool ValidatePatterns()
    {
        for (int patternIndex = 0; patternIndex < Patterns.Length; patternIndex++)
        {
            Vector2Int[] pattern = Patterns[patternIndex];
            if (pattern == null || pattern.Length != 4)
            {
                Debug.LogError("Map 9 pattern " + patternIndex + " must contain exactly four nodes.");
                return false;
            }

            var unique = new HashSet<Vector2Int>();
            foreach (Vector2Int coordinate in pattern)
            {
                if (coordinate.x < 0 || coordinate.x >= 9 || coordinate.y < 0 || coordinate.y >= 5
                    || !unique.Add(coordinate))
                {
                    Debug.LogError("Map 9 pattern " + patternIndex
                        + " contains an invalid or duplicated node: " + coordinate + ".");
                    return false;
                }
            }
        }
        return true;
    }

    private void StartCircuit()
    {
        ClearCircuitVisuals();
        if (gridLookup.Count != ExpectedGridCount)
        {
            IndexGrids();
            if (gridLookup.Count != ExpectedGridCount)
            {
                phase = CircuitPhase.Waiting;
                nextCircuitAt = Time.time + 1f;
                Debug.LogError("Map 9 cannot start a circuit without all 45 planting grids.", this);
                return;
            }
        }

        patternIndex = SelectNextPattern();
        foreach (Vector2Int coordinate in Patterns[patternIndex])
        {
            if (!gridLookup.TryGetValue(coordinate, out PlantGrid grid)) continue;
            SpriteRenderer gridRenderer = grid.GetComponent<SpriteRenderer>();
            var ringObject = new GameObject("Energy Node " + coordinate.x + "-" + coordinate.y,
                typeof(SpriteRenderer));
            ringObject.transform.SetParent(grid.transform, false);
            ringObject.transform.localPosition = new Vector3(NodeFootOffset.x, NodeFootOffset.y, 1f);
            var ring = ringObject.GetComponent<SpriteRenderer>();
            ring.sprite = nodeDormantSprite;
            ring.color = Color.white;
            if (gridRenderer != null)
            {
                ring.sortingLayerID = gridRenderer.sortingLayerID;
                ring.sortingOrder = gridRenderer.sortingOrder - 1;
            }
            // Ép thành elip mặt đất và đặt dưới chân cây trong ô 0.94 x 0.70.
            ringObject.transform.localScale = new Vector3(0.68f, 0.42f, 1f);
            activeNodes.Add(new CircuitNode { Grid = grid, Ring = ring });
        }

        if (activeNodes.Count != Patterns[patternIndex].Length)
        {
            Debug.LogError("Map 9 circuit pattern " + patternIndex + " resolved only "
                + activeNodes.Count + "/" + Patterns[patternIndex].Length + " nodes.", this);
            ClearCircuitVisuals();
            phase = CircuitPhase.Waiting;
            nextCircuitAt = Time.time + 1f;
            return;
        }

        for (int i = 1; i < activeNodes.Count; i++) CreateLink(activeNodes[i - 1], activeNodes[i]);
        phase = CircuitPhase.Active;
        circuitEndsAt = Time.time + CircuitDuration;
        allNodesFilledAt = -1f;
        lastFilled = -1;
        lastSecond = -1;
        RefreshNodeColors();
        SetHud("KÍCH HOẠT MẠCH: 0/" + activeNodes.Count,
            "Trồng cây lên các node phát sáng", 1f, hudActiveSprite);
    }

    private int CountFilledNodes()
    {
        int count = 0;
        foreach (CircuitNode node in activeNodes)
            if (node.Grid != null && node.Grid.HavePlanted && node.Grid.NowPlant != null) count++;
        return count;
    }

    // Ưu tiên mạch có ít cây đặt sẵn nhất. Cách chọn vẫn hoàn toàn xác định để
    // các máy trong phiên mạng cùng nhìn thấy một mẫu mạch.
    private int SelectNextPattern()
    {
        int firstCandidate = (patternIndex + 1) % Patterns.Length;
        int selected = firstCandidate;
        int fewestOccupied = int.MaxValue;

        for (int offset = 0; offset < Patterns.Length; offset++)
        {
            int candidate = (firstCandidate + offset) % Patterns.Length;
            int occupied = 0;
            foreach (Vector2Int coordinate in Patterns[candidate])
            {
                if (gridLookup.TryGetValue(coordinate, out PlantGrid grid)
                    && grid.HavePlanted && grid.NowPlant != null)
                    occupied++;
            }

            if (occupied >= fewestOccupied) continue;
            selected = candidate;
            fewestOccupied = occupied;
            if (occupied == 0) break;
        }

        return selected;
    }

    private void RefreshNodeColors()
    {
        float pulse = 0.86f + Mathf.Sin(Time.time * 5f) * 0.12f;
        bool targetPulse = Mathf.Sin(Time.time * 4f) > -0.15f;
        foreach (CircuitNode node in activeNodes)
        {
            if (node.Ring == null) continue;
            bool filled = node.Grid != null && node.Grid.HavePlanted && node.Grid.NowPlant != null;
            node.Ring.sprite = filled ? nodeChargedSprite
                : targetPulse ? nodeTargetSprite : nodeDormantSprite;
            node.Ring.color = filled ? Color.white : new Color(1f, 1f, 1f, pulse);
        }
    }

    private void CompleteCircuit()
    {
        if (phase != CircuitPhase.Active) return;
        if (NetSession.IsAuthority)
        {
            foreach (Zombie zombie in Object.FindObjectsByType<Zombie>())
            {
                if (zombie == null || !zombie.IsAlive || !zombie.gameObject.activeInHierarchy) continue;
                // Chỉ khóa zombie, không đổi Time.timeScale nên gameplay và UI vẫn tiếp tục chạy.
                SpawnStaticEffect(zombieLightningSprite, zombie.transform, new Vector2(.9f, 1.15f), .32f, 30);
                StartCoroutine(ShowZombieStatus(zombie));
                zombie.ApplyFreeze(2.5f, 5f);
                zombie.beAttacked(PulseDamage);
            }
            if (sunNumber != null) sunNumber.addSun(SuccessReward);
        }
        foreach (CircuitNode node in activeNodes)
        {
            if (node.Ring != null)
            {
                node.Ring.sprite = nodeChargedSprite;
                node.Ring.color = Color.white;
            }
            SpawnOneShot(successFrames, NodeWorldPosition(node.Grid, 1.2f),
                new Vector2(.86f, .70f), node.Grid.transform, 6);
        }
        EndCircuit("XUNG ĐIỆN HOÀN TẤT!", "+" + SuccessReward + " nắng • zombie bị choáng",
            hudSuccessSprite, 1f);
    }

    private void FailCircuit()
    {
        if (phase != CircuitPhase.Active) return;
        if (NetSession.IsAuthority)
        {
            foreach (CircuitNode node in activeNodes)
            {
                if (node.Grid == null || node.Grid.NowPlant == null) continue;
                Plant plant = node.Grid.NowPlant.GetComponent<Plant>();
                if (plant != null)
                {
                    SpawnStaticEffect(plantOverloadSprite, plant.transform,
                        new Vector2(.82f, 1.0f), .55f, 30);
                    plant.beAttacked(OverloadDamage, "");
                }
            }
        }
        foreach (CircuitNode node in activeNodes)
        {
            if (node.Ring != null)
            {
                node.Ring.sprite = nodeOverloadSprite;
                node.Ring.color = Color.white;
            }
            SpawnOneShot(overloadFrames, NodeWorldPosition(node.Grid, 1.2f),
                new Vector2(.86f, .70f), node.Grid.transform, 6);
        }
        EndCircuit("MẠCH BỊ QUÁ TẢI", "Cây trên nút mất " + OverloadDamage + " máu",
            hudFailureSprite, 0f);
    }

    private void EndCircuit(string title, string detail, Sprite icon, float timelineProgress)
    {
        phase = CircuitPhase.Result;
        allNodesFilledAt = -1f;
        resultEndsAt = Time.time + ResultDuration;
        SetHud(title, detail, timelineProgress, icon);
    }

    private void CreateLink(CircuitNode from, CircuitNode to)
    {
        var linkObject = new GameObject("Energy Link", typeof(LineRenderer));
        linkObject.transform.SetParent(transform, false);
        LineRenderer line = linkObject.GetComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.SetPosition(0, NodeWorldPosition(from.Grid, 1f));
        line.SetPosition(1, NodeWorldPosition(to.Grid, 1f));
        line.startWidth = line.endWidth = 0.055f;
        line.startColor = line.endColor = new Color(1f, 1f, 1f, .82f);
        line.textureMode = LineTextureMode.Stretch;
        line.alignment = LineAlignment.View;
        line.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        if (energyLinkSprite != null)
            line.sharedMaterial.mainTexture = energyLinkSprite.texture;
        SpriteRenderer gridRenderer = from.Grid.GetComponent<SpriteRenderer>();
        if (gridRenderer != null)
        {
            line.sortingLayerID = gridRenderer.sortingLayerID;
            line.sortingOrder = gridRenderer.sortingOrder - 2;
        }
        links.Add(line);
    }

    private static Vector3 NodeWorldPosition(PlantGrid grid, float zOffset)
    {
        if (grid == null) return Vector3.zero;
        return grid.transform.position + new Vector3(NodeFootOffset.x, NodeFootOffset.y, zOffset);
    }

    private void ClearCircuitVisuals()
    {
        foreach (CircuitNode node in activeNodes)
            if (node.Ring != null) Destroy(node.Ring.gameObject);
        foreach (LineRenderer link in links)
            if (link != null)
            {
                if (link.sharedMaterial != null) Destroy(link.sharedMaterial);
                Destroy(link.gameObject);
            }
        activeNodes.Clear();
        links.Clear();
    }

    private bool LoadArt()
    {
        nodeDormantSprite = Resources.Load<Sprite>(ArtRoot + "nodes/map9_node_dormant");
        nodeTargetSprite = Resources.Load<Sprite>(ArtRoot + "nodes/map9_node_target");
        nodeChargedSprite = Resources.Load<Sprite>(ArtRoot + "nodes/map9_node_charged");
        nodeOverloadSprite = Resources.Load<Sprite>(ArtRoot + "nodes/map9_node_overload");
        energyLinkSprite = Resources.Load<Sprite>(ArtRoot + "links/map9_energy_link");
        successFrames = LoadFrames(ArtRoot + "vfx/success/map9_success_", 12);
        overloadFrames = LoadFrames(ArtRoot + "vfx/overload/map9_overload_", 8);
        zombieLightningSprite = Resources.Load<Sprite>(ArtRoot + "vfx/status/map9_zombie_lightning_hit");
        zombieFrozenAuraSprite = Resources.Load<Sprite>(ArtRoot + "vfx/status/map9_zombie_frozen_aura");
        zombieSlowAuraSprite = Resources.Load<Sprite>(ArtRoot + "vfx/status/map9_zombie_slow_aura");
        plantOverloadSprite = Resources.Load<Sprite>(ArtRoot + "vfx/status/map9_plant_overload_hit");
        hudPanelSprite = Resources.Load<Sprite>(ArtRoot + "ui/map9_circuit_hud");
        hudTimelineSprite = Resources.Load<Sprite>(ArtRoot + "ui/map9_circuit_hud_timeline");
        hudWaitingSprite = Resources.Load<Sprite>(ArtRoot + "ui/map9_status_waiting");
        hudActiveSprite = Resources.Load<Sprite>(ArtRoot + "ui/map9_status_active");
        hudSuccessSprite = Resources.Load<Sprite>(ArtRoot + "ui/map9_status_success");
        hudFailureSprite = Resources.Load<Sprite>(ArtRoot + "ui/map9_status_failure");

        bool ready = nodeDormantSprite != null && nodeTargetSprite != null
            && nodeChargedSprite != null && nodeOverloadSprite != null && energyLinkSprite != null
            && HasAllFrames(successFrames, 12) && HasAllFrames(overloadFrames, 8)
            && zombieLightningSprite != null && zombieFrozenAuraSprite != null
            && zombieSlowAuraSprite != null && plantOverloadSprite != null
            && hudPanelSprite != null && hudTimelineSprite != null
            && hudWaitingSprite != null && hudActiveSprite != null
            && hudSuccessSprite != null && hudFailureSprite != null;
        if (!ready) Debug.LogError("Map 9 art bundle is incomplete or not imported as Sprite.", this);
        return ready;
    }

    private static Sprite[] LoadFrames(string prefix, int count)
    {
        var frames = new Sprite[count];
        for (int i = 0; i < count; i++)
            frames[i] = Resources.Load<Sprite>(prefix + i.ToString("00"));
        return frames;
    }

    private static bool HasAllFrames(Sprite[] frames, int expected)
    {
        if (frames == null || frames.Length != expected) return false;
        foreach (Sprite frame in frames)
            if (frame == null) return false;
        return true;
    }

    private void SpawnOneShot(Sprite[] frames, Vector3 position, Vector2 worldSize,
        Transform sortingReference, int orderOffset)
    {
        if (frames == null || frames.Length == 0) return;
        StartCoroutine(PlayOneShot(frames, position, worldSize, sortingReference, orderOffset));
    }

    private IEnumerator PlayOneShot(Sprite[] frames, Vector3 position, Vector2 worldSize,
        Transform sortingReference, int orderOffset)
    {
        var effectObject = new GameObject("Map 9 Animated Effect", typeof(SpriteRenderer));
        effectObject.transform.SetParent(transform, true);
        effectObject.transform.position = position;
        SpriteRenderer renderer = effectObject.GetComponent<SpriteRenderer>();
        ApplySorting(renderer, sortingReference, orderOffset);
        foreach (Sprite frame in frames)
        {
            if (frame != null)
            {
                renderer.sprite = frame;
                FitRenderer(renderer, worldSize);
                effectObject.transform.position = position;
            }
            yield return new WaitForSeconds(1f / EffectFramesPerSecond);
        }
        Destroy(effectObject);
    }

    private void SpawnStaticEffect(Sprite sprite, Transform target, Vector2 worldSize,
        float duration, int orderOffset)
    {
        if (sprite == null || target == null) return;
        StartCoroutine(PlayAttachedEffect(sprite, target, worldSize, duration, orderOffset));
    }

    private IEnumerator PlayAttachedEffect(Sprite sprite, Transform target, Vector2 worldSize,
        float duration, int orderOffset)
    {
        var effectObject = new GameObject("Map 9 Attached Effect", typeof(SpriteRenderer));
        effectObject.transform.SetParent(transform, true);
        SpriteRenderer renderer = effectObject.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        ApplySorting(renderer, target, orderOffset);
        FitRenderer(renderer, worldSize);
        float elapsed = 0f;
        while (elapsed < duration && target != null)
        {
            effectObject.transform.position = target.position + new Vector3(0f, .08f, 1.4f);
            elapsed += Time.deltaTime;
            float alpha = elapsed > duration * .65f
                ? Mathf.Clamp01((duration - elapsed) / (duration * .35f))
                : 1f;
            renderer.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }
        Destroy(effectObject);
    }

    private IEnumerator ShowZombieStatus(Zombie zombie)
    {
        if (zombie == null) yield break;
        var effectObject = new GameObject("Map 9 Zombie Freeze Aura", typeof(SpriteRenderer));
        effectObject.transform.SetParent(transform, true);
        SpriteRenderer renderer = effectObject.GetComponent<SpriteRenderer>();
        ApplySorting(renderer, zombie.transform, 24);
        renderer.sprite = zombieFrozenAuraSprite;
        FitRenderer(renderer, new Vector2(1.0f, 1.25f));

        float elapsed = 0f;
        const float frozenDuration = 2.5f;
        const float slowDuration = 5f;
        while (elapsed < frozenDuration + slowDuration && zombie != null && zombie.IsAlive)
        {
            effectObject.transform.position = zombie.transform.position + new Vector3(0f, .05f, 1.3f);
            if (elapsed >= frozenDuration && renderer.sprite != zombieSlowAuraSprite)
            {
                renderer.sprite = zombieSlowAuraSprite;
                FitRenderer(renderer, new Vector2(1.0f, 1.18f));
            }
            float pulse = .82f + Mathf.Sin(Time.time * 7f) * .16f;
            float fade = elapsed > frozenDuration + slowDuration - .45f
                ? Mathf.Clamp01((frozenDuration + slowDuration - elapsed) / .45f)
                : 1f;
            renderer.color = new Color(1f, 1f, 1f, pulse * fade);
            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(effectObject);
    }

    private static void FitRenderer(SpriteRenderer renderer, Vector2 worldSize)
    {
        if (renderer == null || renderer.sprite == null) return;
        Vector2 sourceSize = renderer.sprite.bounds.size;
        renderer.transform.localScale = new Vector3(
            sourceSize.x > 0f ? worldSize.x / sourceSize.x : 1f,
            sourceSize.y > 0f ? worldSize.y / sourceSize.y : 1f,
            1f);
    }

    private static void ApplySorting(SpriteRenderer effect, Transform reference, int orderOffset)
    {
        if (effect == null) return;
        SpriteRenderer referenceRenderer = reference != null
            ? reference.GetComponentInChildren<SpriteRenderer>(true)
            : null;
        if (referenceRenderer != null)
        {
            effect.sortingLayerID = referenceRenderer.sortingLayerID;
            int highestOrder = referenceRenderer.sortingOrder;
            foreach (SpriteRenderer item in reference.GetComponentsInChildren<SpriteRenderer>(true))
                if (item.sortingLayerID == referenceRenderer.sortingLayerID)
                    highestOrder = Mathf.Max(highestOrder, item.sortingOrder);
            effect.sortingOrder = highestOrder + orderOffset;
        }
        else
        {
            effect.sortingLayerName = "Default";
            effect.sortingOrder = 20 + orderOffset;
        }
    }

    private void BuildHud()
    {
        GameObject root = new GameObject("Energy Circuit HUD", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        // Đồng bộ PauseCanvas: Plant bank -> HUD mạch -> Information -> Pause.
        scaler.referenceResolution = new Vector2(800f, 600f);
        scaler.matchWidthOrHeight = .5f;

        GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(root.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(1f, 1f);
        panelRect.pivot = new Vector2(1f, 1f);
        panelRect.anchoredPosition = new Vector2(-150f, -8f);
        panelRect.sizeDelta = new Vector2(214f, 61f);
        Image panelImage = panel.GetComponent<Image>();
        panelImage.sprite = hudPanelSprite;
        panelImage.color = Color.white;
        panelImage.raycastTarget = false;

        GameObject timeline = new GameObject("Timeline", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        timeline.transform.SetParent(panel.transform, false);
        RectTransform timelineRect = timeline.GetComponent<RectTransform>();
        timelineRect.anchorMin = Vector2.zero;
        timelineRect.anchorMax = Vector2.one;
        timelineRect.offsetMin = timelineRect.offsetMax = Vector2.zero;
        timelineFill = timeline.GetComponent<Image>();
        timelineFill.sprite = hudTimelineSprite;
        timelineFill.color = Color.white;
        timelineFill.type = Image.Type.Filled;
        timelineFill.fillMethod = Image.FillMethod.Horizontal;
        timelineFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        timelineFill.fillAmount = 0f;
        timelineFill.raycastTarget = false;

        GameObject iconObject = new GameObject("Status Icon", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        iconObject.transform.SetParent(panel.transform, false);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, .5f);
        iconRect.pivot = new Vector2(.5f, .5f);
        iconRect.anchoredPosition = new Vector2(35f, 1f);
        iconRect.sizeDelta = new Vector2(30f, 30f);
        statusIcon = iconObject.GetComponent<Image>();
        statusIcon.sprite = hudWaitingSprite;
        statusIcon.preserveAspect = true;
        statusIcon.raycastTarget = false;

        titleText = CreateText("Title", panel.transform, 14, new Vector2(22f, -4f), new Vector2(150f, 22f));
        titleText.color = new Color(.35f, 1f, .95f);
        detailText = CreateText("Detail", panel.transform, 10, new Vector2(22f, -24f), new Vector2(150f, 19f));
        detailText.color = new Color(.95f, .92f, .68f);

    }

    private float CurrentTimelineProgress()
    {
        return CircuitDuration > 0f
            ? Mathf.Clamp01((circuitEndsAt - Time.time) / CircuitDuration)
            : 0f;
    }

    private void RefreshTimeline()
    {
        if (timelineFill != null && phase == CircuitPhase.Active)
            timelineFill.fillAmount = CurrentTimelineProgress();
    }

    private static Text CreateText(string name, Transform parent, int size, Vector2 position, Vector2 dimensions)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1f);
        rect.pivot = new Vector2(.5f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
        Text text = obj.GetComponent<Text>();
        text.font = Resources.Load<Font>("Fonts/Baloo2");
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        return text;
    }

    private void SetHud(string title, string detail, float progress, Sprite icon)
    {
        if (titleText != null) titleText.text = title;
        if (detailText != null) detailText.text = detail;
        if (timelineFill != null) timelineFill.fillAmount = Mathf.Clamp01(progress);
        if (statusIcon != null && icon != null) statusIcon.sprite = icon;
    }

    private void OnDestroy()
    {
        ClearCircuitVisuals();
        StopAllCoroutines();
    }
}
