using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Phát animation của Player từ các sheet 128x128 trong Resources/aset/Attack_Player.
/// Idle/Walk/Run là loop; Shot, Attack và Hurt được ưu tiên phát hết một lần.
/// </summary>
public sealed class CampaignPlayerAnimator : MonoBehaviour
{
    private const string Root = "aset/Attack_Player/";
    private static readonly Dictionary<string, Sprite[]> ClipCache = new Dictionary<string, Sprite[]>();

    private SpriteRenderer target;
    private Sprite[] idleFrames;
    private Sprite[] walkFrames;
    private Sprite[] runFrames;
    private Sprite[] activeFrames;
    private float frameRate;
    private float elapsed;
    private float movementAmount;
    private bool oneShot;

    public void Initialize(SpriteRenderer renderer)
    {
        target = renderer;
        idleFrames = Load("Idle");
        walkFrames = Load("Walk");
        runFrames = Load("Run");
        activeFrames = idleFrames;
        frameRate = 10f;
        ApplyFirstFrame();
    }

    public void SetMovement(float amount)
    {
        movementAmount = Mathf.Clamp01(amount);
        if (!oneShot) SelectMovementLoop();
    }

    public void PlayShot() => PlayOneShot("Shot", CampaignCombatTuning.RangedAnimationFps);
    public void PlayMelee(int comboIndex) => PlayOneShot("Attack_" + Mathf.Clamp(comboIndex, 1, 3), CampaignCombatTuning.MeleeAnimationFps);
    public void PlayHurt() => PlayOneShot("Hurt", 14f);
    public void PlayDeath() => PlayOneShot("Dead", 12f);

    private void PlayOneShot(string clip, float fps)
    {
        Sprite[] frames = Load(clip);
        if (frames.Length == 0) return;
        activeFrames = frames;
        frameRate = fps;
        elapsed = 0f;
        oneShot = true;
        ApplyFirstFrame();
    }

    private void SelectMovementLoop()
    {
        Sprite[] next;
        float nextRate;
        if (movementAmount > 0.72f && runFrames.Length > 0)
        {
            next = runFrames;
            nextRate = 14f;
        }
        else if (movementAmount > 0.04f && walkFrames.Length > 0)
        {
            next = walkFrames;
            nextRate = 10f;
        }
        else
        {
            next = idleFrames;
            nextRate = 10f;
        }

        if (ReferenceEquals(activeFrames, next)) return;
        activeFrames = next;
        frameRate = nextRate;
        elapsed = 0f;
        ApplyFirstFrame();
    }

    private void Update()
    {
        if (target == null || activeFrames == null || activeFrames.Length == 0) return;
        elapsed += Time.deltaTime;
        int frame = Mathf.FloorToInt(elapsed * frameRate);
        if (oneShot && frame >= activeFrames.Length)
        {
            oneShot = false;
            SelectMovementLoop();
            return;
        }
        target.sprite = activeFrames[frame % activeFrames.Length];
    }

    private static Sprite[] Load(string clip)
    {
        string path = Root + clip;
        Sprite[] frames;
        if (ClipCache.TryGetValue(path, out frames)) return frames;
        frames = Resources.LoadAll<Sprite>(path).OrderBy(sprite => sprite.name, StringComparer.Ordinal).ToArray();
        ClipCache[path] = frames;
        if (frames.Length == 0) Debug.LogWarning("[Player Animation] Không tìm thấy clip: Resources/" + path);
        return frames;
    }

    private void ApplyFirstFrame()
    {
        if (target != null && activeFrames != null && activeFrames.Length > 0)
            target.sprite = activeFrames[0];
    }
}
