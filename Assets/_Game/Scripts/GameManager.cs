using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Settings")]
    public int startLives = 3;
    public float respawnDelay = 2f;

    [Header("Respawn Logic Settings")]
    public float deathAnimationDuration = 1f;
    public float deathDuration = 3f;
    public float invincibilityDuration = 3f;

    [Header("Level Settings")]
    public float levelDuration = 180f;

    [Header("Timer Colors")]
    public Color timerNormalColor = Color.green;
    public Color timerCriticalColor = Color.red;

    [Header("Drop System")]
    public List<PowerUpData> allowedDrops;
    [Range(0f, 100f)] public float dropChance = 5f;

    [Header("Progress Settings")]
    public int totalAreasPassed = 0;

    [Header("Game State")]
    public int currentCoins = 0;

    [Header("References")]
    public GameObject gameOverPanel;
    public GameObject playerObject;

    [Header("Smoke Bomb Settings")]
    public bool isSmokeBombActive = false;

    [Header("Audio Settings")]
    public AudioSource musicSource;

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

        currentTime = levelDuration;
        isTimerRunning = true;
        if (UIManager.Instance != null) UIManager.Instance.SetTimerColor(timerNormalColor);

        if (playerObject == null) playerObject = GameObject.FindGameObjectWithTag("Player");

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateLives(currentLives);
            UIManager.Instance.UpdateCoins(0);
            UIManager.Instance.UpdateAreaIndicator(totalAreasPassed);
            UIManager.Instance.UpdateTimer(currentTime, levelDuration);
        }

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        Time.timeScale = 1f;

        if (musicSource != null)
        {
            musicSource.loop = true;
            musicSource.Play();
        }
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
            int activeEnemyCount = GameObject.FindGameObjectsWithTag("Enemy").Length;
            int pendingEnemyCount = 0;
            if (WaveSpawner.Instance != null)
            {
                pendingEnemyCount = WaveSpawner.Instance.GetPendingEnemyCount();
            }

            if (activeEnemyCount == 0 && pendingEnemyCount == 0)
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
    }

    public void PlayerDied()
    {
        if (isGameOver) return;

        PlayerController pc = null;
        if (playerObject != null) pc = playerObject.GetComponent<PlayerController>();

        if (pc != null && pc.IsInvincible()) return;

        currentLives--;
        if (UIManager.Instance != null) UIManager.Instance.UpdateLives(currentLives);

        if (currentLives > 0)
        {
            StartCoroutine(RespawnSequence());
        }
        else
        {
            if (playerObject != null) playerObject.SetActive(false);
            if (musicSource != null) musicSource.Stop();
            GameOver();
        }
    }

    IEnumerator RespawnSequence()
    {
        Debug.Log("GIAI ĐOẠN 1: Dừng Game -> Lưu & Xóa Quái -> Diễn Animation");

        isTimerRunning = false;
        if (musicSource != null) musicSource.Stop();

        if (WaveSpawner.Instance != null) WaveSpawner.Instance.OnPlayerDied();

        PlayerController pc = null;
        if (playerObject != null)
        {
            pc = playerObject.GetComponent<PlayerController>();
            if (pc != null) pc.TriggerDeathAnimation();
        }

        yield return new WaitForSeconds(deathAnimationDuration);

        Debug.Log("GIAI ĐOẠN 2: Màn hình trống");

        if (playerObject != null) playerObject.SetActive(false);

        yield return new WaitForSeconds(deathDuration);

        Debug.Log("GIAI ĐOẠN 3: Hồi sinh & Nhấp nháy");

        if (playerObject != null)
        {
            playerObject.transform.position = Vector3.zero;
            playerObject.SetActive(true);
            if (pc != null) pc.ResetState();
        }

        if (pc != null) pc.TriggerRespawnInvincibility(invincibilityDuration);

        yield return new WaitForSeconds(invincibilityDuration);

        // --- TRẢ QUÁI VỀ ---
        if (WaveSpawner.Instance != null) WaveSpawner.Instance.OnPlayerRespawned();

        Debug.Log("GIAI ĐOẠN 4: Kiểm tra trạng thái Timer");

        // --- FIX LOGIC TẠI ĐÂY ---
        // Chỉ bật lại Timer nếu thời gian VẪN CÒN
        if (currentTime > 0)
        {
            isTimerRunning = true;
        }
        else
        {
            // Nếu đã hết giờ, giữ nguyên trạng thái TimeUp
            isTimerRunning = false;
            isWaitingForClear = true;
        }

        if (musicSource != null) musicSource.Play();
    }

    void HandleTimeUp()
    {
        // FIX: Chặn việc gọi hàm này nhiều lần
        if (isWaitingForClear) return;

        Debug.Log("TIME UP! Trigger Sudden Death Mode");
        isTimerRunning = false;

        // Lệnh StopSpawning này có chứa StopAllCoroutines, 
        // nên tuyệt đối không được gọi lại khi đang Respawn quái
        if (WaveSpawner.Instance != null) WaveSpawner.Instance.StopSpawning();

        isWaitingForClear = true;
        CheckSpikeballEndCondition();
    }

    void CheckSpikeballEndCondition()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        if (enemies.Length == 0) return;

        bool allAreSpikeballs = true;
        List<Spikeball> spikeballList = new List<Spikeball>();

        foreach (GameObject enemyObj in enemies)
        {
            Spikeball sb = enemyObj.GetComponent<Spikeball>();
            if (sb == null)
            {
                allAreSpikeballs = false;
                break;
            }
            else
            {
                spikeballList.Add(sb);
            }
        }

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

    // ... (Các hàm PowerUp, AddCoin, AddLife, GlobalStun giữ nguyên) ...
    public PowerUpData GetRandomDrop()
    {
        if (allowedDrops.Count == 0) return null;
        return allowedDrops[Random.Range(0, allowedDrops.Count)];
    }

    public void AddCoin(int amount)
    {
        currentCoins += amount;
        if (UIManager.Instance != null) UIManager.Instance.UpdateCoins(currentCoins);
    }

    public void AddLife(int amount)
    {
        currentLives += amount;
        if (UIManager.Instance != null) UIManager.Instance.UpdateLives(currentLives);
    }

    public void ActivateGlobalStun(float duration)
    {
        StartCoroutine(GlobalStunRoutine(duration));
    }

    IEnumerator GlobalStunRoutine(float duration)
    {
        isSmokeBombActive = true;
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var e in enemies)
        {
            var enemyScript = e.GetComponent<Enemy>();
            if (enemyScript != null) enemyScript.SetStunState(true);
        }
        yield return new WaitForSeconds(duration);
        isSmokeBombActive = false;
        enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var e in enemies)
        {
            var enemyScript = e.GetComponent<Enemy>();
            if (enemyScript != null) enemyScript.SetStunState(false);
        }
    }
}