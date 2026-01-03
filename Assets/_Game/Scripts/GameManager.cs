using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Settings")]
    public int startLives = 3;
    public float respawnDelay = 2f; // (Biến này dùng cho logic cũ, giữ lại để tránh lỗi missing reference nếu muốn)

    [Header("Respawn Logic Settings")]
    public float deathAnimationDuration = 1f; // Thời gian diễn hoạt cảnh chết (Player vẫn hiện chân)
    public float deathDuration = 3f;          // Thời gian màn hình trống (Player biến mất hoàn toàn)
    public float invincibilityDuration = 3f;  // Thời gian bất tử khi sống lại

    [Header("Level Settings")]
    public float levelDuration = 180f; // Thời gian màn chơi

    [Header("Timer Colors")]
    public Color timerNormalColor = Color.green;
    public Color timerCriticalColor = Color.red;

    [Header("Drop System")]
    public List<PowerUpData> allowedDrops; // Kéo tất cả 11 item vào đây trong Inspector
    [Range(0f, 100f)] public float dropChance = 5f; // Tỷ lệ rơi (ví dụ 5%)

    [Header("Progress Settings")]
    public int totalAreasPassed = 0;

    [Header("Game State")]
    public int currentCoins = 0; // Biến lưu tiền hiện tại

    [Header("References")]
    public GameObject gameOverPanel;
    public GameObject playerObject;

    [Header("Smoke Bomb Settings")]
    public bool isSmokeBombActive = false; // Cờ kiểm tra trạng thái khói

    // State Variables
    private float currentTime;
    private bool isTimerRunning = false;
    private bool isWaitingForClear = false;
    private int currentLives;
    private bool isGameOver = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        currentLives = startLives;
        isGameOver = false;
        isWaitingForClear = false;

        // Setup Timer
        currentTime = levelDuration;
        isTimerRunning = true;
        if (UIManager.Instance != null) UIManager.Instance.SetTimerColor(timerNormalColor);

        // Auto-find Player
        if (playerObject == null) playerObject = GameObject.FindGameObjectWithTag("Player");

        // Init UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateLives(currentLives);
            UIManager.Instance.UpdateCoins(0);
            UIManager.Instance.UpdateAreaIndicator(totalAreasPassed);
            UIManager.Instance.UpdateTimer(currentTime, levelDuration);
        }

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (isGameOver)
        {
            if (Input.GetKeyDown(KeyCode.R)) RestartLevel();
            return;
        }

        // --- TIMER LOGIC ---
        if (isTimerRunning)
        {
            currentTime -= Time.deltaTime;

            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateTimer(currentTime, levelDuration);

                // Đổi màu khi còn 10%
                if (currentTime <= levelDuration / 10f)
                    UIManager.Instance.SetTimerColor(timerCriticalColor);
                else
                    UIManager.Instance.SetTimerColor(timerNormalColor);
            }

            if (currentTime <= 0)
            {
                currentTime = 0;
                HandleTimeUp();
            }
        }

        // --- WAITING CLEAR LOGIC ---
        if (isWaitingForClear)
        {
            if (GameObject.FindGameObjectsWithTag("Enemy").Length == 0)
            {
                isWaitingForClear = false;
                CompleteCurrentArea();
            }
        }
    }

    // --- GAMEPLAY EVENTS ---

    public void CompleteCurrentArea()
    {
        totalAreasPassed++;
        Debug.Log("AREA CLEARED! Total Areas: " + totalAreasPassed);
        if (UIManager.Instance != null) UIManager.Instance.UpdateAreaIndicator(totalAreasPassed);
        // Add logic to load next scene here
    }

    public void PlayerDied()
    {
        if (isGameOver) return;

        PlayerController pc = null;
        if (playerObject != null) pc = playerObject.GetComponent<PlayerController>();

        // Nếu đang bất tử thì bỏ qua
        if (pc != null && pc.IsInvincible()) return;

        currentLives--;
        if (UIManager.Instance != null) UIManager.Instance.UpdateLives(currentLives);

        if (currentLives > 0)
        {
            StartCoroutine(RespawnSequence());
        }
        else
        {
            // Xử lý Game Over ngay
            if (playerObject != null) playerObject.SetActive(false);
            GameOver();
        }
    }

    IEnumerator RespawnSequence()
    {
        Debug.Log("GIAI ĐOẠN 1: Dừng Game -> Xóa Quái -> Diễn Animation");

        // 1. Pause Timer & Spawner
        isTimerRunning = false;
        if (WaveSpawner.Instance != null) WaveSpawner.Instance.SetWavePaused(true);

        // 2. Xóa sạch quái NGAY LẬP TỨC
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies) Destroy(enemy);

        // 3. Trigger Animation & Khóa Player
        PlayerController pc = null;
        if (playerObject != null)
        {
            pc = playerObject.GetComponent<PlayerController>();
            if (pc != null) pc.TriggerDeathAnimation();
        }

        // 4. Chờ Animation chạy xong (Player vẫn hiện chân để diễn)
        yield return new WaitForSeconds(deathAnimationDuration);

        Debug.Log("GIAI ĐOẠN 2: Màn hình trống");

        // 5. Ẩn Player hoàn toàn
        if (playerObject != null) playerObject.SetActive(false);

        // Chờ 3s (Death Duration)
        yield return new WaitForSeconds(deathDuration);

        Debug.Log("GIAI ĐOẠN 3: Hồi sinh & Nhấp nháy");

        // 6. Reset Player về giữa & Hiện lại
        if (playerObject != null)
        {
            playerObject.transform.position = Vector3.zero;
            playerObject.SetActive(true);
            if (pc != null) pc.ResetState(); // Reset Animation & Physics
        }

        // 7. Bật bất tử (Nhấp nháy)
        if (pc != null) pc.TriggerRespawnInvincibility(invincibilityDuration);

        // Chờ hết thời gian bất tử (3s)
        yield return new WaitForSeconds(invincibilityDuration);

        Debug.Log("GIAI ĐOẠN 4: Tiếp tục Game");

        // 8. Resume Timer & Spawner
        isTimerRunning = true;
        if (WaveSpawner.Instance != null) WaveSpawner.Instance.SetWavePaused(false);
    }

    void HandleTimeUp()
    {
        isTimerRunning = false;
        if (WaveSpawner.Instance != null) WaveSpawner.Instance.StopSpawning();
        isWaitingForClear = true;
        CheckSpikeballEndCondition();
    }
    void CheckSpikeballEndCondition()
    {
        // 1. Tìm tất cả Enemy đang sống
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        if (enemies.Length == 0) return;

        bool allAreSpikeballs = true;
        List<Spikeball> spikeballList = new List<Spikeball>();

        // 2. Duyệt qua để xem có con nào KHÔNG PHẢI Spikeball không
        foreach (GameObject enemyObj in enemies)
        {
            Spikeball sb = enemyObj.GetComponent<Spikeball>();
            if (sb == null)
            {
                // Phát hiện một kẻ không phải Spikeball -> Điều kiện sai ngay lập tức
                allAreSpikeballs = false;
                break;
            }
            else
            {
                spikeballList.Add(sb);
            }
        }

        // 3. Nếu tất cả đúng là Spikeball -> Giảm máu
        if (allAreSpikeballs)
        {
            Debug.Log("Time Up! Chỉ còn Spikeball -> Giảm máu tất cả về 1.");
            foreach (Spikeball sb in spikeballList)
            {
                sb.Weaken();
            }
        }
    }
    void GameOver()
    {
        isGameOver = true;
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    public PowerUpData GetRandomDrop()
    {
        if (allowedDrops.Count == 0) return null;
        return allowedDrops[Random.Range(0, allowedDrops.Count)];
    }
    public void AddCoin(int amount)
    {
        currentCoins += amount;
        Debug.Log("Nhặt được tiền: " + amount + ". Tổng: " + currentCoins);

        // Cập nhật UI
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateCoins(currentCoins);
    }

    public void AddLife(int amount)
    {
        currentLives += amount;
        Debug.Log("Thêm mạng! Tổng: " + currentLives);

        // Cập nhật UI
        if (UIManager.Instance != null)
            UIManager.Instance.UpdateLives(currentLives);
    }
    public void ActivateGlobalStun(float duration)
    {
        StartCoroutine(GlobalStunRoutine(duration));
    }
    IEnumerator GlobalStunRoutine(float duration)
    {
        isSmokeBombActive = true;

        // 1. Tìm tất cả quái ĐANG CÓ và làm choáng ngay lập tức
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var e in enemies)
        {
            var enemyScript = e.GetComponent<Enemy>();
            if (enemyScript != null) enemyScript.SetStunState(true);
        }

        // 2. Chờ hết thời gian tác dụng
        yield return new WaitForSeconds(duration);

        // 3. Kết thúc hiệu ứng
        isSmokeBombActive = false;

        // 4. Tìm lại tất cả quái (cả cũ lẫn mới sinh ra trong lúc chờ) và giải phóng
        enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var e in enemies)
        {
            var enemyScript = e.GetComponent<Enemy>();
            if (enemyScript != null) enemyScript.SetStunState(false);
        }

        Debug.Log("Hết khói! Quái hoạt động lại.");
    }
}