using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Component quản lý hoạt họa và tương tác cho nút Trợ lý AI:
/// - Hiệu ứng thở nhẹ / bồng bềnh (Idle Breathing / Floating) bằng Time.unscaledTime (vẫn hoạt động khi Pause).
/// - Hỗ trợ hoạt họa nhiều khung hình (Frame-by-frame Sprite Animation) như ảnh GIF.
/// - Hiệu ứng phản hồi khi rê chuột (Hover) và nhún nảy (Bounce) khi click.
/// - Phát âm thanh phản hồi vui nhộn đặc trưng của PvZ.
/// </summary>
[RequireComponent(typeof(Button))]
public class UIAnimatedAdvisor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Tham chiếu hiển thị (Kéo thả ảnh tại đây)")]
    [Tooltip("Image hiển thị avatar nhân vật (nếu để trống sẽ tự lấy Image trên GameObject này)")]
    public Image avatarImage;

    [Header("Hoạt họa Khung hình (Sprite Animation / GIF)")]
    [Tooltip("Danh sách các frame chuyển động lúc nghỉ (Idle). Nếu để trống sẽ dùng ảnh đơn kèm hiệu ứng nhún nhảy.")]
    public Sprite[] idleFrames;

    [Tooltip("Danh sách các frame chuyển động khi đang nói / trả lời")]
    public Sprite[] talkFrames;

    [Tooltip("Tốc độ chuyển frame hoạt họa (khung hình / giây)")]
    public float animationFps = 8f;

    [Header("Hiệu ứng Bồng Bềnh / Nhịp Thở (Idle Motion)")]
    [Tooltip("Bật/Tắt hiệu ứng nhịp thở bồng bềnh")]
    public bool enableBreathing = true;

    [Tooltip("Tần số nhịp thở (độ nhanh/chậm)")]
    public float breathingFrequency = 3f;

    [Tooltip("Biên độ nhịp thở (độ to nhỏ phóng đại)")]
    public float breathingScaleAmount = 0.045f;

    [Tooltip("Biên độ bồng bềnh lên xuống (pixel)")]
    public float floatingYAmount = 2.5f;

    [Header("Âm thanh phản hồi")]
    [Tooltip("Danh sách âm thanh phát khi bấm hỏi")]
    public AudioClip[] clickVoiceClips;

    // Component nội bộ
    private RectTransform rectTransform;
    private AudioSource audioSource;
    private Vector2 initialAnchoredPos;
    private Vector3 initialScale;

    private bool isHovered = false;
    private bool isPressed = false;
    private bool isThinking = false;
    private bool isSpeaking = false;

    private float frameTimer = 0f;
    private int currentFrameIndex = 0;
    private Coroutine bounceCoroutine;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        initialAnchoredPos = rectTransform.anchoredPosition;
        initialScale = rectTransform.localScale;

        if (avatarImage == null)
        {
            // Tìm con Avatar hoặc lấy ngay trên đối tượng này
            Transform avatarChild = transform.Find("Avatar_Image");
            if (avatarChild != null)
                avatarImage = avatarChild.GetComponent<Image>();
            else
                avatarImage = GetComponent<Image>();
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.ignoreListenerPause = true; // Cho phép phát tiếng cả khi Pause
        }
    }

    private void Update()
    {
        float unscaledDelta = Time.unscaledDeltaTime;

        // 1. Cập nhật Frame Hoạt Họa (Sprite Animation)
        UpdateSpriteFrames(unscaledDelta);

        // 2. Cập nhật Hiệu ứng Bồng bềnh & Nhịp thở (Chạy độc lập với Time.timeScale)
        if (enableBreathing && bounceCoroutine == null)
        {
            float time = Time.unscaledTime;
            float freq = isThinking ? breathingFrequency * 2.2f : breathingFrequency;

            float sinVal = Mathf.Sin(time * freq);
            float scaleFactor = 1f + sinVal * breathingScaleAmount;
            if (isHovered && !isPressed) scaleFactor *= 1.08f;
            if (isPressed) scaleFactor *= 0.92f;

            rectTransform.localScale = initialScale * scaleFactor;

            // Nhấp nhô trục Y nhẹ nhàng
            float yOffset = Mathf.Sin(time * freq * 0.8f) * floatingYAmount;
            rectTransform.anchoredPosition = initialAnchoredPos + new Vector2(0f, yOffset);
        }
    }

    private void UpdateSpriteFrames(float deltaTime)
    {
        if (avatarImage == null) return;

        Sprite[] activeFrames = isSpeaking ? talkFrames : idleFrames;
        if (activeFrames == null || activeFrames.Length == 0) return;

        frameTimer += deltaTime;
        float frameInterval = 1f / Mathf.Max(animationFps, 1f);

        if (frameTimer >= frameInterval)
        {
            frameTimer -= frameInterval;
            currentFrameIndex = (currentFrameIndex + 1) % activeFrames.Length;
            if (activeFrames[currentFrameIndex] != null)
            {
                avatarImage.sprite = activeFrames[currentFrameIndex];
            }
        }
    }

    /// <summary>
    /// Kích hoạt hiệu ứng phản hồi nhún nhảy nảy tưng bừng khi click
    /// </summary>
    public void PlayClickReaction()
    {
        // Phát âm thanh
        PlayRandomVoice();

        // Chạy hiệu ứng Squash & Stretch
        if (bounceCoroutine != null) StopCoroutine(bounceCoroutine);
        bounceCoroutine = StartCoroutine(DoBounceAnimation());
    }

    private IEnumerator DoBounceAnimation()
    {
        float duration = 0.28f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;

            // Đồ thị nảy (elastic bounce): co lại rồi bật to rồi về chuẩn
            float scaleY = 1f + Mathf.Sin(t * Mathf.PI * 2f) * 0.22f;
            float scaleX = 1f - Mathf.Sin(t * Mathf.PI * 2f) * 0.14f;

            rectTransform.localScale = new Vector3(initialScale.x * scaleX, initialScale.y * scaleY, initialScale.z);
            yield return null;
        }

        rectTransform.localScale = initialScale;
        rectTransform.anchoredPosition = initialAnchoredPos;
        bounceCoroutine = null;
    }

    /// <summary>
    /// Bật/Tắt trạng thái đang suy nghĩ (nhịp thở nhanh hơn tạo cảm giác tập trung)
    /// </summary>
    public void SetThinking(bool thinking)
    {
        isThinking = thinking;
        if (!thinking)
        {
            rectTransform.localScale = initialScale;
        }
    }

    /// <summary>
    /// Bật/Tắt trạng thái đang nói lời khuyên
    /// </summary>
    public void SetSpeaking(bool speaking)
    {
        isSpeaking = speaking;
        currentFrameIndex = 0;
        frameTimer = 0f;
    }

    private void PlayRandomVoice()
    {
        if (audioSource == null || clickVoiceClips == null || clickVoiceClips.Length == 0) return;

        int index = Random.Range(0, clickVoiceClips.Length);
        if (clickVoiceClips[index] != null)
        {
            audioSource.PlayOneShot(clickVoiceClips[index], 0.9f);
        }
    }

    // --- Pointer Events cho UI Interaction ---
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        isPressed = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isPressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isPressed = false;
    }

    public void SetBasePosition(Vector2 pos)
    {
        initialAnchoredPos = pos;
        if (rectTransform != null)
            rectTransform.anchoredPosition = pos;
    }
}
