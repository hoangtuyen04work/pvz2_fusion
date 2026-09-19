using UnityEditor;
using UnityEngine;

/// <summary>
/// Keeps third-party PvZ frame assets consistent with the project's existing
/// 2D sprite import settings. This applies to imported and generated plant art.
/// </summary>
public sealed class ImportedPvZSpritePostprocessor : AssetPostprocessor
{
    private const uint ImportRevision = 6;
    private const string ImportedRoot = "Assets/Resources/Sprites/Imported/";
    private const string HybridRoot = "Assets/Resources/Sprites/Plants/Hybrids/";

    public override uint GetVersion() => ImportRevision;

    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(ImportedRoot, System.StringComparison.Ordinal) &&
            !assetPath.StartsWith(HybridRoot, System.StringComparison.Ordinal))
        {
            return;
        }

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 250f;
        importer.isReadable = false;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        // Character frames are small and frequently scaled. Block compression can
        // mix RGB from transparent pixels into the visible edge and create a white
        // rectangle/halo in game, so preserve the source RGBA data exactly.
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.compressionQuality = 100;
    }
}
