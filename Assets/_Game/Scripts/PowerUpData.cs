using UnityEngine;

public enum PowerUpType
{
    None,
    Coin,
    Life,
    HeavyMachineGun,
    Shotgun,
    WagonWheel,
    SheriffBadge,
    ScreenNuke,
    SmokeBomb,
    Tombstone,
    Coffee
}

[CreateAssetMenu(fileName = "NewPowerUp", menuName = "JOTPK/PowerUp Data")]
public class PowerUpData : ScriptableObject
{
    [Header("Display Info")]
    public string itemName;
    public Sprite icon;
    public PowerUpType type;

    [Header("Values")]
    public float valueAmount; // Dùng cho Coin (số tiền), Life (số mạng)
    public float duration;    // Dùng cho Item Buff
    public AudioClip activateSound;

    [Header("Drop Settings")]
    [Range(1, 1000)]
    public int dropWeight = 10;
}