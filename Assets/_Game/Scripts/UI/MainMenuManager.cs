using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuManager : MonoBehaviour
{
    #region Configuration & Settings
    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject saveSlotPanel;

    [Header("References")]
    public ConfirmationPopup confirmationPopup;
    #endregion

    #region Unity Lifecycle
    void Start()
    {
        ShowMainPanel();
    }
    #endregion

    #region Core Logic
    public void ShowMainPanel()
    {
        if (mainPanel != null) mainPanel.SetActive(true);
        if (saveSlotPanel != null) saveSlotPanel.SetActive(false);
    }

    public void ShowSaveSlotPanel()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (saveSlotPanel != null)
        {
            saveSlotPanel.SetActive(true);

            SaveSlotUI[] slots = saveSlotPanel.GetComponentsInChildren<SaveSlotUI>();
            foreach (var slot in slots)
            {
                slot.UpdateUI();
            }
        }
    }

    public void RequestDeleteSave(int slotIndex)
    {
        if (SaveSystem.LoadGame(slotIndex) == null) return;

        string title = $"Delete Save #{slotIndex + 1}?";
        string msg = "This action cannot be undone.\nAre you sure you want to delete this save?";

        confirmationPopup.Show(title, msg, true, () =>
        {
            SaveSystem.DeleteSave(slotIndex);
            ShowSaveSlotPanel();
        });
    }

    public void RequestNewGame(int slotIndex)
    {
        GameData existingData = SaveSystem.LoadGame(slotIndex);

        if (existingData == null)
        {
            string title = $"New Game on Slot #{slotIndex + 1}?";
            string msg = "Ready to start a new adventure in this slot?";

            confirmationPopup.Show(title, msg, false, () =>
            {
                StartGameDirectly(slotIndex, true);
            });
        }
        else
        {
            StartGameDirectly(slotIndex, false);
        }
    }

    public void RequestContinueGame(int slotIndex)
    {
        if (SaveSystem.LoadGame(slotIndex) != null)
        {
            StartGameDirectly(slotIndex, false);
        }
    }

    void StartGameDirectly(int slotIndex, bool isNewGame)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadGameAndPlay(slotIndex, isNewGame);
        }
    }
    #endregion

    #region Event Handlers
    public void OnPlayButton()
    {
        ShowSaveSlotPanel();
    }

    public void OnCreditButton()
    {
        SceneManager.LoadScene("Credit");
    }

    public void OnQuitButton()
    {
        Application.Quit();
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif
    }
    #endregion
}