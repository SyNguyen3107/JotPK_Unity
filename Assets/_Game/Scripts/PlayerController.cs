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

    [Header("Power-Ups State")]
    public PowerUpData heldItem; // Item đang giữ trong ô đỏ
    private Coroutine activePowerUpCoroutine; // Để quản lý việc tắt hiệu ứng cũ

    [Header("Nuke Settings")]
    public GameObject explosionFXPrefab; // Kéo Prefab vụ nổ vào đây
    public int explosionCount = 15;      // Số lượng vụ nổ muốn hiển thị
    public float nukeDuration = 2f;      // Thời gian diễn ra hiệu ứng

    // Giới hạn vùng sân đấu để nổ không lấn ra ngoài tường
    // Bạn hãy chỉnh số này khớp với kích thước map của bạn (Ví dụ: -6 đến 6)
    public Vector2 mapBoundsMin = new Vector2(-6f, -5f);
    public Vector2 mapBoundsMax = new Vector2(6f, 5f);

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

    // Các chỉ số gốc (để reset)
    private float defaultFireDelay;
    private float defaultMoveSpeed;
    private GameObject defaultBulletPrefab;

    [Header("Audio")]
    public AudioSource gunAudioSource;
    public AudioSource footstepAudioSource;
    public AudioSource itemsPickupAudioSource;
    public AudioSource itemsActivateAudioSource;
    public AudioClip shootClip;
    public AudioClip footstepClip;
    public AudioClip itemPickupClip;
    public float stepDelay = 0.3f;
    private float nextStepTime = 0f;

    // State Variables
    private Vector2 moveInput;
    private bool isDead = false;
    private bool isInvincible = false;
    private bool isShotgunActive = false; // Bắn chùm 3 tia
    private bool isWheelActive = false;   // Bắn 8 hướng
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

            if (!attemptedToShoot)
                UpdateActiveModel(isMoving, moveInput); // Update hướng theo di chuyển

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
        // Kịch bản 1: Wagon Wheel (Bắn 8 hướng)
        // Lưu ý: Wheel được ưu tiên cao nhất, nếu vừa có Wheel vừa có Shotgun thì bắn Wheel
        if (isWheelActive)
        {
            // Bắn 8 viên xung quanh (cách nhau 45 độ)
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f;

                // Tạo Quaternion xoay
                Quaternion rotation = Quaternion.Euler(0, 0, angle);

                // Xoay vector hướng mặc định (ví dụ hướng lên hoặc hướng phải)
                // Vector2.up là (0,1). Xoay nó đi các góc sẽ ra vòng tròn.
                Vector2 wheelDir = rotation * Vector2.up;

                SpawnBullet(wheelDir);
            }
        }
        // Kịch bản 2: Shotgun (Bắn 3 tia hình nón)
        else if (isShotgunActive)
        {
            // Viên 1: Chính giữa
            SpawnBullet(dir);

            // Viên 2: Lệch trái 15 độ
            Vector2 leftDir = Quaternion.Euler(0, 0, 15f) * dir;
            SpawnBullet(leftDir);

            // Viên 3: Lệch phải 15 độ (hoặc -15)
            Vector2 rightDir = Quaternion.Euler(0, 0, -15f) * dir;
            SpawnBullet(rightDir);
        }
        // Kịch bản 3: Bắn thường
        else
        {
            SpawnBullet(dir);
        }

        // Âm thanh (Chỉ phát 1 lần dù bắn ra bao nhiêu đạn)
        if (gunAudioSource && shootClip) gunAudioSource.PlayOneShot(shootClip);
    }
    // Hàm phụ trợ để sinh ra viên đạn (Tránh code lặp lại trong hàm Shoot)
    void SpawnBullet(Vector2 dir)
    {
        Vector3 spawnPos = transform.position + (Vector3)(dir * bulletOffset);
        if (firePoint != null) spawnPos = firePoint.position; // Nếu dùng firePoint cố định thì Shotgun sẽ hơi lạ, nên tính toán offset theo dir thì đẹp hơn

        // --- TÍNH TOÁN LẠI SPAWN POS CHO CHUẨN ---
        // Nếu không dùng FirePoint, ta cộng offset theo hướng bắn của từng viên đạn
        if (firePoint == null)
        {
            spawnPos = transform.position + (Vector3)(dir.normalized * bulletOffset);
        }

        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        var bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript) bulletScript.Setup(dir);
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
        // === NHÓM 1: ĂN NGAY LẬP TỨC (Coin, Life) ===
        // Các item này không đi vào túi đồ (heldItem) mà cộng thẳng vào chỉ số

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
                GameManager.Instance.AddLife(1); // Mặc định là thêm 1 mạng
                if (itemsPickupAudioSource != null)
                    itemsPickupAudioSource.PlayOneShot(newItem.activateSound);
            }

            return;
        }

        // === NHÓM 2: POWER-UPS CẦM NẮM ===
        // Logic: Nếu đang giữ đồ -> Kích hoạt cái MỚI ngay lập tức
        if (heldItem != null)
        {
            Debug.Log("Túi đầy! Kích hoạt ngay item mới: " + newItem.itemName);
            ActivateItem(newItem);
            if (itemsActivateAudioSource != null && newItem.activateSound!= null)
            {
                itemsActivateAudioSource.PlayOneShot(newItem.activateSound);
            }
        }
        else
        {
            // Túi rỗng -> Nhặt vào túi
            heldItem = newItem;
            if (UIManager.Instance != null) UIManager.Instance.UpdateItem(heldItem.icon);
            if (itemsPickupAudioSource != null && itemPickupClip!= null)
            {
                itemsActivateAudioSource.PlayOneShot(itemPickupClip);
            }

        }
    }

    // --- HÀM KÍCH HOẠT ITEM ---
    // --- 2. ÁP DỤNG HIỆU ỨNG ---
    void ApplyEffect(PowerUpData item)
    {
        switch (item.type)
        {
            // --- HEAVY MACHINE GUN ---
            // Tác dụng: Bắn siêu nhanh + Tự động bắn (giữ nút)
            case PowerUpType.HeavyMachineGun:
                fireDelay = 0.1f;    // Tốc độ bắn cực nhanh
                enableAutoFire = true; // Cho phép giữ nút để sấy

                break;
            // --- COFFEE ---
            case PowerUpType.Coffee:
                // Cộng thêm tốc độ (Ví dụ: 5 + 2 = 7)
                // newItem.valueAmount nên set là khoảng 2 hoặc 3 trong Inspector
                moveSpeed = defaultMoveSpeed + item.valueAmount;
                // Làm animation chân khua nhanh hơn cho kịch tính
                if (legsAnimator != null) legsAnimator.speed = 2f;
                break;
            // --- SHOTGUN ---
            case PowerUpType.Shotgun:
                fireDelay = 0.5f;      // Bắn chậm hơn (0.5s)
                enableAutoFire = false; // Vẫn phải nhấp tay (trừ khi có upgrade gốc)
                isShotgunActive = true; // Bật cờ Shotgun
                break;
            // --- WAGON WHEEL ---
            case PowerUpType.WagonWheel:
                // Tốc bắn giữ nguyên (hoặc theo default)
                enableAutoFire = false;
                isWheelActive = true;   // Bật cờ Wheel
                break;
            // --- SHERIFF BADGE ---
            case PowerUpType.SheriffBadge:
                moveSpeed = defaultMoveSpeed + item.valueAmount;
                if (legsAnimator != null) legsAnimator.speed = 2f;

                fireDelay = 0.1f;       // Siêu nhanh (Ghi đè cái chậm của Shotgun)
                enableAutoFire = true;

                // 3. Hình nón (Shotgun)
                isShotgunActive = true;
                break;
            case PowerUpType.ScreenNuke:
                StartCoroutine(NukeRoutine()); // Gọi Coroutine mới
                break;
        }
        if (item.duration > 0 && item.type != PowerUpType.ScreenNuke) // Nuke tự quản lý thời gian
        {
            activePowerUpCoroutine = StartCoroutine(PowerUpRoutine(item));
        }
        Debug.Log("Đã kích hoạt Buff: " + item.itemName);
    }
    void ActivateItem(PowerUpData item)
    {
        // Phát âm thanh
        if (item.activateSound != null)
            itemsActivateAudioSource.PlayOneShot(item.activateSound);

        // Reset hiệu ứng cũ trước khi áp dụng cái mới (Ghi đè)
        ResetPowerUpEffects();

        // Xử lý hiệu ứng Tức thời (Nuke, Teleport...)
        switch (item.type)
        {
            case PowerUpType.ScreenNuke:
                StartCoroutine(NukeRoutine()); // Gọi Coroutine mới
                break;

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
    // --- 3. RESET HIỆU ỨNG (Sửa lại hàm ResetPowerUpEffects) ---
    void ResetPowerUpEffects()
    {
        if (activePowerUpCoroutine != null) StopCoroutine(activePowerUpCoroutine);

        // Reset chỉ số cơ bản
        fireDelay = defaultFireDelay;
        enableAutoFire = false;
        moveSpeed = defaultMoveSpeed;
        if (legsAnimator != null) legsAnimator.speed = 1f;

        // Reset các cờ súng đạn đặc biệt
        isShotgunActive = false;
        isWheelActive = false;

        Debug.Log("Hết giờ! Trở về trạng thái bình thường.");
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
    IEnumerator NukeRoutine()
    {
        Debug.Log("NUKE ACTIVATED!");

        // 1. Tiêu diệt TẤT CẢ quái ngay lập tức (Không rơi đồ)
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (var e in enemies)
        {
            var enemyScript = e.GetComponent<Enemy>();
            if (enemyScript != null)
            {
                enemyScript.Die(false); // false = Không rơi đồ
            }
            else
            {
                Destroy(e);
            }
        }

        // 2. Tạo hiệu ứng nổ rải rác trong 2 giây
        // Chúng ta sẽ chia nhỏ thời gian để nổ trông tự nhiên
        int explosionsSpawned = 0;

        while (explosionsSpawned < explosionCount)
        {
            // Sinh vị trí ngẫu nhiên trong sân
            float randomX = Random.Range(mapBoundsMin.x, mapBoundsMax.x);
            float randomY = Random.Range(mapBoundsMin.y, mapBoundsMax.y);
            Vector3 explosionPos = new Vector3(randomX, randomY, 0);

            // Tạo Prefab nổ
            if (explosionFXPrefab != null)
            {
                Instantiate(explosionFXPrefab, explosionPos, Quaternion.identity);
            }

            explosionsSpawned++;

            // Chờ một chút ngẫu nhiên trước khi nổ quả tiếp theo
            // Tính toán: Tổng thời gian / Tổng số nổ (có random nhẹ)
            float waitTime = (nukeDuration / explosionCount) * Random.Range(0.5f, 1.5f);
            yield return new WaitForSeconds(waitTime);
        }
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