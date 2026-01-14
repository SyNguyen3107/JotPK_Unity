using UnityEngine;
using System.Collections.Generic;
using System;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;
    public event Action OnUpgradePurchased;

    #region --- CONFIGURATION ---
    [Header("Shop Configuration")]
    public List<UpgradeData> slot1Upgrades; // Boots
    public List<UpgradeData> slot2Upgrades; // Gun
    public List<UpgradeData> slot3Upgrades; // Ammo/Badge
    #endregion

    #region --- STATE ---
    [Header("Current State")]
    public int slot1Index = 0;
    public int slot2Index = 0;
    public int slot3Index = 0;
    public bool hasPurchasedThisRound = false;

    // Accessors cho Save System (Mapping: Slot 1=Boot, Slot 2=Gun, Slot 3=Ammo)
    public int currentBootLevel => slot1Index;
    public int currentGunLevel => slot2Index;
    public int currentAmmoLevel => slot3Index;
    #endregion

    #region --- LIFECYCLE ---
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }
    #endregion

    #region --- SAVE / LOAD LOGIC ---
    public void SetUpgradeLevels(int gunLevel, int bootLevel, int ammoLevel)
    {
        slot2Index = gunLevel; // Gun -> Slot 2
        slot1Index = bootLevel; // Boot -> Slot 1
        slot3Index = ammoLevel; // Ammo -> Slot 3

        // Sau khi load chỉ số, cần áp dụng ngay vào Player hiện tại (nếu có)
        ReapplyAllUpgrades();
    }

    // Hàm này gọi mỗi khi bắt đầu màn chơi mới để cộng lại chỉ số cho Player mới spawn
    public void ReapplyAllUpgrades()
    {
        PlayerController player = FindPlayer();
        if (player == null) return;

        ApplyListUpgrades(player, slot1Upgrades, slot1Index);
        ApplyListUpgrades(player, slot2Upgrades, slot2Index);
        ApplyListUpgrades(player, slot3Upgrades, slot3Index);

        // Cập nhật UI Icon bên trái màn hình
        if (UIManager.Instance != null) UIManager.Instance.UpdateUpgradeIcons();
    }

    void ApplyListUpgrades(PlayerController player, List<UpgradeData> list, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (i < list.Count) player.ApplyPermanentUpgrade(list[i]);
        }
    }
    #endregion

    #region --- SHOP LOGIC ---
    public UpgradeData GetNextUpgradeForSlot(int slotNumber)
    {
        switch (slotNumber)
        {
            case 1: return (slot1Index < slot1Upgrades.Count) ? slot1Upgrades[slot1Index] : null;
            case 2: return (slot2Index < slot2Upgrades.Count) ? slot2Upgrades[slot2Index] : null;
            case 3: return (slot3Index < slot3Upgrades.Count) ? slot3Upgrades[slot3Index] : null;
            default: return null;
        }
    }

    public Sprite GetPurchasedIcon(int slotNumber)
    {
        int index = 0;
        List<UpgradeData> list = null;

        switch (slotNumber)
        {
            case 1: index = slot1Index; list = slot1Upgrades; break;
            case 2: index = slot2Index; list = slot2Upgrades; break;
            case 3: index = slot3Index; list = slot3Upgrades; break;
        }

        if (list != null && index > 0 && index - 1 < list.Count)
            return list[index - 1].icon;

        return null;
    }

    public bool TryPurchaseUpgrade(int slotNumber)
    {
        if (hasPurchasedThisRound) return false;

        UpgradeData itemToBuy = GetNextUpgradeForSlot(slotNumber);
        if (itemToBuy == null) return false;

        if (GameManager.Instance != null && GameManager.Instance.currentCoins >= itemToBuy.cost)
        {
            GameManager.Instance.AddCoin(-itemToBuy.cost);
            AdvanceSlotIndex(slotNumber);
            hasPurchasedThisRound = true;
            ApplyUpgradeEffect(itemToBuy); // Áp dụng và diễn hoạt
            OnUpgradePurchased?.Invoke();
            return true;
        }

        return false;
    }

    public void ResetPurchaseStatus()
    {
        hasPurchasedThisRound = false;
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
    #endregion

    #region --- EFFECT APPLICATION ---
    void ApplyUpgradeEffect(UpgradeData data)
    {
        PlayerController player = FindPlayer();
        if (player != null)
        {
            player.ApplyPermanentUpgrade(data);
            player.TriggerItemGetAnimation(data.icon);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateUpgradeIcons();
        }
    }

    PlayerController FindPlayer()
    {
        if (GameManager.Instance != null && GameManager.Instance.playerObject != null)
            return GameManager.Instance.playerObject.GetComponent<PlayerController>();

        return FindFirstObjectByType<PlayerController>();
    }
    #endregion
}