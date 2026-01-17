using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;
    public GameObject saveSlotPanel;

    [Header("References")]
    public ConfirmationPopup confirmationPopup;

    void Start()
    {
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

            // Cập nhật lại UI của các slot
            SaveSlotUI[] slots = saveSlotPanel.GetComponentsInChildren<SaveSlotUI>();
            foreach (var slot in slots)
            {
                slot.UpdateUI();
            }
        }
    }

    // --- LOGIC XỬ LÝ YÊU CẦU TỪ SLOT ---

    // 1. Yêu cầu Xóa
    public void RequestDeleteSave(int slotIndex)
    {
        if (SaveSystem.LoadGame(slotIndex) == null) return;

        // Nội dung Custom cho việc Xóa
        string title = $"Delete Save #{slotIndex + 1}?";
        string msg = "This action cannot be undone.\nAre you sure you want to delete this save?";

        // Gọi Popup (isDestructive = true để nút màu Đỏ)
        confirmationPopup.Show(title, msg, true, () =>
        {
            SaveSystem.DeleteSave(slotIndex); // Thực hiện xóa
            Debug.Log($"Deleted Save Slot {slotIndex + 1}");

            // QUAN TRỌNG: Làm mới giao diện để slot hiện chữ "Empty" ngay lập tức
            ShowSaveSlotPanel();
        });
    }

    public void RequestNewGame(int slotIndex)
    {
        // 1. Kiểm tra lại cho chắc (dù UI đã check rồi)
        GameData existingData = SaveSystem.LoadGame(slotIndex);

        if (existingData == null)
        {
            // --- ĐÂY LÀ CHỖ BẠN CẦN ---
            // Slot đang trống -> Hiện Popup xác nhận TẠO MỚI

            string title = $"New Game on Slot #{slotIndex + 1}?";
            string msg = "Ready to start a new adventure in this slot?";

            // isDestructive = false (Nút màu Xanh - Hành động tích cực)
            confirmationPopup.Show(title, msg, false, () =>
            {
                // Chỉ khi bấm nút START màu xanh thì mới tạo game
                Debug.Log($"Starting new game on Slot {slotIndex}");
                StartGameDirectly(slotIndex, true);
            });
        }
        else
        {
            // Nếu lỡ hàm này được gọi vào slot ĐÃ CÓ dữ liệu (bug UI chẳng hạn)
            // Ta có thể chuyển sang load game hoặc báo lỗi
            Debug.LogWarning("Slot này không trống! Đang chuyển sang Load Game...");
            StartGameDirectly(slotIndex, false);
        }
    }

    // 3. Yêu cầu Tiếp tục (Load Game)
    public void RequestContinueGame(int slotIndex)
    {
        if (SaveSystem.LoadGame(slotIndex) != null)
        {
            StartGameDirectly(slotIndex, false);
        }
    }

    // Hàm vào game thực tế
    void StartGameDirectly(int slotIndex, bool isNewGame)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadGameAndPlay(slotIndex, isNewGame);
        }
    }

    // --- CÁC HÀM NÚT BẤM CƠ BẢN ---
    public void OnPlayButton() { ShowSaveSlotPanel(); }
    public void OnQuitButton()
    {
        Application.Quit();
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#endif
    }
}