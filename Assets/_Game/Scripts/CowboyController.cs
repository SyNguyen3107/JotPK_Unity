using System.Collections;
using UnityEngine;

public enum BossState
{
    Waiting,    // Mới sinh ra, chưa làm gì cả (chờ GameManager)
    Intro,      // Bắt đầu gáy (3s)
    Idle,
    Moving,
    Dead
}

public class CowboyController : BossController
{
    [Header("Settings")]
    public float idleTime = 2f;
    public float minX = -6.5f;
    public float maxX = 6.5f;

    [Header("Combat")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 0.5f;

    [Header("Boss Death VFX")]
    public int smokeCount = 6;
    public float explosionDuration = 0.5f;

    // --- State Variables ---
    // Gán mặc định ngay khi khai báo biến để tránh lỗi race condition
    private BossState currentState = BossState.Waiting;

    private float stateTimer;
    private Vector3 targetPosition;
    private float shootTimer;
    private PlayerController playerScript;

    // Dùng Awake thay vì Start để khởi tạo các biến tham chiếu
    protected void Awake()
    {
        currentHealth = maxHealth;
    }

    void Start()
    {
        base.Start();

        if (animator != null) animator.SetBool("IsMoving", false);
    }

    // Hàm này được BossManager gọi khi chuyển cảnh xong
    public override void StartBossFight()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerScript = playerObj.GetComponent<PlayerController>();
            if (playerScript != null)
            {
                playerScript.isInputEnabled = false;
                if (playerScript.rb != null) playerScript.rb.linearVelocity = Vector2.zero;
                if (playerScript.legsAnimator != null) playerScript.legsAnimator.SetBool("IsMoving", false);
            }
        }

        currentState = BossState.Intro; // Chuyển trạng thái sang Intro
        stateTimer = 3f;
        Debug.Log("COWBOY BOSS FIGHT STARTED! State changed to Intro.");
    }

    protected override void Update()
    {
        if (currentState == BossState.Dead) return;

        switch (currentState)
        {
            case BossState.Waiting:
                // Boss sẽ đứng im ở đây cho đến khi StartBossFight được gọi
                break;
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

    void HandleIntro()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0)
        {
            // Hết 3s Intro -> Vào chiến đấu
            if (dialogObject != null)
            {
                SetBossDialog(false);
            }
            EnterIdleState();

            // Mở khóa Player
            if (playerScript != null)
            {
                playerScript.isInputEnabled = true;
            }
        }
    }

    void HandleIdle()
    {
        stateTimer -= Time.deltaTime;
        if (animator != null) animator.SetBool("IsMoving", false);
        if (stateTimer <= 0) PickNewMoveTarget();
    }

    void HandleMoving()
    {
        if (animator != null) animator.SetBool("IsMoving", true);

        // Di chuyển
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        // Bắn súng
        shootTimer -= Time.deltaTime;
        if (shootTimer <= 0)
        {
            Shoot();
            shootTimer = fireRate;
        }

        // Kiểm tra đến đích (Chỉ so sánh khoảng cách)
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            EnterIdleState();
        }
    }

    void EnterIdleState()
    {
        currentState = BossState.Idle;
        stateTimer = idleTime;
    }

    void PickNewMoveTarget()
    {
        currentState = BossState.Moving;
        float randomX = Random.Range(minX, maxX);
        // Giữ nguyên Y và Z, chỉ thay đổi X
        targetPosition = new Vector3(randomX, transform.position.y, transform.position.z);
    }

    void Shoot()
    {
        if (bulletPrefab != null)
        {
            Vector3 shootDir = Vector3.down;
            // Tự tìm player nếu chưa có
            if (playerScript == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p) playerScript = p.GetComponent<PlayerController>();
            }

            if (playerScript != null)
            {
                shootDir = (playerScript.transform.position - transform.position).normalized;
            }

            Vector3 spawnPos = (firePoint != null) ? firePoint.position : transform.position;
            GameObject bulletObj = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);

            EnemyBullet bulletScript = bulletObj.GetComponent<EnemyBullet>();
            if (bulletScript != null) bulletScript.Setup(shootDir);
        }
    }

    public override void TakeDamage(int dmg)
    {
        if (currentState == BossState.Intro || currentState == BossState.Dead || currentState == BossState.Waiting) return;

        currentHealth -= dmg;
        if (!isFlashing) StartCoroutine(FlashRoutine());

        // Update UI thông qua BossManager (nếu cần thiết, hoặc để BossManager tự lo trong Update)

        if (currentHealth <= 0) Die();
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
        if (sr != null) sr.enabled = false;
        for (int i = 0; i < smokeCount; i++)
        {
            if (deathEffectPrefab != null)
            {
                Vector3 randomOffset = Random.insideUnitCircle * 0.5f;
                GameObject fx = Instantiate(deathEffectPrefab, transform.position + randomOffset, Quaternion.identity);

                // Âm thanh chết
                if (deathSounds.Length > 0)
                {
                    AudioClip randomClip = deathSounds[Random.Range(0, deathSounds.Length)];
                    DeathEffect deathScript = fx.GetComponent<DeathEffect>();
                    if (deathScript != null) deathScript.PlaySound(randomClip);
                }
            }
            yield return new WaitForSeconds(explosionDuration / smokeCount);
        }
        Destroy(gameObject);
    }

    protected override void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();

            if (player != null)
            {
                if (player.isZombieMode)
                {
                    TakeDamage(10);
                    return;
                }
                if (player.IsInvincible()) return;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.PlayerDied();
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(minX, transform.position.y, 0), new Vector3(maxX, transform.position.y, 0));
        Gizmos.DrawWireSphere(new Vector3(minX, transform.position.y, 0), 0.2f);
        Gizmos.DrawWireSphere(new Vector3(maxX, transform.position.y, 0), 0.2f);
    }
}