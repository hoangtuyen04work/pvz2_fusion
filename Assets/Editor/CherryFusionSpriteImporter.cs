using UnityEditor;

public sealed class CherryFusionSpriteImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.Contains("/Resources/Sprites/Plants/CherryFusions/") || !assetPath.EndsWith(".png")) return;
        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.spritePixelsPerUnit = 100f;
        importer.filterMode = UnityEngine.FilterMode.Bilinear;
    }
}
