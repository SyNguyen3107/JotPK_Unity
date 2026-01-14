using System.Collections.Generic;

[System.Serializable]
public class GameData
{
    // --- CƠ BẢN ---
    public int lives;
    public int coins;
    public int currentLevelIndex;
    public bool isZombieMode;

    // --- UPGRADES (Lưu cấp độ hiện tại, ví dụ: 0, 1, 2) ---
    public int gunLevel;
    public int bootLevel;
    public int ammoLevel; // Hoặc ReloadSpeed / Damage tùy cách bạn đặt tên

    // --- HELD ITEM (Item đang giữ trong ô dự trữ) ---
    // Chúng ta dùng Enum hoặc String. Ví dụ dùng Enum PowerUpType.
    // Nếu không cầm gì, giá trị sẽ là PowerUpType.None
    public PowerUpType heldItemType;
    public bool hasHeldItem; // Biến cờ để biết có đang cầm đồ không

    // Constructor mặc định (New Game)
    public GameData()
    {
        lives = 3;
        coins = 0;
        currentLevelIndex = 1; // Hoặc index của scene đầu tiên

        gunLevel = 0;
        bootLevel = 0;
        ammoLevel = 0;

        hasHeldItem = false;
        heldItemType = PowerUpType.None; // Giả sử bạn có enum None
    }
}