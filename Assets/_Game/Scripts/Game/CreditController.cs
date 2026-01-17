using UnityEngine;
using UnityEngine.SceneManagement;

public class CreditController : MonoBehaviour
{
    #region Configuration & Settings
    [Header("Settings")]
    public string mainMenuSceneName = "MainMenu";
    public float blinkSpeed = 2.0f;

    [Header("UI References")]
    public CanvasGroup pressSpaceCanvasGroup;
    #endregion

    #region Unity Lifecycle
    void Update()
    {
        if (pressSpaceCanvasGroup != null)
        {
            float alpha = 0.2f + Mathf.PingPong(Time.time * blinkSpeed, 0.8f);
            pressSpaceCanvasGroup.alpha = alpha;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            ReturnToMenu();
        }
    }
    #endregion

    #region Core Logic
    void ReturnToMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
    #endregion
}