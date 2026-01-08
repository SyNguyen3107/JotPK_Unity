using UnityEngine;
using UnityEngine.UI; // Cần thư viện này để chỉnh UI

[RequireComponent(typeof(CanvasGroup))] // Tự động thêm CanvasGroup nếu chưa có
public class UIBlinker : MonoBehaviour
{
    [Header("Settings")]
    public float blinkSpeed = 2f;   // Tốc độ nhấp nháy
    public float minAlpha = 0.2f;   // Độ mờ thấp nhất (0 là tàng hình, 1 là rõ nhất)
    public float maxAlpha = 1f;     // Độ mờ cao nhất

    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    void OnEnable()
    {
        // Mỗi khi object được bật (SetActive true), reset lại độ sáng
        if (canvasGroup != null) canvasGroup.alpha = maxAlpha;
    }

    void Update()
    {
        // Tính toán giá trị Alpha dựa trên thời gian (hàm PingPong tạo dao động lên xuống)
        float alpha = Mathf.PingPong(Time.time * blinkSpeed, maxAlpha - minAlpha) + minAlpha;

        // Gán giá trị vào CanvasGroup
        if (canvasGroup != null) canvasGroup.alpha = alpha;
    }
}