using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Chuẩn hóa tự động toàn bộ PNG trong Resources/aset.
/// Goblin dùng grid 480x480; Attack_Player dùng grid 128x128.
/// Arrow và ba background được giữ ở Sprite Single.
/// </summary>
public sealed class AsetSpritePostprocessor : AssetPostprocessor
{
    public const string Root = "Assets/Resources/aset/";
    public const int CellSize = 480;
    public const float PixelsPerUnit = 480f;
    public const int PlayerCellSize = 128;
    private const uint Revision = 2;

    public override uint GetVersion() => Revision;

    private void OnPreprocessTexture()
    {
        if (!IsAsetPng(assetPath)) return;
        TextureImporter importer = (TextureImporter)assetImporter;
        ConfigureCommon(importer);

        if (IsSpriteSheet(assetPath) || IsPlayerAnimationSheet(assetPath))
        {
            int cellSize = IsPlayerAnimationSheet(assetPath) ? PlayerCellSize : CellSize;
            int width;
            int height;
            if (!TryReadPngSize(assetPath, out width, out height) || width % cellSize != 0 || height % cellSize != 0)
            {
                Debug.LogWarning("[Aset Pipeline] Không thể cắt grid " + cellSize + "x" + cellSize + ": " + assetPath);
                importer.spriteImportMode = SpriteImportMode.Single;
                return;
            }

            importer.spriteImportMode = SpriteImportMode.Multiple;
#pragma warning disable 0618
            importer.spritesheet = BuildGrid(assetPath, width, height, cellSize,
                IsPlayerAnimationSheet(assetPath) ? new Vector2(0.5f, 0.08f) : new Vector2(0.5f, 0.08f));
#pragma warning restore 0618
        }
        else
        {
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePivot = IsMap(assetPath) || IsPlayerProjectile(assetPath)
                ? new Vector2(0.5f, 0.5f)
                : new Vector2(0.5f, 0.08f);
        }
    }

    private static void ConfigureCommon(TextureImporter importer)
    {
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = IsPlayerAsset(importer.assetPath) ? PlayerCellSize
            : IsMap(importer.assetPath) ? 100f : PixelsPerUnit;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.isReadable = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.compressionQuality = 90;
        importer.maxTextureSize = IsMap(importer.assetPath) ? 4096
            : IsPlayerAsset(importer.assetPath) ? 2048
            : IsSpriteSheet(importer.assetPath) ? 4096 : 1024;
    }

    private static SpriteMetaData[] BuildGrid(string path, int width, int height, int cellSize, Vector2 pivot)
    {
        int columns = width / cellSize;
        int rows = height / cellSize;
        string prefix = Sanitize(Path.GetFileNameWithoutExtension(path));
        var sprites = new List<SpriteMetaData>(columns * rows);
        int index = 0;

        // PNG được đọc từ trên xuống, Rect của Unity có gốc ở góc trái dưới.
        for (int row = rows - 1; row >= 0; row--)
        for (int column = 0; column < columns; column++)
        {
            sprites.Add(new SpriteMetaData
            {
                name = prefix + "_" + index.ToString("D3"),
                rect = new Rect(column * cellSize, row * cellSize, cellSize, cellSize),
                alignment = (int)SpriteAlignment.Custom,
                pivot = pivot,
                border = Vector4.zero
            });
            index++;
        }
        return sprites.ToArray();
    }

    internal static bool IsAsetPng(string path) =>
        path.StartsWith(Root, StringComparison.OrdinalIgnoreCase) &&
        path.EndsWith(".png", StringComparison.OrdinalIgnoreCase);

    internal static bool IsSpriteSheet(string path) =>
        path.Replace('\\', '/').IndexOf("/PNG/Spritesheets/", StringComparison.OrdinalIgnoreCase) >= 0;

    internal static bool IsPlayerAsset(string path) =>
        path.Replace('\\', '/').IndexOf("/Attack_Player/", StringComparison.OrdinalIgnoreCase) >= 0;

    internal static bool IsPlayerProjectile(string path) =>
        IsPlayerAsset(path) && Path.GetFileNameWithoutExtension(path).Equals("Arrow", StringComparison.OrdinalIgnoreCase);

    internal static bool IsPlayerAnimationSheet(string path) => IsPlayerAsset(path) && !IsPlayerProjectile(path);

    internal static bool IsMap(string path) =>
        path.Replace('\\', '/').IndexOf("/aset/map/", StringComparison.OrdinalIgnoreCase) >= 0;

    internal static int GridSize(string path) => IsPlayerAnimationSheet(path) ? PlayerCellSize : CellSize;

    internal static string Sanitize(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (char character in value)
            builder.Append(char.IsLetterOrDigit(character) ? character : '_');
        return builder.ToString().Trim('_');
    }

    internal static bool TryReadPngSize(string assetPath, out int width, out int height)
    {
        width = height = 0;
        string absolute = Path.GetFullPath(assetPath);
        if (!File.Exists(absolute)) return false;
        byte[] header = new byte[24];
        using (FileStream stream = File.OpenRead(absolute))
        {
            if (stream.Read(header, 0, header.Length) != header.Length) return false;
        }
        if (header[0] != 137 || header[1] != 80 || header[2] != 78 || header[3] != 71) return false;
        width = ReadBigEndianInt(header, 16);
        height = ReadBigEndianInt(header, 20);
        return width > 0 && height > 0;
    }

