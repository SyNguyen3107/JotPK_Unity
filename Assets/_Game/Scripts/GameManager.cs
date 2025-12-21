using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Settings")]
    public int startLives = 3;
    public float respawnDelay = 2f;

    [Header("Level Settings")]
    public float levelDuration = 180f; // Thời gian màn chơi (3 phút)

    [Header("Timer Colors")] // --- MỚI THÊM ---
    public Color timerNormalColor = Color.green; // Màu bình thường (Mặc định Xanh)
    public Color timerCriticalColor = Color.red; // Màu khi sắp hết giờ (Mặc định Đỏ)

    [Header("Progress Settings")]
    public int totalAreasPassed = 0;

    [Header("References")]
    public GameObject gameOverPanel;
    public GameObject playerObject;

    // Biến nội bộ
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

        // --- SETUP TIMER ---
        currentTime = levelDuration;
        isTimerRunning = true;

        // 1. Áp dụng màu "Bình thường" ngay khi bắt đầu
        if (UIManager.Instance != null)
            UIManager.Instance.SetTimerColor(timerNormalColor);

        // Tìm Player
        if (playerObject == null)
            playerObject = GameObject.FindGameObjectWithTag("Player");

        // Cập nhật UI ban đầu
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

        // --- LOGIC 1: ĐẾM NGƯỢC THỜI GIAN ---
        if (isTimerRunning)
        {
            currentTime -= Time.deltaTime;

            if (UIManager.Instance != null)
            {
                // Cập nhật độ dài thanh
                UIManager.Instance.UpdateTimer(currentTime, levelDuration);

                // --- LOGIC ĐỔI MÀU (Đã cập nhật) ---
                // Nếu thời gian còn <= 10%
                if (currentTime <= levelDuration / 10f)
                {
                    UIManager.Instance.SetTimerColor(timerCriticalColor);
                }
                else
                {
                    // Nếu thời gian > 10%, giữ màu bình thường
                    // (Cần set lại mỗi frame đề phòng trường hợp hồi máu/hồi giờ sau này)
                    UIManager.Instance.SetTimerColor(timerNormalColor);
                }
            }

            // Hết giờ!
            if (currentTime <= 0)
            {
                currentTime = 0;
                HandleTimeUp();
            }
        }

        // --- LOGIC 2: CHỜ DIỆT HẾT QUÁI ---
        if (isWaitingForClear)
        {
            int enemyCount = GameObject.FindGameObjectsWithTag("Enemy").Length;
            if (enemyCount == 0)
            {
                isWaitingForClear = false;
                CompleteCurrentArea();
            }
        }
    }

    void HandleTimeUp()
    {
        Debug.Log("Hết giờ! Dừng spawn và chờ diệt quái...");
        isTimerRunning = false;

        // 1. Dừng Spawn quái
        if (WaveSpawner.Instance != null)
        {
            WaveSpawner.Instance.StopSpawning();
        }

        // 2. Chờ dọn dẹp
        isWaitingForClear = true;
    }

    public void CompleteCurrentArea()
    {
        totalAreasPassed++;
        Debug.Log("AREA CLEARED! Tổng số màn: " + totalAreasPassed);

        if (UIManager.Instance != null)
            UIManager.Instance.UpdateAreaIndicator(totalAreasPassed);
    }

    public void PlayerDied()
    {
        if (isGameOver) return;
        currentLives--;
        if (UIManager.Instance != null) UIManager.Instance.UpdateLives(currentLives);

        if (currentLives > 0)
        {
            if (playerObject != null) playerObject.SetActive(false);
            Invoke("RespawnPlayer", respawnDelay);
        }
        else
        {
            if (playerObject != null) playerObject.SetActive(false);
            GameOver();
        }
    }

    void RespawnPlayer()
    {
        if (playerObject != null)
        {
            playerObject.transform.position = Vector3.zero;
            playerObject.SetActive(true);
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
}