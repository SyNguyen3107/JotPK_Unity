using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Left Panel References")]
    // Kéo object cha "ItemDisplay" vào đây để bật/tắt cả cụm
    public GameObject itemDisplayObj;
    // Kéo object con "Icon" vào đây để thay đổi hình ảnh
    public Image itemIconImage;

    public TextMeshProUGUI livesText;
    public TextMeshProUGUI coinsText;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        // Mặc định lúc đầu chưa nhặt item -> Ẩn khung item
        if (itemDisplayObj != null)
            itemDisplayObj.SetActive(false);

        // Cập nhật giá trị ban đầu (tránh hiện text mẫu)
        if (GameManager.Instance != null)
        {
            UpdateLives(GameManager.Instance.startLives);
            UpdateCoins(0); // Tạm thời để 0 vì chưa làm logic tiền
        }
    }

    // Hàm gọi khi nhặt Item
    public void UpdateItem(Sprite newItemSprite)
    {
        if (newItemSprite == null)
        {
            itemDisplayObj.SetActive(false);
        }
        else
        {
            itemDisplayObj.SetActive(true);
            itemIconImage.sprite = newItemSprite;
            // Giữ tỷ lệ ảnh gốc nếu item có hình dạng chữ nhật
            itemIconImage.preserveAspect = true;
        }
    }

    // Hàm gọi khi mất mạng / hồi sinh
    public void UpdateLives(int currentLives)
    {
        // Format hiển thị: "x 3"
        livesText.text = "x" + currentLives.ToString();
    }

    // Hàm gọi khi nhặt tiền
    public void UpdateCoins(int currentCoins)
    {
        coinsText.text = "x" + currentCoins.ToString();
    }
}