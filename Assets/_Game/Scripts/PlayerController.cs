using System.Collections;
using UnityEngine;
using static UnityEditor.Progress; // Lưu ý: Dòng này có thể không cần thiết nếu không dùng Editor script, nhưng tôi giữ nguyên theo file gốc

public class PlayerController : MonoBehaviour
{
    #region Configuration & Settings

    [Header("Movement & Physics")]
    public float moveSpeed = 5f;
    public float maxSpeed = 8f;
    public Rigidbody2D rb;
    public Vector2 mapBoundsMin = new Vector2(-6f, -5f);
    public Vector2 mapBoundsMax = new Vector2(6f, 5f);

    [Header("Combat Settings")]
    public GameObject bulletPrefab;
    public int currentBulletDamage = 1;
    public Transform firePoint;
    public float bulletOffset = 0.5f;
    public float currentFireRate = 0.4f; // Delay giữa các phát bắn (càng nhỏ càng nhanh)
    public float maxFireRate = 0.05f;    // Giới hạn tốc độ bắn nhanh nhất

    [Header("Audio Settings")]
    public AudioSource gunAudioSource;
    public AudioSource footstepAudioSource;
    public AudioSource itemsPickupAudioSource;
    public AudioSource sfxAudioSource;
    public AudioSource zombieAudioSource;

    public AudioClip shootClip;
    public AudioClip footstepClip;
    public AudioClip itemPickupClip;
    public AudioClip upgradePurchasedClip;
    public AudioClip playerDeathClip;
    public AudioClip lightningSound;
    public AudioClip zombieMusic;

    public float stepDelay = 0.3f;

    [Header("Visual References")]
    public GameObject idleStateObject;
    public GameObject activeStateObject;
    public SpriteRenderer bodyRenderer;
    public SpriteRenderer legsRenderer;
    public SpriteRenderer idleRenderer;
    public SpriteRenderer bodySpriteDisplay;
    public Animator legsAnimator;

    [Header("Directional Sprites")]
    public Sprite bodyUp;
    public Sprite bodyDown;
    public Sprite bodyLeft;
    public Sprite bodyRight;

    #endregion

    #region Power-Ups Configuration

    [Header("Power-Up State")]
    public PowerUpData heldItem;
    public bool enableAutoFire = false;

    [Header("Skill: Coffee")]
    public float coffeeSpeed = 7f;

    [Header("Skill: Heavy Machine Gun")]
    public float hmgFireRate = 0.05f; // Sửa lại mặc định cho khớp mô tả (0.05)

    [Header("Skill: Shotgun")]
    public float shotgunSpreadAngle = 15f;
    public float shotgunFireRateDecrease = 0.2f; // Cộng thêm vào delay (bắn chậm đi)

    [Header("Skill: Sheriff Badge")]
    public float sheriffBadgeSpeed = 6.5f;
    public float sheriffBadgeFireRate = 0.1f; // Sửa lại mặc định cho khớp mô tả (0.1)

    [Header("Skill: Nuke")]
    public GameObject explosionFXPrefab;
    public int explosionCount = 15;
    public float nukeDuration = 2f;

    [Header("Skill: Smoke Bomb")]
    public GameObject smokeCloudPrefab;
    public LayerMask obstacleLayer;
    public int smokeCount = 4;
    public float smokeBombDuration = 4f;

    [Header("Skill: Tombstone (Visuals)")]
    public GameObject darknessPrefab;
    public GameObject struckFxObject;
    public GameObject lightningFxObject;
    public Sprite[] struckSprites;
    public Sprite[] lightningSprites;
    public float flashSpeed = 0.1f;
    public float cutsceneDuration = 3f;


    [Header("Skill: Tombstone (Zombie Mode)")]
    public GameObject zombieModelObject;
    public Animator zombieAnimator;
    public float zombieDuration = 8f;
    public float zombieSpeed = 6f;
    private float zombieTimer = 0f; // Biến đếm ngược thời gian Zombie
    private Coroutine tombstoneCoroutine;

