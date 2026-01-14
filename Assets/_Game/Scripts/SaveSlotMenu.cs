using UnityEngine;
using TMPro;
using UnityEngine.UI;
// Không cần SceneManager nữa vì GameManager đã lo việc đó

public class SaveSlotUI : MonoBehaviour
{
    [Header("Settings")]
    public int slotIndex; // 0, 1, 2

    [Header("UI References")]
    public TextMeshProUGUI infoText;
    public Button slotButton;   // Nút chính (Chơi)
    public Button deleteButton; // Nút xóa (Thùng rác)

    void Start()
    {
        UpdateUI();
    }

    public void UpdateUI()
    {
        // 1. Đọc dữ liệu (Read Only)
        GameData data = SaveSystem.LoadGame(slotIndex);

        // 2. Xóa sự kiện cũ để tránh bị chồng chéo (Quan trọng)
        slotButton.onClick.RemoveAllListeners();
        if (deleteButton != null) deleteButton.onClick.RemoveAllListeners();

        if (data != null)
        {
            // --- TRƯỜNG HỢP CÓ FILE SAVE ---
            // Lưu ý: Trong GameManager, ta đã lưu Coin vào biến 'score' của GameData
            infoText.text = $"Slot {slotIndex + 1}\nLevel: {data.currentLevelIndex}\nCoins: {data.coins}";

            // Gán hành động Load Game
            slotButton.onClick.AddListener(LoadThisSlot);

            // Xử lý nút Xóa
            if (deleteButton != null)
            {
                deleteButton.interactable = true;
                deleteButton.onClick.AddListener(DeleteThisSlot);
            }
        }
        else
        {
            // --- TRƯỜNG HỢP SLOT TRỐNG ---
            infoText.text = $"Slot {slotIndex + 1}\nEmpty";

            // Gán hành động New Game
            slotButton.onClick.AddListener(NewGameThisSlot);

            // Khóa nút Xóa
            if (deleteButton != null)
            {
                deleteButton.interactable = false;
            }
        }
    }

    // Wrapper function để gọi GameManager
    void LoadThisSlot()
    {
        Debug.Log($"Loading Slot {slotIndex}...");
        if (GameManager.Instance != null)
        {
            // false = Load Game cũ
            GameManager.Instance.LoadGameAndPlay(slotIndex, false);
        }
    }

    // Wrapper function để gọi GameManager
    void NewGameThisSlot()
    {
        Debug.Log($"Creating New Game at Slot {slotIndex}...");
        if (GameManager.Instance != null)
        {
            // true = Tạo New Game (Reset dữ liệu & Overwrite file cũ)
            GameManager.Instance.LoadGameAndPlay(slotIndex, true);
        }
    }

    public void DeleteThisSlot()
    {
        SaveSystem.DeleteSave(slotIndex);
        UpdateUI(); // Refresh lại giao diện ngay lập tức để hiện chữ "Empty"
    }
}