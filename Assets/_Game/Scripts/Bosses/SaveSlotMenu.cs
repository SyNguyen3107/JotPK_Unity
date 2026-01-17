using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveSlotUI : MonoBehaviour
{
    #region Configuration & Settings
    [Header("Settings")]
    public int slotIndex;

    [Header("UI References")]
    public TextMeshProUGUI infoText;
    public Button slotButton;
    public Button deleteButton;
    #endregion

    #region Runtime Variables
    private MainMenuManager menuManager;
    #endregion

    #region Unity Lifecycle
    void Start()
    {
        menuManager = FindFirstObjectByType<MainMenuManager>();
        UpdateUI();
    }
    #endregion

    #region Core Logic
    public void UpdateUI()
    {
        GameData data = SaveSystem.LoadGame(slotIndex);

        slotButton.onClick.RemoveAllListeners();
        if (deleteButton != null) deleteButton.onClick.RemoveAllListeners();

        if (data != null)
        {
            infoText.text = $"Slot {slotIndex + 1}\nLevel: {data.currentLevelIndex}\nCoins: {data.coins}";

            slotButton.onClick.AddListener(() =>
            {
                if (menuManager) menuManager.RequestContinueGame(slotIndex);
            });

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
            infoText.text = $"Slot {slotIndex + 1}\nEmpty";

            slotButton.onClick.AddListener(() =>
            {
                if (menuManager) menuManager.RequestNewGame(slotIndex);
            });

            if (deleteButton != null)
            {
                deleteButton.interactable = false;
            }
        }
    }
    #endregion
}