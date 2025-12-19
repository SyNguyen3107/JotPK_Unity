using UnityEngine;
using UnityEngine.UI;
using TMPro; // Thư viện để dùng TextMeshPro
using System.Collections.Generic; // Thư viện để dùng List

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("--- LEFT PANEL (Stats) ---")]
    [Tooltip("Object cha chứa cả khung và icon item")]
    public GameObject itemDisplayObj;
    [Tooltip("Image hiển thị icon vật phẩm nhặt được")]
    public Image itemIconImage;

    public TextMeshProUGUI livesText;
    public TextMeshProUGUI coinsText;

    [Header("--- TOP PANEL (Timer) ---")]
    public Image timerBarFill; // Thanh màu xanh lá (Image Type: Filled)
    public float levelDuration = 180f; // Thời gian màn chơi (giây)

    private float timeRemaining;
    private bool isTimerRunning = false;

    [Header("--- RIGHT PANEL (Area Indicator) ---")]
    [Tooltip("Kéo các AreaIcon_0, AreaIcon_1... từ Hierarchy vào danh sách này theo đúng thứ tự")]
    public List<Image> areaIcons;

    void Awake()
    {
        // Setup Singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 1. Cài đặt trạng thái ban đầu cho Left Panel
        if (itemDisplayObj != null)
            itemDisplayObj.SetActive(false); // Ẩn khung item vì chưa nhặt gì

        // (Lưu ý: Lives và Coins sẽ được GameManager gọi Update ngay khi Start nên ko cần set cứng ở đây)

        // 2. Cài đặt Timer
        timeRemaining = levelDuration;
        isTimerRunning = true;

        // 3. Cài đặt Right Panel
        // Mặc định ẩn hết các icon Area (hoặc làm mờ) lúc bắt đầu
        UpdateAreaIndicator(0);
    }

    void Update()
    {
        // --- LOGIC ĐỒNG HỒ ---
        if (isTimerRunning)
        {
            if (timeRemaining > 0)
            {
                timeRemaining -= Time.deltaTime;

                // Cập nhật thanh Fill (Tụt dần từ 1 về 0)
                if (timerBarFill != null)
                {
                    timerBarFill.fillAmount = timeRemaining / levelDuration;
                }
            }
            else
            {
                // Hết giờ
                timeRemaining = 0;
                isTimerRunning = false;
                Debug.Log("Hết giờ! (Level Complete Logic sẽ thêm sau)");

                // Ví dụ: GameManager.Instance.LevelComplete();
            }
        }
    }

    // =========================================================
    // CÁC HÀM CÔNG KHAI (PUBLIC METHODS) ĐỂ GỌI TỪ NƠI KHÁC
    // =========================================================

    // --- LEFT PANEL ---

    public void UpdateLives(int currentLives)
    {
        if (livesText != null)
            livesText.text = "x " + currentLives.ToString();
    }

    public void UpdateCoins(int currentCoins)
    {
        if (coinsText != null)
            coinsText.text = "x " + currentCoins.ToString();
    }

    public void UpdateItem(Sprite newItemSprite)
    {
        if (newItemSprite == null)
        {
            // Không có item -> Ẩn khung
            if (itemDisplayObj != null) itemDisplayObj.SetActive(false);
        }
        else
        {
            // Có item -> Hiện khung và gán ảnh
            if (itemDisplayObj != null) itemDisplayObj.SetActive(true);

            if (itemIconImage != null)
            {
                itemIconImage.sprite = newItemSprite;
                itemIconImage.preserveAspect = true; // Giữ tỉ lệ ảnh gốc
            }
        }
    }

    // --- RIGHT PANEL ---
    public void UpdateAreaIndicator(int areasPassed)
    {
        if (areaIcons == null) return;

        for (int i = 0; i < areaIcons.Count; i++)
        {
            if (areaIcons[i] == null) continue;

            // Logic hiển thị:
            if (i < areasPassed)
            {
                // Đã vượt qua -> Hiện lên
                areaIcons[i].gameObject.SetActive(true);
            }
            else
            {
                // Chưa tới hoặc đang chơi -> Ẩn đi hoàn toàn
                areaIcons[i].gameObject.SetActive(false);
            }
        }
    }
}