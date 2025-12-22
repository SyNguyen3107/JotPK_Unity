using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    [Header("Combat Settings")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletOffset = 0.5f;
    public float fireDelay = 0.4f;
    private float nextFireTime = 0f;

    [Header("Upgrades / Power-ups")]
    public bool enableAutoFire = false;

    [Header("Visual References")]
    public GameObject idleStateObject;
    public GameObject activeStateObject;

    // 3 Thành phần hiển thị quan trọng
    public SpriteRenderer bodyRenderer;
    public SpriteRenderer legsRenderer;
    public SpriteRenderer idleRenderer;

    [Header("Active Model References")]
    public Rigidbody2D rb;
    public Animator legsAnimator; // Chứa Animation Die
    public SpriteRenderer bodySpriteDisplay; // (Thường là bodyRenderer ở trên, gán trùng cũng đc)

    // Sprites hướng (Giữ nguyên của bạn)
    public Sprite bodyUp;
    public Sprite bodyDown;
    public Sprite bodyLeft;
    public Sprite bodyRight;

    [Header("Power-Ups State")]
    public PowerUpData heldItem; // Item đang giữ trong ô đỏ
    private Coroutine activePowerUpCoroutine; // Để quản lý việc tắt hiệu ứng cũ

    // Các chỉ số gốc (để reset)
    private float defaultFireDelay;
    private float defaultMoveSpeed;
    private GameObject defaultBulletPrefab;

    [Header("Audio")]
    public AudioSource gunAudioSource;
    public AudioSource footstepAudioSource;
    public AudioClip shootClip;
    public AudioClip footstepClip;
    public float stepDelay = 0.3f;
    private float nextStepTime = 0f;


    // State Variables
    private Vector2 moveInput;
    private bool isDead = false;
    private bool isInvincible = false;

    private Vector2 lastShootDir = Vector2.zero;
    private float lastShootTime = 0f;
    void Start()
    {
        // Lưu chỉ số gốc
        defaultFireDelay = fireDelay;
        defaultMoveSpeed = moveSpeed;
        defaultBulletPrefab = bulletPrefab;
        // Auto-find animator
        if (legsRenderer != null && legsAnimator == null)
            legsAnimator = legsRenderer.GetComponent<Animator>();
    }
    void FixedUpdate()
    {
        // Block Physics logic if dead
        if (isDead) return;

        if (rb != null)
            rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }
    void Update()
    {
        // --- 1. BLOCK INPUT IF DEAD ---
        if (isDead)
        {
            moveInput = Vector2.zero;
            return;
        }

        // --- 2. MOVEMENT ---
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(moveX, moveY).normalized;

        // --- 3. SHOOTING ---
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

            UpdateActiveModel(moveInput.magnitude > 0.1f, inputShootDir); // Update hướng theo đạn
        }

        // --- 4. VISUAL STATES ---
        bool isMoving = moveInput.magnitude > 0.1f;

        if (!isMoving && !attemptedToShoot)
        {
            SetVisualState(isIdle: true);
        }
        else
        {
            SetVisualState(isIdle: false);

            //if (!attemptedToShoot)
            //    UpdateActiveModel(isMoving, moveInput); // Update hướng theo di chuyển

            HandleFootsteps(isMoving);
        }
        // --- 5. ITEM INPUT ---
        // Nếu nhấn Space và đang giữ item -> Kích hoạt
        if (Input.GetKeyDown(KeyCode.Space) && heldItem != null)
        {
            ActivateItem(heldItem);
            heldItem = null; // Xóa khỏi ô chứa
            if (UIManager.Instance != null) UIManager.Instance.UpdateItem(null);
        }
    }

    // --- HELPER FUNCTIONS ---
    void Shoot(Vector2 dir)
    {
        Vector3 spawnPos = transform.position + (Vector3)(dir * bulletOffset);
        if (firePoint != null) spawnPos = firePoint.position;
        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);

        // (Giả sử Bullet script của bạn có hàm Setup)
        var bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript) bulletScript.Setup(dir);

        if (gunAudioSource && shootClip) gunAudioSource.PlayOneShot(shootClip);
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

    // --- HÀM NHẶT ITEM (Gọi từ ItemPickup) ---
    public void PickUpItem(PowerUpData newItem)
    {
        // 1. Xử lý item Tiêu thụ ngay (Coin, Mạng)
        if (newItem.type == PowerUpType.Coin)
        {
            // Cộng tiền (Logic giả định)
            Debug.Log("Nhặt tiền: " + newItem.valueAmount);
            // GameManager.Instance.AddCoin((int)newItem.valueAmount);
            return;
        }
        if (newItem.type == PowerUpType.Life)
        {
            Debug.Log("Thêm mạng!");
            // GameManager.Instance.AddLife();
            return;
        }

        // 2. Xử lý item Power-up (Cầm nắm)
        // YÊU CẦU CỦA BẠN: "Nếu nhặt item mới khi đang giữ item -> Kích hoạt item MỚI ngay lập tức"
        if (heldItem != null)
        {
            Debug.Log("Đang giữ " + heldItem.itemName + " nhưng nhặt " + newItem.itemName + " -> Kích hoạt cái mới luôn!");
            ActivateItem(newItem); // Kích hoạt luôn cái mới nhặt
            // Item cũ trong túi (heldItem) vẫn giữ nguyên hay mất?
            // Theo logic game gốc: Nhặt cái mới thì cái mới ĐÈ LÊN cái cũ trong túi. 
            // Nhưng theo yêu cầu của bạn: "item mới sẽ ngay lập tức được kích hoạt".
            // Tôi sẽ làm theo ý bạn: Kích hoạt cái mới nhặt, cái trong túi giữ nguyên.
        }
        else
        {
            // Nếu túi rỗng -> Nhặt vào túi
            heldItem = newItem;
            if (UIManager.Instance != null) UIManager.Instance.UpdateItem(heldItem.icon);
        }
    }

    // --- HÀM KÍCH HOẠT ITEM ---
    void ActivateItem(PowerUpData item)
    {
        // Phát âm thanh
        if (item.activateSound != null && gunAudioSource != null)
            gunAudioSource.PlayOneShot(item.activateSound);

        // Reset hiệu ứng cũ trước khi áp dụng cái mới (Ghi đè)
        ResetPowerUpEffects();

        // Xử lý hiệu ứng Tức thời (Nuke, Teleport...)
        switch (item.type)
        {
            case PowerUpType.ScreenNuke:
                KillAllEnemies();
                return; // Nuke xong là hết, không có duration

            case PowerUpType.SmokeBomb:
                TeleportRandomly();
                // Khói mù có thể kéo dài 1 chút để che mắt quái (nếu muốn)
                break;
        }

        // Nếu item có thời gian tác dụng -> Chạy Coroutine đếm ngược
        if (item.duration > 0)
        {
            activePowerUpCoroutine = StartCoroutine(PowerUpRoutine(item));
        }
    }

    IEnumerator PowerUpRoutine(PowerUpData item)
    {
        // 1. ÁP DỤNG HIỆU ỨNG
        ApplyEffect(item);

        // 2. CHỜ HẾT GIỜ
        yield return new WaitForSeconds(item.duration);

        // 3. HẾT GIỜ -> RESET VỀ BÌNH THƯỜNG
        ResetPowerUpEffects();
    }

    void ApplyEffect(PowerUpData item)
    {
        switch (item.type)
        {
            case PowerUpType.HeavyMachineGun:
                fireDelay = 0.1f; // Bắn siêu nhanh
                enableAutoFire = true;
                break;
            case PowerUpType.Shotgun:
                // Cần logic bắn Shotgun ở hàm Shoot (sẽ làm sau)
                // Tạm thời set flag
                break;
            case PowerUpType.Coffee:
                moveSpeed = defaultMoveSpeed + item.valueAmount; // Tăng tốc
                legsAnimator.speed = 2f; // Chân khua nhanh hơn
                break;
            case PowerUpType.SheriffBadge:
                fireDelay = 0.1f;
                enableAutoFire = true;
                moveSpeed = defaultMoveSpeed + 2f; // Shotgun + MG + Speed
                // Thêm logic Shotgun sau
                break;
            case PowerUpType.WagonWheel:
                // Logic bắn 8 hướng
                break;
        }
        Debug.Log("Đã kích hoạt: " + item.itemName);
    }

    void ResetPowerUpEffects()
    {
        // Nếu đang có hiệu ứng chạy dở -> Dừng nó lại
        if (activePowerUpCoroutine != null) StopCoroutine(activePowerUpCoroutine);

        // Trả lại chỉ số gốc
        fireDelay = defaultFireDelay;
        moveSpeed = defaultMoveSpeed;
        enableAutoFire = false;
        bulletPrefab = defaultBulletPrefab;

        if (legsAnimator != null) legsAnimator.speed = 1f;

        // Reset các flag súng ống (sẽ thêm sau)
        // isShotgunActive = false;
        // isWheelActive = false;

        Debug.Log("Đã Reset hiệu ứng!");
    }

    // --- CÁC HÀM HỖ TRỢ ---
    void KillAllEnemies()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var e in enemies) Destroy(e); // Thêm hiệu ứng nổ sau
        Debug.Log("NUKE! Đã diệt " + enemies.Length + " quái.");
    }

    void TeleportRandomly()
    {
        // Dịch chuyển đến vị trí ngẫu nhiên trong map (tránh tường)
        // Tạm thời để random đơn giản
        transform.position = new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), 0);
    }

    // --- DEATH LOGIC ---
    public void TriggerDeathAnimation()
    {
        isDead = true;
        moveInput = Vector2.zero;

        // Dừng vật lý tuyệt đối
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        if (footstepAudioSource) footstepAudioSource.Stop();

        // Xử lý hiển thị: Bắt buộc hiện ActiveModel để Animator chạy
        if (activeStateObject != null) activeStateObject.SetActive(true);
        if (idleStateObject != null) idleStateObject.SetActive(false);

        // Ẩn thân trên (Body) đi, chỉ để Chân diễn animation
        if (bodyRenderer != null) bodyRenderer.enabled = false;

        // Trigger Animation
        if (legsAnimator != null) legsAnimator.SetTrigger("Die");
    }

    public void ResetState()
    {
        isDead = false;

        // Bật lại vật lý
        if (rb != null) rb.simulated = true;

        // Reset Animation & Body
        if (legsAnimator != null) legsAnimator.Play("Idle");
        if (bodyRenderer != null) bodyRenderer.enabled = true;

        ToggleVisibility(true);
    }

    // --- INVINCIBILITY LOGIC ---
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

    void ToggleVisibility(bool isVisible)
    {
        if (bodyRenderer) bodyRenderer.enabled = isVisible;
        if (legsRenderer) legsRenderer.enabled = isVisible;
        if (idleRenderer) idleRenderer.enabled = isVisible;
    }
}