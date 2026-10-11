using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class hỗ trợ nạp icon zombie chuẩn xác, đẹp mắt cho cả Zombie Almanac và TestZombieSpawner.
/// </summary>
public static class ZombieIconHelper
{
    private static readonly Dictionary<string, Sprite> IconCache = new Dictionary<string, Sprite>(System.StringComparer.OrdinalIgnoreCase);

    public static Sprite GetIcon(string zombieName)
    {
        if (string.IsNullOrEmpty(zombieName)) return null;

        if (IconCache.TryGetValue(zombieName, out Sprite cached) && cached != null)
            return cached;

        Sprite raw = LoadIconInternal(zombieName);
        if (raw != null)
        {
            Sprite normalized = TrimTransparentPaddingToFitCard(raw);
            IconCache[zombieName] = normalized ?? raw;
            return IconCache[zombieName];
        }
        return null;
    }

    private static Sprite BaseZombieWithoutFlagCache = null;

    private static Sprite GetCleanBaseZombieWithoutFlag()
    {
        if (BaseZombieWithoutFlagCache != null) return BaseZombieWithoutFlagCache;

        var flagSprites = Resources.LoadAll<Sprite>("Sprites/Imported/MarbleXu/Zombies/FlagZombie/FlagZombie");
        if (flagSprites == null || flagSprites.Length == 0) return null;
        Sprite baseSprite = flagSprites[0];

        try
        {
            Texture2D baseTex = baseSprite.texture;
            RenderTexture rt = RenderTexture.GetTemporary(baseTex.width, baseTex.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            Graphics.Blit(baseTex, rt);
            RenderTexture activeRt = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(baseTex.width, baseTex.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, baseTex.width, baseTex.height), 0, 0);
            tex.Apply();
            RenderTexture.active = activeRt;
            RenderTexture.ReleaseTemporary(rt);

            int w = tex.width;
            int h = tex.height;
            Color[] pixels = tex.GetPixels();

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int index = y * w + x;
                    Color c = pixels[index];
                    if (c.a <= 0.01f) continue;

                    // Xóa lá cờ đỏ ở góc trên bên trái (x từ 50 đến 130)
                    bool isRedFlagCloth = (c.r > 0.35f && c.r > c.g * 1.20f && c.r > c.b * 1.20f);
                    bool inFlagArea = (x >= 50 && x <= 130 && y >= (int)(h * 0.35f));

                    // Xóa cán cờ bằng gỗ nối từ lá cờ xuống tay zombie
                    bool isPoleWood = (x >= 65 && x <= 105 && y >= (int)(h * 0.40f) && y <= (int)(h * 0.85f) &&
                                       c.r > 0.15f && c.r < 0.55f && c.g < 0.35f && c.b < 0.25f);

                    if ((inFlagArea && isRedFlagCloth) || isPoleWood)
                    {
                        pixels[index] = Color.clear;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            BaseZombieWithoutFlagCache = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            return BaseZombieWithoutFlagCache;
        }
        catch
        {
            return baseSprite;
        }
    }

    private static Sprite LoadIconInternal(string zombieName)
    {
        Sprite flaglessZombieSprite = GetCleanBaseZombieWithoutFlag();

        switch (zombieName)
        {
            case "ZombieNormal":
                {
                    var sp = Resources.Load<Sprite>("Sprites/Zombies/ZombieNormal_Clean");
                    if (sp != null) return sp;
                    return flaglessZombieSprite;
                }

            case "ConeZombie":
                {
                    var sp = Resources.Load<Sprite>("Sprites/Zombies/ConeZombie_Clean");
                    if (sp != null) return sp;
                    Sprite coneItem = Resources.Load<Sprite>("Sprites/Zombies/ConeZombie/Zombie_cone1");
                    if (flaglessZombieSprite != null && coneItem != null)
                    {
                        Sprite merged = MergeItemOntoZombie(flaglessZombieSprite, coneItem, new Vector2(0.04f, 0.28f), 0.70f, Color.white);
                        if (merged != null) return merged;
                    }
                    return flaglessZombieSprite;
                }

            case "BucketZombie":
                {
                    var sp = Resources.Load<Sprite>("Sprites/Zombies/BucketZombie_Clean");
                    if (sp != null) return sp;
                    Sprite bucketItem = Resources.Load<Sprite>("Sprites/Zombies/BucketZombie/Zombie_bucket1");
                    if (flaglessZombieSprite != null && bucketItem != null)
                    {
                        Sprite merged = MergeItemOntoZombie(flaglessZombieSprite, bucketItem, new Vector2(0.04f, 0.26f), 0.68f, Color.white);
                        if (merged != null) return merged;
                    }
                    return flaglessZombieSprite;
                }

            case "SnowZombie":
                {
                    var baseNormal = Resources.Load<Sprite>("Sprites/Zombies/ZombieNormal_Clean") ?? flaglessZombieSprite;
                    if (baseNormal != null)
                    {
                        Color frostyCyan = new Color(0.55f, 0.85f, 1.0f, 1.0f);
                        Sprite iceShield = Resources.Load<Sprite>("Sprites/Zombies/SnowZombie/IceShield");
                        if (iceShield != null)
                        {
                            Sprite merged = MergeItemOntoZombie(baseNormal, iceShield, new Vector2(-0.06f, 0.05f), 0.55f, frostyCyan);
                            if (merged != null) return merged;
                        }
                        Sprite tinted = CreateTintedSprite(baseNormal, frostyCyan);
                        if (tinted != null) return tinted;
                    }
                    var snowSprite = Resources.Load<Sprite>("Sprites/Zombies/SnowZombie/SnowZombie");
                    if (snowSprite != null) return snowSprite;
                    return flaglessZombieSprite;
                }

            case "ChineseZombie":
                {
                    var sp = Resources.Load<Sprite>("Sprites/Zombies/ChineseZombie/ChineseZombieImage");
                    if (sp != null) return sp;
                    break;
                }
            case "Ghost":
                {
                    var sp = Resources.Load<Sprite>("Sprites/Zombies/Ghost/GhostImage");
                    if (sp != null) return sp;
                    break;
                }
            case "BoneZombie":
                {
                    var sp = Resources.Load<Sprite>("Sprites/Zombies/BoneZombie/BoneZombieImage");
                    if (sp != null) return sp;
                    break;
                }
            case "IceBlockZombie":
                {
                    var sp = Resources.Load<Sprite>("Sprites/Zombies/IceBlockZombie/IceBlockZombie");
                    if (sp != null) return sp;
                    break;
                }
            case "YetiZombie":
                {
                    var sp = Resources.Load<Sprite>("Sprites/Zombies/YetiZombie/YetiZombie");
                    if (sp != null) return sp;
                    break;
                }
            case "FlagZombie":
                {
                    var sprites = Resources.LoadAll<Sprite>("Sprites/Imported/MarbleXu/Zombies/FlagZombie/FlagZombie");
                    if (sprites != null && sprites.Length > 0) return sprites[0];
                    break;
                }
            case "NewspaperZombie":
                {
                    var sprites = Resources.LoadAll<Sprite>("Sprites/Imported/MarbleXu/Zombies/NewspaperZombie/NewspaperZombie");
                    if (sprites != null && sprites.Length > 0) return sprites[0];
                    break;
                }
            case "PoleVaultingZombie":
                {
                    var sprites = Resources.LoadAll<Sprite>("Sprites/Imported/JiangNan/Zombies/PoleVaultingZombie/PoleVaultingZombie");
                    if (sprites != null && sprites.Length > 0) return sprites[0];
                    sprites = Resources.LoadAll<Sprite>("Sprites/Imported/JiangNan/Zombies/PoleVaultingZombie/PoleVaultingZombieWalk");
                    if (sprites != null && sprites.Length > 0) return sprites[0];
                    break;
                }
            case "FootballZombie":
                {
                    var sprites = Resources.LoadAll<Sprite>("Sprites/Imported/JiangNan/Zombies/FootballZombie/FootballZombie");
                    if (sprites != null && sprites.Length > 0) return sprites[0];
                    break;
                }
            case "ScreenDoorZombie":
                {
                    var sprites = Resources.LoadAll<Sprite>("Sprites/Imported/JiangNan/Zombies/ScreenDoorZombie/ScreenDoorZombie");
                    if (sprites != null && sprites.Length > 0) return sprites[0];
                    sprites = Resources.LoadAll<Sprite>("Sprites/Imported/JiangNan/Zombies/ScreenDoorZombie/HeadWalk1");
                    if (sprites != null && sprites.Length > 0) return sprites[0];
                    break;
                }
            case "BalloonZombie":
                {
                    var sprites = Resources.LoadAll<Sprite>("Sprites/Imported/JiangNan/Zombies/BalloonZombie/Walk");
                    if (sprites != null && sprites.Length > 0) return sprites[0];
                    sprites = Resources.LoadAll<Sprite>("Sprites/Imported/JiangNan/Zombies/BalloonZombie/0");
                    if (sprites != null && sprites.Length > 0) return sprites[0];
                    break;
                }
            case "JackinTheBoxZombie":
                {
                    var sprites = Resources.LoadAll<Sprite>("Sprites/Imported/JiangNan/Zombies/JackinTheBoxZombie/Walk");
                    if (sprites != null && sprites.Length > 0) return sprites[0];
                    sprites = Resources.LoadAll<Sprite>("Sprites/Imported/JiangNan/Zombies/JackinTheBoxZombie/0");
                    if (sprites != null && sprites.Length > 0) return sprites[0];
                    break;
                }
            case "DancingZombie":
                {
                    var sprites = Resources.LoadAll<Sprite>("Sprites/Imported/JiangNan/Zombies/DancingZombie/DancingZombie");
                    if (sprites != null && sprites.Length > 0) return sprites[0];
                    break;
                }
            case "BackupDancer":
                {
                    var sprites = Resources.LoadAll<Sprite>("Sprites/Imported/JiangNan/Zombies/BackupDancer/BackupDancer");
                    if (sprites != null && sprites.Length > 0) return sprites[0];
                    sprites = Resources.LoadAll<Sprite>("Sprites/Imported/JiangNan/Zombies/BackupDancer/Dancing");
                    if (sprites != null && sprites.Length > 0) return sprites[0];
                    break;
                }
            case "DolphinRiderZombie":
                {
                    var sprites = Resources.LoadAll<Sprite>("Sprites/Imported/JiangNan/Zombies/DolphinRiderZombie/Walk1");
                    if (sprites != null && sprites.Length > 0) return sprites[0];
                    sprites = Resources.LoadAll<Sprite>("Sprites/Imported/JiangNan/Zombies/DolphinRiderZombie/0");
                    if (sprites != null && sprites.Length > 0) return sprites[0];
                    break;
                }
            case "SnorkelZombie":
                {
                    var sprites = Resources.LoadAll<Sprite>("Sprites/Imported/JiangNan/Zombies/SnorkelZombie/Walk1");
                    if (sprites != null && sprites.Length > 0) return sprites[0];
                    sprites = Resources.LoadAll<Sprite>("Sprites/Imported/JiangNan/Zombies/SnorkelZombie/0");
                    if (sprites != null && sprites.Length > 0) return sprites[0];
                    break;
                }
            case "Zomboni":
                {
                    var sprites = Resources.LoadAll<Sprite>("Sprites/Imported/JiangNan/Zombies/Zomboni/0");
                    if (sprites != null && sprites.Length > 0) return sprites[0];
                    sprites = Resources.LoadAll<Sprite>("Sprites/Imported/JiangNan/Zombies/Zomboni/1");
                    if (sprites != null && sprites.Length > 0) return sprites[0];
                    break;
                }
            case "Imp":
                {
                    var sprites = Resources.LoadAll<Sprite>("Sprites/Imported/JiangNan/Zombies/Imp/Zombie");
                    if (sprites != null && sprites.Length > 0) return sprites[0];
                    sprites = Resources.LoadAll<Sprite>("Sprites/Imported/JiangNan/Zombies/Imp/0");
                    if (sprites != null && sprites.Length > 0) return sprites[0];
                    break;
                }
            case "GatlingZombie":
                {
                    var sp = Resources.Load<Sprite>("Sprites/Zombies/GatlingZombie/GatlingZombie");
                    if (sp != null) return sp;
                    var sprites = Resources.LoadAll<Sprite>("Sprites/Zombies/GatlingZombie/Walk");
                    if (sprites != null && sprites.Length > 0) return sprites[0];
                    break;
                }
            case "ConeBucketZombie":
                {
                    var sp = Resources.Load<Sprite>("Sprites/Zombies/ConeBucketZombie/ConeBucketZombie");
                    if (sp != null) return sp;
                    var sprites = Resources.LoadAll<Sprite>("Sprites/Zombies/ConeBucketZombie/Walk");
                    if (sprites != null && sprites.Length > 0) return sprites[0];
                    break;
                }
            case "FireImpZombie":
                {
                    var impSprites = Resources.LoadAll<Sprite>("Sprites/Imported/JiangNan/Zombies/Imp/Zombie");
                    Sprite baseImp = (impSprites != null && impSprites.Length > 0) ? impSprites[0] : flaglessZombieSprite;
                    return CreateTintedSprite(baseImp, new Color(1f, 0.45f, 0.25f, 1f));
                }
        }

        // Quy trình Fallback chung
        var dirSprites = Resources.LoadAll<Sprite>("Sprites/Zombies/" + zombieName);
        if (dirSprites != null && dirSprites.Length > 0) return dirSprites[0];

        dirSprites = Resources.LoadAll<Sprite>("Sprites/Imported/JiangNan/Zombies/" + zombieName + "/" + zombieName);
        if (dirSprites != null && dirSprites.Length > 0) return dirSprites[0];

        dirSprites = Resources.LoadAll<Sprite>("Sprites/Imported/JiangNan/Zombies/" + zombieName + "/Walk");
        if (dirSprites != null && dirSprites.Length > 0) return dirSprites[0];

        dirSprites = Resources.LoadAll<Sprite>("Sprites/Imported/JiangNan/Zombies/" + zombieName + "/Walk1");
        if (dirSprites != null && dirSprites.Length > 0) return dirSprites[0];

        dirSprites = Resources.LoadAll<Sprite>("Sprites/Imported/JiangNan/Zombies/" + zombieName + "/Zombie");
        if (dirSprites != null && dirSprites.Length > 0) return dirSprites[0];

        dirSprites = Resources.LoadAll<Sprite>("Sprites/Imported/JiangNan/Zombies/" + zombieName + "/0");
        if (dirSprites != null && dirSprites.Length > 0) return dirSprites[0];

        dirSprites = Resources.LoadAll<Sprite>("Sprites/Imported/MarbleXu/Zombies/" + zombieName + "/" + zombieName);
        if (dirSprites != null && dirSprites.Length > 0) return dirSprites[0];

        return flaglessZombieSprite;
    }

    private static Sprite CreateTintedSprite(Sprite baseSprite, Color tint)
    {
        try
        {
            Texture2D baseTex = baseSprite.texture;
            RenderTexture rt = RenderTexture.GetTemporary(baseTex.width, baseTex.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            Graphics.Blit(baseTex, rt);
            RenderTexture activeRt = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D resultTex = new Texture2D(baseTex.width, baseTex.height, TextureFormat.RGBA32, false);
            resultTex.ReadPixels(new Rect(0, 0, baseTex.width, baseTex.height), 0, 0);
            resultTex.Apply();
            RenderTexture.active = activeRt;
            RenderTexture.ReleaseTemporary(rt);

            Color[] pixels = resultTex.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a > 0.01f)
                {
                    pixels[i].r *= tint.r;
                    pixels[i].g *= tint.g;
                    pixels[i].b *= tint.b;
                }
            }
            resultTex.SetPixels(pixels);
            resultTex.Apply();

            return Sprite.Create(resultTex, new Rect(0, 0, resultTex.width, resultTex.height), new Vector2(0.5f, 0.5f), 100f);
        }
        catch
        {
            return baseSprite;
        }
    }