    [Header("Shop Interactions")]
    public Sprite handsUpSprite;       // Sprite giơ 2 tay lên trời
    public SpriteRenderer itemLiftDisplay; // SpriteRenderer con (nằm trên đầu player) để hiện vật phẩm

    #endregion

    #region Internal State Variables

    // Movement & Combat State
    private Vector2 moveInput;
    private float nextFireTime = 0f;
    private float nextStepTime = 0f;

    // --- LOGIC MỚI: TIMERS ---
    // Thay vì dùng Coroutine, ta lưu thời điểm hết hạn của từng loại buff
    private float hmgExpirationTime = 0f;
    private float shotgunExpirationTime = 0f;
    private float wheelExpirationTime = 0f;
    private float sheriffExpirationTime = 0f;
    private float coffeeExpirationTime = 0f;

    // Flags (Được cập nhật tự động mỗi frame dựa trên Timer)
    private bool isHMGActive = false;
    private bool isShotgunActive = false;
    private bool isWheelActive = false;
    private bool isSheriffActive = false;
    private bool isCoffeeActive = false;

    // Player Status
    private bool isDead = false;
    private bool isInvincible = false;
    [HideInInspector] public bool isZombieMode = false;

    // Defaults for Reset (Lưu giá trị gốc + Upgrade)
    private float defaultFireRate;
    private float defaultMoveSpeed;
    private GameObject defaultBulletPrefab;

    public bool isInputEnabled = true;
    #endregion

    #region Unity Lifecycle

    void Start()
    {
        defaultFireRate = currentFireRate;
        defaultMoveSpeed = moveSpeed;
        defaultBulletPrefab = bulletPrefab;

        if (legsRenderer != null && legsAnimator == null)
            legsAnimator = legsRenderer.GetComponent<Animator>();

        if (rb == null) rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (isDead) return;

        // Chỉ di chuyển vật lý khi không bị khóa input và không phải Kinematic (đang cutscene)
        if (rb != null && rb.bodyType != RigidbodyType2D.Kinematic)
            rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }

    void Update()
    {
        // 1. INPUT CHECK
        if (!isInputEnabled)
        {
            moveInput = Vector2.zero; // Ngắt hoàn toàn tín hiệu di chuyển cũ
            return;
        }
        if (isDead)
        {
            moveInput = Vector2.zero;
            return;
        }

        // 2. LOGIC VŨ KHÍ & BUFF (QUAN TRỌNG: Chạy mỗi frame)
        HandleBuffTimers();
        RecalculateStats();

        // 3. Movement Input
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(moveX, moveY).normalized;

        // 4. Shooting Input
        float shootX = 0f;
        float shootY = 0f;

        if (Input.GetKey(KeyCode.UpArrow)) shootY = 1f;
        else if (Input.GetKey(KeyCode.DownArrow)) shootY = -1f;
        if (Input.GetKey(KeyCode.LeftArrow)) shootX = -1f;
        else if (Input.GetKey(KeyCode.RightArrow)) shootX = 1f;

        bool attemptedToShoot = (shootX != 0 || shootY != 0);

        if (attemptedToShoot && Time.time >= nextFireTime)
        {
            Vector2 inputShootDir = new Vector2(shootX, shootY);
            Shoot(inputShootDir.normalized);
            nextFireTime = Time.time + currentFireRate;

            if (!isZombieMode)
            {
                UpdateActiveModel(moveInput.magnitude > 0.1f, inputShootDir);
            }
        }

        // 5. Visual State Logic
        HandleVisuals(attemptedToShoot);

        // 6. Item Activation
        if (Input.GetKeyDown(KeyCode.Space) && heldItem != null)
        {
            ActivateItem(heldItem);
            heldItem = null;
            if (UIManager.Instance != null) UIManager.Instance.UpdateItem(null);
        }
    }

