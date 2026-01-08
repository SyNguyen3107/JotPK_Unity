using UnityEngine;
using TMPro; // Nhớ dùng thư viện TextMeshPro

public class ShopSlot : MonoBehaviour
{
    [Header("Configuration")]
    public int slotID; // Điền 1, 2 hoặc 3 vào đây trong Inspector

    [Header("References")]
    public SpriteRenderer iconRenderer;
    public TextMeshPro priceText; // Dùng TextMeshPro (World Space)

    // Dữ liệu món hàng hiện tại (để sau này xử lý mua)
    [HideInInspector] public UpgradeData currentItem;

    void Start()
    {
        // Khi bàn được sinh ra, tự động cập nhật hiển thị
        UpdateSlotDisplay();
    }

    public void UpdateSlotDisplay()
    {
        if (UpgradeManager.Instance == null) return;

        // Lấy món hàng tiếp theo từ Manager
        currentItem = UpgradeManager.Instance.GetNextUpgradeForSlot(slotID);

        if (currentItem != null)
        {
            // Có hàng -> Hiện Icon và Giá
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
        }
        else
        {
            // Hết hàng (đã mua max cấp) -> Ẩn đi
            if (iconRenderer != null) iconRenderer.enabled = false;
            if (priceText != null) priceText.gameObject.SetActive(false);
        }
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Gọi Manager để thử mua hàng
            bool success = UpgradeManager.Instance.TryPurchaseUpgrade(slotID);

            if (success)
            {
                // Nếu mua thành công -> Cập nhật lại hiển thị của bàn (ẩn món vừa mua đi)
                UpdateSlotDisplay();

                // Các hiệu ứng khác (trừ tiền, animation player) đã được UpgradeManager gọi rồi
            }
        }
    }
}