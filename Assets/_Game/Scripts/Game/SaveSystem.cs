using UnityEngine;
using System.IO;

public static class SaveSystem
{
    #region Core Logic
    public static void SaveGame(GameData data, int slotIndex)
    {
        string json = JsonUtility.ToJson(data, true);
        string path = GetSavePath(slotIndex);
        File.WriteAllText(path, json);
    }

    public static GameData LoadGame(int slotIndex)
    {
        string path = GetSavePath(slotIndex);
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            return JsonUtility.FromJson<GameData>(json);
        }
        else
        {
            return null;
        }
    }

    public static void DeleteSave(int slotIndex)
    {
        string path = GetSavePath(slotIndex);
        if (File.Exists(path)) File.Delete(path);
    }
    #endregion

    #region Helpers
    private static string GetSavePath(int slotIndex)
    {
        return Application.persistentDataPath + "/save_" + slotIndex + ".json";
    }
    #endregion
}