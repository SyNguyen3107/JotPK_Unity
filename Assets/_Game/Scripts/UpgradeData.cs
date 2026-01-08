using UnityEngine;

public enum UpgradeType
{
    MoveSpeed,      // Slot 1: Giày
    FireRate,       // Slot 2: Súng
    AmmoDamage,     // Slot 3: Đạn
    ExtraLife,      // Mạng
    SheriffBadge,   // Huy hiệu
    SuperGun        // Súng Shotgun vĩnh viễn
}

[CreateAssetMenu(fileName = "NewUpgrade", menuName = "JOTPK/Upgrade Data")]
public class UpgradeData : ScriptableObject
{
    [Header("Display Info")]
    public string upgradeName;
    public Sprite icon;             // Sprite này sẽ dùng cho cả trên bàn Shop và dưới UI
    [TextArea] public string description;

    [Header("Cost & Logic")]
    public int cost;                // Giá tiền
    public UpgradeType type;        // Loại nâng cấp
    public float valueAmount;       // Giá trị cộng thêm
}