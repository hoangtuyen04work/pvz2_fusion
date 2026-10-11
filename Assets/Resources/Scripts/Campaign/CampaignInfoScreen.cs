using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Dựng ba scene điều hướng độc lập: Cài đặt, Tiến trình và Thành tích.</summary>
public sealed class CampaignInfoScreen : MonoBehaviour
{
    private Transform panel;
    private Text musicValue;
    private Text sfxValue;
    private Text shakeValue;

    private void Awake()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        BuildBackground();
        BuildCommonUi();

        string scene = SceneManager.GetActiveScene().name;
        if (scene == CampaignBootstrap.SettingsScene) BuildSettings();
        else if (scene == CampaignBootstrap.ProgressScene) BuildProgress();
        else BuildAchievements();
    }

    private void BuildBackground()
    {
        var cameraObject = new GameObject("Information Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.035f, 0.08f, 0.03f);
    }

    private void BuildCommonUi()
    {
        CampaignUI.EnsureEventSystem();
        Canvas canvas = CampaignUI.Canvas("Campaign Screen Canvas", 1000);
        Image shade = CampaignUI.Image("Background", canvas.transform, new Color(0.025f, 0.07f, 0.03f, 1f));
        CampaignUI.Stretch(shade.rectTransform);
        Image card = CampaignUI.Image("Content Card", shade.transform, new Color(0.08f, 0.16f, 0.055f, 1f));
        CampaignUI.Anchor(card.rectTransform, 0.17f, 0.08f, 0.83f, 0.92f);
        panel = card.transform;

        Button campaign = CampaignUI.Button("Campaign", card.transform, "TIẾP TỤC CHIẾN DỊCH", new Color(0.45f, 0.7f, 0.14f),
            () => SceneManager.LoadScene(CampaignBootstrap.GameScene));
        CampaignUI.Anchor(campaign.GetComponent<RectTransform>(), 0.07f, 0.04f, 0.39f, 0.14f);
        Button home = CampaignUI.Button("Home", card.transform, "TRANG CHỦ", new Color(0.28f, 0.4f, 0.18f),
            () => SceneManager.LoadScene("MainMenu"));
        CampaignUI.Anchor(home.GetComponent<RectTransform>(), 0.61f, 0.04f, 0.93f, 0.14f);

        Button progress = CampaignUI.Button("Progress Nav", card.transform, "TIẾN TRÌNH", new Color(0.22f, 0.43f, 0.54f),
            () => SceneManager.LoadScene(CampaignBootstrap.ProgressScene));
        CampaignUI.Anchor(progress.GetComponent<RectTransform>(), 0.40f, 0.04f, 0.60f, 0.14f);
    }

    private void BuildSettings()
    {
        AddTitle("CÀI ĐẶT CHIẾN DỊCH", "Các thay đổi được lưu ngay trên thiết bị");
        musicValue = AddSettingRow("Nhạc nền", 0.62f, ToggleMusic);
        sfxValue = AddSettingRow("Hiệu ứng âm thanh", 0.47f, ToggleSfx);
        shakeValue = AddSettingRow("Rung màn hình", 0.32f, ToggleShake);
        RefreshSettings();
    }

    private void BuildProgress()
    {
        AddTitle("LỊCH SỬ TIẾN TRÌNH", "Checkpoint hiện tại: Map " + (CampaignProgress.Checkpoint + 1));
        string[] names = { "Đồng Cỏ Bình Minh", "Sông Băng", "Nghĩa Địa Trăng" };
        for (int i = 0; i < 3; i++)
        {
            int map = i + 1;
            Image row = CampaignUI.Image("Map " + map, panel, i < CampaignProgress.HighestMap
                ? new Color(0.16f, 0.34f, 0.11f, 1f) : new Color(0.13f, 0.16f, 0.12f, 1f));
            float top = 0.70f - i * 0.17f;
            CampaignUI.Anchor(row.rectTransform, 0.08f, top - 0.12f, 0.92f, top);
            Text value = CampaignUI.Text("Value", row.transform,
                "MAP " + map + " — " + names[i] + "\nHoàn thành: " + CampaignProgress.MapClears(map) + " lần  •  Gần nhất: " + CampaignProgress.MapLastClear(map),
                24, TextAnchor.MiddleLeft, Color.white);
            CampaignUI.Anchor(value.rectTransform, 0.04f, 0.05f, 0.96f, 0.95f);
        }
        Text note = CampaignUI.Text("Save Note", panel, "Tiến trình được lưu sau mỗi map hoàn thành.", 21,
            TextAnchor.MiddleCenter, new Color(0.75f, 1f, 0.42f));
        CampaignUI.Anchor(note.rectTransform, 0.08f, 0.17f, 0.92f, 0.24f);
    }

    private void BuildAchievements()
    {
        AddTitle("THÀNH TÍCH CAO", "Điểm cao nhất: " + CampaignProgress.BestScore.ToString("N0")
            + "  •  Thắng: " + CampaignProgress.Wins + "  •  Thua: " + CampaignProgress.Losses);
        AddBadge("NHÀ THÁM HIỂM", "Hoàn thành Map 1", CampaignProgress.HighestMap >= 1, 0.62f);
        AddBadge("PHÁ BĂNG", "Hoàn thành Map 2", CampaignProgress.HighestMap >= 2, 0.45f);
        AddBadge("VỆ BINH BA CÕI", "Chiến thắng toàn bộ chiến dịch", CampaignProgress.Wins >= 1, 0.28f);
    }

    private void AddTitle(string titleValue, string subtitleValue)
    {
        Text title = CampaignUI.Text("Title", panel, titleValue, 48, TextAnchor.MiddleCenter, new Color(0.66f, 1f, 0.28f));
        CampaignUI.Anchor(title.rectTransform, 0.05f, 0.82f, 0.95f, 0.95f);
        Text subtitle = CampaignUI.Text("Subtitle", panel, subtitleValue, 24, TextAnchor.MiddleCenter, Color.white);
        CampaignUI.Anchor(subtitle.rectTransform, 0.06f, 0.73f, 0.94f, 0.82f);
    }

    private Text AddSettingRow(string label, float y, UnityEngine.Events.UnityAction action)
    {
        Image row = CampaignUI.Image(label, panel, new Color(0.13f, 0.24f, 0.10f, 1f));
        CampaignUI.Anchor(row.rectTransform, 0.12f, y, 0.88f, y + 0.11f);
        Text name = CampaignUI.Text("Name", row.transform, label, 26, TextAnchor.MiddleLeft, Color.white);
        CampaignUI.Anchor(name.rectTransform, 0.05f, 0.08f, 0.55f, 0.92f);
        Button toggle = CampaignUI.Button("Toggle", row.transform, string.Empty, new Color(0.25f, 0.52f, 0.15f), action);
        CampaignUI.Anchor(toggle.GetComponent<RectTransform>(), 0.66f, 0.16f, 0.94f, 0.84f);
        return toggle.GetComponentInChildren<Text>();
    }

    private void AddBadge(string title, string condition, bool unlocked, float y)
    {
        Image row = CampaignUI.Image(title, panel, unlocked
            ? new Color(0.22f, 0.42f, 0.10f, 1f) : new Color(0.13f, 0.14f, 0.12f, 1f));
        CampaignUI.Anchor(row.rectTransform, 0.10f, y, 0.90f, y + 0.12f);
        Text icon = CampaignUI.Text("Icon", row.transform, unlocked ? "★" : "🔒", 38, TextAnchor.MiddleCenter,
            unlocked ? new Color(1f, 0.84f, 0.22f) : new Color(0.55f, 0.55f, 0.55f));
        CampaignUI.Anchor(icon.rectTransform, 0.02f, 0.05f, 0.15f, 0.95f);
        Text text = CampaignUI.Text("Text", row.transform, title + "\n" + condition, 25, TextAnchor.MiddleLeft,
            unlocked ? Color.white : new Color(0.6f, 0.6f, 0.6f));
        CampaignUI.Anchor(text.rectTransform, 0.16f, 0.06f, 0.96f, 0.94f);
    }

    private void ToggleMusic() { CampaignProgress.SetMusic(!CampaignProgress.MusicEnabled); RefreshSettings(); }
    private void ToggleSfx() { CampaignProgress.SetSfx(!CampaignProgress.SfxEnabled); RefreshSettings(); }
    private void ToggleShake() { CampaignProgress.SetShake(!CampaignProgress.ShakeEnabled); RefreshSettings(); }

    private void RefreshSettings()
    {
        if (musicValue != null) musicValue.text = CampaignProgress.MusicEnabled ? "BẬT" : "TẮT";
        if (sfxValue != null) sfxValue.text = CampaignProgress.SfxEnabled ? "BẬT" : "TẮT";
        if (shakeValue != null) shakeValue.text = CampaignProgress.ShakeEnabled ? "BẬT" : "TẮT";
    }
}
