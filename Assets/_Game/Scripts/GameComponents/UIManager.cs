using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("--- LEFT PANEL (Stats) ---")]
    public GameObject itemDisplayObj;
    public Image itemIconImage;
    public TextMeshProUGUI livesText;
    public TextMeshProUGUI coinsText;

    [Header("--- TOP PANEL (Timer) ---")]
    public Image timerBarFill;
    // Đã xóa biến levelDuration và timeRemaining ở đây

    [Header("--- RIGHT PANEL (Area Indicator) ---")]
    public List<Image> areaIcons;


    public GameObject exitArrowObject;
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // --- CÁC HÀM CẬP NHẬT UI (PUBLIC) ---

    public void UpdateTimer(float currentTime, float maxTime)
    {
        if (timerBarFill != null)
        {
            // Cập nhật độ dài thanh
            timerBarFill.fillAmount = currentTime / maxTime;
        }
    }
    public void ToggleHUD(bool isActive)
    {
        // Giả sử toàn bộ UI của bạn nằm trong 1 Panel chính hoặc Canvas
        // Nếu chưa có biến reference tới Panel chính, bạn có thể gọi gameObject.SetActive
        // hoặc duyệt qua các thành phần con.

        // Cách đơn giản nhất: Ẩn/Hiện canvas hiện tại (nếu UIManager gắn trên Canvas)
        GetComponent<Canvas>().enabled = isActive;

        // Hoặc nếu UIManager quản lý các panel con:
        // transform.GetChild(0).gameObject.SetActive(isActive); // Ví dụ
    }
    public void SetTimerColor(Color color)
    {
        if (timerBarFill != null)
        {
            timerBarFill.color = color;
        }
    }

    public void UpdateLives(int currentLives)
    {
        if (livesText != null) livesText.text = "x " + currentLives;
    }

    public void UpdateCoins(int currentCoins)
    {
        if (coinsText != null) coinsText.text = "x " + currentCoins;
    }

    public void UpdateItem(Sprite newItemSprite)
    {
        if (newItemSprite == null)
        {
            if (itemDisplayObj != null) itemDisplayObj.SetActive(false);
        }
        else
        {
            if (itemDisplayObj != null) itemDisplayObj.SetActive(true);
            if (itemIconImage != null)
            {
                itemIconImage.sprite = newItemSprite;
                itemIconImage.preserveAspect = true;
            }
        }
    }

    public void UpdateAreaIndicator(int areasPassed)
    {
        if (areaIcons == null) return;
        for (int i = 0; i < areaIcons.Count; i++)
        {
            if (areaIcons[i] == null) continue;
            if (i < areasPassed) areaIcons[i].gameObject.SetActive(true);
            else areaIcons[i].gameObject.SetActive(false);
        }
    }
    public void ToggleExitArrow(bool isActive)
    {
        if (exitArrowObject != null)
        {
            exitArrowObject.SetActive(isActive);

            // Nếu muốn thêm hiệu ứng nhấp nháy (Animation), bạn có thể xử lý ở đây
            // Hoặc đơn giản là gắn Animator vào object ExitArrow trên Unity
        }
    }
}