using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BuffSlotUI : MonoBehaviour
{
    [Header("UI References")]
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descText;

    public Image timerRingFillImage;
    // ---------------------

    // Cập nhật hàm Setup để nhận thêm totalDuration
    public void Setup(PowerUpData data, float remainingTime, float totalDuration)
    {
        if (data == null) return;

        if (iconImage != null) iconImage.sprite = data.icon;
        if (nameText != null) nameText.text = data.itemName;
        if (descText != null) descText.text = data.description;

        if (timerRingFillImage != null)
        {
            float fillPercentage = (totalDuration > 0) ? (remainingTime / totalDuration) : 0f;

            timerRingFillImage.fillAmount = Mathf.Clamp01(fillPercentage);
        }
    }
}