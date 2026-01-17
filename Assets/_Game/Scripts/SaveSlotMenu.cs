using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SaveSlotUI : MonoBehaviour
{
    [Header("Settings")]
    public int slotIndex; // 0, 1, 2

    [Header("UI References")]
    public TextMeshProUGUI infoText;
    public Button slotButton;   // Nút chính
    public Button deleteButton; // Nút xóa

    private MainMenuManager menuManager;

    void Start()
    {
        // Tự tìm MainMenuManager trong Scene
        menuManager = FindFirstObjectByType<MainMenuManager>();
        UpdateUI();
    }

    public void UpdateUI()
    {
        // Đọc dữ liệu
        GameData data = SaveSystem.LoadGame(slotIndex);

        // Xóa listener cũ
        slotButton.onClick.RemoveAllListeners();
        if (deleteButton != null) deleteButton.onClick.RemoveAllListeners();

        if (data != null)
        {
            // --- CÓ DỮ LIỆU ---
            infoText.text = $"Slot {slotIndex + 1}\nLevel: {data.currentLevelIndex}\nCoins: {data.coins}";

            // Bấm nút chính -> Gọi hàm Continue của MainMenuManager
            slotButton.onClick.AddListener(() =>
            {
                if (menuManager) menuManager.RequestContinueGame(slotIndex);
            });

            // Bấm nút xóa -> Gọi hàm Delete của MainMenuManager (để hiện bảng hỏi)
            if (deleteButton != null)
            {
                deleteButton.interactable = true;
                deleteButton.onClick.AddListener(() =>
                {
                    if (menuManager) menuManager.RequestDeleteSave(slotIndex);
                });
            }
        }
        else
        {
            // --- SLOT TRỐNG ---
            infoText.text = $"Slot {slotIndex + 1}\nEmpty";

            // Bấm nút chính -> Gọi hàm NewGame của MainMenuManager
            slotButton.onClick.AddListener(() =>
            {
                if (menuManager) menuManager.RequestNewGame(slotIndex);
            });

            // Khóa nút xóa
            if (deleteButton != null)
            {
                deleteButton.interactable = false;
            }
        }
    }
}