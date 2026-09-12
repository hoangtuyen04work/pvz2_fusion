using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ImportedPvZValidation
{
    public static void Run()
    {
        var errors = new List<string>();
        ValidateTextures(errors);
        ValidatePlants(errors);
        ValidateZombies(errors);
        ValidateSunDisplay(errors);
        ValidateProjectiles(errors);
        ValidateGameplaySceneSunCounter(errors);

        if (errors.Count > 0)
            throw new InvalidOperationException("Imported PvZ validation failed:\n- " + string.Join("\n- ", errors));

        Debug.Log("Imported PvZ validation passed: all sprites, 21 loadout entries, 13 imported plants, and 2 imported zombies are valid.");
    }

    private static void ValidateTextures(List<string> errors)
    {
        string root = "Assets/Resources/Sprites/Imported/MarbleXu";
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { root });
        if (guids.Length != 441) errors.Add("Expected 441 imported textures, found " + guids.Length + ".");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || importer.textureType != TextureImporterType.Sprite)
                errors.Add("Texture is not imported as Sprite: " + path);
            else if (Math.Abs(importer.spritePixelsPerUnit - 250f) > 0.01f)
                errors.Add("Unexpected pixels-per-unit: " + path);
        }
    }

    private static void ValidatePlants(List<string> errors)
    {
        foreach (PlantLoadoutEntry entry in PlantLoadoutCatalog.All)
        {
            if (entry.Cost < 0 || entry.Cooldown < 0f || string.IsNullOrWhiteSpace(entry.PlantName))
                errors.Add("Invalid seed packet metadata: " + entry.Key);
            bool legacy = Resources.Load<GameObject>("Prefabs/Plants/" + entry.Key) != null ||
                          (entry.Key == "PeaShooter" && Resources.Load<GameObject>("Prefabs/Plants/PeaShooterSingle") != null);
            if (!legacy && !ImportedPlantRuntime.Supports(entry.Key))
                errors.Add("No plant implementation: " + entry.Key);
            if (Resources.Load<Sprite>(entry.IconPath) == null && ImportedPlantRuntime.Preview(entry.Key) == null)
                errors.Add("No loadout icon/preview: " + entry.Key);

            GameObject packetRoot = new GameObject("Packet Validation Root", typeof(RectTransform));
            Card packet = SeedPacketFactory.CreateGameplayCard(entry, packetRoot.transform);
            if (packet == null || packet.plantName != entry.PlantName || packet.sunNeeded != entry.Cost ||
                packet.myButton == null || packet.lowerImage == null || SeedPacketFactory.LoadIcon(entry) == null)
                errors.Add("Invalid unified seed packet: " + entry.Key);
            else if (packet.transform.Find("Cost Background/Sun Cost") == null || packet.lowerImage.rectTransform.anchorMin.y < 0.21f)
                errors.Add("Sun cost is missing or covered by cooldown: " + entry.Key);
            else if (ImportedPlantRuntime.Supports(entry.Key) && SeedPacketFactory.LoadIcon(entry) != ImportedPlantRuntime.CardPreview(entry.Key))
                errors.Add("Imported seed packet does not use the normalized card artwork: " + entry.Key);
            UnityEngine.Object.DestroyImmediate(packetRoot);

            if (!ImportedPlantRuntime.Supports(entry.Key)) continue;
            GameObject instance = ImportedPlantRuntime.CreatePlant(entry.Key, Vector3.zero, null);
            if (instance == null || instance.GetComponent<Plant>() == null || instance.GetComponent<SpriteRenderer>()?.sprite == null || instance.GetComponent<Collider2D>() == null)
                errors.Add("Invalid imported plant factory result: " + entry.Key);
            else if (instance.GetComponent<SpriteRenderer>().bounds.size.y < (entry.Key == "Spikeweed" ? 0.42f : 0.60f))
                errors.Add("Imported plant is still visually too small: " + entry.Key);
            if (entry.Key == "RepeaterPea" && instance != null)
                ValidateShooterTargeting(instance.GetComponent<ImportedPlant>(), errors);
            if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
        }
    }

    private static void ValidateShooterTargeting(ImportedPlant shooter, List<string> errors)
    {
        GameObject zombieObject = new GameObject("Target Boundary Validation", typeof(Animator), typeof(AudioSource), typeof(BoxCollider2D));
        Zombie zombie = zombieObject.AddComponent<Zombie>();
        zombie.pos_row = 0;
        zombie.bloodVolume = 100;
        zombie.transform.position = new Vector3(6f, 0f, 0f);
        if (shooter.HasTargetInLane(0, 99f))
            errors.Add("Shooter targets a zombie before it enters the lawn.");
        zombie.transform.position = new Vector3(4f, 0f, 0f);
        if (!shooter.HasTargetInLane(0, 99f))
            errors.Add("Shooter ignores a valid zombie inside the lawn.");
        UnityEngine.Object.DestroyImmediate(zombieObject);
    }

    private static void ValidateSunDisplay(List<string> errors)
    {
        GameObject go = new GameObject("Sun Display Validation", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Text));
        GameObject foreground = new GameObject("Foreground Sun Validation", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Text));
        SunNumber counter = go.AddComponent<SunNumber>();
        counter.SetForegroundText(foreground.GetComponent<UnityEngine.UI.Text>());
        counter.Initialize(175, new List<Card>());
        UnityEngine.UI.Text text = go.GetComponent<UnityEngine.UI.Text>();
        if (!text.enabled || text.text != "175" || foreground.GetComponent<UnityEngine.UI.Text>().text != "175")
            errors.Add("Total sun display does not initialize or mirror to its foreground layer.");
        UnityEngine.Object.DestroyImmediate(go);
        UnityEngine.Object.DestroyImmediate(foreground);

        GameObject managerObject = new GameObject("Sun Counter Factory Validation");
        GameObject canvasRoot = new GameObject("Canvas Validation", typeof(RectTransform), typeof(Canvas));
        GameObject motionPanel = new GameObject("Motion Panel Validation", typeof(RectTransform));
        motionPanel.transform.SetParent(canvasRoot.transform, false);
        GameObject seedBank = new GameObject("Seed Bank Validation", typeof(RectTransform));
        seedBank.transform.SetParent(motionPanel.transform, false);
        seedBank.GetComponent<RectTransform>().anchoredPosition = new Vector2(30f, 0f);
        GameObject positionedSun = new GameObject("Sun Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Text));
        positionedSun.transform.SetParent(seedBank.transform, false);
        UIManagement manager = managerObject.AddComponent<UIManagement>();
        manager.seedBank = seedBank;
        var positioningMethod = typeof(UIManagement).GetMethod("PlaceSunCounterAboveSeedBank",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        positioningMethod?.Invoke(manager, new object[] { positionedSun });
        UnityEngine.UI.Text positionedText = positionedSun.GetComponent<UnityEngine.UI.Text>();
        RectTransform positionedRect = positionedSun.GetComponent<RectTransform>();
        if (positioningMethod == null || positionedSun.transform.parent != motionPanel.transform ||
            positionedSun.transform.GetSiblingIndex() != motionPanel.transform.childCount - 1 || positionedText.maskable ||
            positionedRect.anchoredPosition != new Vector2(64f, -62.5f))
            errors.Add("Sun Text is not positioned as the final, unmasked element of the gameplay UI panel.");
        UnityEngine.Object.DestroyImmediate(managerObject);
        UnityEngine.Object.DestroyImmediate(canvasRoot);
    }

    private static void ValidateProjectiles(List<string> errors)
    {
        GameObject firstTorchwood = new GameObject("Projectile Test Torchwood A");
        GameObject secondTorchwood = new GameObject("Projectile Test Torchwood B");

        ImportedProjectile normal = ImportedProjectile.Create("RepeaterPea", Vector3.zero, 2, 20, 0f);
        if (normal.GetComponent<SpriteRenderer>()?.sprite == null || normal.GetComponent<SpriteRenderer>().bounds.size.x < 0.30f ||
            normal.GetComponent<CircleCollider2D>() == null || normal.GetComponent<Rigidbody2D>() == null || !normal.CompareTag("Pea"))
            errors.Add("Normal imported pea is missing its visual size, physics, or Pea identity.");
        normal.PassThroughTorchwood(2, 30, firstTorchwood);
        if (!normal.IsFire || normal.Damage != 30 || normal.AppliesSlow)
            errors.Add("Normal pea does not become a valid fire pea.");

        ImportedProjectile frozen = ImportedProjectile.Create("SnowPea", Vector3.zero, 1, 20, 0f);
        frozen.PassThroughTorchwood(1, 30, firstTorchwood);
        if (frozen.IsFire || frozen.AppliesSlow)
            errors.Add("The first Torchwood must thaw a snow pea without igniting it.");
        frozen.PassThroughTorchwood(1, 30, secondTorchwood);
        if (!frozen.IsFire || frozen.Damage != 30)
            errors.Add("A thawed pea must ignite at the next Torchwood.");

        ImportedProjectile spore = ImportedProjectile.Create("ScaredyShroom", Vector3.zero, 0, 20, 0f);
        Sprite sporeSprite = spore.GetComponent<SpriteRenderer>()?.sprite;
        spore.PassThroughTorchwood(0, 30, firstTorchwood);
        if (spore.CanIgnite || spore.IsFire || sporeSprite == null || !sporeSprite.name.StartsWith("BulletMushRoom"))
            errors.Add("Mushroom spores must use spore artwork and ignore Torchwood.");

        ImportedProjectile otherRow = ImportedProjectile.Create("Threepeater", Vector3.zero, 3, 20, 0f);
        otherRow.PassThroughTorchwood(2, 30, firstTorchwood);
        if (otherRow.IsFire) errors.Add("Torchwood ignited a projectile from another row.");

        UnityEngine.Object.DestroyImmediate(normal.gameObject);
        UnityEngine.Object.DestroyImmediate(frozen.gameObject);
        UnityEngine.Object.DestroyImmediate(spore.gameObject);
        UnityEngine.Object.DestroyImmediate(otherRow.gameObject);
        UnityEngine.Object.DestroyImmediate(firstTorchwood);
        UnityEngine.Object.DestroyImmediate(secondTorchwood);
    }

    private static void ValidateGameplaySceneSunCounter(List<string> errors)
    {
        EditorSceneManager.OpenScene("Assets/Scenes/GameScene.unity", OpenSceneMode.Single);
        GameObject sunObject = GameObject.Find("Sun Text");
        UnityEngine.UI.Text sunText = sunObject != null ? sunObject.GetComponent<UnityEngine.UI.Text>() : null;
        Transform panel = GameObject.Find("MotionPanel-top")?.transform;
        if (sunObject == null || sunText == null || sunObject.GetComponent<SunNumber>() == null || panel == null ||
            sunObject.transform.parent != panel || sunObject.transform.GetSiblingIndex() != panel.childCount - 1 ||
            !sunObject.activeSelf || !sunText.enabled || string.IsNullOrWhiteSpace(sunText.text))
            errors.Add("GameScene does not serialize Sun Text as the final visible child of MotionPanel-top.");
    }

    private static void ValidateZombies(List<string> errors)
    {
        foreach (string key in new[] { "FlagZombie", "NewspaperZombie" })
        {
            GameObject instance = ImportedZombieRuntime.Create(key, Vector3.zero, null);
            if (instance == null || instance.GetComponent<Zombie>() == null || instance.GetComponent<SpriteRenderer>()?.sprite == null || instance.GetComponent<Rigidbody2D>() == null)
                errors.Add("Invalid imported zombie factory result: " + key);
            if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
        }
    }
}
