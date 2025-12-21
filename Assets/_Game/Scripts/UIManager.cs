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

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Xóa hàm Start và Update cũ liên quan đến Timer

    // --- CÁC HÀM CẬP NHẬT UI (PUBLIC) ---

    public void UpdateTimer(float currentTime, float maxTime)
    {
        if (timerBarFill != null)
        {
            // Cập nhật độ dài thanh
            timerBarFill.fillAmount = currentTime / maxTime;
        }
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
}