    private static Sprite MergeItemOntoZombie(Sprite baseSprite, Sprite itemSprite, Vector2 normalizedOffset, float scaleFactor, Color baseTint)
    {
        try
        {
            Texture2D baseTex = baseSprite.texture;
            Texture2D itemTex = itemSprite.texture;

            RenderTexture rt1 = RenderTexture.GetTemporary(baseTex.width, baseTex.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            Graphics.Blit(baseTex, rt1);
            RenderTexture activeRt = RenderTexture.active;
            RenderTexture.active = rt1;
            Texture2D resultTex = new Texture2D(baseTex.width, baseTex.height, TextureFormat.RGBA32, false);
            resultTex.ReadPixels(new Rect(0, 0, baseTex.width, baseTex.height), 0, 0);
            resultTex.Apply();

            RenderTexture rt2 = RenderTexture.GetTemporary(itemTex.width, itemTex.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            Graphics.Blit(itemTex, rt2);
            RenderTexture.active = rt2;
            Texture2D readableItem = new Texture2D(itemTex.width, itemTex.height, TextureFormat.RGBA32, false);
            readableItem.ReadPixels(new Rect(0, 0, itemTex.width, itemTex.height), 0, 0);
            readableItem.Apply();
            RenderTexture.active = activeRt;
            RenderTexture.ReleaseTemporary(rt1);
            RenderTexture.ReleaseTemporary(rt2);

            int w = resultTex.width;
            int h = resultTex.height;

            if (baseTint != Color.white)
            {
                Color[] pixels = resultTex.GetPixels();
                for (int i = 0; i < pixels.Length; i++)
                {
                    if (pixels[i].a > 0.01f)
                    {
                        pixels[i].r *= baseTint.r;
                        pixels[i].g *= baseTint.g;
                        pixels[i].b *= baseTint.b;
                    }
                }
                resultTex.SetPixels(pixels);
            }

            int itemW = Mathf.RoundToInt(readableItem.width * scaleFactor);
            int itemH = Mathf.RoundToInt(readableItem.height * scaleFactor);

            int startX = Mathf.Clamp(Mathf.RoundToInt(w * 0.5f - itemW * 0.5f + normalizedOffset.x * w), 0, w - 1);
            int startY = Mathf.Clamp(Mathf.RoundToInt(h * 0.65f + normalizedOffset.y * h), 0, h - 1);

            for (int iy = 0; iy < itemH; iy++)
            {
                for (int ix = 0; ix < itemW; ix++)
                {
                    int sampleX = Mathf.Clamp(Mathf.FloorToInt(ix / scaleFactor), 0, readableItem.width - 1);
                    int sampleY = Mathf.Clamp(Mathf.FloorToInt(iy / scaleFactor), 0, readableItem.height - 1);

                    Color itemColor = readableItem.GetPixel(sampleX, sampleY);
                    if (itemColor.a <= 0.01f) continue;

                    int targetX = startX + ix;
                    int targetY = startY + iy;
                    if (targetX < 0 || targetX >= w || targetY < 0 || targetY >= h) continue;

                    Color bg = resultTex.GetPixel(targetX, targetY);
                    float outA = itemColor.a + bg.a * (1f - itemColor.a);
                    Color blended = (itemColor * itemColor.a + bg * bg.a * (1f - itemColor.a)) / Mathf.Max(0.001f, outA);
                    blended.a = outA;
                    resultTex.SetPixel(targetX, targetY, blended);
                }
            }

            resultTex.Apply();
            Object.DestroyImmediate(readableItem);

            return Sprite.Create(resultTex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }
        catch
        {
            return baseSprite;
        }
    }

    /// <summary>
    /// Cắt bớt phần viền trong suốt thừa thãi (padding) của sprite nguồn,
    /// giúp hiển thị icon zombie trên thẻ to rõ, đồng đều và cân đối như thẻ thực tế.
    /// </summary>
    private static Sprite TrimTransparentPaddingToFitCard(Sprite sprite)
    {
        if (sprite == null) return null;

        try
        {
            Texture2D srcTex = sprite.texture;
            Rect spriteRect = sprite.rect;
            int xOffset = Mathf.RoundToInt(spriteRect.x);
            int yOffset = Mathf.RoundToInt(spriteRect.y);
            int origW = Mathf.RoundToInt(spriteRect.width);
            int origH = Mathf.RoundToInt(spriteRect.height);

            if (origW <= 4 || origH <= 4) return sprite;

            // Đọc pixel an toàn qua RenderTexture
            RenderTexture rt = RenderTexture.GetTemporary(srcTex.width, srcTex.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            Graphics.Blit(srcTex, rt);
            RenderTexture activeRt = RenderTexture.active;
            RenderTexture.active = rt;

            Texture2D readable = new Texture2D(origW, origH, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(xOffset, yOffset, origW, origH), 0, 0);
            readable.Apply();

            RenderTexture.active = activeRt;
            RenderTexture.ReleaseTemporary(rt);

            Color[] pixels = readable.GetPixels();
            int minX = origW, maxX = -1, minY = origH, maxY = -1;

            for (int y = 0; y < origH; y++)
            {
                int rowOffset = y * origW;
                for (int x = 0; x < origW; x++)
                {
                    if (pixels[rowOffset + x].a > 0.05f)
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }

            if (minX > maxX || minY > maxY)
            {
                Object.DestroyImmediate(readable);
                return sprite; // Toàn bộ trong suốt
            }

            int visW = (maxX - minX) + 1;
            int visH = (maxY - minY) + 1;

            // Thêm viền đệm tự nhiên (padding 12%) để zombie không bị quá to hay sát mép thẻ
            int padX = Mathf.Max(4, Mathf.RoundToInt(visW * 0.12f));
            int padY = Mathf.Max(4, Mathf.RoundToInt(visH * 0.12f));

            int cropMinX = Mathf.Max(0, minX - padX);
            int cropMaxX = Mathf.Min(origW - 1, maxX + padX);
            int cropMinY = Mathf.Max(0, minY - padY);
            int cropMaxY = Mathf.Min(origH - 1, maxY + padY);

            int targetW = (cropMaxX - cropMinX) + 1;
            int targetH = (cropMaxY - cropMinY) + 1;

            // Cân bằng tỉ lệ khung (aspect ratio mục tiêu khoảng 0.75 - 0.85 tương đương ô thẻ UI)
            // để các zombie quá ốm hoặc quá béo không bị scale phóng đại bất thường
            float currentAspect = (float)targetW / Mathf.Max(1, targetH);
            const float targetAspect = 0.80f; // Tỉ lệ chuẩn của ô thẻ trên thanh điều khiển

            int finalW = targetW;
            int finalH = targetH;
            if (currentAspect < targetAspect)
            {
                // Hình quá ốm dọc -> mở rộng thêm bề ngang canvas
                finalW = Mathf.RoundToInt(targetH * targetAspect);
            }
            else if (currentAspect > 1.25f)
            {
                // Hình quá bè ngang -> mở rộng thêm chiều cao canvas để khi preserveAspect không bị chiếm trọn
                finalH = Mathf.RoundToInt(targetW / 1.15f);
            }

            Texture2D trimmedTex = new Texture2D(finalW, finalH, TextureFormat.RGBA32, false);
            Color[] clearFill = new Color[finalW * finalH];
            trimmedTex.SetPixels(clearFill);

            Color[] croppedPixels = readable.GetPixels(cropMinX, cropMinY, targetW, targetH);
            int pasteX = (finalW - targetW) / 2;
            int pasteY = (finalH - targetH) / 2;
            trimmedTex.SetPixels(pasteX, pasteY, targetW, targetH, croppedPixels);
            trimmedTex.Apply();

            Object.DestroyImmediate(readable);

            return Sprite.Create(trimmedTex, new Rect(0, 0, finalW, finalH), new Vector2(0.5f, 0.5f), 100f);
        }
        catch
        {
            return sprite;
        }
    }
}

