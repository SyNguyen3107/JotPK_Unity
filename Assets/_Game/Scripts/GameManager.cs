using UnityEngine;
using UnityEngine.SceneManagement; // Thư viện để reload lại màn chơi

public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // Singleton

    [Header("Game Settings")]
    public int startLives = 3;       // Số mạng ban đầu
    public float respawnDelay = 1f;  // Thời gian chờ hồi sinh

    [Header("References")]
    public GameObject gameOverPanel; // Panel Game Over (nếu bạn đã tạo)

    // Biến trạng thái (Private)
    private int currentLives;
    private bool isGameOver = false;

    void Awake()
    {
        // Setup Singleton: Đảm bảo chỉ có 1 GameManager tồn tại
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        currentLives = startLives;
        isGameOver = false;

        // 1. Cập nhật UI ban đầu thông qua UIManager
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateLives(currentLives);
            // Sau này sẽ thêm: UIManager.Instance.UpdateCoins(currentCoins);
        }

        // 2. Ẩn bảng Game Over (đề phòng quên tắt trong Editor)
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        // Đảm bảo thời gian chạy bình thường (phòng trường hợp restart khi đang pause)
        Time.timeScale = 1f;
    }

    void Update()
    {
        // Nếu Game Over, cho phép nhấn R để chơi lại
        if (isGameOver && Input.GetKeyDown(KeyCode.R))
        {
            RestartLevel();
        }
    }

    // --- LOGIC CHÍNH ---

    // Hàm này được gọi bởi Enemy khi chạm vào Player
    public void PlayerDied()
    {
        if (isGameOver) return;

        currentLives--;

        // Cập nhật giao diện ngay lập tức
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateLives(currentLives);
        }

        if (currentLives > 0)
        {
            // Còn mạng -> Hồi sinh
            Debug.Log("Player chết! Còn " + currentLives + " mạng.");

            // Có thể thêm Coroutine để delay việc hồi sinh nếu muốn
            RespawnPlayer();
        }
        else
        {
            // Hết mạng -> Thua cuộc
            GameOver();
        }
    }

    void RespawnPlayer()
    {
        // 1. Tìm Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // 2. Đặt lại vị trí về giữa map
            player.transform.position = Vector3.zero;

            // (Tùy chọn) Kích hoạt hiệu ứng nhấp nháy bất tử ở đây
            // player.GetComponent<PlayerController>().TriggerInvincibility();
        }

        // 3. (Tùy chọn) Xóa bớt quái gần điểm hồi sinh để tránh chết oan
        // ClearEnemiesAroundSpawn();
    }

    void GameOver()
    {
        isGameOver = true;
        Debug.Log("GAME OVER!");

        // Hiện bảng thông báo
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        // Dừng toàn bộ hoạt động của game
        Time.timeScale = 0f;
    }

    void RestartLevel()
    {
        // Load lại Scene hiện tại
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}