using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    #region Configuration & Settings
    [Header("Movement & Physics")]
    public float moveSpeed = 4f;
    public float maxSpeed = 8f;
    public Vector2 mapBoundsMin = new Vector2(-6f, -5f);
    public Vector2 mapBoundsMax = new Vector2(6f, 5f);

    [Header("Combat Settings")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletOffset = 0.5f;
    public int currentBulletDamage = 1;
    public float currentFireRate = 0.4f;
    public float maxFireRate = 0.05f;

    [Header("Skill Settings")]
    public float coffeeSpeed = 7f;
    public float hmgFireRate = 0.05f;
    public float shotgunSpreadAngle = 15f;
    public float shotgunFireRateDecrease = 0.2f;
    public float sheriffBadgeSpeed = 6.5f;
    public float sheriffBadgeFireRate = 0.1f;

    [Header("Skill Effects")]
    public GameObject explosionFXPrefab;
    public int explosionCount = 15;
    public float nukeDuration = 2f;

    public GameObject smokeCloudPrefab;
    public LayerMask obstacleLayer;
    public int smokeCount = 4;
    public float smokeBombDuration = 4f;

    [Header("Visual Effects (Tombstone)")]
    public GameObject darknessPrefab;
    public GameObject struckFxObject;
    public GameObject lightningFxObject;
    public Sprite[] struckSprites;
    public float flashSpeed = 0.1f;
    public float cutsceneDuration = 3f;

    [Header("Zombie Mode")]
    public GameObject zombieModelObject;
    public Animator zombieAnimator;
    public float zombieDuration = 8f;
    public float zombieSpeed = 6f;

    [Header("Shop Visuals")]
    public Sprite handsUpSprite;
    public SpriteRenderer itemLiftDisplay;
    #endregion

    #region Audio References
    [Header("Audio")]
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
    #endregion

    #region Component References
    [Header("Visuals")]
    public Rigidbody2D rb;
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

    #region Runtime Variables
    public PowerUpData heldItem;
    public bool hasStoredItem => heldItem != null;
    public PowerUpType storedItemType => heldItem != null ? heldItem.type : PowerUpType.None;

    private Vector2 moveInput;
    public bool isInputEnabled = true;
    private float nextFireTime = 0f;
    private float nextStepTime = 0f;

    private float hmgExpirationTime = 0f;
    private float shotgunExpirationTime = 0f;
    private float wheelExpirationTime = 0f;
    private float sheriffExpirationTime = 0f;
    private float coffeeExpirationTime = 0f;

    private bool isHMGActive = false;
    private bool isShotgunActive = false;
    private bool isWheelActive = false;
    private bool isSheriffActive = false;
    private bool isCoffeeActive = false;
    public bool enableAutoFire = false;

    private bool isDead = false;
    private bool isInvincible = false;
    [HideInInspector] public bool isZombieMode = false;

    public float defaultFireRate = 0.4f;
    public float defaultMoveSpeed = 4f;
    private float zombieTimer = 0f;
    private Coroutine tombstoneCoroutine;
    private float invincibilityExpirationTime = 0f;
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        defaultFireRate = currentFireRate;
        defaultMoveSpeed = moveSpeed;
    }

    void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (legsRenderer != null && legsAnimator == null)
            legsAnimator = legsRenderer.GetComponent<Animator>();
    }

    void Update()
    {
        if (!isInputEnabled || isDead)
        {
            moveInput = Vector2.zero;
            return;
        }

        HandleBuffTimers();
        RecalculateStats();

        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput = moveInput.normalized;

        float shootX = 0f, shootY = 0f;
        if (Input.GetKey(KeyCode.UpArrow)) shootY = 1f;
        else if (Input.GetKey(KeyCode.DownArrow)) shootY = -1f;
        if (Input.GetKey(KeyCode.LeftArrow)) shootX = -1f;
        else if (Input.GetKey(KeyCode.RightArrow)) shootX = 1f;

        bool attemptedToShoot = (shootX != 0 || shootY != 0);
        if (attemptedToShoot && Time.time >= nextFireTime)
        {
            Vector2 shootDir = new Vector2(shootX, shootY).normalized;
            Shoot(shootDir);
            nextFireTime = Time.time + currentFireRate;

            if (!isZombieMode) UpdateActiveModel(moveInput.magnitude > 0.1f, shootDir);
        }

        HandleVisuals(attemptedToShoot);

        if (Input.GetKeyDown(KeyCode.Space) && heldItem != null)
        {
            ActivateItem(heldItem);
            heldItem = null;
            if (UIManager.Instance != null) UIManager.Instance.UpdateItem(null);
        }
    }

    void FixedUpdate()
    {
        if (isDead) return;
        if (rb != null && rb.bodyType != RigidbodyType2D.Kinematic)
            rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }
    #endregion

    #region Inventory & Save Support
    public void SetStoredItem(PowerUpType itemType)
    {
        if (GameManager.Instance == null) return;

        PowerUpData data = GameManager.Instance.allowedDrops.Find(x => x.type == itemType);
        if (data != null)
        {
            heldItem = data;
            if (UIManager.Instance != null) UIManager.Instance.UpdateItem(heldItem.icon);
        }
    }

    public void ClearStoredItem()
    {
        heldItem = null;
        if (UIManager.Instance != null) UIManager.Instance.UpdateItem(null);
    }

    public void PickUpItem(PowerUpData newItem)
    {
        if (newItem.type == PowerUpType.Coin)
        {
            if (GameManager.Instance != null) GameManager.Instance.AddCoin((int)newItem.valueAmount);
            PlaySound(itemsPickupAudioSource, newItem.activateSound);
            return;
        }
        if (newItem.type == PowerUpType.Life)
        {
            if (GameManager.Instance != null) GameManager.Instance.AddLife(1);
            PlaySound(itemsPickupAudioSource, newItem.activateSound);
            return;
        }

        if (heldItem != null)
        {
            ActivateItem(newItem);
            PlaySound(sfxAudioSource, newItem.activateSound);
        }
        else
        {
            heldItem = newItem;
            if (UIManager.Instance != null) UIManager.Instance.UpdateItem(heldItem.icon);
            PlaySound(itemsPickupAudioSource, itemPickupClip);
        }
    }

    void ActivateItem(PowerUpData item)
    {
        PlaySound(sfxAudioSource, item.activateSound);

        switch (item.type)
        {
            case PowerUpType.ScreenNuke: StartCoroutine(NukeRoutine()); return;
            case PowerUpType.SmokeBomb:
                if (GameManager.Instance != null) GameManager.Instance.ActivateGlobalStun(smokeBombDuration);
                TeleportAndSmoke();
                return;
            case PowerUpType.Tombstone:
                if (tombstoneCoroutine != null) StopCoroutine(tombstoneCoroutine);
                tombstoneCoroutine = StartCoroutine(TombstoneRoutine());
                return;
        }

        if (item.duration > 0) ApplyBuffDuration(item);
    }

    public void ApplyPermanentUpgrade(UpgradeData data)
    {
        switch (data.type)
        {
            case UpgradeType.MoveSpeed:
                if (defaultMoveSpeed == 0 && moveSpeed > 0) defaultMoveSpeed = moveSpeed;
                defaultMoveSpeed += data.valueAmount;
                moveSpeed = defaultMoveSpeed;
                break;

            case UpgradeType.FireRate:
                if (defaultFireRate == 0 && currentFireRate > 0) defaultFireRate = currentFireRate;
                defaultFireRate = Mathf.Max(maxFireRate, defaultFireRate - data.valueAmount);
                currentFireRate = defaultFireRate;
                break;

            case UpgradeType.AmmoDamage:
                currentBulletDamage += (int)data.valueAmount;
                break;

            case UpgradeType.ExtraLife:
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.AddLife(1);
                }
                break;

            case UpgradeType.SheriffBadge:
                PowerUpData badge = ScriptableObject.CreateInstance<PowerUpData>();
                badge.type = PowerUpType.SheriffBadge;
                badge.duration = 10f;
                badge.icon = data.icon;
                PickUpItem(badge);
                break;

            case UpgradeType.SuperGun:
                shotgunExpirationTime = float.MaxValue;
                isShotgunActive = true;
                break;
        }
    }
    #endregion

    #region Stats & Buffs
    public struct ActiveBuffInfo
    {
        public PowerUpType type;
        public float remainingTime;
    }
    void HandleBuffTimers()
    {
        float now = Time.time;
        isHMGActive = now < hmgExpirationTime;
        isShotgunActive = now < shotgunExpirationTime;
        isWheelActive = now < wheelExpirationTime;
        isSheriffActive = now < sheriffExpirationTime;
        isCoffeeActive = now < coffeeExpirationTime;
    }
    public List<ActiveBuffInfo> GetActiveBuffs()
    {
        List<ActiveBuffInfo> list = new List<ActiveBuffInfo>();
        float now = Time.time;

        if (isHMGActive) list.Add(new ActiveBuffInfo { type = PowerUpType.HeavyMachineGun, remainingTime = hmgExpirationTime - now });
        if (isShotgunActive) list.Add(new ActiveBuffInfo { type = PowerUpType.Shotgun, remainingTime = shotgunExpirationTime - now });
        if (isWheelActive) list.Add(new ActiveBuffInfo { type = PowerUpType.WagonWheel, remainingTime = wheelExpirationTime - now });
        if (isSheriffActive) list.Add(new ActiveBuffInfo { type = PowerUpType.SheriffBadge, remainingTime = sheriffExpirationTime - now });
        if (isCoffeeActive) list.Add(new ActiveBuffInfo { type = PowerUpType.Coffee, remainingTime = coffeeExpirationTime - now });
        // 2. Tombstone (Zombie Mode)
        if (isZombieMode)
        {
            // Lưu ý: zombieTimer là biến đếm ngược (Duration -> 0) nên dùng trực tiếp, không cần trừ 'now'
            list.Add(new ActiveBuffInfo
            {
                type = PowerUpType.Tombstone,
                remainingTime = zombieTimer
            });
        }

        // 3. SmokeBomb (Hiển thị dưới dạng Invincibility)
        // Chỉ hiện nếu đang bất tử MÀ KHÔNG PHẢI do Zombie (để tránh hiện 2 icon cùng lúc)
        if (isInvincible && !isZombieMode)
        {
            float timeLeft = invincibilityExpirationTime - now;
            if (timeLeft > 0)
            {
                list.Add(new ActiveBuffInfo
                {
                    type = PowerUpType.SmokeBomb,
                    remainingTime = timeLeft
                });
            }
        }

        return list;
    }
    void RecalculateStats()
    {
        float targetSpeed = defaultMoveSpeed;
        if (isZombieMode) targetSpeed = Mathf.Max(targetSpeed, zombieSpeed);
        if (isCoffeeActive) targetSpeed = Mathf.Max(targetSpeed, coffeeSpeed);
        if (isSheriffActive) targetSpeed = Mathf.Max(targetSpeed, sheriffBadgeSpeed);

        moveSpeed = targetSpeed;
        if (legsAnimator != null) legsAnimator.speed = (moveSpeed > 5.5f) ? 2f : 1f;

        float targetFireRate = defaultFireRate;
        enableAutoFire = false;

        if (isShotgunActive && !isHMGActive && !isSheriffActive)
            targetFireRate += shotgunFireRateDecrease;

        if (isSheriffActive)
        {
            targetFireRate = Mathf.Min(targetFireRate, sheriffBadgeFireRate);
            enableAutoFire = true;
        }
        if (isHMGActive)
        {
            targetFireRate = Mathf.Min(targetFireRate, hmgFireRate);
            enableAutoFire = true;
        }

        currentFireRate = targetFireRate;

        if (isWheelActive && !isHMGActive && !isSheriffActive) enableAutoFire = false;
    }

    void ApplyBuffDuration(PowerUpData item)
    {
        float duration = item.duration;
        float now = Time.time;
        switch (item.type)
        {
            case PowerUpType.HeavyMachineGun: hmgExpirationTime = Mathf.Max(now, hmgExpirationTime) + duration; break;
            case PowerUpType.Shotgun: shotgunExpirationTime = Mathf.Max(now, shotgunExpirationTime) + duration; break;
            case PowerUpType.WagonWheel: wheelExpirationTime = Mathf.Max(now, wheelExpirationTime) + duration; break;
            case PowerUpType.SheriffBadge: sheriffExpirationTime = Mathf.Max(now, sheriffExpirationTime) + duration; break;
            case PowerUpType.Coffee: coffeeExpirationTime = Mathf.Max(now, coffeeExpirationTime) + duration; break;
        }
    }
    #endregion

    #region Combat System
    void Shoot(Vector2 dir)
    {
        if (isWheelActive)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector2 wheelDir = Quaternion.Euler(0, 0, i * 45f) * Vector2.up;
                SpawnBullet(wheelDir);
            }
        }
        else if (isShotgunActive || isSheriffActive)
        {
            SpawnBullet(dir);
            SpawnBullet(Quaternion.Euler(0, 0, shotgunSpreadAngle) * dir);
            SpawnBullet(Quaternion.Euler(0, 0, -shotgunSpreadAngle) * dir);
        }
        else
        {
            SpawnBullet(dir);
        }
        PlaySound(gunAudioSource, shootClip);
    }

    void SpawnBullet(Vector2 dir)
    {
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + (Vector3)(dir.normalized * bulletOffset);
        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        bullet.GetComponent<Bullet>()?.Setup(dir, currentBulletDamage);
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
                if (zSR != null && moveInput.x != 0) zSR.flipX = (moveInput.x < 0);
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

    void UpdateActiveModel(bool isMoving, Vector2 dir)
    {
        if (legsAnimator) legsAnimator.SetBool("IsMoving", isMoving);
        if (bodySpriteDisplay == null) return;

        if (dir.y > 0) bodySpriteDisplay.sprite = bodyUp;
        else if (dir.y < 0) bodySpriteDisplay.sprite = bodyDown;
        else if (dir.x < 0) bodySpriteDisplay.sprite = bodyLeft;
        else if (dir.x > 0) bodySpriteDisplay.sprite = bodyRight;
    }

    void SetVisualState(bool isIdle)
    {
        if (idleStateObject) idleStateObject.SetActive(isIdle);
        if (activeStateObject) activeStateObject.SetActive(!isIdle);
    }

    void HandleFootsteps(bool isMoving)
    {
        if (isMoving && Time.time >= nextStepTime)
        {
            PlaySound(footstepAudioSource, footstepClip);
            nextStepTime = Time.time + stepDelay;
        }
    }

    void PlaySound(AudioSource source, AudioClip clip)
    {
        if (source != null && clip != null) source.PlayOneShot(clip);
    }

    void ToggleVisibility(bool isVisible)
    {
        if (bodyRenderer) bodyRenderer.enabled = isVisible;
        if (legsRenderer) legsRenderer.enabled = isVisible;
        if (idleRenderer) idleRenderer.enabled = isVisible;
    }

    public void TriggerItemGetAnimation(Sprite itemSprite) => StartCoroutine(ItemGetRoutine(itemSprite));

    IEnumerator ItemGetRoutine(Sprite itemSprite)
    {
        isInputEnabled = false;
        moveInput = Vector2.zero;
        if (rb) rb.linearVelocity = Vector2.zero;

        if (bodySpriteDisplay && handsUpSprite) bodySpriteDisplay.sprite = handsUpSprite;
        if (itemLiftDisplay)
        {
            itemLiftDisplay.sprite = itemSprite;
            itemLiftDisplay.gameObject.SetActive(true);
        }

        PlaySound(itemsPickupAudioSource, upgradePurchasedClip);
        yield return new WaitForSeconds(2f);

        if (itemLiftDisplay) itemLiftDisplay.gameObject.SetActive(false);
        isInputEnabled = true;
        if (legsAnimator) legsAnimator.Play("Idle");
    }
    #endregion

    #region Skill Routines
    IEnumerator NukeRoutine()
    {
        Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        BossController[] bossControllers = FindObjectsByType<BossController>(FindObjectsSortMode.None);

        foreach (var b in bossControllers)
            if (b != null) b.TakeDamage(10);

        foreach (var e in allEnemies)
        {
            if (e != null && !(e is BossController)) e.Die(false);
        }

        int explosionsSpawned = 0;
        while (explosionsSpawned < explosionCount)
        {
            Vector3 randPos = new Vector3(Random.Range(mapBoundsMin.x, mapBoundsMax.x), Random.Range(mapBoundsMin.y, mapBoundsMax.y), 0);
            if (explosionFXPrefab != null) Instantiate(explosionFXPrefab, randPos, Quaternion.identity);
            explosionsSpawned++;
            yield return new WaitForSeconds((explosionCount > 0) ? (nukeDuration / explosionCount) : 0.1f);
        }
    }

    void TeleportAndSmoke()
    {
        Vector3 targetPos = FindSafePosition();
        transform.position = targetPos;
        TriggerRespawnInvincibility(3f);
        StartCoroutine(SpawnMultipleSmokes(targetPos));
    }

    Vector3 FindSafePosition()
    {
        for (int i = 0; i < 20; i++)
        {
            Vector2 candidate = new Vector2(Random.Range(mapBoundsMin.x, mapBoundsMax.x), Random.Range(mapBoundsMin.y, mapBoundsMax.y));
            if (!Physics2D.OverlapCircle(candidate, 1f, obstacleLayer)) return candidate;
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
        //--- PHASE 1: CUTSCENE ---
        if (GameManager.Instance != null) GameManager.Instance.canPause = false;
        if (zombieAudioSource != null) zombieAudioSource.Stop();
        if (zombieModelObject != null) zombieModelObject.SetActive(false);
        isZombieMode = false;
        isInvincible = true;

        if (GameManager.Instance?.musicSource != null) GameManager.Instance.musicSource.Pause();
        if (UIManager.Instance != null) UIManager.Instance.ToggleHUD(false);

        isDead = true;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        if (activeStateObject != null) activeStateObject.SetActive(false);
        if (idleStateObject != null) idleStateObject.SetActive(false);
        isInputEnabled = false;

        if (struckFxObject != null) struckFxObject.SetActive(true);
        if (lightningFxObject != null) lightningFxObject.SetActive(true);
        PlaySound(zombieAudioSource, lightningSound);
        if (GameManager.Instance != null) GameManager.Instance.ActivateGlobalStun(cutsceneDuration);

        GameObject darkness = null;
        if (darknessPrefab != null)
        {
            darkness = Instantiate(darknessPrefab, Vector3.zero, Quaternion.identity);
            darkness.transform.SetParent(Camera.main.transform);
            darkness.transform.localPosition = new Vector3(0, 0, 10);
        }

        float timer = 0f;
        SpriteRenderer struckSR = struckFxObject?.GetComponent<SpriteRenderer>();
        while (timer < cutsceneDuration)
        {
            if (struckSprites.Length > 0 && struckSR != null)
                struckSR.sprite = struckSprites[Random.Range(0, struckSprites.Length)];
            yield return new WaitForSeconds(flashSpeed);
            timer += flashSpeed;
        }

        if (struckFxObject) struckFxObject.SetActive(false);
        if (lightningFxObject) lightningFxObject.SetActive(false);
        if (darkness) Destroy(darkness);
        if (UIManager.Instance) UIManager.Instance.ToggleHUD(true);


        //--- PHASE 2: ZOMBIE MODE ---
        if (GameManager.Instance != null) GameManager.Instance.canPause = true;
        isZombieMode = true;
        isDead = false;
        if (zombieModelObject) zombieModelObject.SetActive(true);
        isInputEnabled = true;

        RecalculateStats();
        PlaySound(zombieAudioSource, zombieMusic);

        zombieTimer = zombieDuration;
        while (zombieTimer > 0)
        {
            zombieTimer -= Time.deltaTime;
            if (GameManager.Instance.currentLives <= 0) break;
            yield return null;
        }

        isZombieMode = false;
        isInvincible = false;
        if (zombieModelObject) zombieModelObject.SetActive(false);
        if (activeStateObject) activeStateObject.SetActive(true);
        if (bodyRenderer) bodyRenderer.enabled = true;
        if (legsAnimator) legsAnimator.Play("Idle");

        if (GameManager.Instance?.musicSource != null) GameManager.Instance.musicSource.UnPause();
        tombstoneCoroutine = null;
    }
    #endregion

    #region Cutscene & Helpers
    public void PlayVictoryPose(Sprite itemSprite)
    {
        if (rb) rb.linearVelocity = Vector2.zero;
        if (bodySpriteDisplay && handsUpSprite) bodySpriteDisplay.sprite = handsUpSprite;
        if (itemLiftDisplay)
        {
            itemLiftDisplay.sprite = itemSprite;
            itemLiftDisplay.gameObject.SetActive(true);
        }
        PlaySound(itemsPickupAudioSource, upgradePurchasedClip);
        if (legsAnimator) legsAnimator.Play("Idle");
    }

    public void StopVictoryPose()
    {
        if (itemLiftDisplay) itemLiftDisplay.gameObject.SetActive(false);
        SetVisualState(isIdle: true);
        if (legsAnimator)
        {
            legsAnimator.SetBool("IsMoving", false);
            legsAnimator.Play("Idle");
        }
        if (rb) rb.linearVelocity = Vector2.zero;
    }

    public IEnumerator MoveToPosition(Vector3 targetPos, float duration)
    {
        isInputEnabled = false;
        moveInput = Vector2.zero;
        SetPhysicsForCutscene(false);
        if (legsAnimator) legsAnimator.SetBool("IsMoving", true);

        float elapsed = 0f;
        Vector3 startPos = transform.position;
        float stepTimer = 0f;

        while (elapsed < duration)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            if (Time.time >= stepTimer)
            {
                PlaySound(footstepAudioSource, footstepClip);
                stepTimer = Time.time + stepDelay;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPos;
        SetPhysicsForCutscene(true);
        if (legsAnimator) legsAnimator.SetBool("IsMoving", false);
    }

    public void SetPhysicsForCutscene(bool isPhysicsOn)
    {
        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = isPhysicsOn ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
        }
        if (GetComponent<Collider2D>()) GetComponent<Collider2D>().enabled = isPhysicsOn;
    }

    public void SetMapBounds(Vector2 newMin, Vector2 newMax)
    {
        mapBoundsMin = newMin;
        mapBoundsMax = newMax;
        Debug.Log($"Map bounds set to {mapBoundsMin} -> {mapBoundsMax}");
    }
    #endregion

    #region Death & Status
    public void KillEnemyOnContact() { }

    public void TriggerDeathAnimation()
    {
        isDead = true;
        PlaySound(sfxAudioSource, playerDeathClip);
        moveInput = Vector2.zero;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }
        if (footstepAudioSource) footstepAudioSource.Stop();

        SetVisualState(isIdle: false);
        if (bodyRenderer) bodyRenderer.enabled = false;
        if (legsAnimator) legsAnimator.SetTrigger("Die");
    }

    public void ResetState()
    {
        isDead = false;
        if (rb != null) rb.simulated = true;

        if (legsAnimator != null)
        {
            legsAnimator.ResetTrigger("Die");
            legsAnimator.Rebind();
            legsAnimator.Update(0f);
            legsAnimator.SetBool("IsMoving", false);
        }

        if (bodyRenderer != null) bodyRenderer.enabled = true;
        ToggleVisibility(true);
    }

    public bool IsInvincible() => isInvincible;

    public void TriggerRespawnInvincibility(float duration)
    {
        invincibilityExpirationTime = Time.time + duration;
        StartCoroutine(InvincibilityRoutine(duration));
    }

    IEnumerator InvincibilityRoutine(float duration)
    {
        isInvincible = true;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            ToggleVisibility(false);
            yield return new WaitForSeconds(0.1f);
            ToggleVisibility(true);
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.2f;
        }
        ToggleVisibility(true);
        isInvincible = false;
    }
    #endregion
}