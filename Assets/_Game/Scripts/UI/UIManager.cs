using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    #region Configuration & Settings
    [Header("--- LEFT PANEL (Stats) ---")]
    public GameObject itemDisplayObj;
    public Image itemIconImage;
    public TextMeshProUGUI livesText;
    public TextMeshProUGUI coinsText;
    public Image slot1Icon;
    public Image slot2Icon;
    public Image slot3Icon;

    [Header("--- TOP PANEL (Timer) ---")]
    public Image timerBarFill;

    [Header("--- RIGHT PANEL (Area Indicator) ---")]
    public List<Image> areaIcons;
    public TextMeshProUGUI levelNameText;
    public CanvasGroup levelNameCanvasGroup;

    [Header("--- BOTTOM PANEL (BOSS HP)")]
    public Image bossHPFill;
    public GameObject bossHealthBarObject;
    public TextMeshProUGUI bossNameText;

    [Header("Pause Menu")]
    public GameObject pauseMenuPanel;
    public GameObject gameOverPanel;
    [Header("Pause Menu Buff Display")]
    public Transform buffListContainer;
    public GameObject buffSlotPrefab;
    [Header("Pause Menu Panels")]
    public PlayerStatsUI playerStatsUI;

    [Header("Tutorial UI")]
    public GameObject tutorialPanel;
    public CanvasGroup tutorialCanvasGroup;

    [Header("World UI")]
    public GameObject exitArrowObject;
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        UpdateUpgradeIcons();
        UpdateLives(GameManager.Instance != null ? GameManager.Instance.currentLives : 0);
        UpdateCoins(GameManager.Instance != null ? GameManager.Instance.currentCoins : 0);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P) || Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TogglePause();
            }
        }
    }
    #endregion

    #region Core UI Updates
    public void UpdateLives(int currentLives)
    {
        if (livesText != null) livesText.text = "x " + currentLives;
    }

    public void UpdateCoins(int currentCoins)
    {
        if (coinsText != null) coinsText.text = "x " + currentCoins;
    }

    public void UpdateTimer(float currentTime, float maxTime)
    {
        if (timerBarFill != null)
        {
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

    public void ToggleHUD(bool isActive)
    {
        GetComponent<Canvas>().enabled = isActive;
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
    public void ShowLevelName(string name)
    {
        if (levelNameText != null)
        {
            levelNameText.text = name;
            StartCoroutine(LevelNameFadeRoutine());
        }
    }
    public void ToggleExitArrow(bool isActive)
    {
        if (exitArrowObject != null)
        {
            exitArrowObject.SetActive(isActive);
        }
    }
    #endregion

    #region Inventory & Upgrades UI
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
            img.color = Color.white;
            img.gameObject.SetActive(true);
        }
        else
        {
            img.color = new Color(1, 1, 1, 0);
        }
    }
    #endregion

    #region Boss UI
    public void ToggleBossUI(bool isActive)
    {
        if (bossHealthBarObject != null)
        {
            bossHealthBarObject.SetActive(isActive);
        }
        if (bossNameText != null)
        {
            bossNameText.gameObject.SetActive(isActive);
        }
    }

    public void UpdateBossHealth(float current, float max)
    {
        if (bossHPFill != null)
        {
            bossHPFill.fillAmount = current / max;
        }
    }
    public void SetBossName(string name)
    {
        if (bossNameText != null)
        {
            bossNameText.text = name;
        }
        else
        {
            int currentLevelIndex = GameManager.Instance != null ? GameManager.Instance.currentLevelIndex : 0;
            if (currentLevelIndex == 4 || currentLevelIndex == 8)
            {
                bossNameText.text = "JUAREZ The Deadeye";
            }
            else if (currentLevelIndex == 12)
            {
                bossNameText.text = "FECTOR The Immortal";
            }
        }
    }
    #endregion

    #region Menu & Popup Handling
    public void ShowPauseMenu(bool isVisible)
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(isVisible);
            if (isVisible)
                UpdatePauseMenuBuffs();
            if (playerStatsUI != null) 
                playerStatsUI.UpdateStats();
        }
    }
    void UpdatePauseMenuBuffs()
    {
        if (buffListContainer == null || buffSlotPrefab == null) return;

        foreach (Transform child in buffListContainer)
        {
            Destroy(child.gameObject);
        }

        if (GameManager.Instance != null && GameManager.Instance.playerObject != null)
        {
            PlayerController pc = GameManager.Instance.playerObject.GetComponent<PlayerController>();
            if (pc != null)
            {
                var activeBuffs = pc.GetActiveBuffs();

                foreach (var buff in activeBuffs)
                {
                    PowerUpData data = GameManager.Instance.allowedDrops.Find(x => x.type == buff.type);

                    if (data != null)
                    {
                        GameObject slotObj = Instantiate(buffSlotPrefab, buffListContainer);
                        BuffSlotUI slotScript = slotObj.GetComponent<BuffSlotUI>();
                        if (slotScript != null)
                        {
                            slotScript.Setup(data, buff.remainingTime, data.duration);
                        }
                    }
                }
            }
        }
    }
    public void ShowTutorial(bool show)
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(show);
        if (tutorialCanvasGroup != null) tutorialCanvasGroup.alpha = 1f;
    }
    #endregion

    #region Event Handlers
    public void OnResumeButton()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TogglePause();
        }
    }

    public void OnRestartButton()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RetryLevel();
        }
    }

    public void OnExitButton()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ExitToMainMenu();
        }
    }
    #endregion

    #region Coroutines
    public IEnumerator FadeOutTutorial(float duration)
    {
        if (tutorialCanvasGroup == null) yield break;

        float startAlpha = tutorialCanvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            tutorialCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
            yield return null;
        }

        tutorialCanvasGroup.alpha = 0f;
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
    }
    IEnumerator LevelNameFadeRoutine()
    {
        if (levelNameCanvasGroup == null) yield break;

        // 1. Hiện rõ (Fade In)
        float duration = 0.5f;
        float elapsed = 0f;
        levelNameCanvasGroup.alpha = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            levelNameCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }
        levelNameCanvasGroup.alpha = 1f;

        // 2. Giữ nguyên trong 3 giây
        yield return new WaitForSeconds(3f);

        // 3. Mờ dần (Fade Out)
        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            levelNameCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            yield return null;
        }
        levelNameCanvasGroup.alpha = 0f;
    }
    #endregion
}