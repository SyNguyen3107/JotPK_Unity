using UnityEngine;

public abstract class BossManager : MonoBehaviour
{
    #region Configuration & Settings
    [Header("Base Boss References")]
    public BossController activeBossScript;
    public Transform playerRespawnPoint;
    public BoxCollider2D playerZoneLimit;

    [Header("Base Victory Setup")]
    public GameObject lootPrefab;
    public Transform lootSpawnPoint;

    [Header("Base Audio")]
    public AudioClip bossMusic;
    #endregion

    #region Runtime Variables
    protected bool isBossActive = false;
    protected bool victoryTriggered = false;
    #endregion

    #region Unity Lifecycle
    protected virtual void Update()
    {
        if (isBossActive && activeBossScript != null)
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateBossHealth(activeBossScript.currentHealth, activeBossScript.maxHealth);
            }

            if (activeBossScript.currentHealth <= 0)
            {
                HandleBossVictory();
            }
        }
    }
    #endregion

    #region Core Logic
    public virtual void ActivateBossLevel()
    {
        ResetBossState();

        if (activeBossScript == null)
        {
            activeBossScript = GetComponentInChildren<BossController>();
            if (activeBossScript == null)
            {
                return;
            }
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetTimerRunning(false);

            if (GameManager.Instance.musicSource != null)
                GameManager.Instance.musicSource.pitch = 1f;

            if (playerRespawnPoint != null)
            {
                GameManager.Instance.useOverrideRespawn = true;
                GameManager.Instance.overrideRespawnPosition = playerRespawnPoint.position;
            }
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ToggleBossUI(true);
            UIManager.Instance.ToggleHUD(true);

            if (activeBossScript != null)
            {
                UIManager.Instance.UpdateBossHealth(activeBossScript.currentHealth, activeBossScript.maxHealth);
            }
        }

        if (GameManager.Instance != null && bossMusic != null)
        {
            if (GameManager.Instance.musicSource != null)
            {
                GameManager.Instance.musicSource.Stop();
                GameManager.Instance.musicSource.clip = bossMusic;
                GameManager.Instance.musicSource.Play();
            }
        }

        if (playerZoneLimit != null)
        {
            Vector2 zoneMin = playerZoneLimit.bounds.min;
            Vector2 zoneMax = playerZoneLimit.bounds.max;

            if (GameManager.Instance != null && GameManager.Instance.playerObject != null)
            {
                PlayerController pc = GameManager.Instance.playerObject.GetComponent<PlayerController>();
                if (pc != null) pc.SetMapBounds(zoneMin, zoneMax);
            }
        }

        if (activeBossScript != null)
        {
            isBossActive = true;
            activeBossScript.gameObject.SetActive(true);
            activeBossScript.StartBossFight();
        }
    }

    private void ResetBossState()
    {
        isBossActive = false;
        victoryTriggered = false;

        if (activeBossScript != null)
        {
            activeBossScript.gameObject.SetActive(true);
        }
    }

    protected virtual void HandleBossVictory()
    {
        if (victoryTriggered) return;
        victoryTriggered = true;
        isBossActive = false;

        if (GameManager.Instance != null)
        {
            int totalAreasPassed = ++GameManager.Instance.totalAreasPassed;
            if (UIManager.Instance != null) UIManager.Instance.UpdateAreaIndicator(totalAreasPassed);
        }

        if (GameManager.Instance != null && GameManager.Instance.musicSource != null)
        {
            GameManager.Instance.musicSource.Stop();
        }

        if (UIManager.Instance != null) UIManager.Instance.ToggleBossUI(false);

        if (lootPrefab != null)
        {
            Vector3 spawnPos = (activeBossScript != null) ? activeBossScript.transform.position : transform.position;
            if (lootSpawnPoint != null) spawnPos = lootSpawnPoint.position;
            Instantiate(lootPrefab, spawnPos, Quaternion.identity);
        }

        if (GameManager.Instance != null && GameManager.Instance.playerObject != null)
        {
            PlayerController pc = GameManager.Instance.playerObject.GetComponent<PlayerController>();
            if (pc != null) pc.SetMapBounds(new Vector2(-1000, -1000), new Vector2(1000, 1000));
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.useOverrideRespawn = false;
        }
    }
    #endregion

    #region Abstract Methods
    public abstract void OnLootCollected(Sprite itemSprite);
    #endregion
}