using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System; // Cần thư viện này để dùng Action

public class ConfirmationPopup : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI messageText;
    public TextMeshProUGUI confirmButtonText; // Chữ trên nút (YES/DELETE/START)

    public Button confirmButton;
    public Button cancelButton;

    [Header("Colors")]
    public Color destructiveColor = new Color(1f, 0.3f, 0.3f); // Đỏ (Xóa)
    public Color constructiveColor = new Color(0.3f, 1f, 0.3f); // Xanh (Chơi mới)

    private Action onConfirmCallback; // Hành động sẽ làm khi bấm Yes

    void Awake()
    {
        // Gán sự kiện cho nút
        confirmButton.onClick.AddListener(OnConfirmClicked);
        cancelButton.onClick.AddListener(OnCancelClicked);

        gameObject.SetActive(false); // Mặc định ẩn
    }

    // Hàm này được MainMenuManager gọi để cài đặt nội dung
    public void Show(string title, string message, bool isDestructive, Action confirmAction)
    {
        titleText.text = title;
        messageText.text = message;

        onConfirmCallback = confirmAction;
        gameObject.SetActive(true);
    }

    void OnConfirmClicked()
    {
        onConfirmCallback?.Invoke(); // Chạy hành động đã lưu
        Hide();
    }

    void OnCancelClicked()
    {
        Hide();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}