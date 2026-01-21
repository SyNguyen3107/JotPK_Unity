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
    [TextArea(3, 5)]
    public string description;

    [Header("Values")]
    public float valueAmount;
    public float duration;
    public AudioClip activateSound;

    [Header("Drop Settings")]
    [Range(1, 1000)]
    public int dropWeight = 10;
}