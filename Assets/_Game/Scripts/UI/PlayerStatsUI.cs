using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerStatsUI : MonoBehaviour
{
    [Header("Visuals")]
    public Image playerAvatarImage;
    public Sprite defaultPlayerSprite; // Kéo sprite nhân vật vào đây

    [Header("Upgrade Icons")]
    public Image bootIcon;
    public Image gunIcon;
    public Image ammoIcon;
    public Sprite emptySlotSprite; // Kéo một hình tròn mờ hoặc trống vào đây

    [Header("Stats Text")]
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI fireRateText;
    public TextMeshProUGUI damageText;

    public void UpdateStats()
    {
        // 1. Cập nhật Avatar (Nếu muốn đổi avatar theo trạng thái Zombie thì code thêm ở đây)
        if (playerAvatarImage != null && defaultPlayerSprite != null)
        {
            playerAvatarImage.sprite = defaultPlayerSprite;
        }

        // 2. Cập nhật Icon Nâng cấp (Lấy từ UpgradeManager)
        UpdateUpgradeIcon(bootIcon, 1); // Slot 1: Boots
        UpdateUpgradeIcon(gunIcon, 2);  // Slot 2: Gun
        UpdateUpgradeIcon(ammoIcon, 3); // Slot 3: Ammo

        // 3. Cập nhật Chỉ số (Lấy từ PlayerController)
        if (GameManager.Instance != null && GameManager.Instance.playerObject != null)
        {
            PlayerController pc = GameManager.Instance.playerObject.GetComponent<PlayerController>();
            if (pc != null)
            {
                if (speedText != null)
                    speedText.text = $"Move Speed: {pc.moveSpeed:F1}";

                if (fireRateText != null)
                {
                    // FireRate càng nhỏ bắn càng nhanh, nên hiển thị số đạn/giây sẽ dễ hiểu hơn
                    // Hoặc hiển thị thô nếu bạn muốn (pc.currentFireRate)
                    float shotsPerSec = (pc.currentFireRate > 0) ? (1f / pc.currentFireRate) : 0;
                    fireRateText.text = $"Fire Rate: {shotsPerSec:F1} /s";
                }

                if (damageText != null)
                    damageText.text = $"Damage: {pc.currentBulletDamage}";
            }
        }
    }

    void UpdateUpgradeIcon(Image img, int slotIndex)
    {
        if (img == null) return;

        Sprite icon = null;
        if (UpgradeManager.Instance != null)
        {
            // Hàm này bạn đã viết trong UpgradeManager rồi
            icon = UpgradeManager.Instance.GetPurchasedIcon(slotIndex);
        }

        if (icon != null)
        {
            img.sprite = icon;
            img.color = Color.white; // Hiện rõ
        }
        else
        {
            // Nếu chưa mua cấp nào
            if (emptySlotSprite != null) img.sprite = emptySlotSprite;
            img.color = new Color(1, 1, 1, 0.3f); // Làm mờ đi
        }
    }
}