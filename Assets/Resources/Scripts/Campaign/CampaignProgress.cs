using System;
using UnityEngine;

/// <summary>Dữ liệu bền vững của Chiến dịch Ba Cõi.</summary>
public static class CampaignProgress
{
    private const string Prefix = "ThreeWorlds.";

    public static int Checkpoint => Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "Checkpoint", 0), 0, 2);
    public static int HighestMap => Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "HighestMap", 0), 0, 3);
    public static int BestScore => PlayerPrefs.GetInt(Prefix + "BestScore", 0);
    public static int CheckpointScore => Mathf.Max(0, PlayerPrefs.GetInt(Prefix + "CheckpointScore", 0));
    public static int Wins => PlayerPrefs.GetInt(Prefix + "Wins", 0);
    public static int Losses => PlayerPrefs.GetInt(Prefix + "Losses", 0);
    public static bool MusicEnabled => PlayerPrefs.GetInt(Prefix + "Music", 1) != 0;
    public static bool SfxEnabled => PlayerPrefs.GetInt(Prefix + "Sfx", 1) != 0;
    public static bool ShakeEnabled => PlayerPrefs.GetInt(Prefix + "Shake", 1) != 0;

    public static int MapClears(int map) => PlayerPrefs.GetInt(Prefix + "Map" + map + "Clears", 0);
    public static string MapLastClear(int map) => PlayerPrefs.GetString(Prefix + "Map" + map + "Last", "Chưa hoàn thành");

    public static void CompleteMap(int mapIndex, int score, int health)
    {
        int number = mapIndex + 1;
        PlayerPrefs.SetInt(Prefix + "Map" + number + "Clears", MapClears(number) + 1);
        PlayerPrefs.SetString(Prefix + "Map" + number + "Last", DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
        PlayerPrefs.SetInt(Prefix + "HighestMap", Mathf.Max(HighestMap, number));
        PlayerPrefs.SetInt(Prefix + "BestScore", Mathf.Max(BestScore, score));
        PlayerPrefs.SetInt(Prefix + "CheckpointScore", score);
        PlayerPrefs.SetInt(Prefix + "CheckpointHealth", Mathf.Clamp(health, 1, 100));
        PlayerPrefs.SetInt(Prefix + "Checkpoint", Mathf.Min(2, mapIndex + 1));
        PlayerPrefs.Save();
    }

    public static void RecordWin(int score)
    {
        PlayerPrefs.SetInt(Prefix + "Wins", Wins + 1);
        PlayerPrefs.SetInt(Prefix + "BestScore", Mathf.Max(BestScore, score));
        PlayerPrefs.SetInt(Prefix + "Checkpoint", 0);
        PlayerPrefs.SetInt(Prefix + "CheckpointHealth", 100);
        PlayerPrefs.SetInt(Prefix + "CheckpointScore", 0);
        PlayerPrefs.Save();
    }

    public static void RecordLoss(int score)
    {
        PlayerPrefs.SetInt(Prefix + "Losses", Losses + 1);
        PlayerPrefs.SetInt(Prefix + "BestScore", Mathf.Max(BestScore, score));
        PlayerPrefs.Save();
    }

    public static int CheckpointHealth() => Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "CheckpointHealth", 100), 25, 100);

    public static void StartNewRun()
    {
        PlayerPrefs.SetInt(Prefix + "Checkpoint", 0);
        PlayerPrefs.SetInt(Prefix + "CheckpointHealth", 100);
        PlayerPrefs.SetInt(Prefix + "CheckpointScore", 0);
        PlayerPrefs.Save();
    }

    public static void SetMusic(bool value) => SetBool("Music", value);
    public static void SetSfx(bool value) => SetBool("Sfx", value);
    public static void SetShake(bool value) => SetBool("Shake", value);

    private static void SetBool(string key, bool value)
    {
        PlayerPrefs.SetInt(Prefix + key, value ? 1 : 0);
        PlayerPrefs.Save();
    }
}
