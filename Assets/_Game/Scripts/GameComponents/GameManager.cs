using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    #region --- CONSTANTS (STRING REFERENCES) ---
    private const string TAG_PLAYER = "Player";
    private const string TAG_ENEMY = "Enemy";
    private const string TAG_DEATH_FX = "DeathFX";
    private const string NAME_INITIAL_MAP = "S1A1";
    private const string NAME_ENEMY_SPAWN_POINTS = "SpawnPoints"; // Tên cũ
    private const string NAME_PLAYER_SPAWN_POINT = "PlayerSpawnPoint"; // --- MỚI ---
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
    public Gate currentGate;
    public float mapHeight = 20f;
    public float transitionTime = 3f;

    [Header("Debug / Testing")]
    public int startingLevelIndex = 0;

    [Header("Level Management")]
    public List<LevelConfig> allLevels;

    [Header("Boss Logic")]
    public Vector3? overrideRespawnPosition = null;

    private int currentLevelCoinsSpawned = 0;
    private int targetCoinsForThisLevel = 0;

    [Header("Drop System")]
    public List<PowerUpData> allowedDrops;
    [Range(0f, 100f)] public float dropChance = 30f;

    [Header("Shop Settings")]
    public GameObject vendorPrefab;
    public int levelsPerShop = 2;

    [Header("Audio")]
    public AudioSource musicSource;
    public AudioClip defaultLevelMusic;
    #endregion

    #region --- STATE VARIABLES ---
    [Header("References")]
    public GameObject gameOverPanel;
    public GameObject playerObject;

    [Header("Debug Info")]
    [SerializeField] private GameObject currentMapInstance;
    [SerializeField] private float currentTime;
    [SerializeField] public int currentLives;
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
        currentLevelIndex = startingLevelIndex;
        totalAreasPassed = 0;

        GameObject existingMap = GameObject.Find(NAME_INITIAL_MAP);

        // --- LOGIC SINH MAP TEST ---
        if (currentLevelIndex < allLevels.Count)
        {
            LevelConfig levelData = allLevels[currentLevelIndex];
            levelDuration = levelData.levelDuration;

            if (currentLevelIndex > 0)
            {
                if (existingMap != null) Destroy(existingMap);
                currentMapInstance = Instantiate(levelData.mapPrefab, Vector3.zero, Quaternion.identity);
                currentMapInstance.name = "Map_" + levelData.levelName;
            }
            else
            {
                currentMapInstance = existingMap;
                if (currentMapInstance == null) Debug.LogWarning($"Không tìm thấy Map khởi đầu '{NAME_INITIAL_MAP}'!");
            }
        }
        else
        {
            Debug.LogError("Starting Level Index vượt quá số lượng màn chơi trong danh sách!");
        }

        // Setup State
        currentTime = levelDuration;
        isTimerRunning = true;
        Time.timeScale = 1f;

        if (playerObject == null) playerObject = GameObject.FindGameObjectWithTag(TAG_PLAYER);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // --- SETUP MAP HIỆN TẠI ---
        if (currentMapInstance != null)
        {
            currentGate = currentMapInstance.GetComponentInChildren<Gate>();

            // 1. TÌM VỊ TRÍ PLAYER (PlayerSpawnPoint)
            Transform pSpawn = currentMapInstance.transform.Find(NAME_PLAYER_SPAWN_POINT);
            if (playerObject != null)
            {
                if (pSpawn != null)
                    playerObject.transform.position = pSpawn.position;
                else
                    playerObject.transform.position = Vector3.zero; // Fallback
            }

            // 2. SETUP ENEMY WAVES (SpawnPoints)
            if (WaveSpawner.Instance != null && currentLevelIndex < allLevels.Count)
            {
                LevelConfig levelData = allLevels[currentLevelIndex];
                WaveSpawner.Instance.waves = levelData.waves;

                Transform enemySpawnParent = currentMapInstance.transform.Find(NAME_ENEMY_SPAWN_POINTS);
                if (enemySpawnParent != null)
                {
                    Transform[] autoSpawnPoints = new Transform[enemySpawnParent.childCount];
                    for (int i = 0; i < enemySpawnParent.childCount; i++)
                    {
                        autoSpawnPoints[i] = enemySpawnParent.GetChild(i);
                    }
                    WaveSpawner.Instance.UpdateLevelData(Vector3.zero, levelData.waves, autoSpawnPoints);
                }
                else
                {
                    Debug.LogWarning($"Map thiếu '{NAME_ENEMY_SPAWN_POINTS}'. Enemy có thể không spawn đúng chỗ.");
                }
            }
        }

        // Init UI & Audio
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetTimerColor(timerNormalColor);
            UIManager.Instance.UpdateLives(currentLives);
            UIManager.Instance.UpdateCoins(0);
            UIManager.Instance.UpdateAreaIndicator(totalAreasPassed);
            UIManager.Instance.UpdateTimer(currentTime, levelDuration);
            UIManager.Instance.ToggleHUD(true);
        }

        if (musicSource != null)
        {
            if (defaultLevelMusic != null) musicSource.clip = defaultLevelMusic;
            musicSource.loop = true;
            musicSource.Play();
        }

        if (currentLevelIndex > 0 && WaveSpawner.Instance != null)
        {
            WaveSpawner.Instance.SetWavePaused(false);
            WaveSpawner.Instance.StartNextLevelWaves();
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
    public void SetTimerRunning(bool isRunning)
    {
        isTimerRunning = isRunning;
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
        Debug.Log("LEVEL CLEARED!");

        if (musicSource != null) musicSource.Stop();

        totalAreasPassed++;
        currentTime = levelDuration;
        isTimerRunning = false;

        if (UIManager.Instance != null) UIManager.Instance.UpdateAreaIndicator(totalAreasPassed);

        if (currentGate != null) currentGate.OpenGate();
        else Debug.LogWarning("Current Gate is NULL!");

        // --- SPAWN SHOP ---
        bool shouldSpawnShop = (totalAreasPassed > 0) && (totalAreasPassed % levelsPerShop == 0);

        if (shouldSpawnShop && vendorPrefab != null)
        {
            if (UpgradeManager.Instance != null) UpgradeManager.Instance.ResetPurchaseStatus();
            GameObject vendor = Instantiate(vendorPrefab, Vector3.zero, Quaternion.identity);
            if (currentMapInstance != null)
            {
                vendor.transform.SetParent(currentMapInstance.transform);
            }
        }
    }

    public void StartLevelTransition()
    {
        StartCoroutine(TransitionRoutine());
    }

    IEnumerator TransitionRoutine()
    {
        currentLevelIndex++;
        if (currentLevelIndex >= allLevels.Count)
        {
            Debug.Log("WIN! Đã hết màn chơi trong danh sách.");
            yield break;
        }

        LevelConfig nextLevelData = allLevels[currentLevelIndex];

        // Tạo map mới ở vị trí offset (dưới màn hình)
        Vector3 offsetPosition = new Vector3(0f, -mapHeight, 0);
        GameObject newMap = Instantiate(nextLevelData.mapPrefab, offsetPosition, Quaternion.identity);
        newMap.name = "Map_" + nextLevelData.levelName;

        // Tắt input player trong lúc chuyển
        PlayerController pc = playerObject.GetComponent<PlayerController>();
        if (pc != null) pc.isInputEnabled = false;

        // --- TÌM ĐIỂM ĐÍCH CHO PLAYER (PlayerSpawnPoint của map mới) ---
        Vector3 targetWorldPos = offsetPosition; // Mặc định là tâm map mới
        Transform newPSpawn = newMap.transform.Find(NAME_PLAYER_SPAWN_POINT);
        if (newPSpawn != null)
        {
            // Player đi từ cổng map cũ -> PlayerSpawnPoint của map mới
            targetWorldPos = newPSpawn.position;
        }
        else
        {
            // Fallback: Nếu không có điểm spawn, đi đến giữa map
            targetWorldPos = offsetPosition;
        }

        // Di chuyển Camera và Player
        StartCoroutine(MoveCamera(offsetPosition, transitionTime));
        // Player đi đến đúng điểm Spawn Point (đang ở tọa độ thế giới)
        yield return StartCoroutine(pc.MoveToPosition(targetWorldPos, transitionTime));

        // ORIGIN SHIFT (Dời trục tọa độ về 0,0)
        if (currentMapInstance != null)
        {
            if (transform.IsChildOf(currentMapInstance.transform))
            {
                transform.SetParent(null);
            }
            Destroy(currentMapInstance);
        }

        ClearLeftoverEffects();

        // Đặt map mới về 0,0
        newMap.transform.position = Vector3.zero;

        // Đặt Player về đúng vị trí (Lúc này PlayerSpawnPoint cũng đã về tọa độ cục bộ tương ứng)
        if (newPSpawn != null)
            playerObject.transform.position = newPSpawn.position;
        else
            playerObject.transform.position = Vector3.zero;

        // Reset Camera
        Camera.main.transform.position = new Vector3(0, 0, -10);

        Physics2D.SyncTransforms();
        currentMapInstance = newMap;

        // --- SETUP MAP MỚI (Enemy Spawn Points) ---
        Gate newGate = newMap.GetComponentInChildren<Gate>();

        if (WaveSpawner.Instance != null)
        {
            Transform enemySpawnParent = newMap.transform.Find(NAME_ENEMY_SPAWN_POINTS);
            Transform[] newSpawnPoints = new Transform[0];

            if (enemySpawnParent != null)
            {
                newSpawnPoints = new Transform[enemySpawnParent.childCount];
                for (int i = 0; i < enemySpawnParent.childCount; i++)
                {
                    newSpawnPoints[i] = enemySpawnParent.GetChild(i);
                }
            }
            // Gán điểm spawn quái cho WaveSpawner
            WaveSpawner.Instance.UpdateLevelData(Vector3.zero, nextLevelData.waves, newSpawnPoints);
        }

        if (UIManager.Instance != null) UIManager.Instance.ToggleHUD(true);
        levelDuration = nextLevelData.levelDuration;
        currentTime = levelDuration;

        currentGate = newGate;
        if (currentGate != null) currentGate.CloseGate();

        Debug.Log("Transition Done. Waiting 3s...");
        yield return new WaitForSeconds(3f);

        // --- PHÂN LUỒNG: BOSS vs MAP THƯỜNG ---
        BossManager bossMgr = newMap.GetComponentInChildren<BossManager>();

        if (bossMgr != null)
        {
            // >> NẾU LÀ BOSS: Kích hoạt Boss (Code BossManager tự lo nhạc/UI)
            bossMgr.ActivateBossLevel();
            isTimerRunning = false;
        }
        else
        {
            // >> NẾU LÀ MAP THƯỜNG:
            // Bật nhạc thường
            if (musicSource != null)
            {
                if (defaultLevelMusic != null) musicSource.clip = defaultLevelMusic;
                musicSource.loop = true;
                musicSource.Play();
            }

            // Bật Timer & Wave
            isTimerRunning = true;
            if (WaveSpawner.Instance != null)
            {
                WaveSpawner.Instance.SetWavePaused(false);
                WaveSpawner.Instance.StartNextLevelWaves();
            }
        }

        // Mở input cho player
        if (pc != null) pc.isInputEnabled = true;
    }

    void ClearLeftoverEffects()
    {
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
        // --- GIAI ĐOẠN 1: DỪNG GAME & ANIMATION ---
        isTimerRunning = false;
        if (musicSource != null) musicSource.Stop();
        if (WaveSpawner.Instance != null) WaveSpawner.Instance.OnPlayerDied();

        PlayerController pc = playerObject != null ? playerObject.GetComponent<PlayerController>() : null;
        if (pc != null) pc.TriggerDeathAnimation();

        yield return new WaitForSeconds(deathAnimationDuration);

        // --- GIAI ĐOẠN 2: ẨN PLAYER & CHỜ ---
        if (playerObject != null) playerObject.SetActive(false);
        yield return new WaitForSeconds(deathDuration);

        // --- GIAI ĐOẠN 3: HỒI SINH & ĐẶT VỊ TRÍ ---
        if (playerObject != null)
        {
            // ƯU TIÊN 1: Vị trí Custom (Boss)
            if (overrideRespawnPosition.HasValue)
            {
                playerObject.transform.position = overrideRespawnPosition.Value;
            }
            // ƯU TIÊN 2: PlayerSpawnPoint (Map thường)
            else if (currentMapInstance != null)
            {
                Transform pSpawn = currentMapInstance.transform.Find(NAME_PLAYER_SPAWN_POINT);
                if (pSpawn != null)
                    playerObject.transform.position = pSpawn.position;
                else
                    playerObject.transform.position = Vector3.zero;
            }
            else
            {
                playerObject.transform.position = Vector3.zero;
            }

            playerObject.SetActive(true);
            if (pc != null) pc.ResetState();
        }

        // --- GIAI ĐOẠN 4: BẤT TỬ TẠM THỜI ---
        if (pc != null) pc.TriggerRespawnInvincibility(invincibilityDuration);
        yield return new WaitForSeconds(invincibilityDuration);

        // --- GIAI ĐOẠN 5: RESUME GAME ---
        if (WaveSpawner.Instance != null) WaveSpawner.Instance.OnPlayerRespawned();

        // XỬ LÝ TIMER KHI RESUME
        if (overrideRespawnPosition.HasValue)
        {
            isTimerRunning = false; // Map Boss
        }
        else if (currentTime > 0)
        {
            isTimerRunning = true; // Map thường
        }
        else
        {
            isTimerRunning = false;
            isWaitingForClear = true;
        }

        if (musicSource != null) musicSource.Play();
    }
    #endregion

    #region --- DROPS & ITEMS ---
    public PowerUpData GetDropItemLogic() { return GetWeightedRandomItem(); }

    private PowerUpData GetWeightedRandomItem()
    {
        if (allowedDrops == null || allowedDrops.Count == 0) return null;
        int totalWeight = 0;
        foreach (var item in allowedDrops) totalWeight += item.dropWeight;
        int randomValue = Random.Range(0, totalWeight);
        foreach (var item in allowedDrops)
        {
            randomValue -= item.dropWeight;
            if (randomValue < 0) return item;
        }
        return allowedDrops[0];
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

    public void RegisterCoinSpawn() { currentLevelCoinsSpawned++; }

    public void ActivateGlobalStun(float duration) { StartCoroutine(GlobalStunRoutine(duration)); }

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