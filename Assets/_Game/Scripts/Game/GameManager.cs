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

    public bool useOverrideRespawn = false;
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
    public List<string> levelNames;

    private int currentLevelCoinsSpawned = 0;

    [Header("Drop System")]
    public List<PowerUpData> allowedDrops;
    [Range(0f, 100f)] public float dropChance = 30f;

    [Header("Shop Settings")]
    public GameObject vendorPrefab;
    [Tooltip("Danh sách index các level mà sau khi hoàn thành sẽ có Shop (Bắt đầu từ 0)")]
    public List<int> shopSpawnLevels;

    [Header("Gopher Event Settings")]
    public GameObject gopherPrefab;
    [Range(0, 100)] public float gopherSpawnChance = 5f;
    public float gopherSpawnDistance = 6.5f;

    private bool isGopherScheduled = false;
    private float gopherSpawnTime = 0f;

    [Header("Audio")]
    public AudioSource musicSource;
    public AudioClip defaultLevelMusic;
    public AudioClip gameOverClip;
    public AudioClip pauseClip;
    public AudioClip unpauseClip;

    [Header("UI Audio Global")]
    public AudioSource uiAudioSource;
    public AudioClip buttonHoverClip;
    public AudioClip buttonClickClip;

    [Header("Tutorial Settings")]
    public bool isTutorialActive = false;
    private bool hasGameStarted = false;
    #endregion

    #region --- STATE VARIABLES ---
    [Header("References")]
    public GameObject gameOverPanel;
    public GameObject playerObject;

    private WaveSpawner myWaveSpawner;

    [Header("Debug Info")]
    [SerializeField] private GameObject currentMapInstance;
    [SerializeField] private float currentTime;
    [SerializeField] public int currentLives;
    [SerializeField] public int currentLevelIndex = 0;

    [Header("Game Flags")]
    public int totalAreasPassed = 0;
    public int currentCoins = 0;
    public bool isSmokeBombActive = false;

    private bool isTimerRunning = false;
    private bool isWaitingForClear = false;
    private bool isGameOver = false;

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

            myWaveSpawner = GetComponent<WaveSpawner>();
        }
        else Destroy(gameObject);
    }

    void Start()
    {
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
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveY = Input.GetAxisRaw("Vertical");
            bool shootKeys = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow) ||
                             Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow);

            if (moveX != 0 || moveY != 0 || shootKeys)
            {
                StartCoroutine(StartGameWithDelay());
            }

            return;
        }
        HandleTimer();
        HandleWaitingForClear();
    }
    #endregion

    #region --- INITIALIZATION & SETUP ---

    public void LoadGameAndPlay(int slotIndex, bool isNewGame)
    {
        currentSlotIndex = slotIndex;
        if (isNewGame)
        {
            currentLives = startLives;
            currentCoins = 0;
            currentLevelIndex = startingLevelIndex;

            currentData = new GameData();
            currentData.lives = startLives;
            currentData.coins = 0;
            currentData.currentLevelIndex = startingLevelIndex;

            SaveGame();
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
        isGameOver = false;
        isWaitingForClear = false;

        GameObject existingMap = GameObject.Find(NAME_INITIAL_MAP);

        if (currentLevelIndex < allLevels.Count)
        {
            LevelConfig levelData = allLevels[currentLevelIndex];
            levelDuration = levelData.levelDuration;

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
            return;
        }

        currentTime = levelDuration;
        isTimerRunning = true;
        Time.timeScale = 1f;

        if (playerObject == null) playerObject = GameObject.FindGameObjectWithTag(TAG_PLAYER);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        if (currentMapInstance != null)
        {
            currentGate = currentMapInstance.GetComponentInChildren<Gate>();

            Transform pSpawn = currentMapInstance.transform.Find(NAME_PLAYER_SPAWN_POINT);
            if (playerObject != null)
            {
                playerObject.transform.position = (pSpawn != null) ? pSpawn.position : Vector3.zero;
                playerObject.SetActive(true);
            }

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

                myWaveSpawner.UpdateLevelData(Vector3.zero, levelData.waves, foundSpawnPoints);
            }
        }

        ApplyPersistentData();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetTimerColor(timerNormalColor);
            UIManager.Instance.UpdateLives(currentLives);
            UIManager.Instance.UpdateCoins(currentCoins);

            string nameToShow = "";

            if (levelNames != null && currentLevelIndex < levelNames.Count)
            {
                nameToShow = levelNames[currentLevelIndex];
            }
            else
            {
                if (currentLevelIndex >= 0 && currentLevelIndex <= 4)
                {
                    nameToShow = $"Prairie\nArea {currentLevelIndex + 1}";
                }
                else if (currentLevelIndex >= 5 && currentLevelIndex <= 8)
                {
                    nameToShow = $"Forest\nArea {currentLevelIndex - 4}";
                }
                else if (currentLevelIndex >= 9 && currentLevelIndex <= 12)
                {
                    nameToShow = $"Graveyard\nArea {currentLevelIndex - 8}";
                }
                else
                {
                    Debug.LogWarning("Level name not defined for current level index: " + currentLevelIndex);
                }
            }
            UIManager.Instance.ShowLevelName(nameToShow);

            UIManager.Instance.UpdateTimer(currentTime, levelDuration);
            UIManager.Instance.ToggleHUD(true);
            if (gameOverPanel == null) gameOverPanel = UIManager.Instance.gameOverPanel;
        }
        if (gameOverPanel != null) gameOverPanel.SetActive(false);


        if (currentLevelIndex == 0)
        {
            isTutorialActive = true;
            hasGameStarted = false;
            isTimerRunning = false;

            if (UIManager.Instance != null) UIManager.Instance.ShowTutorial(true);

            if (musicSource != null) musicSource.Stop();
            if (myWaveSpawner != null) myWaveSpawner.StopSpawning();
        }
        else
        {
            isTutorialActive = false;
            if (UIManager.Instance != null) UIManager.Instance.ShowTutorial(false);

            StartGameplayMechanics();
        }
    }
    void StartGameplayMechanics()
    {
        isGopherScheduled = false;
        BossManager bossMgr = currentMapInstance.GetComponentInChildren<BossManager>();

        if (bossMgr != null)
        {
            bossMgr.ActivateBossLevel();
            isTimerRunning = false;
            if (myWaveSpawner != null) myWaveSpawner.StopSpawning();
        }
        else
        {
            if (gopherPrefab != null)
            {
                float roll = Random.Range(0f, 100f);
                if (roll <= gopherSpawnChance)
                {
                    isGopherScheduled = true;
                    float minTime = 10f;
                    float maxTime = Mathf.Max(10f, levelDuration - 10f);
                    gopherSpawnTime = Random.Range(minTime, maxTime);
                }
            }
            if (musicSource != null)
            {
                if (defaultLevelMusic != null) musicSource.clip = defaultLevelMusic;
                musicSource.loop = true;
                musicSource.Play();
            }
            if (musicSource != null)
            {
                musicSource.Stop();
                musicSource.time = 0f;

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
    }

    IEnumerator StartGameWithDelay()
    {
        hasGameStarted = true;

        if (UIManager.Instance != null)
        {
            StartCoroutine(UIManager.Instance.FadeOutTutorial(3f));
        }

        yield return new WaitForSeconds(3f);

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
            isGopherScheduled = false;
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

        isTimerRunning = false;
        isWaitingForClear = true;

        if (myWaveSpawner != null) myWaveSpawner.StopSpawning();

        CheckSpikeballEndCondition();
    }

    void HandleWaitingForClear()
    {
        if (!isWaitingForClear) return;

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
        if (musicSource != null) musicSource.Stop();

        totalAreasPassed++;
        currentTime = levelDuration;
        isTimerRunning = false;

        if (UIManager.Instance != null) UIManager.Instance.UpdateAreaIndicator(totalAreasPassed);
        if (currentGate != null) currentGate.OpenGate();

        bool shouldSpawnShop = false;

        if (shopSpawnLevels != null && shopSpawnLevels.Contains(currentLevelIndex))
        {
            shouldSpawnShop = true;
        }
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
            yield break;
        }

        LevelConfig nextLevelData = allLevels[currentLevelIndex];

        Vector3 offsetPosition = new Vector3(0f, -mapHeight, 0);
        GameObject newMap = Instantiate(nextLevelData.mapPrefab, offsetPosition, Quaternion.identity);
        newMap.name = "Map_" + nextLevelData.levelName;

        PlayerController pc = playerObject.GetComponent<PlayerController>();
        if (pc != null) pc.isInputEnabled = false;

        Vector3 targetWorldPos = offsetPosition;
        Transform newPSpawn = newMap.transform.Find(NAME_PLAYER_SPAWN_POINT);
        if (newPSpawn != null) targetWorldPos = newPSpawn.position;

        StartCoroutine(MoveCamera(offsetPosition, transitionTime));
        yield return StartCoroutine(pc.MoveToPosition(targetWorldPos, transitionTime));

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

        Gate newGate = newMap.GetComponentInChildren<Gate>();

        if (myWaveSpawner != null)
        {
            Transform enemySpawnParent = newMap.transform.Find(NAME_ENEMY_SPAWN_POINTS);
            Transform[] newSpawnPoints = new Transform[0];

            if (enemySpawnParent != null)
            {
                newSpawnPoints = new Transform[enemySpawnParent.childCount];
                for (int i = 0; i < enemySpawnParent.childCount; i++)
                    newSpawnPoints[i] = enemySpawnParent.GetChild(i);
            }

            myWaveSpawner.UpdateLevelData(Vector3.zero, nextLevelData.waves, newSpawnPoints);
        }

        if (UIManager.Instance != null) UIManager.Instance.ToggleHUD(true);
        levelDuration = nextLevelData.levelDuration;
        currentTime = levelDuration;
        currentGate = newGate;
        if (currentGate != null) currentGate.CloseGate();

        SaveGame();

        yield return new WaitForSeconds(3f);

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
            StartCoroutine(GameOverSequence());
        }
    }

    IEnumerator RespawnSequence()
    {
        isTimerRunning = false;
        if (musicSource != null) musicSource.Stop();

        if (myWaveSpawner != null) myWaveSpawner.OnPlayerDied();

        PlayerController pc = playerObject != null ? playerObject.GetComponent<PlayerController>() : null;
        if (pc != null) pc.TriggerDeathAnimation();

        yield return new WaitForSeconds(deathAnimationDuration);

        if (playerObject != null) playerObject.SetActive(false);
        yield return new WaitForSeconds(deathDuration);

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
        if (!canPause) return;

        isPaused = !isPaused;

        if (isPaused)
        {
            Time.timeScale = 0f;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowPauseMenu(true);
            }
            if (musicSource != null) musicSource.Pause();
            if (uiAudioSource != null)
                uiAudioSource.PlayOneShot(pauseClip);
        }
        else
        {
            Time.timeScale = 1f;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowPauseMenu(false);
            }
            if (uiAudioSource != null)
                uiAudioSource.PlayOneShot(unpauseClip);
            if (musicSource != null) musicSource.UnPause();
        }
    }

    public void ExitToMainMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        if (musicSource != null)
        {
            musicSource.Stop();
            musicSource.time = 0f;
        }
        SceneManager.LoadScene("MainMenu");
    }
    public void SaveGame()
    {
        if (currentSlotIndex == -1) return;

        currentData.lives = this.currentLives;
        currentData.coins = this.currentCoins;
        currentData.currentLevelIndex = this.currentLevelIndex;

        if (UpgradeManager.Instance != null)
        {
            currentData.bootLevel = UpgradeManager.Instance.currentBootLevel;
            currentData.gunLevel = UpgradeManager.Instance.currentGunLevel;
            currentData.ammoLevel = UpgradeManager.Instance.currentAmmoLevel;
        }

        SaveSystem.SaveGame(currentData, currentSlotIndex);
    }

    void ClearLeftoverEffects()
    {
        GameObject[] fxList = GameObject.FindGameObjectsWithTag(TAG_DEATH_FX);
        foreach (var fx in fxList) Destroy(fx);
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
        isTimerRunning = false;

        if (playerObject != null) playerObject.SetActive(false);
        if (myWaveSpawner != null) myWaveSpawner.StopSpawning();

        if (musicSource != null)
        {
            musicSource.Stop();
            if (gameOverClip != null)
            {
                musicSource.loop = false;
                musicSource.clip = gameOverClip;
                musicSource.Play();


                yield return new WaitForSecondsRealtime(3f);
            }
        }

        Time.timeScale = 0f;
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }

    public void RetryLevel()
    {
        Time.timeScale = 1f;
        isPaused = false;

        LoadGameAndPlay(currentSlotIndex, false);
    }
    #endregion
    void SpawnGopher()
    {
        if (gopherPrefab == null) return;

        int side = Random.Range(0, 4);
        Vector3 spawnPos = Vector3.zero;

        float randomOffset = Random.Range(-6.5f, 6.5f);

        switch (side)
        {
            case 0:
                spawnPos = new Vector3(randomOffset, gopherSpawnDistance, 0);
                break;
            case 1:
                spawnPos = new Vector3(randomOffset, -gopherSpawnDistance, 0);
                break;
            case 2:
                spawnPos = new Vector3(-gopherSpawnDistance, randomOffset, 0);
                break;
            case 3:
                spawnPos = new Vector3(gopherSpawnDistance, randomOffset, 0);
                break;
        }

        Instantiate(gopherPrefab, spawnPos, Quaternion.identity);
    }
    public void PlayHoverSound()
    {
        if (uiAudioSource != null && buttonHoverClip != null)
        {
            uiAudioSource.ignoreListenerPause = true;
            uiAudioSource.PlayOneShot(buttonHoverClip);
        }
    }

    public void PlayClickSound()
    {
        if (uiAudioSource != null && buttonClickClip != null)
        {
            uiAudioSource.ignoreListenerPause = true;
            uiAudioSource.PlayOneShot(buttonClickClip);
        }
    }
}