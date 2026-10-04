using UnityEditor;
using UnityEngine;

/// <summary>Nhập các artwork UI runtime dưới dạng Sprite.</summary>
[InitializeOnLoad]
public static class RuntimeUiAssetSetup
{
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
        foreach (string assetPath in AssetPaths)
        {
            AssetDatabase.ImportAsset(assetPath,
                ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) continue;

            bool changed = importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single
                || importer.mipmapEnabled
                || !importer.alphaIsTransparency
                || importer.wrapMode != TextureWrapMode.Clamp;
            if (!changed) continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }
    }
}
