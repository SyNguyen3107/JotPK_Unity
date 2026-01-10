using System.Collections;
using UnityEngine;

public enum BossState
{
    Intro,      // Lúc hiện dialog (3s đầu)
    Idle,       // Đứng nấp sau rào
    Moving,     // Đang chạy sang vị trí mới
    Dead        // Đã chết
}

public class CowboyController : Enemy
{
    [Header("Settings")]
    public float idleTime = 2f; // Thời gian nấp sau rào
    public float minX = -6.5f;    // Giới hạn di chuyển bên trái
    public float maxX = 6.5f;     // Giới hạn di chuyển bên phải

    [Header("Combat")]
    public GameObject bulletPrefab;
    public Transform firePoint; // Điểm bắn đạn (đầu súng)
    public float fireRate = 0.5f;

    [Header("Visuals")]
    public GameObject dialogObject;

    [Header("Boss Death VFX")]
    public GameObject screenFlashObject;
    public int smokeCount = 6;
    public float explosionDuration = 0.5f;

    // --- State Variables ---
    private BossState currentState;
    private float stateTimer;
    private Vector3 targetPosition;
    private float shootTimer;
    private bool isFlashing = false;
    private PlayerController playerScript;

    void Start()
    {
        base.Start();
        // Setup Máu (Sẽ cập nhật logic lấy từ Manager sau)
        currentHealth = maxHealth;

        // Bắt đầu Intro
        currentState = BossState.Intro;
        stateTimer = 3f; // 3 giây intro

        if (dialogObject != null) dialogObject.SetActive(true);

        // Đảm bảo boss ở trạng thái Idle khi bắt đầu
        if (animator != null) animator.SetBool("IsMoving", false);

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerScript = playerObj.GetComponent<PlayerController>();
            if (playerScript != null)
            {
                // Khóa Input
                playerScript.isInputEnabled = false;

                // Dừng hẳn chuyển động (tránh trượt)
                if (playerScript.rb != null) playerScript.rb.linearVelocity = Vector2.zero;
                if (playerScript.legsAnimator != null) playerScript.legsAnimator.SetBool("IsMoving", false);
            }
        }
    }

    protected override void Update()
    {
        if (currentState == BossState.Dead) return;

        switch (currentState)
        {
            case BossState.Intro:
                HandleIntro();
                break;
            case BossState.Idle:
                HandleIdle();
                break;
            case BossState.Moving:
                HandleMoving();
                break;
        }
    }

    // --- LOGIC CÁC TRẠNG THÁI ---

    void HandleIntro()
    {
        // Trong intro, Boss đứng yên, hiện dialog
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0)
        {
            // Hết 3s Intro -> Vào trận
            if (dialogObject != null) dialogObject.SetActive(false);
            EnterIdleState();

            if (playerScript != null)
            {
                playerScript.isInputEnabled = true;
            }
        }
    }

    void HandleIdle()
    {
        stateTimer -= Time.deltaTime;

        // Animation Idle
        if (animator != null) animator.SetBool("IsMoving", false);

        if (stateTimer <= 0)
        {
            PickNewMoveTarget();
        }
    }

    void HandleMoving()
    {
        // 1. Animation Moving
        if (animator != null) animator.SetBool("IsMoving", true);

        // 2. Di chuyển
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        // 3. Bắn súng (Boss bắn khi đang di chuyển)
        shootTimer -= Time.deltaTime;
        if (shootTimer <= 0)
        {
            Shoot();
            shootTimer = fireRate;
        }

        // 4. Kiểm tra đến đích
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            EnterIdleState();
        }
    }

    // --- CÁC HÀM PHỤ TRỢ ---

    void EnterIdleState()
    {
        currentState = BossState.Idle;
        stateTimer = idleTime;
    }

    void PickNewMoveTarget()
    {
        currentState = BossState.Moving;

        // Random vị trí X mới
        float randomX = Random.Range(minX, maxX);

        // Giữ nguyên Y và Z
        targetPosition = new Vector3(randomX, transform.position.y, transform.position.z);
    }

    void Shoot()
    {
        if (bulletPrefab != null)
        {
            // Bắn về phía Player
            Vector3 shootDir = Vector3.down;
            if (playerTransform != null) // playerTransform có sẵn từ Enemy
            {
                shootDir = (playerTransform.position - transform.position).normalized;
            }

            // Dùng FirePoint nếu có, không thì dùng tâm (dễ bị tự hủy đạn)
            Vector3 spawnPos = (firePoint != null) ? firePoint.position : transform.position;

            GameObject bulletObj = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);

            // QUAN TRỌNG: Prefab đạn của Boss phải dùng script EnemyBullet (xem bên dưới)
            EnemyBullet bulletScript = bulletObj.GetComponent<EnemyBullet>();
            if (bulletScript != null)
            {
                bulletScript.Setup(shootDir);
            }
        }
    }

    public void TakeDamage(int dmg)
    {
        if (currentState == BossState.Intro || currentState == BossState.Dead) return;

        currentHealth -= dmg;
        Debug.Log($"Boss HP: {currentHealth}");

        // --- MỚI: Gọi hiệu ứng chớp trắng ---
        if (!isFlashing)
        {
            StartCoroutine(FlashRoutine());
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    IEnumerator FlashRoutine()
    {
        isFlashing = true;

        // 1. Lưu trạng thái cũ
        bool wasAnimEnabled = false;
        if (animator != null)
        {
            wasAnimEnabled = animator.enabled;
            animator.enabled = false; // Tắt Animator để nó không tự đổi sprite lại
        }

        // 2. Hiện sprite trắng
        Sprite originalSprite = sr.sprite; // Lưu sprite hiện tại (phòng hờ)
        if (whiteSprite != null)
        {
            sr.sprite = whiteSprite;
        }

        // 3. Chờ 0.1 giây
        yield return new WaitForSeconds(0.1f);

        // 4. Khôi phục trạng thái
        if (animator != null)
        {
            animator.enabled = wasAnimEnabled; // Bật lại, Animator sẽ tự update frame tiếp theo
        }
        else
        {
            sr.sprite = originalSprite; // Nếu không dùng animator thì trả lại sprite cũ thủ công
        }

        isFlashing = false;
    }

    public override void Die(bool dropLoot = true)
    {
        if (isDead) return;
        isDead = true;
        currentState = BossState.Dead;

        if (animator != null) animator.SetBool("IsMoving", false);
        if (rb != null) rb.linearVelocity = Vector2.zero;
        GetComponent<Collider2D>().enabled = false;

        StartCoroutine(BossDeathSequence());
    }

    IEnumerator BossDeathSequence()
    {
        // A. Hiệu ứng Nháy màn hình
        if (screenFlashObject != null)
        {
            screenFlashObject.SetActive(true);
            yield return new WaitForSeconds(0.1f);
            screenFlashObject.SetActive(false);
        }

        // B. Ẩn Boss
        if (sr != null) sr.enabled = false;

        // C. Nổ khói liên hoàn (Tận dụng logic của Enemy.cs)
        // Lặp lại việc spawn hiệu ứng nhiều lần
        for (int i = 0; i < smokeCount; i++)
        {
            // Kiểm tra xem có prefab hiệu ứng chết ở class cha (Enemy) chưa
            if (deathEffectPrefab != null)
            {
                // Random vị trí lệch một chút xung quanh tâm
                Vector3 randomOffset = Random.insideUnitCircle * 0.5f;

                // Spawn Effect
                GameObject fx = Instantiate(deathEffectPrefab, transform.position + randomOffset, Quaternion.identity);

                // --- LOGIC COPY TỪ ENEMY.CS ---
                // Random âm thanh và phát qua script DeathEffect của cái FX đó
                if (deathSounds.Length > 0)
                {
                    AudioClip randomClip = deathSounds[Random.Range(0, deathSounds.Length)];
                    DeathEffect deathScript = fx.GetComponent<DeathEffect>();

                    if (deathScript != null)
                    {
                        deathScript.PlaySound(randomClip);
                    }
                }
                // ------------------------------
            }

            // Chờ giữa các lần nổ
            yield return new WaitForSeconds(explosionDuration / smokeCount);
        }

        // D. Kết thúc
        Debug.Log("BOSS VISUALS DONE. READY FOR LOOT & CUTSCENE.");
        Destroy(gameObject);
    }

    // Vẽ Gizmos để bạn dễ chỉnh vùng di chuyển MinX - MaxX
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(minX, transform.position.y, 0), new Vector3(maxX, transform.position.y, 0));
        Gizmos.DrawWireSphere(new Vector3(minX, transform.position.y, 0), 0.2f);
        Gizmos.DrawWireSphere(new Vector3(maxX, transform.position.y, 0), 0.2f);
    }
}