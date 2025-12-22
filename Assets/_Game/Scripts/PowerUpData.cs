using UnityEngine;

public enum PowerUpType
{
    // --- VŨ KHÍ & BUFF (Cầm được) ---
    HeavyMachineGun,     // Súng máy (Bắn nhanh)
    Shotgun,        // Bắn 3 tia chùm
    WagonWheel,          // Bắn 8 hướng
    Coffee,         // Tăng tốc chạy
    SheriffBadge,   // Tổng hợp (Súng máy + Shotgun + Tốc chạy)

    // --- TÁC DỤNG TỨC THỜI (Dùng ngay, có thể hoặc không cầm) ---
    ScreenNuke,           // Tiêu diệt toàn bộ quái
    SmokeBomb,      // Dịch chuyển tức thời / Làm choáng (trong game này ta làm dịch chuyển cho dễ)
    Tombstone,      // Biến thành Zombie (Bất tử + Chạy nhanh + Húc chết quái)

    // --- TÀI NGUYÊN (Ăn là cộng luôn, không vào ô chứa) ---
    Coin, // Tiền (1 hoặc 5)
    Life            // Mạng
}

[CreateAssetMenu(fileName = "NewPowerUp", menuName = "JOTPK/PowerUp Data")]
public class PowerUpData : ScriptableObject
{
    public string itemName;
    public PowerUpType type;
    public Sprite icon;       // Hình ảnh hiển thị trên UI và khi rơi dưới đất
    public float duration;    // Thời gian tác dụng (Nuke hay Coin thì để 0)
    public AudioClip activateSound; // Âm thanh khi kích hoạt

    [Header("Values")]
    public float valueAmount; // Dùng cho Coin (1 hoặc 5) hoặc Speed (tốc độ cộng thêm)
}