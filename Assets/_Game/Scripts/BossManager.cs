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
        Debug.Log($"[BossManager] ACTIVATING BOSS LEVEL: {gameObject.name}");

        // 0. Reset trạng thái cũ (Quan trọng khi oad Game)
        ResetBossState();

        // 1. Tự động tìm BossController nếu chưa gán (Fix lỗi quên kéo thả)
        if (activeBossScript == null)
        {
            activeBossScript = GetComponentInChildren<BossController>();
            if (activeBossScript == null)
            {
                Debug.LogError("[BossManager] LỖI: Không tìm thấy BossController nào trong Map!");
                return;
            }
        }

        // 2. Setup GameManager & Respawn
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetTimerRunning(false); // Tắt đồng hồ đếm ngược

            // Reset Pitch nhạc về bình thường
            if (GameManager.Instance.musicSource != null)
                GameManager.Instance.musicSource.pitch = 1f;

            // Đặt lại điểm hồi sinh cho Player (để nếu chết thì spawn ở cửa phòng boss)
            if (playerRespawnPoint != null)
                GameManager.Instance.overrideRespawnPosition = playerRespawnPoint.position;
        }

        // 3. Setup UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ToggleBossUI(true);
            UIManager.Instance.ToggleHUD(true);

            // Cập nhật máu ngay lập tức để tránh thanh máu bị rỗng lúc đầu
            if (activeBossScript != null)
            {
                UIManager.Instance.UpdateBossHealth(activeBossScript.currentHealth, activeBossScript.maxHealth);
            }
        }

        // 4. Audio
        if (GameManager.Instance != null && bossMusic != null)
        {
            if (GameManager.Instance.musicSource != null)
            {
                GameManager.Instance.musicSource.Stop();
                GameManager.Instance.musicSource.clip = bossMusic;
                GameManager.Instance.musicSource.Play();
            }
        }

        // 5. Map Bounds (Khóa Camera/Player vào vùng đấu boss)
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

        // 6. Kích hoạt Boss (Đa hình)
        if (activeBossScript != null)
        {
            isBossActive = true;
            // Đảm bảo Boss Object đang bật
            activeBossScript.gameObject.SetActive(true);
            activeBossScript.StartBossFight();
        }
    }

    protected virtual void Update()
    {
        if (isBossActive && activeBossScript != null)
        {
            // Chỉ cập nhật UI nếu có instance
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateBossHealth(activeBossScript.currentHealth, activeBossScript.maxHealth);
            }

            // Kiểm tra điều kiện thắng
            if (activeBossScript.currentHealth <= 0)
            {
                HandleBossVictory();
            }
        }
    }

    // Reset các cờ để đảm bảo boss fight bắt đầu sạch sẽ
    private void ResetBossState()
    {
        isBossActive = false;
        victoryTriggered = false;

        // Nếu Boss đã từng bị tắt, bật lại nó
        if (activeBossScript != null)
        {
            activeBossScript.gameObject.SetActive(true);
            // Có thể cần reset máu boss ở đây nếu muốn:
            // activeBossScript.currentHealth = activeBossScript.maxHealth;
        }
    }

    // Mỗi boss có cách xử lý thắng khác nhau (Cowboy có cầu, Fector thì không)
    protected virtual void HandleBossVictory()
    {
        if (victoryTriggered) return;
        victoryTriggered = true;
        isBossActive = false;
        int totalAreasPassed = ++GameManager.Instance.totalAreasPassed;
        if (UIManager.Instance != null) UIManager.Instance.UpdateAreaIndicator(totalAreasPassed);
        Debug.Log("[BossManager] VICTORY!");

        // Dừng nhạc
        if (GameManager.Instance != null && GameManager.Instance.musicSource != null)
        {
            GameManager.Instance.musicSource.Stop();
        }

        // Tắt thanh máu Boss
        if (UIManager.Instance != null) UIManager.Instance.ToggleBossUI(false);

        // Spawn Loot chung
        if (lootPrefab != null)
        {
            Vector3 spawnPos = (activeBossScript != null) ? activeBossScript.transform.position : transform.position;
            if (lootSpawnPoint != null) spawnPos = lootSpawnPoint.position;
            Instantiate(lootPrefab, spawnPos, Quaternion.identity);
        }

        // Mở map bounds (Cho player đi tự do)
        if (GameManager.Instance != null && GameManager.Instance.playerObject != null)
        {
            PlayerController pc = GameManager.Instance.playerObject.GetComponent<PlayerController>();
            if (pc != null) pc.SetMapBounds(new Vector2(-1000, -1000), new Vector2(1000, 1000));
        }

        // Reset Override Respawn (Để màn sau spawn đúng chỗ)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.useOverrideRespawn = false;
        }
    }

    // Abstract Method: Mỗi boss quản lý việc nhặt đồ khác nhau (Gopher vs Pose)
    public abstract void OnLootCollected(Sprite itemSprite);
}