using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    #region --- CONSTANTS (STRING REFERENCES) ---
    // Định nghĩa các chuỗi tên/tag ở đây để tránh gõ sai chính tả sau này
    private const string TAG_PLAYER = "Player";
    private const string TAG_ENEMY = "Enemy";
    private const string TAG_DEATH_FX = "DeathFX";
    private const string NAME_INITIAL_MAP = "S1A1"; // Tên map có sẵn trên Scene lúc đầu
    private const string NAME_SPAWN_POINTS = "SpawnPoints";
    #endregion

    #region --- SETTINGS ---
    [Header("Game Settings")]
    public int startLives = 3;

    [Header("Level Settings")]
    public float levelDuration = 180f;
    public Color timerNormalColor = Color.green;
    public Color timerCriticalColor = Color.red;

    [Header("Respawn Logic")]
    public float deathAnimationDuration = 1f;
    public float deathDuration = 3f;
    public float invincibilityDuration = 3f;

    [Header("Transition Settings")]
    public Gate currentGate;            // Cổng của map hiện tại
    public float mapHeight = 20f;       // Khoảng cách Y tạm thời khi chuyển map
    public float transitionTime = 3f;   // Thời gian di chuyển camera/player

    [Header("Level Management")]
    public List<LevelConfig> allLevels; // Danh sách cấu hình các màn chơi

    [Header("Drop System")]
    public List<PowerUpData> allowedDrops;
    [Range(0f, 100f)] public float dropChance = 5f;

    [Header("Audio")]
    public AudioSource musicSource;
    #endregion

    #region --- STATE VARIABLES ---
    [Header("References")]
    public GameObject gameOverPanel;
    public GameObject playerObject;

    // [SerializeField] giúp hiện biến private lên Inspector để Debug
    [Header("Debug Info")]
    [SerializeField] private GameObject currentMapInstance;
    [SerializeField] private float currentTime;
    [SerializeField] private int currentLives;
    [SerializeField] private int currentLevelIndex = 0;

    [Header("Game Flags")]
    public int totalAreasPassed = 0;
    public int currentCoins = 0;
    public bool isSmokeBombActive = false;

    private bool isTimerRunning = false;
    private bool isWaitingForClear = false;
    private bool isGameOver = false;
    #endregion

    #region --- UNITY LIFECYCLE ---
    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        InitializeGame();
    }

    void Update()
    {
        if (isGameOver)
        {
            if (Input.GetKeyDown(KeyCode.R)) RestartLevel();
            return;
        }

        HandleTimer();
        HandleWaitingForClear();
    }
    #endregion

    #region --- INITIALIZATION ---
    void InitializeGame()
    {
        currentLives = startLives;
        isGameOver = false;
        isWaitingForClear = false;
        currentLevelIndex = 0;
        totalAreasPassed = 0;

        // 1. Setup Level 1 Data
        if (allLevels.Count > 0)
        {
            LevelConfig firstLevel = allLevels[0];
            levelDuration = firstLevel.levelDuration;

            // Sync Wave Spawner
            if (WaveSpawner.Instance != null)
            {
                WaveSpawner.Instance.waves = firstLevel.waves;
            }
        }

        // 2. Setup State
        currentTime = levelDuration;
        isTimerRunning = true;
        Time.timeScale = 1f;

        // 3. Find References
        // Tìm map đầu tiên (Lưu ý: Tên object trên Scene phải khớp với biến NAME_INITIAL_MAP)
        currentMapInstance = GameObject.Find(NAME_INITIAL_MAP);
        if (currentMapInstance != null)
        {
            currentGate = currentMapInstance.GetComponentInChildren<Gate>();
            if (currentGate == null) Debug.LogError("LỖI: Không tìm thấy script Gate trong Map đầu tiên!");
        }
        else
        {
            Debug.LogWarning($"Không tìm thấy Map khởi đầu tên '{NAME_INITIAL_MAP}'!");
        }

        if (playerObject == null) playerObject = GameObject.FindGameObjectWithTag(TAG_PLAYER);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // 4. Init UI & Audio
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetTimerColor(timerNormalColor);
            UIManager.Instance.UpdateLives(currentLives);
            UIManager.Instance.UpdateCoins(0);
            UIManager.Instance.UpdateAreaIndicator(totalAreasPassed);
            UIManager.Instance.UpdateTimer(currentTime, levelDuration);
        }

        if (musicSource != null)
        {
            musicSource.loop = true;
            musicSource.Play();
        }
    }
    #endregion

    #region --- CORE GAME LOOP ---
    void HandleTimer()
    {
        if (!isTimerRunning) return;

        currentTime -= Time.deltaTime;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateTimer(currentTime, levelDuration);
            UIManager.Instance.SetTimerColor(currentTime <= levelDuration / 10f ? timerCriticalColor : timerNormalColor);
        }

        if (currentTime <= 0)
        {
            currentTime = 0;
            HandleTimeUp();
        }
    }

    void HandleTimeUp()
    {
        if (isWaitingForClear) return;

        Debug.Log("TIME UP! Trigger Sudden Death Mode");
        isTimerRunning = false;
        isWaitingForClear = true;

        if (WaveSpawner.Instance != null) WaveSpawner.Instance.StopSpawning();

        CheckSpikeballEndCondition();
    }

    void HandleWaitingForClear()
    {
        if (!isWaitingForClear) return;

        int activeEnemyCount = GameObject.FindGameObjectsWithTag(TAG_ENEMY).Length;
        int pendingEnemyCount = (WaveSpawner.Instance != null) ? WaveSpawner.Instance.GetPendingEnemyCount() : 0;

        if (activeEnemyCount == 0 && pendingEnemyCount == 0)
        {
            isWaitingForClear = false;
            CompleteCurrentArea();
        }
    }

    // Logic đặc biệt: Nếu chỉ còn Spikeball khi hết giờ thì làm yếu chúng đi
    void CheckSpikeballEndCondition()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(TAG_ENEMY);
        if (enemies.Length == 0) return;

        List<Spikeball> spikeballList = new List<Spikeball>();
        bool allAreSpikeballs = true;

        foreach (GameObject enemyObj in enemies)
        {
            Spikeball sb = enemyObj.GetComponent<Spikeball>();
            if (sb == null)
            {
                allAreSpikeballs = false;
                break;
            }
            spikeballList.Add(sb);
        }

        if (allAreSpikeballs)
        {
            Debug.Log("Time Up! Chỉ còn Spikeball -> Giảm máu tất cả.");
            foreach (Spikeball sb in spikeballList) sb.Weaken();
        }
    }
    #endregion

    #region --- LEVEL TRANSITION (ORIGIN SHIFT) ---
    public void CompleteCurrentArea()
    {
        Debug.Log("LEVEL CLEARED! OPENING GATE...");
        totalAreasPassed++;
        currentTime = levelDuration;
        isTimerRunning = false;

        if (UIManager.Instance != null) UIManager.Instance.UpdateAreaIndicator(totalAreasPassed);

        if (currentGate != null) currentGate.OpenGate();
        else Debug.LogWarning("Current Gate is NULL!");
    }

    public void StartLevelTransition()
    {
        StartCoroutine(TransitionRoutine());
    }

    IEnumerator TransitionRoutine()
    {
        // 1. Chuẩn bị dữ liệu Level tiếp theo
        currentLevelIndex++;
        if (currentLevelIndex >= allLevels.Count)
        {
            Debug.Log("WIN! Đã hết màn chơi trong danh sách.");
            yield break;
        }

        LevelConfig nextLevelData = allLevels[currentLevelIndex];

        // 2. SPAWN MAP MỚI (Tại vị trí tạm thời Offset -20)
        Vector3 offsetPosition = new Vector3(0, -mapHeight, 0);
        GameObject newMap = Instantiate(nextLevelData.mapPrefab, offsetPosition, Quaternion.identity);
        newMap.name = "Map_" + nextLevelData.levelName; // Đặt tên map mới cho dễ debug

        // 3. CUTSCENE DI CHUYỂN
        PlayerController pc = playerObject.GetComponent<PlayerController>();

        // Chạy Camera song song
        StartCoroutine(MoveCamera(offsetPosition, transitionTime));

        // Chờ Player đi bộ xong (PlayerController sẽ tự lo việc Kinematic/Input)
        yield return StartCoroutine(pc.MoveToPosition(offsetPosition, transitionTime));

        // ====================================================
        // GIAI ĐOẠN "DỊCH CHUYỂN TỨC THỜI" (ORIGIN SHIFT)
        // ====================================================

        // A. Xóa Map cũ (Kèm cơ chế bảo vệ GameManager)
        if (currentMapInstance != null)
        {
            // Nếu GameManager lỡ làm con của Map cũ, tự tách ra để không bị Destroy
            if (transform.IsChildOf(currentMapInstance.transform))
            {
                Debug.LogError("[Critical] GameManager đang nằm trong Map cũ! Đang tự tách parent...");
                transform.SetParent(null);
            }
            Destroy(currentMapInstance);
        }

        // B. Dọn dẹp Effect chết chóc còn sót lại
        ClearLeftoverEffects();

        // C. Dời mọi thứ về toạ độ gốc (0,0)
        newMap.transform.position = Vector3.zero;
        playerObject.transform.position = Vector3.zero;
        Camera.main.transform.position = new Vector3(0, 0, -10);

        // Cập nhật vật lý ngay lập tức để tránh lỗi xuyên tường
        Physics2D.SyncTransforms();

        // D. Cập nhật tham chiếu
        currentMapInstance = newMap;

        // ====================================================

        // 4. SETUP MAP MỚI
        Gate newGate = newMap.GetComponentInChildren<Gate>();

        // Cập nhật Spawner cho map mới
        if (WaveSpawner.Instance != null)
        {
            Transform spawnParent = newMap.transform.Find(NAME_SPAWN_POINTS);
            Transform[] newSpawnPoints = new Transform[0];

            if (spawnParent != null)
            {
                newSpawnPoints = new Transform[spawnParent.childCount];
                for (int i = 0; i < spawnParent.childCount; i++)
                {
                    newSpawnPoints[i] = spawnParent.GetChild(i);
                }
            }
            else
            {
                Debug.LogError($"LỖI: Không tìm thấy object '{NAME_SPAWN_POINTS}' trong prefab {newMap.name}");
            }

            // Gửi dữ liệu: Toạ độ 0, Wave mới, Điểm spawn mới
            WaveSpawner.Instance.UpdateLevelData(Vector3.zero, nextLevelData.waves, newSpawnPoints);
        }

        // 5. FINALIZE & START GAME
        levelDuration = nextLevelData.levelDuration;
        currentTime = levelDuration;

        currentGate = newGate;
        if (currentGate != null) currentGate.CloseGate();

        Debug.Log("Transition Done. Waiting 3s...");
        yield return new WaitForSeconds(3f);

        // Kích hoạt lại game
        pc.isInputEnabled = true;
        isTimerRunning = true;

        if (WaveSpawner.Instance != null)
        {
            WaveSpawner.Instance.SetWavePaused(false);
            WaveSpawner.Instance.StartNextLevelWaves();
        }
    }

    void ClearLeftoverEffects()
    {
        // Tìm và xóa các FX còn sót (Yêu cầu Prefab FX phải có Tag đúng)
        GameObject[] fxList = GameObject.FindGameObjectsWithTag(TAG_DEATH_FX);
        foreach (var fx in fxList)
        {
            Destroy(fx);
        }
    }

    IEnumerator MoveCamera(Vector3 targetCenter, float duration)
    {
        Transform camTrans = Camera.main.transform;
        Vector3 startPos = camTrans.position;
        Vector3 endPos = new Vector3(targetCenter.x, targetCenter.y, startPos.z);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            camTrans.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        camTrans.position = endPos;
    }
    #endregion

    #region --- PLAYER STATE ---
    public void PlayerDied()
    {
        if (isGameOver) return;

        PlayerController pc = playerObject != null ? playerObject.GetComponent<PlayerController>() : null;
        if (pc != null && pc.IsInvincible()) return;

        currentLives--;
        if (UIManager.Instance != null) UIManager.Instance.UpdateLives(currentLives);

        if (currentLives > 0) StartCoroutine(RespawnSequence());
        else
        {
            if (playerObject != null) playerObject.SetActive(false);
            if (musicSource != null) musicSource.Stop();
            GameOver();
        }
    }

    IEnumerator RespawnSequence()
    {
        // Giai đoạn 1: Dừng & Animation chết
        isTimerRunning = false;
        if (musicSource != null) musicSource.Stop();
        if (WaveSpawner.Instance != null) WaveSpawner.Instance.OnPlayerDied();

        PlayerController pc = playerObject != null ? playerObject.GetComponent<PlayerController>() : null;
        if (pc != null) pc.TriggerDeathAnimation();

        yield return new WaitForSeconds(deathAnimationDuration);

        // Giai đoạn 2: Biến mất
        if (playerObject != null) playerObject.SetActive(false);
        yield return new WaitForSeconds(deathDuration);

        // Giai đoạn 3: Hồi sinh tại (0,0)
        if (playerObject != null)
        {
            playerObject.transform.position = Vector3.zero; // Luôn đúng vì logic Origin Shift
            playerObject.SetActive(true);
            if (pc != null) pc.ResetState();
        }

        if (pc != null) pc.TriggerRespawnInvincibility(invincibilityDuration);
        yield return new WaitForSeconds(invincibilityDuration);

        // Giai đoạn 4: Resume
        if (WaveSpawner.Instance != null) WaveSpawner.Instance.OnPlayerRespawned();

        if (currentTime > 0) isTimerRunning = true;
        else
        {
            isTimerRunning = false;
            isWaitingForClear = true;
        }

        if (musicSource != null) musicSource.Play();
    }
    #endregion

    #region --- DROPS & ITEMS ---
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
        SetAllEnemiesStun(true);
        yield return new WaitForSeconds(duration);
        isSmokeBombActive = false;
        SetAllEnemiesStun(false);
    }

    void SetAllEnemiesStun(bool isStunned)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(TAG_ENEMY);
        foreach (var e in enemies)
        {
            var enemyScript = e.GetComponent<Enemy>();
            if (enemyScript != null) enemyScript.SetStunState(isStunned);
        }
    }
    #endregion

    #region --- SYSTEM ---
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
    #endregion
}