    #endregion

    #region Core Logic: Buffs & Stats (UPDATED)

    // Hàm này kiểm tra xem buff nào còn hạn sử dụng
    void HandleBuffTimers()
    {
        float now = Time.time;
        isHMGActive = now < hmgExpirationTime;
        isShotgunActive = now < shotgunExpirationTime;
        isWheelActive = now < wheelExpirationTime;
        isSheriffActive = now < sheriffExpirationTime;
        isCoffeeActive = now < coffeeExpirationTime;
    }

    // Hàm này tổng hợp tất cả các buff đang active để ra chỉ số cuối cùng
    // LOGIC: LUÔN LẤY CHỈ SỐ TỐT NHẤT (Max Speed, Min Delay)
    void RecalculateStats()
    {
        // --- 1. TÍNH TOÁN TỐC ĐỘ DI CHUYỂN ---
        // Bắt đầu từ tốc độ gốc (đã bao gồm nâng cấp Boots)
        float targetSpeed = defaultMoveSpeed;

        // So sánh với các buff, lấy giá trị lớn nhất (nhanh nhất)
        if (isZombieMode)
        {
            targetSpeed = Mathf.Max(targetSpeed, zombieSpeed);
        }
        if (isCoffeeActive)
        {
            targetSpeed = Mathf.Max(targetSpeed, coffeeSpeed);
        }
        if (isSheriffActive)
        {
            targetSpeed = Mathf.Max(targetSpeed, sheriffBadgeSpeed);
        }

        moveSpeed = targetSpeed;

        // Hiệu ứng visual chạy nhanh (chỉ cần tốc độ > gốc một chút là tính)
        if (legsAnimator != null)
        {
            legsAnimator.speed = (moveSpeed > 5.5f) ? 2f : 1f;
        }


        // --- 2. TÍNH TOÁN TỐC ĐỘ BẮN (FIRE RATE) ---
        // Bắt đầu từ tốc độ gốc (đã bao gồm nâng cấp Gun)
        float targetFireRate = defaultFireRate;
        enableAutoFire = false;

        // A. Xử lý logic Shotgun cơ bản
        // Shotgun chỉ làm chậm tốc độ nếu KHÔNG CÓ các buff súng xịn hơn (HMG/Sheriff)
        if (isShotgunActive && !isHMGActive && !isSheriffActive)
        {
            targetFireRate += shotgunFireRateDecrease; // Cộng thêm delay (bắn chậm đi)
        }

        // B. Xử lý các Buff súng xịn (Lấy Min vì delay càng nhỏ bắn càng nhanh)

        if (isSheriffActive)
        {
            targetFireRate = Mathf.Min(targetFireRate, sheriffBadgeFireRate);
            enableAutoFire = true;
        }

        if (isHMGActive)
        {
            // HMG thường là nhanh nhất (0.05), Min sẽ đảm bảo nó đè được cả Sheriff (0.1)
            targetFireRate = Mathf.Min(targetFireRate, hmgFireRate);
            enableAutoFire = true;
        }

        currentFireRate = targetFireRate;

        // C. Xử lý Wheel (chỉ tắt auto fire, tốc độ giữ theo tính toán ở trên)
        if (isWheelActive && !isHMGActive && !isSheriffActive)
        {
            enableAutoFire = false;
        }
    }

    #endregion

    #region Item System

