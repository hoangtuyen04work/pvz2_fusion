using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Bảo đảm các frame Map 8 được nhập đúng sau khi chúng được sinh bên ngoài Unity.
/// Chỉ reimport asset khi cấu hình thực tế chưa đúng.
/// </summary>
[InitializeOnLoad]
public static class Map8RuntimeAssetSetup
{
    private const string RuntimeRoot = "Assets/Resources/Sprites/Map8Fx";

    static Map8RuntimeAssetSetup()
    {
        EditorApplication.delayCall += EnsureImported;
    }

    [MenuItem("Tools/Map 8/Reimport Runtime Sprites")]
    private static void EnsureImported()
    {
        if (!Directory.Exists(RuntimeRoot)) return;

        string[] files = Directory.GetFiles(RuntimeRoot, "*.png", SearchOption.AllDirectories);
        foreach (string file in files)
        {
            string assetPath = file.Replace('\\', '/');
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(assetPath,
                    ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
                importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            }

            if (importer == null)
            {
                Debug.LogError("Could not import Map 8 runtime sprite: " + assetPath);
                continue;
            }

            bool changed = importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single
                || importer.spritePixelsPerUnit != 100f
                || importer.mipmapEnabled
                || !importer.alphaIsTransparency
                || importer.filterMode != FilterMode.Bilinear
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

        AssetDatabase.SaveAssets();
    }
}
