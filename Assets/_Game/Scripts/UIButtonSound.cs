using UnityEngine;
using UnityEngine.EventSystems; // Cần thư viện này để bắt sự kiện chuột
using UnityEngine.UI;

// Script này tự động bắt sự kiện Di chuột (PointerEnter) và Click (PointerClick)
public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    private Button btn;

    void Awake()
    {
        btn = GetComponent<Button>();
    }

    // Khi chuột di vào vùng nút
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Kiểm tra nếu nút đang tương tác được (Interactable) mới phát tiếng
        if (btn != null && !btn.interactable) return;

        if (UIManager.Instance != null)
        {
            GameManager.Instance.PlayHoverSound();
        }
    }

    // Khi nhấn chuột vào nút
    public void OnPointerClick(PointerEventData eventData)
    {
        if (btn != null && !btn.interactable) return;

        if (UIManager.Instance != null)
        {
            GameManager.Instance.PlayClickSound();
        }
    }
}