using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Tạo bốn scene rỗng có bootstrap runtime và đăng ký chúng vào Build Settings.</summary>
public static class CampaignSceneBuilder
{
    private static readonly string[] SceneNames =
    {
        "CampaignScene", "CampaignSettings", "CampaignProgress", "CampaignAchievements"
    };

    [MenuItem("Tools/Three Worlds/Create Campaign Scenes")]
    public static void CreateScenes()
    {
        const string folder = "Assets/Scenes";
        foreach (string name in SceneNames)
        {
            string path = folder + "/" + name + ".unity";
            if (System.IO.File.Exists(path)) continue;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), path);
        }

        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (string name in SceneNames)
        {
            string path = folder + "/" + name + ".unity";
            bool exists = scenes.Exists(scene => scene.path == path);
            if (!exists) scenes.Add(new EditorBuildSettingsScene(path, true));
        }
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Created and registered Three Worlds campaign scenes.");
    }
}
