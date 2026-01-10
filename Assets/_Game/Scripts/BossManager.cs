using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BossManager : MonoBehaviour
{
    [Header("References")]
    public CowboyController bossScript;
    public Transform playerRespawnPoint; // Vị trí sau hàng rào

    [Header("Victory Setup")]
    public GameObject bridgeObject;      // Object Cây Cầu (Mặc định tắt)
    public GameObject lootPrefab;        // Prefab vật phẩm rớt ra
    public Transform lootSpawnPoint;     // Vị trí rớt đồ (thường là chỗ Boss chết)
    public string nextSceneName;         // Tên màn chơi tiếp theo (Stage 2 hoặc Map khác)

    [Header("Cutscene - Gopher Squad")]
    public GameObject gopherSquadPrefab; // Prefab chứa hình ảnh 3 con Gopher
    public Transform gopherSpawnPoint;   // Điểm xuất phát của Gopher (Thường là mép trên màn hình)

    private bool victoryTriggered = false;


    [Header("Player Movement Limits")]
    public BoxCollider2D playerZoneLimit;
    [Header("Audio")]
    public AudioClip bossMusic;

    private bool isBossActive = false;

    void Start()
    {
        // 1. Dừng Timer của GameManager ngay lập tức
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetTimerRunning(false);
            GameManager.Instance.overrideRespawnPosition = playerRespawnPoint.position;
        }
        if (playerZoneLimit != null)
        {
            // Lấy giới hạn từ BoxCollider2D
            Vector2 zoneMin = playerZoneLimit.bounds.min;
            Vector2 zoneMax = playerZoneLimit.bounds.max;

            // Tìm Player và áp dụng giới hạn mới
            PlayerController pc = GameObject.FindAnyObjectByType<PlayerController>(); 
            if (pc != null)
            {
                pc.SetMapBounds(zoneMin, zoneMax);
            }
        }
        // 2. Setup UI
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ToggleBossUI(true); // Hiện thanh máu dưới
         }

        // 3. Phát nhạc Boss
        if (GameManager.Instance != null && bossMusic != null)
        {
            GameManager.Instance.musicSource.Stop();
            GameManager.Instance.musicSource.clip = bossMusic;
            GameManager.Instance.musicSource.Play();
        }

        if (bossScript != null)
        {
            isBossActive = true;
        }
    }

    void Update()
    {
        if (isBossActive && bossScript != null && UIManager.Instance != null)
        {
            // Cập nhật thanh máu liên tục
            UIManager.Instance.UpdateBossHealth(bossScript.currentHealth, bossScript.maxHealth);

            // Kiểm tra chiến thắng
            if (bossScript.currentHealth <= 0)
            {
                HandleBossVictory();
            }
        }
    }

    void HandleBossVictory()
    {
        if (victoryTriggered) return; // Đảm bảo chỉ chạy 1 lần
        victoryTriggered = true;
        isBossActive = false;
        GameManager.Instance.musicSource.Stop(); //tắt nhạc boss

        // 1. Tắt UI Boss
        if (UIManager.Instance != null) UIManager.Instance.ToggleBossUI(false);

        // 2. Hiện Cầu (nếu có)
        if (bridgeObject != null) bridgeObject.SetActive(true);

        // 3. Rơi Vật phẩm
        if (lootPrefab != null)
        {
            // Nếu Boss script còn đó thì lấy vị trí boss, nếu không thì lấy vị trí định sẵn
            Vector3 spawnPos = (bossScript != null) ? bossScript.transform.position : transform.position;
            if (lootSpawnPoint != null) spawnPos = lootSpawnPoint.position;

            Instantiate(lootPrefab, spawnPos, Quaternion.identity);
        }
        if (GameManager.Instance != null && GameManager.Instance.playerObject != null)
        {
            PlayerController pc = GameManager.Instance.playerObject.GetComponent<PlayerController>();
            // Reset giới hạn map về cực lớn hoặc về mặc định của map để đi qua cầu
            if (pc != null) pc.SetMapBounds(new Vector2(-100, -100), new Vector2(100, 100));
        }
        // Reset điểm hồi sinh để qua màn sau không bị lỗi vị trí
        if (GameManager.Instance != null)
        {
            GameManager.Instance.overrideRespawnPosition = null;
        }

        Debug.Log("BOSS DEFEATED! Starting Cutscene logic...");
        // (Sẽ gọi logic Cutscene ở Bước 3 & 4)
    }
    // --- HÀM MỚI: GỌI TỪ BOSSLOOT KHI NHẶT ĐỒ ---
    public void OnLootCollected()
    {
        StartCoroutine(GopherCutsceneRoutine());
    }

    IEnumerator GopherCutsceneRoutine()
    {
        Debug.Log("Starting Gopher Cutscene...");

        // 1. Khóa Player
        GameObject player = GameManager.Instance.playerObject;
        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null)
        {
            pc.isInputEnabled = false; // Đứng im
            if (pc.rb != null) pc.rb.linearVelocity = Vector2.zero;
            if (pc.legsAnimator != null) pc.legsAnimator.SetBool("IsMoving", false);
            // Có thể thêm anim: Player giơ cúp chiến thắng (Victory Pose)
        }

        // 2. Spawn Gopher Squad (3 con Gopher)
        GameObject gophers = null;
        if (gopherSquadPrefab != null && gopherSpawnPoint != null)
        {
            gophers = Instantiate(gopherSquadPrefab, gopherSpawnPoint.position, Quaternion.identity);
        }

        // 3. Gopher chạy đến chỗ Player
        if (gophers != null && player != null)
        {
            float speed = 5f;
            // Di chuyển Gophers đến vị trí Player
            while (Vector3.Distance(gophers.transform.position, player.transform.position) > 0.1f)
            {
                gophers.transform.position = Vector3.MoveTowards(gophers.transform.position, player.transform.position, speed * Time.deltaTime);
                yield return null;
            }

            // 4. "Bắt" Player (Gán Player làm con của Gophers để đi theo)
            player.transform.SetParent(gophers.transform);

            // (Tuỳ chọn) Tắt Sprite Player đi nếu Gopher đã có hình "Gopher đang khiêng người"
            // player.SetActive(false); 

            yield return new WaitForSeconds(0.5f); // Nghỉ một chút tạo dáng

            // 5. Gopher khiêng Player đi ra khỏi màn hình (Đi xuống hoặc đi lên)
            Vector3 exitPos = gophers.transform.position + Vector3.down * 10f; // Đi xuống dưới 10 đơn vị

            while (Vector3.Distance(gophers.transform.position, exitPos) > 0.1f)
            {
                gophers.transform.position = Vector3.MoveTowards(gophers.transform.position, exitPos, speed * Time.deltaTime);
                yield return null;
            }
        }

        // 6. Màn hình đen & Chuyển màn
        // Nếu bạn có SceneFader thì gọi ở đây. Nếu không thì load luôn.
        Debug.Log("Loading Next Level: " + nextSceneName);

        yield return new WaitForSeconds(1f);

        // Logic chuyển màn
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            // Nếu chưa có tên màn tiếp theo thì reload để test
            Debug.LogWarning("No Next Scene Name provided! Reloading current scene.");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
    void OnDestroy()
    {
        // Dọn dẹp khi rời scene
        if (GameManager.Instance != null) GameManager.Instance.overrideRespawnPosition = null;
        if (UIManager.Instance != null) UIManager.Instance.ToggleBossUI(false);
    }
}