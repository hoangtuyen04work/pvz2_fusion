using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public sealed class EndlessScoreRecord
{
    public string playerName;
    public int score;
    public int wave;
    public int kills;
    public float durationSeconds;
    public int seed;
    public string playedAtUtc;
    public string gameVersion;
}

[Serializable]
internal sealed class EndlessScoreFile
{
    public int schemaVersion = 1;
    public List<EndlessScoreRecord> records = new List<EndlessScoreRecord>();
}

/// <summary>Lưu top 10 cục bộ trong persistentDataPath, tách hoàn toàn khỏi điểm Arena.</summary>
public static class EndlessLeaderboard
{
    private const int MaximumRecords = 10;
    private const string FileName = "endless_leaderboard.json";

    public static List<EndlessScoreRecord> Load()
    {
        string path = Path.Combine(Application.persistentDataPath, FileName);
        try
        {
            if (!File.Exists(path)) return new List<EndlessScoreRecord>();
            EndlessScoreFile data = JsonUtility.FromJson<EndlessScoreFile>(File.ReadAllText(path));
            if (data == null || data.records == null) return new List<EndlessScoreRecord>();
            Sort(data.records);
            return data.records;
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Không đọc được bảng xếp hạng Endless: " + exception.Message);
            return new List<EndlessScoreRecord>();
        }
    }

    public static void Add(EndlessScoreRecord record)
    {
        List<EndlessScoreRecord> records = Load();
        records.Add(record);
        Sort(records);
        if (records.Count > MaximumRecords)
            records.RemoveRange(MaximumRecords, records.Count - MaximumRecords);

        string path = Path.Combine(Application.persistentDataPath, FileName);
        string temporaryPath = path + ".tmp";
        try
        {
            string json = JsonUtility.ToJson(new EndlessScoreFile { records = records }, true);
            File.WriteAllText(temporaryPath, json);
            if (File.Exists(path)) File.Delete(path);
            File.Move(temporaryPath, path);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Không lưu được bảng xếp hạng Endless: " + exception.Message);
        }
    }

    public static string Format(int maximumRows = 10)
    {
        List<EndlessScoreRecord> records = Load();
        if (records.Count == 0) return "Chưa có thành tích. Hãy là người đầu tiên!";

        var lines = new List<string>();
        int count = Mathf.Min(maximumRows, records.Count);
        for (int index = 0; index < count; index++)
        {
            EndlessScoreRecord item = records[index];
            lines.Add(string.Format("{0,2}. {1,-12}  W{2,-3}  {3,7:N0} điểm",
                index + 1, ShortName(item.playerName), item.wave, item.score));
        }
        return string.Join("\n", lines);
    }

    private static string ShortName(string value)
    {
        string name = string.IsNullOrWhiteSpace(value) ? "Người chơi" : value.Trim();
        return name.Length <= 12 ? name : name.Substring(0, 12);
    }

    private static void Sort(List<EndlessScoreRecord> records)
    {
        records.Sort((left, right) =>
        {
            int byWave = right.wave.CompareTo(left.wave);
            if (byWave != 0) return byWave;
            int byScore = right.score.CompareTo(left.score);
            if (byScore != 0) return byScore;
            return left.durationSeconds.CompareTo(right.durationSeconds);
        });
    }
}
