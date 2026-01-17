using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Dùng TextMeshPro
using UnityEngine.UI; // Dùng nếu Press Space là Image

public class CreditController : MonoBehaviour
{
    [Header("Settings")]
    public string mainMenuSceneName = "MainMenu";
    public float blinkSpeed = 2.0f; // Tốc độ nhấp nháy

    [Header("UI References")]
    // Bạn có thể kéo Text hoặc Image vào đây đều được (miễn là nó có CanvasGroup)
    public CanvasGroup pressSpaceCanvasGroup;

    void Update()
    {
        // 1. Logic Nhấp nháy (Blink)
        if (pressSpaceCanvasGroup != null)
        {
            // Tạo hiệu ứng mờ dần đều (PingPong) từ 0.2 đến 1.0 (không để tắt hẳn để dễ nhìn)
            float alpha = 0.2f + Mathf.PingPong(Time.time * blinkSpeed, 0.8f);
            pressSpaceCanvasGroup.alpha = alpha;
        }

        // 2. Logic Bắt phím Space
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ReturnToMenu();
        }
    }

    void ReturnToMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}