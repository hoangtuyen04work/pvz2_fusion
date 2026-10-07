using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Nhập các artwork UI runtime dưới dạng Sprite.</summary>
[InitializeOnLoad]
public static class RuntimeUiAssetSetup
{
    private const string Map9ArtFolder = "Assets/Resources/Sprites/Map9_Art";

    private static readonly string[] AssetPaths =
    {
        "Assets/Resources/Prefabs/UI_LIST_PLANT_SELECTED.png",
        "Assets/Resources/Prefabs/UI_management_list_plant.png",
        "Assets/Resources/Prefabs/UI/winner.png",
        "Assets/Resources/Prefabs/UI/over.png",
        "Assets/Resources/Sprites/UI/MainMenu/menu_Main_final.png",
        "Assets/Resources/Sprites/UI/MainMenu/logo.png",
        "Assets/Resources/GameUI/cancel.png",
        "Assets/Resources/GameUI/confirm.png",
        "Assets/Resources/GameUI/dialog_main.png",
        "Assets/Resources/GameUI/dialog_child.png",
        "Assets/Resources/GameUI/button1.png",
        "Assets/Resources/GameUI/button2.png",
        "Assets/Resources/GameUI/menu_child1.png",
        "Assets/Resources/GameUI/menu_child2.png",
        "Assets/Resources/GameUI/off.png",
        "Assets/Resources/GameUI/on.png",
        "Assets/Resources/GameUI/pause.png",
        "Assets/Resources/GameUI/icon_information.png",
        "Assets/Resources/GameUI/icon_arrow_down.png",
        "Assets/Resources/GameUI/ui_notice_boxchat.png",
        "Assets/Resources/GameUI/play.png",
        "Assets/Resources/GameUI/return.png",
        "Assets/Resources/GameUI/setting.png",
        "Assets/Resources/Sprites/UI/Intro/khung.png",
        "Assets/Resources/Sprites/UI/Intro/process.png",
        "Assets/Resources/Sprites/UI/Intro/process_timeline.png",
        "Assets/Resources/Sprites/UI/Intro/zoombie_spritesheet.png"
    };

    static RuntimeUiAssetSetup()
    {
        EditorApplication.delayCall += EnsureImported;
    }

    [MenuItem("Tools/UI/Reimport Plant UI Frames")]
    private static void EnsureImported()
    {
        var paths = new HashSet<string>(AssetPaths);
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Map9ArtFolder }))
            paths.Add(AssetDatabase.GUIDToAssetPath(guid));

        foreach (string assetPath in paths)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(assetPath,
                    ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null) continue;
            }

            bool isMap9 = assetPath.StartsWith(Map9ArtFolder);
            bool isMap9Background = assetPath.EndsWith("/background/map9.png");
            bool isMap9Hud = assetPath.Contains("/ui/map9_circuit_hud");
            bool isMap9Link = assetPath.Contains("/links/");
            float pixelsPerUnit = isMap9 && !isMap9Background ? 256f : 100f;
            int maxSize = isMap9Background || isMap9Hud ? 2048 : isMap9Link ? 512 : 256;

            bool changed = importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single
                || importer.mipmapEnabled
                || !importer.alphaIsTransparency
                || importer.wrapMode != TextureWrapMode.Clamp
                || !Mathf.Approximately(importer.spritePixelsPerUnit, pixelsPerUnit);
            if (!changed) continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            if (isMap9)
            {
                importer.maxTextureSize = maxSize;
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
            }
            importer.SaveAndReimport();
        }
    }
}
