using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Phát trực tiếp animation từ các spritesheet đã cắt trong Resources/aset.
/// Dùng cache tĩnh để nhiều NPC cùng loại không tải và sắp xếp sprite lặp lại.
/// </summary>
public sealed class CampaignCharacterAnimator : MonoBehaviour
{
    private static readonly Dictionary<string, Sprite[]> ClipCache = new Dictionary<string, Sprite[]>();

    private SpriteRenderer target;
    private string pack;
    private Sprite[] idleFrames;
    private Sprite[] moveFrames;
    private Sprite[] activeFrames;
    private float activeFrameRate;
    private float elapsed;
    private bool moving;
    private bool oneShot;

    public void Initialize(SpriteRenderer renderer, string packName)
    {
        target = renderer;
        pack = packName;
        idleFrames = Load("Left - Idle");
        moveFrames = Load("Left - Walking");
        activeFrames = idleFrames;
        activeFrameRate = 12f;
        ApplyFirstFrame();
    }

    public void SetMoving(bool value)
    {
        moving = value;
        if (!oneShot) SelectLoop();
    }

    public void PlayAttack() => PlayOneShot("Left - Attacking", 18f);
    public void PlayHurt() => PlayOneShot("Left - Hurt", 18f);
    public void PlayDeath() => PlayOneShot("Dying", 14f);

    private void PlayOneShot(string clipName, float frameRate)
    {
        Sprite[] frames = Load(clipName);
        if (frames.Length == 0) return;
        activeFrames = frames;
        activeFrameRate = frameRate;
        elapsed = 0f;
        oneShot = true;
        ApplyFirstFrame();
    }

    private void SelectLoop()
    {
        Sprite[] next = moving && moveFrames.Length > 0 ? moveFrames : idleFrames;
        if (ReferenceEquals(activeFrames, next)) return;
        activeFrames = next;
        activeFrameRate = moving ? 16f : 12f;
        elapsed = 0f;
        ApplyFirstFrame();
    }

    private void Update()
    {
        if (target == null || activeFrames == null || activeFrames.Length == 0) return;
        elapsed += Time.deltaTime;
        int rawFrame = Mathf.FloorToInt(elapsed * activeFrameRate);
        if (oneShot && rawFrame >= activeFrames.Length)
        {
            oneShot = false;
            SelectLoop();
            return;
        }
        target.sprite = activeFrames[rawFrame % activeFrames.Length];
    }

    private Sprite[] Load(string clipName)
    {
        string path = "aset/" + pack + "/PNG/Spritesheets/" + clipName;
        Sprite[] cached;
        if (ClipCache.TryGetValue(path, out cached)) return cached;
        cached = Resources.LoadAll<Sprite>(path).OrderBy(sprite => sprite.name, StringComparer.Ordinal).ToArray();
        ClipCache[path] = cached;
        if (cached.Length == 0) Debug.LogWarning("[Campaign Art] Không tìm thấy sprite: Resources/" + path);
        return cached;
    }

    private void ApplyFirstFrame()
    {
        if (target != null && activeFrames != null && activeFrames.Length > 0) target.sprite = activeFrames[0];
    }
}
