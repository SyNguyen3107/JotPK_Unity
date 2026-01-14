using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;      // Bảng chứa nút Play/Quit
    public GameObject saveSlotPanel;  // Bảng chứa 3 Slot Save

    void Start()
    {
        // Mặc định khi vào game: Hiện Main, Ẩn Save Slot
        ShowMainPanel();
    }

    // --- CÁC HÀM ĐIỀU KHIỂN PANEL ---

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

            // Cập nhật lại UI của các slot (để hiển thị đúng data mới nhất)
            // Tìm tất cả script SaveSlotUI trong panel và gọi UpdateUI
            SaveSlotUI[] slots = saveSlotPanel.GetComponentsInChildren<SaveSlotUI>();
            foreach (var slot in slots)
            {
                slot.UpdateUI();
            }
        }
    }

    // --- CÁC HÀM GẮN VÀO NÚT BẤM ---

    public void OnPlayButton()
    {
        ShowSaveSlotPanel();
    }

    public void OnQuitButton()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();

        // Đoạn này để Quit hoạt động được ngay cả khi đang test trong Unity Editor
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif
    }
}