using UnityEngine;

public class Gate : MonoBehaviour
{
    [Header("References")]
    public GameObject bottomGateObject; // Kéo object "Bottom_Gate" từ trong Grid vào đây

    // Script này cần gắn trên object có BoxCollider2D (IsTrigger = true)

    private bool isOpen = false;

    void Start()
    {
        CloseGate();
    }

    public void OpenGate()
    {
        isOpen = true;

        // 1. Tắt object tường gạch để "Mở cổng"
        if (bottomGateObject != null) bottomGateObject.SetActive(false);
        // ------------------------

        // 2. Gọi UI bật mũi tên
        if (UIManager.Instance != null) UIManager.Instance.ToggleExitArrow(true);
    }

    public void CloseGate()
    {
        isOpen = false;

        // 1. Bật lại object cổng dưới -> Tường xuất hiện lại
        if (bottomGateObject != null) bottomGateObject.SetActive(true);

        // 2. Gọi UI tắt mũi tên
        if (UIManager.Instance != null) UIManager.Instance.ToggleExitArrow(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Chỉ khi cổng đã mở (isOpen = true) thì Trigger mới có tác dụng
        if (isOpen && other.CompareTag("Player"))
        {
            // Khi chạm vào cổng thì tắt mũi tên ngay
            if (UIManager.Instance != null) UIManager.Instance.ToggleExitArrow(false);
            // Gọi GameManager chuyển màn
            GameManager.Instance.StartLevelTransition();
        }
    }
}