using UnityEngine;
using UnityEngine.SceneManagement; // Thư viện để reload lại màn chơi

public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // Singleton

    [Header("Game Settings")]
    public int startLives = 3;       // Số mạng ban đầu
    public float respawnDelay = 1f;  // Thời gian chờ hồi sinh

    [Header("Progress Settings")]
    public int totalAreasPassed = 0; // Biến đếm tổng số màn đã qua (Start = 0)

    [Header("References")]
    public GameObject gameOverPanel; // Panel Game Over

    // Biến trạng thái (Private)
    private int currentLives;
    private bool isGameOver = false;

    void Awake()
    {
        // Setup Singleton
        if (Instance == null)
        {
            Instance = this;
            // Nếu bạn muốn giữ GameManager khi chuyển Scene (ví dụ sang Forest), hãy dùng:
            DontDestroyOnLoad(gameObject); 
            // Nhưng hiện tại ta đang làm mọi thứ trong 1 Scene nên chưa cần dòng trên.
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

        // 1. Cập nhật UI Mạng & Tiền
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateLives(currentLives);
            UIManager.Instance.UpdateCoins(0); // Tạm thời là 0

            // 2. Cập nhật UI Area Indicator
            // Mới vào game, totalAreasPassed = 0 -> Ẩn hết icon
            UIManager.Instance.UpdateAreaIndicator(totalAreasPassed);
        }

        // 3. Ẩn bảng Game Over
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        Time.timeScale = 1f;
    }

    void Update()
    {
        // Nếu Game Over, cho phép nhấn R để chơi lại
        if (isGameOver && Input.GetKeyDown(KeyCode.R))
        {
            RestartLevel();
        }

        // --- TEST NHANH (Xóa khi build thật) ---
        // Nhấn phím P để giả lập việc qua màn
        if (Input.GetKeyDown(KeyCode.P))
        {
            CompleteCurrentArea();
        }
    }

    // --- LOGIC GAMEPLAY ---

    // Gọi hàm này khi tiêu diệt hết quái trong WaveSpawner
    public void CompleteCurrentArea()
    {
        totalAreasPassed++;
        Debug.Log("Đã vượt qua Area! Tổng số màn đã qua: " + totalAreasPassed);

        // Cập nhật UI hiện thêm 1 icon mới
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateAreaIndicator(totalAreasPassed);
        }

        // Logic mở cổng hoặc chuyển cảnh sẽ thêm ở đây...
    }

    // Gọi hàm này khi Player chạm vào Enemy
    public void PlayerDied()
    {
        if (isGameOver) return;

        currentLives--;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateLives(currentLives);
        }

        if (currentLives > 0)
        {
            Debug.Log("Player chết! Còn " + currentLives + " mạng.");
            Invoke("RespawnPlayer", respawnDelay); // Dùng Invoke để delay hồi sinh
        }
        else
        {
            GameOver();
        }
    }

    void RespawnPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = Vector3.zero; // Về giữa map
            player.SetActive(true); // Đảm bảo player được bật lại nếu bị tắt

            // Trigger hiệu ứng bất tử nếu có
        }
    }

    void GameOver()
    {
        isGameOver = true;
        Debug.Log("GAME OVER!");

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        Time.timeScale = 0f;
    }

    void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}