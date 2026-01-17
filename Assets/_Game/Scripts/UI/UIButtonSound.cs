using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    #region Runtime Variables
    private Button btn;
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        btn = GetComponent<Button>();
    }
    #endregion

    #region Interface Implementations
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (btn != null && !btn.interactable)
        {
            return;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayHoverSound();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (btn != null && !btn.interactable)
        {
            return;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayClickSound();
        }
    }
    #endregion
}