using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Công cụ build gọi được từ dòng lệnh, dùng khi không mở giao diện Unity.
///
/// Ví dụ:
///   Unity.exe -quit -batchmode -nographics -projectPath "E:/Document/Game/pvz2_fusion"
///             -executeMethod BuildTools.BuildWindows
///             -buildOutput "E:/Document/Game/pvz2_fusion/Build/PvZ2Fusion/PvZ2Fusion.exe"
///             -logFile build.log
/// </summary>
public static class BuildTools
{
    private const string DefaultOutput = "Build/PvZ2Fusion/PvZ2Fusion.exe";

    [MenuItem("Build/Windows 64-bit")]
    public static void BuildWindowsFromMenu()
    {
        Build(Path.Combine(Directory.GetCurrentDirectory(), DefaultOutput));
    }

    /// <summary>Điểm vào cho dòng lệnh.</summary>
    public static void BuildWindows()
    {
        string output = ReadArgument("-buildOutput");
        if (string.IsNullOrEmpty(output))
            output = Path.Combine(Directory.GetCurrentDirectory(), DefaultOutput);

        Build(output);
    }

    private static void Build(string output)
    {
        List<string> scenes = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled) scenes.Add(scene.path);
        }

        if (scenes.Count == 0)
        {
            Debug.LogError("Không có scene nào được bật trong Build Settings.");
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return;
        }

        string folder = Path.GetDirectoryName(output);
        if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        BuildPlayerOptions options = new BuildPlayerOptions();
        options.scenes = scenes.ToArray();
        options.locationPathName = output;
        options.target = BuildTarget.StandaloneWindows64;
        options.targetGroup = BuildTargetGroup.Standalone;
        options.options = BuildOptions.None;

        Debug.Log("Bắt đầu build ra: " + output + " với " + scenes.Count + " scene.");

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("Build xong: " + summary.outputPath
                + " · " + (summary.totalSize / 1048576) + " MB"
                + " · " + summary.totalTime);
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError("Build thất bại: " + summary.result + ", " + summary.totalErrors + " lỗi.");
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }

    /// <summary>Đọc một tham số dạng "-tên giá_trị" trên dòng lệnh.</summary>
    private static string ReadArgument(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name) return args[i + 1];
        }
        return null;
    }
}