    private static int ReadBigEndianInt(byte[] bytes, int offset) =>
        (bytes[offset] << 24) | (bytes[offset + 1] << 16) | (bytes[offset + 2] << 8) | bytes[offset + 3];
}

/// <summary>Các lệnh tuần tự xuất hiện trong menu Tools/Aset Pipeline.</summary>
public static class AsetSpritePipeline
{
    [MenuItem("Tools/Aset Pipeline/1. Phân tích và cấu hình Sprite")]
    public static void AnalyzeAndConfigure()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { AsetSpritePostprocessor.Root.TrimEnd('/') });
        var pngPaths = guids.Select(AssetDatabase.GUIDToAssetPath)
            .Where(AsetSpritePostprocessor.IsAsetPng).OrderBy(path => path).ToArray();
        int sheets = 0;
        int singles = 0;
        int frames = 0;
        var invalid = new List<string>();

        try
        {
            AssetDatabase.StartAssetEditing();
            for (int i = 0; i < pngPaths.Length; i++)
            {
                string path = pngPaths[i];
                if (AsetSpritePostprocessor.IsSpriteSheet(path) || AsetSpritePostprocessor.IsPlayerAnimationSheet(path))
                {
                    int cellSize = AsetSpritePostprocessor.GridSize(path);
                    int width;
                    int height;
                    if (AsetSpritePostprocessor.TryReadPngSize(path, out width, out height) &&
                        width % cellSize == 0 && height % cellSize == 0)
                    {
                        sheets++;
                        frames += width / cellSize * (height / cellSize);
                    }
                    else invalid.Add(path);
                }
                else singles++;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh();
        }

        ValidatePlayerAndMaps(invalid);
        WriteReport(pngPaths.Length, sheets, singles, frames, invalid);
        Debug.Log($"[Aset Pipeline] Hoàn tất: {pngPaths.Length} PNG, {sheets} sheet/{frames} frame, {singles} ảnh Single, {invalid.Count} lỗi.");
    }

    [MenuItem("Tools/Aset Pipeline/1. Phân tích và cấu hình Sprite", true)]
    private static bool ValidateAnalyze() => AssetDatabase.IsValidFolder(AsetSpritePostprocessor.Root.TrimEnd('/'));

    private static void ValidatePlayerAndMaps(ICollection<string> invalid)
    {
        (string clip, int frames)[] expected =
        {
            ("Idle", 9), ("Walk", 8), ("Run", 8),
            ("Attack_1", 5), ("Attack_2", 5), ("Attack_3", 6),
            ("Shot", 14), ("Hurt", 3), ("Dead", 5), ("Jump", 9)
        };
        foreach ((string clip, int frameCount) in expected)
        {
            string path = AsetSpritePostprocessor.Root + "Attack_Player/" + clip + ".png";
            int actual = AssetDatabase.LoadAllAssetRepresentationsAtPath(path).OfType<Sprite>().Count();
            if (actual != frameCount) invalid.Add(path + " (cần " + frameCount + " frame, nhận " + actual + ")");
        }

        string arrow = AsetSpritePostprocessor.Root + "Attack_Player/Arrow.png";
        if (AssetDatabase.LoadAssetAtPath<Sprite>(arrow) == null) invalid.Add(arrow + " (thiếu projectile)");
        for (int map = 1; map <= 3; map++)
        {
            string path = AsetSpritePostprocessor.Root + "map/map" + map + ".png";
            if (AssetDatabase.LoadAssetAtPath<Sprite>(path) == null) invalid.Add(path + " (background không hợp lệ)");
        }
    }

    private static void WriteReport(int pngCount, int sheets, int singles, int frames, IList<string> invalid)
    {
        const string folder = "Assets/Generated/AsetEntities";
        EnsureFolder(folder);
        var report = new StringBuilder();
        report.AppendLine("BÁO CÁO ASET SPRITE PIPELINE");
        report.AppendLine("Sinh lúc: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        report.AppendLine("PNG: " + pngCount);
        report.AppendLine("Spritesheet: " + sheets);
        report.AppendLine("Frame đã cắt: " + frames);
        report.AppendLine("Ảnh Single: " + singles);
        report.AppendLine("Goblin: grid 480x480, PPU 480");
        report.AppendLine("Attack_Player: grid 128x128, PPU 128; Arrow là Sprite Single");
        report.AppendLine("Phân loại: Idle | Walk/Run | Attack_1..3 | Shot | Hurt/Dead/Jump | Arrow(projectile)");
        report.AppendLine("Map: Sprite Single, PPU 100, max size 4096");
        if (invalid.Count > 0)
        {
            report.AppendLine("\nFILE KHÔNG HỢP LỆ:");
            foreach (string path in invalid) report.AppendLine("- " + path);
        }
        File.WriteAllText(Path.Combine(folder, "AsetImportReport.txt"), report.ToString(), Encoding.UTF8);
        AssetDatabase.ImportAsset(folder + "/AsetImportReport.txt");
    }

    internal static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
