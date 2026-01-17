using UnityEngine;

public class Gate : MonoBehaviour
{
    #region Configuration & Settings
    [Header("References")]
    public GameObject bottomGateObject;
    #endregion

    #region Runtime Variables
    private bool isOpen = false;
    #endregion

    #region Unity Lifecycle
    void Start()
    {
        CloseGate();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isOpen && other.CompareTag("Player"))
        {
            if (UIManager.Instance != null) UIManager.Instance.ToggleExitArrow(false);
            GameManager.Instance.StartLevelTransition();
        }
    }
    #endregion

    #region Core Logic
    public void OpenGate()
    {
        isOpen = true;

        if (bottomGateObject != null) bottomGateObject.SetActive(false);

        if (UIManager.Instance != null) UIManager.Instance.ToggleExitArrow(true);
    }

    public void CloseGate()
    {
        isOpen = false;

        if (bottomGateObject != null) bottomGateObject.SetActive(true);

        if (UIManager.Instance != null) UIManager.Instance.ToggleExitArrow(false);
    }
    #endregion
}