using UnityEngine;
using System.Collections.Generic;
using System;
public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;

    public event Action OnUpgradePurchased;

    [Header("Shop Configuration Lists")]
    // Kéo thả các ScriptableObject vào đây theo thứ tự xuất hiện
    public List<UpgradeData> slot1Upgrades; // Slot 1: Thường là Giày
    public List<UpgradeData> slot2Upgrades; // Slot 2: Thường là Súng
    public List<UpgradeData> slot3Upgrades; // Slot 3: Thường là Đạn/Badge

    [Header("State (Read Only)")]
    // Index = 0 nghĩa là chưa mua gì (đang chờ mua món đầu tiên)
    public int slot1Index = 0;
    public int slot2Index = 0;
    public int slot3Index = 0;

    public bool hasPurchasedThisRound = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // DontDestroyOnLoad(gameObject); // Bật dòng này nếu GameManager không giữ object này
    }

    // --- LOGIC CHO SHOP (CÁI BÀN) ---
    public UpgradeData GetNextUpgradeForSlot(int slotNumber)
    {
        switch (slotNumber)
        {
            case 1:
                if (slot1Index < slot1Upgrades.Count) return slot1Upgrades[slot1Index];
                break;
            case 2:
                if (slot2Index < slot2Upgrades.Count) return slot2Upgrades[slot2Index];
                break;
            case 3:
                if (slot3Index < slot3Upgrades.Count) return slot3Upgrades[slot3Index];
                break;
        }
        return null; // Đã mua hết sạch đồ trong slot này
    }

    // --- LOGIC CHO UI (LEFT PANEL) ---
    // Hàm này trả về Icon của món đồ ĐÃ MUA GẦN NHẤT để hiển thị lên UI
    public Sprite GetPurchasedIcon(int slotNumber)
    {
        int currentIndex = 0;
        List<UpgradeData> currentList = null;

        switch (slotNumber)
        {
            case 1: currentIndex = slot1Index; currentList = slot1Upgrades; break;
            case 2: currentIndex = slot2Index; currentList = slot2Upgrades; break;
            case 3: currentIndex = slot3Index; currentList = slot3Upgrades; break;
        }

        // currentIndex là món "Sắp mua". 
        // Muốn lấy món "Đã mua", ta phải lấy (currentIndex - 1).
        if (currentList != null && currentIndex > 0)
        {
            return currentList[currentIndex - 1].icon;
        }

        return null; // Chưa mua món nào ở slot này -> Trả về null (UI sẽ ẩn hoặc hiện ô trống)
    }

    // --- XỬ LÝ GIAO DỊCH ---
    public bool TryPurchaseUpgrade(int slotNumber)
    {
        if (hasPurchasedThisRound)
        {
            Debug.Log("Shop: Đã mua đồ trong vòng này rồi!");
            return false;
        }

        UpgradeData itemToBuy = GetNextUpgradeForSlot(slotNumber);
        if (itemToBuy == null) return false;

        if (GameManager.Instance != null && GameManager.Instance.currentCoins >= itemToBuy.cost)
        {
            // 1. Trừ tiền
            GameManager.Instance.AddCoin(-itemToBuy.cost);

            // 2. Tăng tiến trình
            AdvanceSlotIndex(slotNumber);

            // 3. Đánh dấu đã mua
            hasPurchasedThisRound = true;

            // 4. Áp dụng hiệu ứng
            ApplyUpgradeEffect(itemToBuy);

            // 5. PHÁT TÍN HIỆU: "Đã mua xong, dọn hàng thôi!"
            OnUpgradePurchased?.Invoke(); // <--- THÊM DÒNG NÀY

            return true;
        }
        else
        {
            Debug.Log("Shop: Không đủ tiền!");
            return false;
        }
    }

    void AdvanceSlotIndex(int slotNumber)
    {
        switch (slotNumber)
        {
            case 1: slot1Index++; break;
            case 2: slot2Index++; break;
            case 3: slot3Index++; break;
        }
    }

    public void ResetPurchaseStatus()
    {
        hasPurchasedThisRound = false;
    }
    void ApplyUpgradeEffect(UpgradeData data)
    {
        // 1. Tìm Player và áp dụng chỉ số
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player == null) player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();

        if (player != null)
        {
            // Cộng chỉ số
            player.ApplyPermanentUpgrade(data);

            // Diễn hoạt cảnh giơ tay
            player.TriggerItemGetAnimation(data.icon);
        }

        // 2. Cập nhật UI bên trái
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateUpgradeIcons();
        }
    }
}