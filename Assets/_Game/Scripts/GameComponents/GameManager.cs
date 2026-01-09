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
    public Gate currentGate;
    public float mapHeight = 20f;
    public float transitionTime = 3f;

    [Header("Debug / Testing")]
    public int startingLevelIndex = 0;

    [Header("Level Management")]
    public List<LevelConfig> allLevels;

    [Header("Economy Balance")]
    public int minCoinsPerLevel = 10;
    public int maxCoinsPerLevel = 30;

    private int currentLevelCoinsSpawned = 0;
    private int targetCoinsForThisLevel = 0;

    [Header("Drop System")]
    public PowerUpData coinData; // --- MỚI: Kéo thả Coin Data vào đây để làm "Bảo hiểm" ---
    public List<PowerUpData> allowedDrops;
    [Range(0f, 100f)] public float dropChance = 30f; // Tăng mặc định lên chút cho dễ test

    [Header("Shop Settings")]
    public GameObject vendorPrefab;
    public int levelsPerShop = 2;

    [Header("Audio")]
    public AudioSource musicSource;
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

        // --- MỚI: Reset kinh tế ngay khi bắt đầu game ---
        ResetLevelEconomy();

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

            // --- AUTO-WIRE SPAWN POINTS ---
            if (currentMapInstance != null)
            {
                currentGate = currentMapInstance.GetComponentInChildren<Gate>();
                if (currentGate == null) Debug.LogError("LỖI: Map này thiếu script Gate!");

                if (WaveSpawner.Instance != null)
                {
                    WaveSpawner.Instance.waves = levelData.waves;

                    Transform spawnParent = currentMapInstance.transform.Find(NAME_SPAWN_POINTS);
                    if (spawnParent != null)
                    {
                        Transform[] autoSpawnPoints = new Transform[spawnParent.childCount];
                        for (int i = 0; i < spawnParent.childCount; i++)
                        {
                            autoSpawnPoints[i] = spawnParent.GetChild(i);
                        }
                        WaveSpawner.Instance.UpdateLevelData(Vector3.zero, levelData.waves, autoSpawnPoints);
                        Debug.Log($"[Auto-Setup] Đã tự gán {autoSpawnPoints.Length} điểm spawn cho Level {currentLevelIndex}.");
                    }
                    else
                    {
                        Debug.LogError($"LỖI: Không tìm thấy object '{NAME_SPAWN_POINTS}' trong map hiện tại.");
                    }
                }
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

        // Init UI & Audio
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
            Debug.Log("SHOP SPAWNED!");
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

        Vector3 offsetPosition = new Vector3(0, -mapHeight, 0);
        GameObject newMap = Instantiate(nextLevelData.mapPrefab, offsetPosition, Quaternion.identity);
        newMap.name = "Map_" + nextLevelData.levelName;

        // --- MỚI: Reset kinh tế cho level mới ---
        ResetLevelEconomy();

        PlayerController pc = playerObject.GetComponent<PlayerController>();
        StartCoroutine(MoveCamera(offsetPosition, transitionTime));
        yield return StartCoroutine(pc.MoveToPosition(offsetPosition, transitionTime));

        // ORIGIN SHIFT
        if (currentMapInstance != null)
        {
            if (transform.IsChildOf(currentMapInstance.transform))
            {
                transform.SetParent(null);
            }
            Destroy(currentMapInstance);
        }

        ClearLeftoverEffects();

        newMap.transform.position = Vector3.zero;
        playerObject.transform.position = Vector3.zero;
        Camera.main.transform.position = new Vector3(0, 0, -10);

        Physics2D.SyncTransforms();
        currentMapInstance = newMap;

        // SETUP MAP MỚI
        Gate newGate = newMap.GetComponentInChildren<Gate>();

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
            WaveSpawner.Instance.UpdateLevelData(Vector3.zero, nextLevelData.waves, newSpawnPoints);
        }

        levelDuration = nextLevelData.levelDuration;
        currentTime = levelDuration;

        currentGate = newGate;
        if (currentGate != null) currentGate.CloseGate();

        Debug.Log("Transition Done. Waiting 3s...");
        yield return new WaitForSeconds(3f);
        musicSource.Play();

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
        isTimerRunning = false;
        if (musicSource != null) musicSource.Stop();
        if (WaveSpawner.Instance != null) WaveSpawner.Instance.OnPlayerDied();

        PlayerController pc = playerObject != null ? playerObject.GetComponent<PlayerController>() : null;
        if (pc != null) pc.TriggerDeathAnimation();

        yield return new WaitForSeconds(deathAnimationDuration);

        if (playerObject != null) playerObject.SetActive(false);
        yield return new WaitForSeconds(deathDuration);

        if (playerObject != null)
        {
            playerObject.transform.position = Vector3.zero;
            playerObject.SetActive(true);
            if (pc != null) pc.ResetState();
        }

        if (pc != null) pc.TriggerRespawnInvincibility(invincibilityDuration);
        yield return new WaitForSeconds(invincibilityDuration);

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

    #region --- DROPS & ITEMS (CẬP NHẬT) ---

    // 1. Hàm chính để Enemy gọi khi muốn rơi đồ
    public PowerUpData GetDropItemLogic()
    {
        return GetWeightedRandomItem();
    }

    // 2. Thuật toán chọn item theo Trọng số (Weight)
    private PowerUpData GetWeightedRandomItem()
    {
        if (allowedDrops == null || allowedDrops.Count == 0) return null;

        // Tính tổng trọng số
        int totalWeight = 0;
        foreach (var item in allowedDrops)
        {
            totalWeight += item.dropWeight;
        }

        // Random
        int randomValue = Random.Range(0, totalWeight);

        // Duyệt tìm item
        foreach (var item in allowedDrops)
        {
            randomValue -= item.dropWeight;
            if (randomValue < 0)
            {
                return item;
            }
        }

        return allowedDrops[0]; // Fallback
    }

    // --- Các hàm quản lý Coin/Economy ---

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

    public void ResetLevelEconomy()
    {
        currentLevelCoinsSpawned = 0;
        targetCoinsForThisLevel = Random.Range(minCoinsPerLevel, maxCoinsPerLevel + 1);
        Debug.Log($"Level Start: Target Coins to spawn = {targetCoinsForThisLevel}");
    }


    public void RegisterCoinSpawn()
    {
        currentLevelCoinsSpawned++;
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