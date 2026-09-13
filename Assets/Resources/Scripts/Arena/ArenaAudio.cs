using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Cửa duy nhất để phát hiệu ứng âm thanh ngắn trong đấu trường.
/// Mọi nơi muốn kêu một tiếng đều đi qua đây, nhờ vậy nút tắt tiếng chỉ cần
/// hạ một cờ là im toàn bộ, không phải đi sửa từng chỗ gọi PlayOneShot.
/// </summary>
public static class ArenaSfx
{
    private const string PrefKey = "ArenaSfxEnabled";

    private static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
    private static bool loaded;
    private static bool enabled = true;

    /// <summary>Hiệu ứng âm thanh ngắn đang bật hay tắt. Ghi nhớ qua các lần chơi.</summary>
    public static bool Enabled
    {
        get
        {
            if (!loaded)
            {
                enabled = PlayerPrefs.GetInt(PrefKey, 1) == 1;
                loaded = true;
            }
            return enabled;
        }
        set
        {
            enabled = value;
            loaded = true;
            PlayerPrefs.SetInt(PrefKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    /// <summary>Nạp một clip từ Resources, có nhớ đệm để không đọc đĩa nhiều lần.</summary>
    public static AudioClip Clip(string resourcePath)
    {
        if (string.IsNullOrEmpty(resourcePath)) return null;
        AudioClip clip;
        if (cache.TryGetValue(resourcePath, out clip)) return clip;
        clip = Resources.Load<AudioClip>(resourcePath);
        cache[resourcePath] = clip;
        return clip;
    }

    public static void Play(AudioSource source, AudioClip clip, float volume = 1f)
    {
        if (!Enabled || source == null || clip == null) return;
        source.PlayOneShot(clip, volume);
    }

    public static void Play(AudioSource source, string resourcePath, float volume = 1f)
    {
        Play(source, Clip(resourcePath), volume);
    }
}

/// <summary>
/// Nhạc nền của đấu trường. Tách hẳn khỏi ArenaSfx vì đề bài yêu cầu
/// tắt hiệu ứng ngắn và tắt nhạc nền là hai nút riêng biệt.
/// </summary>
public sealed class ArenaMusic : MonoBehaviour
{
    private const string PrefKey = "ArenaMusicEnabled";
    private const string Track = "Sounds/Background/Music_Day";

    private AudioSource source;

    public static bool Enabled
    {
        get { return PlayerPrefs.GetInt(PrefKey, 1) == 1; }
        set
        {
            PlayerPrefs.SetInt(PrefKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    private void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.clip = Resources.Load<AudioClip>(Track);
        source.loop = true;
        source.volume = 0.38f;
        source.playOnAwake = false;
        Apply();
    }

    /// <summary>Đồng bộ trạng thái phát với cờ đang lưu.</summary>
    public void Apply()
    {
        if (source == null || source.clip == null) return;
        if (Enabled && !source.isPlaying) source.Play();
        else if (!Enabled && source.isPlaying) source.Stop();
    }

    public void SetEnabled(bool value)
    {
        Enabled = value;
        Apply();
    }
}

/// <summary>
/// Bốn đối tượng SoundOn / SoundOff / MusicOn / MusicOff trên màn hình.
/// Mỗi cặp dùng chung một ô neo nên khi đổi trạng thái, đối tượng này
/// thay đúng vị trí hiển thị của đối tượng kia, và hai đối tượng cùng kích thước.
/// </summary>
public sealed class ArenaAudioToggle : MonoBehaviour
{
    private GameObject soundOn;
    private GameObject soundOff;
    private GameObject musicOn;
    private GameObject musicOff;
    private ArenaMusic music;
    private AudioSource clickSource;

    public void Build(Transform canvas, ArenaMusic musicPlayer)
    {
        music = musicPlayer;
        clickSource = gameObject.AddComponent<AudioSource>();
        clickSource.playOnAwake = false;

        //Tên đối tượng đặt theo hành động nó làm, đúng như đề bài:
        //bấm SoundOff là tắt tiếng, bấm SoundOn là bật tiếng.
        //Icon thì vẽ theo trạng thái hiện tại để người chơi không bị rối.
        //Hai đối tượng dùng chung một ô neo nên cái này thay đúng chỗ của cái kia,
        //và vì ô neo giống nhau nên kích thước hiển thị cũng bằng nhau.
        soundOff = CreateIconButton(canvas, "SoundOff", ArenaIcons.SoundOn(),
            0.828f, 0.885f, 0.878f, 0.965f, TurnSoundOff);
        soundOn = CreateIconButton(canvas, "SoundOn", ArenaIcons.SoundOff(),
            0.828f, 0.885f, 0.878f, 0.965f, TurnSoundOn);

        //Cặp nhạc nền cũng vậy: bấm MusicOn là phát nhạc, bấm MusicOff là tắt nhạc
        musicOff = CreateIconButton(canvas, "MusicOff", ArenaIcons.MusicOn(),
            0.884f, 0.885f, 0.934f, 0.965f, TurnMusicOff);
        musicOn = CreateIconButton(canvas, "MusicOn", ArenaIcons.MusicOff(),
            0.884f, 0.885f, 0.934f, 0.965f, TurnMusicOn);

        Refresh();
    }

    private void TurnSoundOff()
    {
        ArenaSfx.Enabled = false;
        Refresh();
    }

    private void TurnSoundOn()
    {
        ArenaSfx.Enabled = true;
        Refresh();
        //Kêu một tiếng để người chơi biết tiếng đã bật lại
        ArenaSfx.Play(clickSource, "Sounds/UI/buttonClick", 0.8f);
    }

    private void TurnMusicOff()
    {
        if (music != null) music.SetEnabled(false);
        Refresh();
    }

    private void TurnMusicOn()
    {
        if (music != null) music.SetEnabled(true);
        Refresh();
    }

    private void Refresh()
    {
        //Đang bật tiếng thì hiện nút SoundOff để người chơi bấm vào mà tắt
        bool sound = ArenaSfx.Enabled;
        soundOff.SetActive(sound);
        soundOn.SetActive(!sound);

        //Đang phát nhạc thì hiện nút MusicOff
        bool tune = ArenaMusic.Enabled;
        musicOff.SetActive(tune);
        musicOn.SetActive(!tune);
    }

    private static GameObject CreateIconButton(Transform parent, string name, Sprite icon,
        float xMin, float yMin, float xMax, float yMax, UnityEngine.Events.UnityAction action)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(xMin, yMin);
        rect.anchorMax = new Vector2(xMax, yMax);
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        var image = go.GetComponent<Image>();
        image.sprite = icon;
        image.preserveAspect = true;
        image.color = Color.white;

        go.GetComponent<Button>().onClick.AddListener(action);
        go.AddComponent<ArenaPulseButton>();
        return go;
    }
}

/// <summary>
/// Bốn icon bật/tắt được vẽ thẳng bằng mã nguồn thành Texture2D.
/// Làm vậy để không phải kèm file ảnh nào, và cũng khớp với cách
/// cả đấu trường này được dựng hoàn toàn bằng code.
/// </summary>
public static class ArenaIcons
{
    private const int Size = 96;

    private static readonly Color Body = new Color(0.96f, 0.98f, 0.92f, 1f);
    private static readonly Color Plate = new Color(0.11f, 0.24f, 0.08f, 0.92f);
    private static readonly Color Off = new Color(0.86f, 0.20f, 0.13f, 1f);

    private static Sprite soundOn;
    private static Sprite soundOff;
    private static Sprite musicOn;
    private static Sprite musicOff;
    private static Sprite crate;

    public static Sprite SoundOn()
    {
        if (soundOn == null) soundOn = Build(true, false, false);
        return soundOn;
    }

    public static Sprite SoundOff()
    {
        if (soundOff == null) soundOff = Build(true, true, false);
        return soundOff;
    }

    public static Sprite MusicOn()
    {
        if (musicOn == null) musicOn = Build(false, false, true);
        return musicOn;
    }

    public static Sprite MusicOff()
    {
        if (musicOff == null) musicOff = Build(false, true, true);
        return musicOff;
    }

    /// <summary>Thùng gỗ dùng cho vật phẩm Z, cũng vẽ bằng code cho đồng bộ.</summary>
    public static Sprite Crate()
    {
        if (crate != null) return crate;

        var pixels = NewCanvas();
        var wood = new Color(0.55f, 0.36f, 0.16f, 1f);
        var edge = new Color(0.30f, 0.19f, 0.08f, 1f);
        var band = new Color(0.74f, 0.56f, 0.24f, 1f);

        FillRect(pixels, 12, 12, 84, 84, wood);
        FillRect(pixels, 12, 12, 84, 18, edge);
        FillRect(pixels, 12, 78, 84, 84, edge);
        FillRect(pixels, 12, 12, 18, 84, edge);
        FillRect(pixels, 78, 12, 84, 84, edge);
        DrawLine(pixels, 18, 18, 78, 78, band, 5);
        DrawLine(pixels, 78, 18, 18, 78, band, 5);

        crate = ToSprite(pixels);
        return crate;
    }

    private static Sprite Build(bool speaker, bool crossed, bool note)
    {
        var pixels = NewCanvas();

        //Nền tròn cho cả bốn icon để hai đối tượng trong một cặp trông cùng khuôn
        FillDisc(pixels, 48f, 48f, 45f, Plate);

        if (speaker) DrawSpeaker(pixels, !crossed);
        if (note) DrawNote(pixels);
        if (crossed) DrawLine(pixels, 24, 72, 72, 24, Off, 7);

        return ToSprite(pixels);
    }

    private static void DrawSpeaker(Color[] pixels, bool withWaves)
    {
        //Hộp loa
        FillRect(pixels, 24, 40, 36, 56, Body);

        //Loa hình loe: nửa chiều cao lớn dần theo x
        for (int x = 36; x <= 54; x++)
        {
            float t = (x - 36) / 18f;
            int half = Mathf.RoundToInt(Mathf.Lerp(8f, 24f, t));
            FillRect(pixels, x, 48 - half, x + 1, 48 + half, Body);
        }

        if (!withWaves) return;

        //Hai vòng sóng bên phải
        DrawArc(pixels, 54f, 48f, 13f, Body, 4);
        DrawArc(pixels, 54f, 48f, 22f, Body, 4);
    }

    private static void DrawNote(Color[] pixels)
    {
        FillDisc(pixels, 38f, 34f, 12f, Body);   //Bầu nốt
        FillRect(pixels, 46, 34, 52, 72, Body);  //Đuôi nốt
        FillRect(pixels, 52, 62, 70, 72, Body);  //Cờ nốt
    }

    // ---------- Bút vẽ cơ bản ----------

    private static Color[] NewCanvas()
    {
        var pixels = new Color[Size * Size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;
        return pixels;
    }

    private static void Plot(Color[] pixels, int x, int y, Color color)
    {
        if (x < 0 || y < 0 || x >= Size || y >= Size) return;
        int index = y * Size + x;
        //Trộn theo alpha để nét vẽ sau không xoá hẳn nét trước
        Color under = pixels[index];
        float a = color.a;
        pixels[index] = new Color(
            color.r * a + under.r * (1f - a),
            color.g * a + under.g * (1f - a),
            color.b * a + under.b * (1f - a),
            a + under.a * (1f - a));
    }

    private static void FillRect(Color[] pixels, int xMin, int yMin, int xMax, int yMax, Color color)
    {
        for (int y = yMin; y < yMax; y++)
            for (int x = xMin; x < xMax; x++)
                Plot(pixels, x, y, color);
    }

    private static void FillDisc(Color[] pixels, float cx, float cy, float radius, Color color)
    {
        int min = Mathf.FloorToInt(Mathf.Min(cx, cy) - radius - 1f);
        int max = Mathf.CeilToInt(Mathf.Max(cx, cy) + radius + 1f);
        for (int y = min; y <= max; y++)
        {
            for (int x = min; x <= max; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                if (dx * dx + dy * dy <= radius * radius) Plot(pixels, x, y, color);
            }
        }
    }

    private static void DrawArc(Color[] pixels, float cx, float cy, float radius, Color color, int thickness)
    {
        float half = thickness * 0.5f;
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                float dx = x - cx;
                if (dx < 2f) continue;                      //Chỉ lấy nửa cung bên phải
                float dy = y - cy;
                if (Mathf.Abs(dy) > dx * 1.15f) continue;   //Thu hẹp thành hình quạt
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                if (Mathf.Abs(distance - radius) <= half) Plot(pixels, x, y, color);
            }
        }
    }

    private static void DrawLine(Color[] pixels, int x0, int y0, int x1, int y1, Color color, int thickness)
    {
        float steps = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
        if (steps <= 0f) return;
        float radius = thickness * 0.5f;
        for (int i = 0; i <= steps; i++)
        {
            float t = i / steps;
            float cx = Mathf.Lerp(x0, x1, t);
            float cy = Mathf.Lerp(y0, y1, t);
            FillDisc(pixels, cx, cy, radius, color);
        }
    }

    private static Sprite ToSprite(Color[] pixels)
    {
        var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), 100f);
    }
}
