using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    #region Configuration & Settings

    [Header("Movement & Physics")]
    public float moveSpeed = 5f;
    public Rigidbody2D rb;
    public Vector2 mapBoundsMin = new Vector2(-6f, -5f);
    public Vector2 mapBoundsMax = new Vector2(6f, 5f);

    [Header("Combat Settings")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletOffset = 0.5f;
    public float fireDelay = 0.4f;

    [Header("Audio Settings")]
    public AudioSource gunAudioSource;
    public AudioSource footstepAudioSource;
    public AudioSource itemsPickupAudioSource;
    public AudioSource itemsActivateAudioSource;

    public AudioClip shootClip;
    public AudioClip footstepClip;
    public AudioClip itemPickupClip;
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
    private bool isHMGActive = false;
    public bool enableAutoFire = false;

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
    public AudioClip lightningSound;
    public float flashSpeed = 0.1f;
    public float cutsceneDuration = 3f;

    [Header("Skill: Tombstone (Zombie Mode)")]
    public GameObject zombieModelObject;
    public Animator zombieAnimator;
    public AudioClip zombieMusic;
    public float zombieDuration = 8f;
    public float zombieSpeedBoost = 3f;

    #endregion

    #region Internal State Variables

    // Movement & Combat State
    private Vector2 moveInput;
    private float nextFireTime = 0f;
    private float nextStepTime = 0f;
    private bool isShotgunActive = false;
    private bool isWheelActive = false;

    // Player Status
    private bool isDead = false;
    private bool isInvincible = false;
    [HideInInspector] public bool isZombieMode = false;

    // Defaults for Reset
    private float defaultFireDelay;
    private float defaultMoveSpeed;
    private GameObject defaultBulletPrefab;
    private Coroutine activePowerUpCoroutine;
    public bool isInputEnabled = true;
    #endregion

    #region Unity Lifecycle

    void Start()
    {
        defaultFireDelay = fireDelay;
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
        if (rb != null && !rb.isKinematic)
            rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }

    void Update()
    {
        if (!isInputEnabled)
        {
            moveInput = Vector2.zero; // <--- THÊM DÒNG NÀY: Ngắt hoàn toàn tín hiệu di chuyển cũ
            return;
        }
        if (isDead)
        {
            moveInput = Vector2.zero;
            return;
        }

        // 1. Movement Input
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(moveX, moveY).normalized;

        // 2. Shooting Input
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
            nextFireTime = Time.time + fireDelay;

            if (!isZombieMode)
            {
                UpdateActiveModel(moveInput.magnitude > 0.1f, inputShootDir);
            }
        }

        // 3. Visual State Logic
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

                bool isMoving = moveInput.magnitude > 0.1f;
                zombieAnimator.SetBool("IsMoving", isMoving);
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

                if (!attemptedToShoot)
                    UpdateActiveModel(isMoving, moveInput);

                HandleFootsteps(isMoving);
            }
        }

        // 4. Item Activation
        if (Input.GetKeyDown(KeyCode.Space) && heldItem != null)
        {
            ActivateItem(heldItem);
            heldItem = null;
            if (UIManager.Instance != null) UIManager.Instance.UpdateItem(null);
        }
    }

    #endregion

    #region Combat Logic

    void Shoot(Vector2 dir)
    {
        if (isWheelActive)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f;
                Quaternion rotation = Quaternion.Euler(0, 0, angle);
                Vector2 wheelDir = rotation * Vector2.up;
                SpawnBullet(wheelDir);
            }
        }
        else if (isShotgunActive)
        {
            SpawnBullet(dir);
            Vector2 leftDir = Quaternion.Euler(0, 0, 15f) * dir;
            SpawnBullet(leftDir);
            Vector2 rightDir = Quaternion.Euler(0, 0, -15f) * dir;
            SpawnBullet(rightDir);
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

        if (firePoint == null)
        {
            spawnPos = transform.position + (Vector3)(dir.normalized * bulletOffset);
        }

        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        var bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript) bulletScript.Setup(dir);
    }

    #endregion

    #region Visual & Audio Logic

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
        if (legsAnimator) legsAnimator.SetBool("IsMoving", isMoving); // Sửa lại IsMoving cho đúng với logic Update

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

    #region Item System

    public void PickUpItem(PowerUpData newItem)
    {
        // Instant Items
        if (newItem.type == PowerUpType.Coin)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddCoin((int)newItem.valueAmount);
                if (itemsPickupAudioSource != null)
                    itemsPickupAudioSource.PlayOneShot(newItem.activateSound);
            }
            return;
        }
        if (newItem.type == PowerUpType.Life)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddLife(1);
                if (itemsPickupAudioSource != null)
                    itemsPickupAudioSource.PlayOneShot(newItem.activateSound);
            }
            return;
        }

        // Held Items
        if (heldItem != null)
        {
            ActivateItem(newItem);
            if (itemsActivateAudioSource != null && newItem.activateSound != null)
            {
                itemsActivateAudioSource.PlayOneShot(newItem.activateSound);
            }
        }
        else
        {
            heldItem = newItem;
            if (UIManager.Instance != null) UIManager.Instance.UpdateItem(heldItem.icon);
            if (itemsPickupAudioSource != null && itemPickupClip != null)
            {
                itemsActivateAudioSource.PlayOneShot(itemPickupClip);
            }
        }
    }

    void ActivateItem(PowerUpData item)
    {
        if (item.activateSound != null)
            itemsActivateAudioSource.PlayOneShot(item.activateSound);

        // --- LOGIC COROUTINE AN TOÀN ---

        // 1. Những item dạng "Sự kiện tức thời" (Instant) -> Không ảnh hưởng đến vũ khí đang cầm
        if (item.type == PowerUpType.ScreenNuke || item.type == PowerUpType.SmokeBomb || item.type == PowerUpType.Tombstone)
        {
            // Chạy logic riêng, KHÔNG RESET vũ khí
            switch (item.type)
            {
                case PowerUpType.ScreenNuke: StartCoroutine(NukeRoutine()); break;
                case PowerUpType.SmokeBomb:
                    if (GameManager.Instance != null) GameManager.Instance.ActivateGlobalStun(smokeBombDuration);
                    TeleportAndSmoke();
                    break;
                case PowerUpType.Tombstone: StartCoroutine(TombstonePhase1()); break;
            }
            return; // Thoát hàm luôn, không chạy xuống logic vũ khí
        }

        // 2. Những item dạng "Buff Thời Gian" (Coffee, Guns...)
        if (item.duration > 0)
        {
            // A. Nếu đang có buff cũ chạy, DỪNG NÓ LẠI ngay lập tức
            // Để nó không tự động Reset khi hết giờ cũ
            if (activePowerUpCoroutine != null) StopCoroutine(activePowerUpCoroutine);

            // B. Bắt đầu Coroutine mới
            // Coroutine này sẽ chờ hết thời gian mới rồi mới Reset TẤT CẢ
            activePowerUpCoroutine = StartCoroutine(PowerUpRoutine(item));
        }
    }
    IEnumerator PowerUpRoutine(PowerUpData item)
    {
        ApplyEffect(item);
        yield return new WaitForSeconds(item.duration);
        ResetPowerUpEffects();
    }
    void ApplyEffect(PowerUpData item)
    {
        switch (item.type)
        {
            case PowerUpType.HeavyMachineGun:
                isHMGActive = true; // Bật cờ
                break;

            case PowerUpType.Shotgun:
                isShotgunActive = true; // Bật cờ
                break;

            case PowerUpType.WagonWheel:
                isWheelActive = true; // Bật cờ
                break;

            case PowerUpType.Coffee:
                // Coffee cộng dồn tốc độ, không cần bật cờ
                moveSpeed += item.valueAmount;
                if (legsAnimator != null) legsAnimator.speed = 2f;
                break;

            case PowerUpType.SheriffBadge:
                // Sheriff badge xử lý riêng vì nó là trùm
                moveSpeed += item.valueAmount;
                if (legsAnimator != null) legsAnimator.speed = 2f;
                break;
        }

        // Sau khi bật cờ xong, tính toán lại chỉ số thực tế
        RecalculateWeaponStats();

        Debug.Log("Activated Buff: " + item.itemName + " | Current Delay: " + fireDelay);
    }
    void RecalculateWeaponStats()
    {
        // 1. Reset về mặc định trước
        fireDelay = defaultFireDelay;
        enableAutoFire = false;

        // 2. Nếu có Coffee hoặc Sheriff, tốc độ chạy đã được cộng dồn ở ApplyEffect nên không cần reset ở đây
        //    (Trừ khi bạn muốn logic phức tạp hơn, nhưng tạm thời giữ nguyên logic Coffee của bạn)

        // 3. Xử lý logic vũ khí kết hợp
        // Ưu tiên 1: Sheriff Badge (Mạnh nhất)
        if (heldItem != null && heldItem.type == PowerUpType.SheriffBadge)
        {
            fireDelay = 0.1f;
            enableAutoFire = true;
            isShotgunActive = true;
            return; // Sheriff bao trọn gói rồi nên return luôn
        }

        // Ưu tiên 2: Heavy Machine Gun (Tốc độ bắn siêu nhanh)
        if (isHMGActive)
        {
            fireDelay = 0.05f;      // Lấy tốc độ của HMG
            enableAutoFire = true;  // Lấy auto fire của HMG
                                    // Lưu ý: Không return, để nó chạy tiếp xuống dưới check Shotgun/Wheel
        }

        // Ưu tiên 3: Shotgun (Nếu KHÔNG có HMG thì dùng tốc độ chậm, có HMG rồi thì giữ tốc độ nhanh ở trên)
        if (isShotgunActive && !isHMGActive)
        {
            fireDelay = 0.7f;
            enableAutoFire = false;
        }

        // Ưu tiên 4: Wheel (Tương tự Shotgun)
        if (isWheelActive && !isHMGActive)
        {
            enableAutoFire = false;
        }

        // TỔNG KẾT:
        // - Nếu có HMG + Shotgun: fireDelay = 0.05 (nhanh), Auto = true, isShotgun = true (bắn chùm).
        // - Nếu có HMG + Wheel: fireDelay = 0.05 (nhanh), Auto = true, isWheel = true (bắn 8 hướng).
    }
    void ResetPowerUpEffects()
    {
        // Dừng Coroutine đếm ngược cũ nếu còn chạy
        if (activePowerUpCoroutine != null) StopCoroutine(activePowerUpCoroutine);

        // Reset các biến cờ
        isHMGActive = false;
        isShotgunActive = false;
        isWheelActive = false;

        // Reset chỉ số gốc
        moveSpeed = defaultMoveSpeed;
        if (legsAnimator != null) legsAnimator.speed = 1f;

        // Tính toán lại (về mặc định)
        RecalculateWeaponStats();
    }

    #endregion

    #region Skill Routines

    // --- NUKE ---
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

    // --- SMOKE BOMB ---
    void TeleportAndSmoke()
    {
        Vector3 targetPos = FindSafePosition();
        transform.position = targetPos;
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

    // --- TOMBSTONE ---
    IEnumerator TombstonePhase1()
    {
        if (UIManager.Instance != null) UIManager.Instance.ToggleHUD(false);

        isDead = true;
        if (rb != null) rb.linearVelocity = Vector2.zero; // Unity 6 / 2023.3+ syntax
        if (activeStateObject != null) activeStateObject.SetActive(false);

        if (struckFxObject != null) struckFxObject.SetActive(true);
        if (lightningFxObject != null) lightningFxObject.SetActive(true);

        GameObject darkness = null;
        if (darknessPrefab != null)
        {
            darkness = Instantiate(darknessPrefab, Vector3.zero, Quaternion.identity);
            darkness.transform.SetParent(Camera.main.transform);
            darkness.transform.localPosition = new Vector3(0, 0, 10);
        }

        if (itemsActivateAudioSource != null && lightningSound != null)
        {
            itemsActivateAudioSource.PlayOneShot(lightningSound);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ActivateGlobalStun(cutsceneDuration);
        }

        float timer = 0f;
        SpriteRenderer struckSR = struckFxObject.GetComponent<SpriteRenderer>();
        SpriteRenderer[] boltSRs = lightningFxObject.GetComponentsInChildren<SpriteRenderer>();

        while (timer < cutsceneDuration)
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
            timer += flashSpeed;
        }

        if (struckFxObject != null) struckFxObject.SetActive(false);
        if (lightningFxObject != null) lightningFxObject.SetActive(false);
        if (darkness != null) Destroy(darkness);

        if (UIManager.Instance != null) UIManager.Instance.ToggleHUD(true);
        StartCoroutine(TombstonePhase2());
    }

    IEnumerator TombstonePhase2()
    {
        isZombieMode = true;
        isDead = false;

        if (activeStateObject != null) activeStateObject.SetActive(false);
        if (idleStateObject != null) idleStateObject.SetActive(false);
        if (zombieModelObject != null) zombieModelObject.SetActive(true);

        float originalSpeed = moveSpeed;
        moveSpeed += zombieSpeedBoost;

        if (rb != null) rb.linearVelocity = Vector2.zero;

        if (itemsActivateAudioSource != null && zombieMusic != null)
        {
            itemsActivateAudioSource.PlayOneShot(zombieMusic);
        }

        yield return new WaitForSeconds(zombieDuration);

        isZombieMode = false;
        moveSpeed = originalSpeed;

        if (zombieModelObject != null) zombieModelObject.SetActive(false);
        if (activeStateObject != null) activeStateObject.SetActive(true);
        if (bodyRenderer != null) bodyRenderer.enabled = true;
        if (legsAnimator != null) legsAnimator.Play("Idle");
    }

    public void KillEnemyOnContact()
    {
        // FX for zombie collision
    }

    #endregion

    #region Death & Respawn

    public void TriggerDeathAnimation()
    {
        isDead = true;
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

    // --- HÀM DI CHUYỂN CUTSCENE (ĐÃ SỬA LỖI TRÔI & VA TƯỜNG) ---
    public IEnumerator MoveToPosition(Vector3 targetPos, float duration)
    {
        isInputEnabled = false; // Khóa điều khiển
        moveInput = Vector2.zero;
        // 1. Dừng ngay lập tức mọi quán tính
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            // 2. Chuyển sang chế độ Kinematic (Bóng ma)
            // Để đi xuyên qua tường (Top_Gate) và không bị Physics engine can thiệp
            rb.isKinematic = true;
        }

        // Animation đi bộ
        if (legsAnimator != null) legsAnimator.SetBool("IsMoving", true); // Sửa thành IsMoving viết hoa

        float elapsed = 0f;
        Vector3 startPos = transform.position;

        while (elapsed < duration)
        {
            // Di chuyển mượt mà (Lerp)
            transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPos;

        // 3. Trả lại trạng thái Vật Lý bình thường
        if (rb != null)
        {
            rb.isKinematic = false;
        }

        if (legsAnimator != null) legsAnimator.SetBool("IsMoving", false);

        // Input sẽ được GameManager bật lại sau
    }
}