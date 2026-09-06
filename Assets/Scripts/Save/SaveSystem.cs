using System;
using System.IO;
using UnityEngine;

// JSON save file in Application.persistentDataPath. Writes go to a temp file
// first and are then moved into place, so a crash or a phone losing power
// mid-write cannot leave a half-written save behind.
public static class SaveSystem
{
    private const string FileName = "soulboundgate_run.json";
    private const string TempFileName = "soulboundgate_run.tmp";
    private const string BackupFileName = "soulboundgate_run.bak";

    public static string SavePath
    {
        get { return Path.Combine(Application.persistentDataPath, FileName); }
    }

    private static string TempPath
    {
        get { return Path.Combine(Application.persistentDataPath, TempFileName); }
    }

    private static string BackupPath
    {
        get { return Path.Combine(Application.persistentDataPath, BackupFileName); }
    }

    public static bool Save(SaveData data)
    {
        if (data == null) { return false; }

        data.saveVersion = SaveData.CurrentVersion;

        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(TempPath, json);

            // File.Replace needs the destination to exist; a first-ever save does not.
            if (File.Exists(SavePath))
            {
                File.Replace(TempPath, SavePath, BackupPath);
            }
            else
            {
                File.Move(TempPath, SavePath);
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[SaveSystem] Could not write save: " + e.Message);
            return false;
        }
    }

    public static SaveData Load()
    {
        SaveData data = ReadFile(SavePath);

        // A save that failed validation may still have a good previous version
        // sitting next to it from the last atomic replace.
        if (data == null) { data = ReadFile(BackupPath); }

        return data;
    }

    public static bool HasValidSave()
    {
        SaveData data = Load();
        return data != null && data.runActive && !data.runCompleted;
    }

    public static void Delete()
    {
        TryDelete(SavePath);
        TryDelete(TempPath);
        TryDelete(BackupPath);
    }

    private static SaveData ReadFile(string path)
    {
        try
        {
            if (!File.Exists(path)) { return null; }

            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json)) { return null; }

            SaveData data = JsonUtility.FromJson<SaveData>(json);
            if (data == null) { return null; }

            // Older or newer files are not trusted rather than half-applied.
            if (data.saveVersion != SaveData.CurrentVersion)
            {
                Debug.LogWarning("[SaveSystem] Save version " + data.saveVersion + " is not supported; ignoring.");
                return null;
            }

            if (data.scenes == null) { data.scenes = new System.Collections.Generic.List<SceneStateData>(); }
            if (string.IsNullOrEmpty(data.currentScene)) { data.currentScene = GameScenes.Scene1; }

            return data;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[SaveSystem] Could not read save at " + path + ": " + e.Message);
            return null;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) { File.Delete(path); }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[SaveSystem] Could not delete " + path + ": " + e.Message);
        }
    }
}
