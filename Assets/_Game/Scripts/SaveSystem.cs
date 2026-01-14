using UnityEngine;
using System.IO;

public static class SaveSystem
{
    // Đường dẫn lưu file: C:/Users/Name/AppData/LocalLow/YourCompany/YourGame/
    private static string GetSavePath(int slotIndex)
    {
        return Application.persistentDataPath + "/save_" + slotIndex + ".json";
    }

    public static void SaveGame(GameData data, int slotIndex)
    {
        string json = JsonUtility.ToJson(data, true);
        string path = GetSavePath(slotIndex);
        File.WriteAllText(path, json);
        Debug.Log("Saved to: " + path);
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
            return null; // Không tìm thấy file save
        }
    }

    // Hàm xóa save (nếu muốn làm nút Delete)
    public static void DeleteSave(int slotIndex)
    {
        string path = GetSavePath(slotIndex);
        if (File.Exists(path)) File.Delete(path);
    }
}