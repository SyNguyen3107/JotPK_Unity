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
    public Image slot1Icon; // Icon Giày (Slot 1)
    public Image slot2Icon; // Icon Súng (Slot 2)
    public Image slot3Icon; // Icon Đạn (Slot 3)

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
    public void Start()
    {
        UpdateUpgradeIcons();
        UpdateLives(GameManager.Instance != null ? GameManager.Instance.currentLives : 0);
        UpdateCoins(GameManager.Instance != null ? GameManager.Instance.currentCoins : 0);
    }

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
        GetComponent<Canvas>().enabled = isActive;
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
        }
    }
    public void UpdateUpgradeIcons()
    {
        if (UpgradeManager.Instance == null) return;

        UpdateSingleIcon(slot1Icon, UpgradeManager.Instance.GetPurchasedIcon(1));
        UpdateSingleIcon(slot2Icon, UpgradeManager.Instance.GetPurchasedIcon(2));
        UpdateSingleIcon(slot3Icon, UpgradeManager.Instance.GetPurchasedIcon(3));
    }

    void UpdateSingleIcon(Image img, Sprite icon)
    {
        if (img == null) return;

        if (icon != null)
        {
            img.sprite = icon;
            img.color = Color.white; // Hiện rõ
            img.gameObject.SetActive(true);
        }
        else
        {
            img.color = new Color(1, 1, 1, 0); // Trong suốt hoặc ẩn đi
            // img.gameObject.SetActive(false); // Hoặc tắt hẳn
        }
    }
}