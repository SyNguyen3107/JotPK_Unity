using UnityEngine;

public enum UpgradeType
{
    MoveSpeed,
    FireRate,
    AmmoDamage,
    ExtraLife,
    SheriffBadge,
    SuperGun
}

[CreateAssetMenu(fileName = "NewUpgrade", menuName = "JOTPK/Upgrade Data")]
public class UpgradeData : ScriptableObject
{
    [Header("Display Info")]
    public string upgradeName;
    public Sprite icon;
    public string displayLabel;

    [Header("Cost & Logic")]
    public int cost;
    public UpgradeType type;
    public float valueAmount;
}