    public void PickUpItem(PowerUpData newItem)
    {
        // Instant Items (Coin, Life)
        if (newItem.type == PowerUpType.Coin)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddCoin((int)newItem.valueAmount);
                if (itemsPickupAudioSource != null) itemsPickupAudioSource.PlayOneShot(newItem.activateSound);
            }
            return;
        }
        if (newItem.type == PowerUpType.Life)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddLife(1);
                if (itemsPickupAudioSource != null) itemsPickupAudioSource.PlayOneShot(newItem.activateSound);
            }
            return;
        }

        // Held Items
        if (heldItem != null)
        {
            ActivateItem(newItem); // Dùng ngay item mới nhặt
            if (sfxAudioSource != null && newItem.activateSound != null)
                sfxAudioSource.PlayOneShot(newItem.activateSound);
        }
        else
        {
            heldItem = newItem; // Cất vào túi
            if (UIManager.Instance != null) UIManager.Instance.UpdateItem(heldItem.icon);
            if (itemsPickupAudioSource != null && itemPickupClip != null)
                sfxAudioSource.PlayOneShot(itemPickupClip);
        }
    }

    void ActivateItem(PowerUpData item)
    {
        if (item.activateSound != null)
            sfxAudioSource.PlayOneShot(item.activateSound);

        // 1. Skill Items (Xử lý riêng)
        if (item.type == PowerUpType.ScreenNuke || item.type == PowerUpType.SmokeBomb || item.type == PowerUpType.Tombstone)
        {
            switch (item.type)
            {
                case PowerUpType.ScreenNuke: StartCoroutine(NukeRoutine()); break;
                case PowerUpType.SmokeBomb:
                    if (GameManager.Instance != null) GameManager.Instance.ActivateGlobalStun(smokeBombDuration);
                    TeleportAndSmoke();
                    break;
                case PowerUpType.Tombstone:
                    //RESET HOÀN TOÀN
                    // Dù đang là người hay Zombie, khi ăn Tombstone mới:
                    // 1. Ngắt ngay tiến trình cũ
                    if (tombstoneCoroutine != null) StopCoroutine(tombstoneCoroutine);

                    // 2. Chạy lại quy trình từ đầu (Sét đánh -> Biến hình -> Timer mới)
                    tombstoneCoroutine = StartCoroutine(TombstoneRoutine());
                    break;
            }
            return;
        }

        // 2. Buff Items (Cộng thời gian)
        if (item.duration > 0)
        {
            ApplyBuffDuration(item);
        }
    }

    void ApplyBuffDuration(PowerUpData item)
    {
        float duration = item.duration;
        float now = Time.time;

        // Logic: Nếu đang còn hạn thì cộng dồn (hoặc làm mới), nếu hết hạn thì đặt mốc mới từ bây giờ
        switch (item.type)
        {
            case PowerUpType.HeavyMachineGun:
                hmgExpirationTime = Mathf.Max(now, hmgExpirationTime) + duration;
                break;

            case PowerUpType.Shotgun:
                shotgunExpirationTime = Mathf.Max(now, shotgunExpirationTime) + duration;
                break;

            case PowerUpType.WagonWheel:
                wheelExpirationTime = Mathf.Max(now, wheelExpirationTime) + duration;
                break;

            case PowerUpType.SheriffBadge:
                sheriffExpirationTime = Mathf.Max(now, sheriffExpirationTime) + duration;
                break;

            case PowerUpType.Coffee:
                coffeeExpirationTime = Mathf.Max(now, coffeeExpirationTime) + duration;
                break;
        }
        Debug.Log($"Activated Buff: {item.itemName}");
    }

    #endregion

    #region Combat Execution

    void Shoot(Vector2 dir)
    {
        // Logic bắn đạn dựa trên các cờ đang active
        // Lưu ý: Sheriff active cũng bật isShotgunActive và isAutoFire trong RecalculateStats rồi
        // Nhưng ở đây ta check trực tiếp isSheriffActive để chắc chắn bắn chùm

        if (isWheelActive)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f;
                Vector2 wheelDir = Quaternion.Euler(0, 0, angle) * Vector2.up;
                SpawnBullet(wheelDir);
            }
        }
        else if (isShotgunActive || isSheriffActive)
        {
            SpawnBullet(dir);
            SpawnBullet(Quaternion.Euler(0, 0, shotgunSpreadAngle) * dir);
            SpawnBullet(Quaternion.Euler(0, 0, -1 * shotgunSpreadAngle) * dir);
        }
        else
        {
            SpawnBullet(dir);
        }

        if (gunAudioSource && shootClip) gunAudioSource.PlayOneShot(shootClip);
    }

    void SpawnBullet(Vector2 dir)
    {
        Vector3 spawnPos = transform.position + (Vector3)(dir * bulletOffset);
        if (firePoint != null) spawnPos = firePoint.position;
        else spawnPos = transform.position + (Vector3)(dir.normalized * bulletOffset);

        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        var bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript)
        {
            bulletScript.Setup(dir, currentBulletDamage);
        }
    }

    #endregion

    #region Visuals & Audio

    void HandleVisuals(bool attemptedToShoot)
    {
        if (isZombieMode)
        {
            if (zombieAnimator != null)
            {
                SpriteRenderer zSR = zombieAnimator.GetComponent<SpriteRenderer>();
                if (zSR != null)
                {
                    if (moveInput.x < 0) zSR.flipX = true;
                    else if (moveInput.x > 0) zSR.flipX = false;
                }
                zombieAnimator.SetBool("IsMoving", moveInput.magnitude > 0.1f);
            }
        }
        else
        {
            bool isMoving = moveInput.magnitude > 0.1f;
            if (!isMoving && !attemptedToShoot)
            {
                SetVisualState(isIdle: true);
            }
            else
            {
                SetVisualState(isIdle: false);
                if (!attemptedToShoot) UpdateActiveModel(isMoving, moveInput);
                HandleFootsteps(isMoving);
            }
        }
    }

    void HandleFootsteps(bool isMoving)
    {
        if (isMoving && Time.time >= nextStepTime)
        {
            if (footstepAudioSource && footstepClip) footstepAudioSource.PlayOneShot(footstepClip);
            nextStepTime = Time.time + stepDelay;
        }
    }

    void SetVisualState(bool isIdle)
    {
        if (idleStateObject) idleStateObject.SetActive(isIdle);
        if (activeStateObject) activeStateObject.SetActive(!isIdle);
    }

    void UpdateActiveModel(bool isMoving, Vector2 dir)
    {
        if (legsAnimator) legsAnimator.SetBool("IsMoving", isMoving);
        if (bodySpriteDisplay == null) return;

        if (dir.y > 0) bodySpriteDisplay.sprite = bodyUp;
        else if (dir.y < 0) bodySpriteDisplay.sprite = bodyDown;
        else if (dir.x < 0) bodySpriteDisplay.sprite = bodyLeft;
        else if (dir.x > 0) bodySpriteDisplay.sprite = bodyRight;
    }

    void ToggleVisibility(bool isVisible)
    {
        if (bodyRenderer) bodyRenderer.enabled = isVisible;
        if (legsRenderer) legsRenderer.enabled = isVisible;
        if (idleRenderer) idleRenderer.enabled = isVisible;
    }

    #endregion

    #region Skill Routines (Nuke, Smoke, Tombstone)

    IEnumerator NukeRoutine()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var e in enemies)
        {
            var enemyScript = e.GetComponent<Enemy>();
            if (enemyScript != null) enemyScript.Die(false);
            else Destroy(e);
        }

        int explosionsSpawned = 0;
        while (explosionsSpawned < explosionCount)
        {
            float randomX = Random.Range(mapBoundsMin.x, mapBoundsMax.x);
            float randomY = Random.Range(mapBoundsMin.y, mapBoundsMax.y);
            Vector3 explosionPos = new Vector3(randomX, randomY, 0);

            if (explosionFXPrefab != null)
                Instantiate(explosionFXPrefab, explosionPos, Quaternion.identity);

            explosionsSpawned++;
            float waitTime = (nukeDuration / explosionCount) * Random.Range(0.5f, 1.5f);
            yield return new WaitForSeconds(waitTime);
        }
    }

    void TeleportAndSmoke()
    {
        Vector3 targetPos = FindSafePosition();
        transform.position = targetPos;
        StartCoroutine(InvincibilityRoutine(3f));
        StartCoroutine(SpawnMultipleSmokes(targetPos));
    }

    Vector3 FindSafePosition()
    {
        int maxAttempts = 20;
        for (int i = 0; i < maxAttempts; i++)
        {
            float randX = Random.Range(mapBoundsMin.x, mapBoundsMax.x);
            float randY = Random.Range(mapBoundsMin.y, mapBoundsMax.y);
            Vector2 candidatePos = new Vector2(randX, randY);
            Collider2D hit = Physics2D.OverlapCircle(candidatePos, 1f, obstacleLayer);
            if (hit == null) return candidatePos;
        }
        return transform.position;
    }

    IEnumerator SpawnMultipleSmokes(Vector3 centerPos)
    {
        if (smokeCloudPrefab == null) yield break;
        for (int i = 0; i < smokeCount; i++)
        {
            Vector3 offset = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0);
            Instantiate(smokeCloudPrefab, centerPos + offset, Quaternion.identity);
            yield return new WaitForSeconds(0.1f);
        }
    }

    IEnumerator TombstoneRoutine()
    {
        // ==================================================
        // PHASE 0: DỌN DẸP & CHUẨN BỊ (QUAN TRỌNG CHO VIỆC RESET)
        // ==================================================

        // Nếu đang là Zombie mà ăn thêm cái nữa -> Tắt ngay nhạc Zombie cũ đang phát
        if (zombieAudioSource != null) zombieAudioSource.Stop();

        // Đảm bảo tắt model Zombie (nếu đang hiện) để nhường chỗ cho effect Sét đánh
        if (zombieModelObject != null) zombieModelObject.SetActive(false);

        // Tạm thời tắt cờ Zombie để Stats tính lại về tốc độ gốc (tránh việc cutscene mà chạy nhanh)
        isZombieMode = false;

        // ==================================================
        // PHASE 1: BIẾN HÌNH (CUTSCENE)
        // ==================================================

        // 1. Pause nhạc nền chính
        if (GameManager.Instance != null && GameManager.Instance.musicSource != null)
            GameManager.Instance.musicSource.Pause();

        if (UIManager.Instance != null) UIManager.Instance.ToggleHUD(false);

        // 2. Setup hiệu ứng sét đánh
        isDead = true; // Khoá input
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // Ẩn model người thường
        if (activeStateObject != null) activeStateObject.SetActive(false);

        if (struckFxObject != null) struckFxObject.SetActive(true);
        if (lightningFxObject != null) lightningFxObject.SetActive(true);

        // Tạo màn đêm
        GameObject darkness = null;
        if (darknessPrefab != null)
        {
            darkness = Instantiate(darknessPrefab, Vector3.zero, Quaternion.identity);
            darkness.transform.SetParent(Camera.main.transform);
            darkness.transform.localPosition = new Vector3(0, 0, 10);
        }

        // Âm thanh sét (Chạy đè lên mọi âm thanh khác)
        if (zombieAudioSource != null && lightningSound != null)
            zombieAudioSource.PlayOneShot(lightningSound);

        if (GameManager.Instance != null)
            GameManager.Instance.ActivateGlobalStun(cutsceneDuration);

        // Hiệu ứng nhấp nháy
        float cutsceneTimer = 0f;
        SpriteRenderer struckSR = struckFxObject.GetComponent<SpriteRenderer>();
        SpriteRenderer[] boltSRs = lightningFxObject.GetComponentsInChildren<SpriteRenderer>();

        while (cutsceneTimer < cutsceneDuration)
        {
            if (struckSprites.Length > 0 && struckSR != null)
                struckSR.sprite = struckSprites[Random.Range(0, struckSprites.Length)];

            if (lightningSprites.Length > 0 && boltSRs.Length > 0)
            {
                foreach (var sr in boltSRs)
                {
                    sr.sprite = lightningSprites[Random.Range(0, lightningSprites.Length)];
                    sr.flipX = (Random.value > 0.5f);
                }
            }
            yield return new WaitForSeconds(flashSpeed);
            cutsceneTimer += flashSpeed;
        }

        // Dọn dẹp Phase 1
        if (struckFxObject != null) struckFxObject.SetActive(false);
        if (lightningFxObject != null) lightningFxObject.SetActive(false);
        if (darkness != null) Destroy(darkness);
        if (UIManager.Instance != null) UIManager.Instance.ToggleHUD(true);

        // ==================================================
        // PHASE 2: ZOMBIE MODE
        // ==================================================

        isZombieMode = true;
        isDead = false; // Mở lại input

        // Đổi Model
        if (activeStateObject != null) activeStateObject.SetActive(false);
        if (idleStateObject != null) idleStateObject.SetActive(false);
        if (zombieModelObject != null) zombieModelObject.SetActive(true);

        // Cập nhật stats ngay để lấy tốc độ zombie
        RecalculateStats();

        // PHÁT NHẠC ZOMBIE
        if (zombieAudioSource != null && zombieMusic != null)
            zombieAudioSource.PlayOneShot(zombieMusic);

        // --- TIMER LOOP ---
        zombieTimer = zombieDuration;

        while (zombieTimer > 0)
        {
            zombieTimer -= Time.deltaTime;

            if (GameManager.Instance.currentLives <= 0)
            {
                break;
            }

            yield return null;
        }

        // ==================================================
        // KẾT THÚC: RESET
        // ==================================================

        isZombieMode = false;

        // Reset Visual
        if (zombieModelObject != null) zombieModelObject.SetActive(false);
        if (activeStateObject != null) activeStateObject.SetActive(true);
        if (bodyRenderer != null) bodyRenderer.enabled = true;
        if (legsAnimator != null) legsAnimator.Play("Idle");

        // Unpause nhạc nền chính
        if (GameManager.Instance != null && GameManager.Instance.musicSource != null)
        {
            GameManager.Instance.musicSource.UnPause();
        }

        tombstoneCoroutine = null;
    }

    public void KillEnemyOnContact() { }

    #endregion

    #region Death & Respawn

    public void TriggerDeathAnimation()
    {
        isDead = true;
        if (sfxAudioSource != null && playerDeathClip != null)
            sfxAudioSource.PlayOneShot(playerDeathClip);

        moveInput = Vector2.zero;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }
        if (footstepAudioSource) footstepAudioSource.Stop();
        if (activeStateObject != null) activeStateObject.SetActive(true);
        if (idleStateObject != null) idleStateObject.SetActive(false);
        if (bodyRenderer != null) bodyRenderer.enabled = false;
        if (legsAnimator != null) legsAnimator.SetTrigger("Die");
    }

    public void ResetState()
    {
        isDead = false;
        if (rb != null) rb.simulated = true;
        if (legsAnimator != null) legsAnimator.Play("Idle");
        if (bodyRenderer != null) bodyRenderer.enabled = true;
        ToggleVisibility(true);
    }

    public bool IsInvincible() => isInvincible;

    public void TriggerRespawnInvincibility(float duration)
    {
        StartCoroutine(InvincibilityRoutine(duration));
    }

    IEnumerator InvincibilityRoutine(float duration)
    {
        isInvincible = true;
        float elapsed = 0f;
        float flashInterval = 0.1f;
        while (elapsed < duration)
        {
            ToggleVisibility(false);
            yield return new WaitForSeconds(flashInterval);
            ToggleVisibility(true);
            yield return new WaitForSeconds(flashInterval);
            elapsed += (flashInterval * 2);
        }
        ToggleVisibility(true);
        isInvincible = false;
    }

    #endregion

    // Cutscene Movement
    public IEnumerator MoveToPosition(Vector3 targetPos, float duration)
    {
        isInputEnabled = false;
        moveInput = Vector2.zero; // Ngắt input cũ ngay lập tức

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic; // Tắt vật lý để đi xuyên tường
        }

        if (legsAnimator != null) legsAnimator.SetBool("IsMoving", true);

        float elapsed = 0f;
        Vector3 startPos = transform.position;
        float cutsceneStepTimer = 0f;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);

            if (Time.time >= cutsceneStepTimer)
            {
                if (footstepAudioSource && footstepClip)
                {
                    // nên chỉ cần PlayOneShot là nó tự dùng Volume đã chỉnh trong Inspector.
                    footstepAudioSource.PlayOneShot(footstepClip);
                }
                // Cập nhật lần phát tiếp theo dựa trên stepDelay
                cutsceneStepTimer = Time.time + stepDelay;
            }

            elapsed += Time.deltaTime;
            yield return null;

            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPos;

        if (rb != null) rb.bodyType = RigidbodyType2D.Dynamic; // Bật lại vật lý
        if (legsAnimator != null) legsAnimator.SetBool("IsMoving", false);
    }

    public void ApplyPermanentUpgrade(UpgradeData data)
    {
        switch (data.type)
        {
            case UpgradeType.MoveSpeed:
                defaultMoveSpeed += data.valueAmount; // Tăng base speed
                moveSpeed = defaultMoveSpeed;         // Cập nhật ngay
                break;

            case UpgradeType.FireRate:
                // Giảm base delay (bắn nhanh hơn)
                // Lưu ý: Đảm bảo không giảm quá sâu (ví dụ không dưới 0.05)
                defaultFireRate = Mathf.Max(maxFireRate, defaultFireRate - data.valueAmount);
                currentFireRate = defaultFireRate;
                break;

            case UpgradeType.AmmoDamage:
                currentBulletDamage += (int)data.valueAmount;
                Debug.Log("Bullet Damage Upgraded to: " + currentBulletDamage);
                break;

            case UpgradeType.ExtraLife:
                if (GameManager.Instance != null) GameManager.Instance.AddLife(1);
                break;

            case UpgradeType.SheriffBadge:
                PowerUpData badge = ScriptableObject.CreateInstance<PowerUpData>();
                badge.type = PowerUpType.SheriffBadge;
                badge.duration = 10f;
                badge.icon = data.icon;
                badge.activateSound = itemsPickupAudioSource != null ? itemsPickupAudioSource.clip : null;
                PickUpItem(badge);
                break;

            case UpgradeType.SuperGun:
                shotgunExpirationTime = float.MaxValue;
                isShotgunActive = true;
                break;
        }
    }
    public void TriggerItemGetAnimation(Sprite itemSprite)
    {
        StartCoroutine(ItemGetRoutine(itemSprite));
    }

    IEnumerator ItemGetRoutine(Sprite itemSprite)
    {
        isInputEnabled = false;
        moveInput = Vector2.zero;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        if (bodySpriteDisplay != null && handsUpSprite != null)
        {
            bodySpriteDisplay.sprite = handsUpSprite;
        }

        if (itemLiftDisplay != null)
        {
            itemLiftDisplay.sprite = itemSprite;
            itemLiftDisplay.gameObject.SetActive(true);
        }

        if (itemsPickupAudioSource != null && upgradePurchasedClip != null)
            itemsPickupAudioSource.PlayOneShot(upgradePurchasedClip);

        yield return new WaitForSeconds(2f);

        if (itemLiftDisplay != null) itemLiftDisplay.gameObject.SetActive(false);
        isInputEnabled = true;

        if (legsAnimator != null) legsAnimator.Play("Idle");
    }
}