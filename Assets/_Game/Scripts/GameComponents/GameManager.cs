using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    #region --- CONSTANTS ---
    private const string TAG_PLAYER = "Player";
    private const string TAG_ENEMY = "Enemy";
    private const string TAG_DEATH_FX = "DeathFX";
    private const string NAME_INITIAL_MAP = "S1A1";
    private const string NAME_ENEMY_SPAWN_POINTS = "SpawnPoints";
    private const string NAME_PLAYER_SPAWN_POINT = "PlayerSpawnPoint";
    private const string SCENE_GAME_NAME = "Game";
    #endregion

    #region --- SETTINGS ---
    [Header("Game Settings")]
    public int startLives = 3;

    [Header("Pause System")]
    public bool isPaused = false;
    public bool canPause = true;

    [Header("Level Settings")]
    public float levelDuration = 180f;
    public Color timerNormalColor = Color.green;
    public Color timerCriticalColor = Color.red;

    public bool useOverrideRespawn = false; // Checkbox để bật tính năng này
    public Vector3 overrideRespawnPosition;

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

    private int currentLevelCoinsSpawned = 0;

    [Header("Drop System")]
    public List<PowerUpData> allowedDrops;
    [Range(0f, 100f)] public float dropChance = 30f;

    [Header("Shop Settings")]
    public GameObject vendorPrefab;
    public int levelsPerShop = 2;

    [Header("Gopher Event Settings")]
    public GameObject gopherPrefab;
    [Range(0, 100)] public float gopherSpawnChance = 5f; // 5%
    public float gopherSpawnDistance = 6.5f; // Khoảng cách từ tâm (xa hơn map một chút)

    // Biến runtime
    private bool isGopherScheduled = false;
    private float gopherSpawnTime = 0f;

    [Header("Audio")]
    public AudioSource musicSource;
    public AudioClip defaultLevelMusic;
    public AudioClip gameOverClip;

    [Header("Tutorial Settings")]
    public bool isTutorialActive = false; // Biến cờ để biết đang trong giai đoạn tutorial
    private bool hasGameStarted = false;  // Biến cờ để đảm bảo game chỉ start 1 lần
    #endregion

    #region --- STATE VARIABLES ---
    [Header("References")]
    public GameObject gameOverPanel;
    public GameObject playerObject;

    // --- KHẮC PHỤC LỖI SPAWN: Dùng biến này thay cho Instance ---
    private WaveSpawner myWaveSpawner;

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

    // Save System
    public int currentSlotIndex = -1;
    public GameData currentData;
    #endregion

    #region --- UNITY LIFECYCLE ---
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // LẤY COMPONENT NGAY TẠI ĐÂY
            myWaveSpawner = GetComponent<WaveSpawner>();
        }
        else Destroy(gameObject);
    }

    void Start()
    {
        // Tự động chạy nếu đang test trực tiếp trong Scene Game
        if (SceneManager.GetActiveScene().name == SCENE_GAME_NAME)
        {
            if (currentData == null)
            {
                currentLives = startLives;
                currentLevelIndex = startingLevelIndex;
            }
            SetupLevel();
        }
    }

    void Update()
    {
        if (isGameOver)
        {
            if (Input.GetKeyDown(KeyCode.R)) RestartLevel();
            return;
        }
        if (isTutorialActive && !hasGameStarted)
        {
            // Kiểm tra các phím di chuyển hoặc bắn
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveY = Input.GetAxisRaw("Vertical");
            bool shootKeys = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow) ||
                             Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow);

            // Nếu có bất kỳ tín hiệu nào
            if (moveX != 0 || moveY != 0 || shootKeys)
            {
                StartCoroutine(StartGameWithDelay());
            }

            // QUAN TRỌNG: Return luôn để không chạy Timer hay logic khác
            return;
        }
        HandleTimer();
        HandleWaitingForClear();
    }
    #endregion

    #region --- INITIALIZATION & SETUP ---

    // Hàm gọi từ Menu
    public void LoadGameAndPlay(int slotIndex, bool isNewGame)
    {
        currentSlotIndex = slotIndex;
        if (isNewGame)
        {
            // Reset biến local trước
            currentLives = startLives;
            currentCoins = 0;
            currentLevelIndex = startingLevelIndex;

            // Tạo Data mới
            currentData = new GameData();
            currentData.lives = startLives;
            currentData.coins = 0;
            currentData.currentLevelIndex = startingLevelIndex;

            SaveGame(); // Lưu file lần đầu
        }
        else
        {
            currentData = SaveSystem.LoadGame(slotIndex);
        }

        if (currentData != null)
        {
            currentLives = currentData.lives;
            currentCoins = currentData.coins;
            currentLevelIndex = currentData.currentLevelIndex;
        }

        SceneManager.LoadScene(SCENE_GAME_NAME);
    }

    public void SetupLevel()
    {
        // 1. Reset Trạng thái
        isGameOver = false;
        isWaitingForClear = false;

        GameObject existingMap = GameObject.Find(NAME_INITIAL_MAP);

        // --- 2. SPAWN MAP ---
        if (currentLevelIndex < allLevels.Count)
        {
            LevelConfig levelData = allLevels[currentLevelIndex];
            levelDuration = levelData.levelDuration;

            // Nếu map chưa có sẵn (hoặc khác tên), spawn mới
            if (currentMapInstance == null || currentMapInstance.name != "Instance_" + levelData.levelName)
            {
                if (existingMap != null && currentLevelIndex > 0) Destroy(existingMap);
                if (currentMapInstance != null) Destroy(currentMapInstance);

                currentMapInstance = Instantiate(levelData.mapPrefab, Vector3.zero, Quaternion.identity);
                currentMapInstance.name = "Instance_" + levelData.levelName;
            }
        }
        else
        {
            Debug.LogError("Level Index out of range!");
            return;
        }

        // --- 3. SETUP TIMING & REFS ---
        currentTime = levelDuration;
        isTimerRunning = true;
        Time.timeScale = 1f;

        if (playerObject == null) playerObject = GameObject.FindGameObjectWithTag(TAG_PLAYER);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // --- 4. SETUP CONTENT (PLAYER & ENEMY) ---
        if (currentMapInstance != null)
        {
            currentGate = currentMapInstance.GetComponentInChildren<Gate>();

            // Setup Player Position
            Transform pSpawn = currentMapInstance.transform.Find(NAME_PLAYER_SPAWN_POINT);
            if (playerObject != null)
            {
                playerObject.transform.position = (pSpawn != null) ? pSpawn.position : Vector3.zero;
                playerObject.SetActive(true);
            }

            // Setup Wave Data
            if (myWaveSpawner != null && currentLevelIndex < allLevels.Count)
            {
                LevelConfig levelData = allLevels[currentLevelIndex];
                Transform enemySpawnParent = currentMapInstance.transform.Find(NAME_ENEMY_SPAWN_POINTS);
                Transform[] foundSpawnPoints = new Transform[0];

                if (enemySpawnParent != null)
                {
                    foundSpawnPoints = new Transform[enemySpawnParent.childCount];
                    for (int i = 0; i < enemySpawnParent.childCount; i++)
                        foundSpawnPoints[i] = enemySpawnParent.GetChild(i);
                }

                // Nạp dữ liệu vào Spawner (nhưng chưa START vội)
                myWaveSpawner.UpdateLevelData(Vector3.zero, levelData.waves, foundSpawnPoints);
            }
        }

        ApplyPersistentData();

        // --- 5. UI SETUP ---
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetTimerColor(timerNormalColor);
            UIManager.Instance.UpdateLives(currentLives);
            UIManager.Instance.UpdateCoins(currentCoins);
            UIManager.Instance.UpdateAreaIndicator(totalAreasPassed);
            UIManager.Instance.UpdateTimer(currentTime, levelDuration);
            UIManager.Instance.ToggleHUD(true);
            if (gameOverPanel == null) gameOverPanel = UIManager.Instance.gameOverPanel;
        }
        if (gameOverPanel != null) gameOverPanel.SetActive(false);


        // --- 6. START LOGIC (ĐÃ CẬP NHẬT TUTORIAL) ---

        // NẾU LÀ MÀN ĐẦU TIÊN (Index 0) -> VÀO CHẾ ĐỘ CHỜ (TUTORIAL)
        if (currentLevelIndex == 0)
        {
            Debug.Log("[SetupLevel] Tutorial Phase: Waiting for input...");
            isTutorialActive = true;
            hasGameStarted = false;
            isTimerRunning = false; // Dừng Timer

            // Hiển thị ảnh hướng dẫn
            if (UIManager.Instance != null) UIManager.Instance.ShowTutorial(true);

            // Tắt nhạc & Spawner
            if (musicSource != null) musicSource.Stop();
            if (myWaveSpawner != null) myWaveSpawner.StopSpawning();
        }
        else
        {
            // CÁC MÀN KHÁC: CHẠY LUÔN
            isTutorialActive = false;
            if (UIManager.Instance != null) UIManager.Instance.ShowTutorial(false);

            StartGameplayMechanics(); // Gọi hàm helper bên dưới
        }
    }
    void StartGameplayMechanics()
    {
        // Reset trạng thái Gopher mỗi khi vào màn mới
        isGopherScheduled = false;
        // Logic phân luồng Boss/Normal cũ được chuyển vào đây
        BossManager bossMgr = currentMapInstance.GetComponentInChildren<BossManager>();

        if (bossMgr != null)
        {
            Debug.Log("[GameManager] BOSS LEVEL START.");
            bossMgr.ActivateBossLevel();
            isTimerRunning = false;
            if (myWaveSpawner != null) myWaveSpawner.StopSpawning();
        }
        else
        {
            Debug.Log("[GameManager] NORMAL LEVEL START.");
            if (gopherPrefab != null)
            {
                float roll = Random.Range(0f, 100f);
                if (roll <= gopherSpawnChance)
                {
                    isGopherScheduled = true;
                    // Chọn thời điểm ngẫu nhiên (từ giây thứ 10 đến trước khi hết giờ 10s)
                    float minTime = 10f;
                    float maxTime = Mathf.Max(10f, levelDuration - 10f);
                    gopherSpawnTime = Random.Range(minTime, maxTime);

                    Debug.Log($"[Gopher] Will spawn at timer: {gopherSpawnTime}");
                }
            }
            // Bật nhạc thường
            if (musicSource != null)
            {
                if (defaultLevelMusic != null) musicSource.clip = defaultLevelMusic;
                musicSource.loop = true;
                musicSource.Play();
            }

            isTimerRunning = true;

            // Bật Spawner
            if (myWaveSpawner != null)
            {
                myWaveSpawner.SetWavePaused(false);
                myWaveSpawner.StartNextLevelWaves();
            }
        }
    }

    // --- HÀM 2: COROUTINE ĐẾM NGƯỢC 3 GIÂY ---
    IEnumerator StartGameWithDelay()
    {
        hasGameStarted = true; // Khóa input
        Debug.Log("Input Detected! Game starts in 3s...");

        // 1. UI Tutorial mờ dần
        if (UIManager.Instance != null)
        {
            StartCoroutine(UIManager.Instance.FadeOutTutorial(3f));
        }

        // 2. Đếm ngược 3 giây
        yield return new WaitForSeconds(3f);

        // 3. Bắt đầu game thật sự
        isTutorialActive = false;
        StartGameplayMechanics();
    }
    #endregion

    #region --- CORE GAME LOOP (TIMER & CLEAR) ---
    void HandleTimer()
    {
        if (!isTimerRunning) return;

        currentTime -= Time.deltaTime;
        if (isGopherScheduled && currentTime <= gopherSpawnTime)
        {
            SpawnGopher();
            isGopherScheduled = false; // Đảm bảo chỉ spawn 1 lần
        }
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

        Debug.Log("TIME UP! Stop Spawning & Wait for Clear.");
        isTimerRunning = false;
        isWaitingForClear = true; // Bật cờ chờ dọn quái

        // 1. Dừng sinh quái ngay lập tức
        if (myWaveSpawner != null) myWaveSpawner.StopSpawning();

        // 2. Kiểm tra Spikeball (nếu còn sót lại)
        CheckSpikeballEndCondition();
    }

    void HandleWaitingForClear()
    {
        if (!isWaitingForClear) return;

        // Logic cũ: Phải giết hết quái đang sống VÀ quái đang chờ spawn phải bằng 0
        int activeEnemyCount = GameObject.FindGameObjectsWithTag(TAG_ENEMY).Length;
        int pendingEnemyCount = (myWaveSpawner != null) ? myWaveSpawner.GetPendingEnemyCount() : 0;

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

        // Logic: Nếu tất cả quái còn lại là Spikeball thì làm yếu chúng đi
        bool allAreSpikeballs = true;
        List<Spikeball> list = new List<Spikeball>();

        foreach (GameObject e in enemies)
        {
            Spikeball sb = e.GetComponent<Spikeball>();
            if (sb == null) { allAreSpikeballs = false; break; }
            list.Add(sb);
        }

        if (allAreSpikeballs)
            foreach (var sb in list) sb.Weaken();
    }
    #endregion

    #region --- LEVEL TRANSITION ---
    public void CompleteCurrentArea()
    {
        Debug.Log("LEVEL CLEARED!");
        if (musicSource != null) musicSource.Stop();

        totalAreasPassed++;
        currentTime = levelDuration;
        isTimerRunning = false;

        if (UIManager.Instance != null) UIManager.Instance.UpdateAreaIndicator(totalAreasPassed);
        if (currentGate != null) currentGate.OpenGate();

        // Spawn Shop
        bool shouldSpawnShop = (totalAreasPassed > 0) && (totalAreasPassed % levelsPerShop == 0);
        if (shouldSpawnShop && vendorPrefab != null && currentMapInstance != null)
        {
            if (UpgradeManager.Instance != null) UpgradeManager.Instance.ResetPurchaseStatus();
            GameObject vendor = Instantiate(vendorPrefab, Vector3.zero, Quaternion.identity);
            vendor.transform.SetParent(currentMapInstance.transform);
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
            Debug.Log("WIN!");
            yield break;
        }

        LevelConfig nextLevelData = allLevels[currentLevelIndex];

        // 1. Tạo Map Mới (Offset xuống dưới)
        Vector3 offsetPosition = new Vector3(0f, -mapHeight, 0);
        GameObject newMap = Instantiate(nextLevelData.mapPrefab, offsetPosition, Quaternion.identity);
        newMap.name = "Map_" + nextLevelData.levelName;

        PlayerController pc = playerObject.GetComponent<PlayerController>();
        if (pc != null) pc.isInputEnabled = false;

        // 2. Di chuyển Camera & Player
        Vector3 targetWorldPos = offsetPosition;
        Transform newPSpawn = newMap.transform.Find(NAME_PLAYER_SPAWN_POINT);
        if (newPSpawn != null) targetWorldPos = newPSpawn.position;

        StartCoroutine(MoveCamera(offsetPosition, transitionTime));
        yield return StartCoroutine(pc.MoveToPosition(targetWorldPos, transitionTime));

        // 3. Origin Shift (Xóa map cũ, đưa map mới về 0)
        if (currentMapInstance != null)
        {
            if (transform.IsChildOf(currentMapInstance.transform)) transform.SetParent(null);
            Destroy(currentMapInstance);
        }
        ClearLeftoverEffects();

        newMap.transform.position = Vector3.zero;
        if (newPSpawn != null) playerObject.transform.position = newPSpawn.position;
        else playerObject.transform.position = Vector3.zero;

        Camera.main.transform.position = new Vector3(0, 0, -10);
        Physics2D.SyncTransforms();
        currentMapInstance = newMap;

        // 4. SETUP WAVE CHO MAP MỚI (FIX LỖI SPAWN TẠI ĐÂY)
        Gate newGate = newMap.GetComponentInChildren<Gate>();

        if (myWaveSpawner != null) // Dùng myWaveSpawner thay vì Instance
        {
            Transform enemySpawnParent = newMap.transform.Find(NAME_ENEMY_SPAWN_POINTS);
            Transform[] newSpawnPoints = new Transform[0];

            if (enemySpawnParent != null)
            {
                newSpawnPoints = new Transform[enemySpawnParent.childCount];
                for (int i = 0; i < enemySpawnParent.childCount; i++)
                    newSpawnPoints[i] = enemySpawnParent.GetChild(i);
            }

            // Cập nhật dữ liệu wave mới cho Spawner
            myWaveSpawner.UpdateLevelData(Vector3.zero, nextLevelData.waves, newSpawnPoints);
        }

        if (UIManager.Instance != null) UIManager.Instance.ToggleHUD(true);
        levelDuration = nextLevelData.levelDuration;
        currentTime = levelDuration;
        currentGate = newGate;
        if (currentGate != null) currentGate.CloseGate();

        // 5. Save Game (Auto Save khi qua màn)
        SaveGame();

        Debug.Log("Transition Done. Waiting 3s...");
        yield return new WaitForSeconds(3f);

        // 6. Start Level Logic
        BossManager bossMgr = newMap.GetComponentInChildren<BossManager>();
        if (bossMgr != null)
        {
            bossMgr.ActivateBossLevel();
            isTimerRunning = false;
        }
        else
        {
            if (musicSource != null)
            {
                if (defaultLevelMusic != null) musicSource.clip = defaultLevelMusic;
                musicSource.loop = true;
                musicSource.Play();
            }

            isTimerRunning = true;
            if (myWaveSpawner != null)
            {
                myWaveSpawner.SetWavePaused(false);
                myWaveSpawner.StartNextLevelWaves();
            }
        }

        if (pc != null) pc.isInputEnabled = true;
    }
    #endregion

    #region --- PLAYER STATE (DEATH & RESPAWN) ---
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
            // Hết mạng -> Gọi chuỗi Game Over
            StartCoroutine(GameOverSequence());
        }
    }

    IEnumerator RespawnSequence()
    {
        // 1. Dừng Game Logic
        isTimerRunning = false;
        if (musicSource != null) musicSource.Stop();

        // KHÔI PHỤC LOGIC CŨ: Báo cho Spawner biết player chết để thu gom quái
        if (myWaveSpawner != null) myWaveSpawner.OnPlayerDied();

        PlayerController pc = playerObject != null ? playerObject.GetComponent<PlayerController>() : null;
        if (pc != null) pc.TriggerDeathAnimation();

        yield return new WaitForSeconds(deathAnimationDuration);

        if (playerObject != null) playerObject.SetActive(false);
        yield return new WaitForSeconds(deathDuration);

        // 2. Hồi sinh
        if (playerObject != null)
        {
            if (useOverrideRespawn)
                playerObject.transform.position = overrideRespawnPosition;
            else if (currentMapInstance != null)
            {
                Transform pSpawn = currentMapInstance.transform.Find(NAME_PLAYER_SPAWN_POINT);
                playerObject.transform.position = (pSpawn != null) ? pSpawn.position : Vector3.zero;
            }
            playerObject.SetActive(true);
            if (pc != null) pc.ResetState();
        }

        if (pc != null) pc.TriggerRespawnInvincibility(invincibilityDuration);
        yield return new WaitForSeconds(invincibilityDuration);

        // 3. Resume Game -> Báo Spawner trả lại quái
        if (myWaveSpawner != null) myWaveSpawner.OnPlayerRespawned();

        if (useOverrideRespawn) isTimerRunning = false;
        else if (currentTime > 0) isTimerRunning = true;

        if (musicSource != null) musicSource.Play();
    }
    #endregion

    #region --- SYSTEM & HELPERS ---
    void ApplyPersistentData()
    {
        if (currentData == null) return;

        this.currentLives = currentData.lives;
        this.currentCoins = currentData.coins;

        if (UpgradeManager.Instance != null)
        {
            UpgradeManager.Instance.SetUpgradeLevels(currentData.gunLevel, currentData.bootLevel, currentData.ammoLevel);
        }
        if (playerObject != null)
        {
            PlayerController pc = playerObject.GetComponent<PlayerController>();
            if (pc != null)
            {
                if (currentData.hasHeldItem) pc.SetStoredItem(currentData.heldItemType);
                else pc.ClearStoredItem();
            }
        }
    }
    public void TogglePause()
    {
        // Nếu trạng thái không cho phép -> Bỏ qua
        if (!canPause) return;

        isPaused = !isPaused;

        if (isPaused)
        {
            // Dừng game
            Time.timeScale = 0f;
            if (UIManager.Instance != null) UIManager.Instance.ShowPauseMenu(true);
            if (musicSource != null) musicSource.Pause();
        }
        else
        {
            // Tiếp tục game
            Time.timeScale = 1f;
            if (UIManager.Instance != null) UIManager.Instance.ShowPauseMenu(false);
            if (musicSource != null) musicSource.UnPause();
        }
    }

    public void ExitToMainMenu()
    {
        // Quan trọng: Phải trả lại tốc độ thời gian trước khi load scene khác
        Time.timeScale = 1f;
        isPaused = false;

        // Reset các trạng thái game nếu cần
        SceneManager.LoadScene("MainMenu");
    }
    public void SaveGame()
    {
        if (currentSlotIndex == -1) return;
        currentData.lives = this.currentLives;
        currentData.coins = this.currentCoins;
        currentData.currentLevelIndex = this.currentLevelIndex;
        SaveSystem.SaveGame(currentData, currentSlotIndex);
    }

    void ClearLeftoverEffects()
    {
        GameObject[] fxList = GameObject.FindGameObjectsWithTag(TAG_DEATH_FX);
        foreach (var fx in fxList) Destroy(fx);
    }

    // Các hàm Drop System, Audio, IEnum MoveCamera... (Giữ nguyên như cũ)
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
    public void AddCoin(int amount) { currentCoins += amount; if (UIManager.Instance != null) UIManager.Instance.UpdateCoins(currentCoins); }
    public void AddLife(int amount) { currentLives += amount; if (UIManager.Instance != null) UIManager.Instance.UpdateLives(currentLives); }
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
        foreach (var e in enemies) { var s = e.GetComponent<Enemy>(); if (s) s.SetStunState(isStunned); }
    }
    void RestartLevel() { SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Time.timeScale = 1f;
        if (scene.name == SCENE_GAME_NAME) SetupLevel();
    }
    IEnumerator GameOverSequence()
    {
        isGameOver = true;
        isTimerRunning = false; // Dừng đồng hồ

        // 1. Xử lý nhân vật
        if (playerObject != null) playerObject.SetActive(false);
        if (myWaveSpawner != null) myWaveSpawner.StopSpawning(); // Dừng sinh quái

        // 2. Xử lý âm thanh
        if (musicSource != null)
        {
            musicSource.Stop();
            if (gameOverClip != null)
            {
                // Phát nhạc Game Over (không loop)
                musicSource.loop = false;
                musicSource.clip = gameOverClip;
                musicSource.Play();


                yield return new WaitForSecondsRealtime(3f);
            }
        }

        // 3. Hiển thị Menu (Dừng game hoàn toàn)
        Time.timeScale = 0f;
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }

    // --- HÀM CHO NÚT RESTART (QUAN TRỌNG) ---
    public void RetryLevel()
    {
        // Trả lại thời gian trước khi load
        Time.timeScale = 1f;
        isPaused = false;

        // LOGIC CHÌA KHÓA: 
        // Thay vì chỉ load scene, ta gọi lại hàm LoadGameAndPlay với isNewGame = false.
        // Hàm này sẽ đọc file Save (được tạo lúc bắt đầu màn chơi) -> Khôi phục Máu, Tiền, Item về lúc mới vào màn.
        LoadGameAndPlay(currentSlotIndex, false);
    }
    #endregion
    void SpawnGopher()
    {
        if (gopherPrefab == null) return;

        // Chọn ngẫu nhiên 1 trong 4 cạnh: 0=Top, 1=Bottom, 2=Left, 3=Right
        int side = Random.Range(0, 4);
        Vector3 spawnPos = Vector3.zero;

        // Random một điểm trên cạnh đó (giả sử map hình vuông/chữ nhật)
        // mapHeight là biến bạn đã có, ta dùng gopherSpawnDistance cho an toàn
        float randomOffset = Random.Range(-6.5f, 6.5f);

        switch (side)
        {
            case 0: // Top (Y dương)
                spawnPos = new Vector3(randomOffset, gopherSpawnDistance, 0);
                break;
            case 1: // Bottom (Y âm)
                spawnPos = new Vector3(randomOffset, -gopherSpawnDistance, 0);
                break;
            case 2: // Left (X âm)
                spawnPos = new Vector3(-gopherSpawnDistance, randomOffset, 0);
                break;
            case 3: // Right (X dương)
                spawnPos = new Vector3(gopherSpawnDistance, randomOffset, 0);
                break;
        }

        Instantiate(gopherPrefab, spawnPos, Quaternion.identity);
        Debug.Log($"[Gopher] Spawned at {spawnPos}");
    }
}