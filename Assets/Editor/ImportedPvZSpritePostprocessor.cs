using UnityEditor;
using UnityEngine;

/// <summary>
/// Keeps third-party PvZ frame assets consistent with the project's existing
/// 2D sprite import settings. This only applies to the isolated Imported tree.
/// </summary>
public sealed class ImportedPvZSpritePostprocessor : AssetPostprocessor
{
    private const string ImportedRoot = "Assets/Resources/Sprites/Imported/";

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(ImportedRoot, System.StringComparison.Ordinal))
        {
            return;
        }

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 250f;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Compressed;
    }
}
