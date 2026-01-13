using UnityEngine;

public abstract class BossManager : MonoBehaviour
{
    [Header("Base Boss References")]
    // Dùng BossController để chấp nhận cả Cowboy lẫn Fector
    public BossController activeBossScript;

    public Transform playerRespawnPoint;
    public BoxCollider2D playerZoneLimit;

    [Header("Base Victory Setup")]
    public GameObject lootPrefab;
    public Transform lootSpawnPoint;

    [Header("Base Audio")]
    public AudioClip bossMusic;

    protected bool isBossActive = false;
    protected bool victoryTriggered = false;

    // --- LOGIC CHUNG CHO MỌI BOSS ---
    public virtual void ActivateBossLevel()
    {
        Debug.Log("ACTIVATING BOSS LEVEL (BASE)...");

        // 1. Setup UI & Timer
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetTimerRunning(false);
            if (GameManager.Instance.musicSource != null) GameManager.Instance.musicSource.pitch = 1f;

            if (playerRespawnPoint != null)
                GameManager.Instance.overrideRespawnPosition = playerRespawnPoint.position;
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ToggleBossUI(true);
            UIManager.Instance.ToggleHUD(true);
        }

        // 2. Audio
        if (GameManager.Instance != null && bossMusic != null)
        {
            GameManager.Instance.musicSource.Stop();
            GameManager.Instance.musicSource.clip = bossMusic;
            GameManager.Instance.musicSource.Play();
        }

        // 3. Map Bounds
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

        // 4. Kích hoạt Boss (Đa hình)
        if (activeBossScript != null)
        {
            isBossActive = true;
            activeBossScript.StartBossFight();
        }
    }

    protected virtual void Update()
    {
        if (isBossActive && activeBossScript != null && UIManager.Instance != null)
        {
            UIManager.Instance.UpdateBossHealth(activeBossScript.currentHealth, activeBossScript.maxHealth);
            if (activeBossScript.currentHealth <= 0)
            {
                HandleBossVictory();
            }
        }
    }

    // Mỗi boss có cách xử lý thắng khác nhau (Cowboy có cầu, Fector thì không)
    protected virtual void HandleBossVictory()
    {
        if (victoryTriggered) return;
        victoryTriggered = true;
        isBossActive = false;

        if (GameManager.Instance != null && GameManager.Instance.musicSource != null)
        {
            GameManager.Instance.musicSource.Stop();
        }
        if (UIManager.Instance != null) UIManager.Instance.ToggleBossUI(false);

        // Spawn Loot chung
        if (lootPrefab != null)
        {
            Vector3 spawnPos = (activeBossScript != null) ? activeBossScript.transform.position : transform.position;
            if (lootSpawnPoint != null) spawnPos = lootSpawnPoint.position;
            Instantiate(lootPrefab, spawnPos, Quaternion.identity);
        }

        // Mở map bounds
        if (GameManager.Instance != null && GameManager.Instance.playerObject != null)
        {
            PlayerController pc = GameManager.Instance.playerObject.GetComponent<PlayerController>();
            if (pc != null) pc.SetMapBounds(new Vector2(-1000, -1000), new Vector2(1000, 1000));
        }
    }

    // Abstract Method: Mỗi boss quản lý việc nhặt đồ khác nhau (Gopher vs Pose)
    public abstract void OnLootCollected(Sprite itemSprite);
}