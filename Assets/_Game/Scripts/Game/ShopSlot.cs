using TMPro;
using UnityEngine;

public class ShopSlot : MonoBehaviour
{
    #region Configuration & Settings
    [Header("Configuration")]
    public int slotID;

    [Header("References")]
    public SpriteRenderer iconRenderer;
    public TextMeshPro priceText;
    public TextMeshPro labelText;
    #endregion

    #region Runtime Variables
    [HideInInspector] public UpgradeData currentItem;
    #endregion

    #region Unity Lifecycle
    void Start()
    {
        UpdateSlotDisplay();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            bool success = UpgradeManager.Instance.TryPurchaseUpgrade(slotID);

            if (success)
            {
                UpdateSlotDisplay();
            }
        }
    }
    #endregion

    #region Core Logic
    public void UpdateSlotDisplay()
    {
        if (UpgradeManager.Instance == null) return;

        currentItem = UpgradeManager.Instance.GetNextUpgradeForSlot(slotID);

        if (currentItem != null)
        {
            if (iconRenderer != null)
            {
                iconRenderer.sprite = currentItem.icon;
                iconRenderer.enabled = true;
            }

            if (priceText != null)
            {
                priceText.text = currentItem.cost.ToString();
                priceText.gameObject.SetActive(true);
            }
            if (labelText)
            {
                labelText.text = currentItem.displayLabel;
                labelText.gameObject.SetActive(true);
            }
        }
        else
        {
            if (iconRenderer != null) iconRenderer.enabled = false;
            if (priceText != null) priceText.gameObject.SetActive(false);
            if (labelText) labelText.gameObject.SetActive(false);
        }
    }
    #endregion
}