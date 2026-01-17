using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmationPopup : MonoBehaviour
{
    #region Configuration & Settings
    [Header("UI References")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI messageText;
    public Button confirmButton;
    public Button cancelButton;
    #endregion

    #region Runtime Variables
    private Action onConfirmCallback;
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        confirmButton.onClick.AddListener(OnConfirmClicked);
        cancelButton.onClick.AddListener(OnCancelClicked);

        gameObject.SetActive(false);
    }
    #endregion

    #region Core Logic
    public void Show(string title, string message, bool isDestructive, Action confirmAction)
    {
        titleText.text = title;
        messageText.text = message;

        onConfirmCallback = confirmAction;
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
    #endregion

    #region Event Handlers
    void OnConfirmClicked()
    {
        onConfirmCallback?.Invoke();
        Hide();
    }

    void OnCancelClicked()
    {
        Hide();
    }
    #endregion
}