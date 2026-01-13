using System.Collections;
using UnityEngine;

// 1. THÊM TRẠNG THÁI WAITING
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
    private BossState currentState;
    private float stateTimer;
    private Vector3 targetPosition;
    private float shootTimer;
    private PlayerController playerScript;

    void Start()
    {
        base.Start();
        currentHealth = maxHealth;
        currentState = BossState.Waiting;

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
        currentState = BossState.Intro;
        stateTimer = 3f;
        Debug.Log("COWBOY BOSS FIGHT STARTED!");
    }

    protected override void Update()
    {
        if (currentState == BossState.Dead) return;

        switch (currentState)
        {
            case BossState.Waiting:
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
                //Tắt lại nếu chưa tắt
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
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        shootTimer -= Time.deltaTime;
        if (shootTimer <= 0)
        {
            Shoot();
            shootTimer = fireRate;
        }
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f) EnterIdleState();
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
        targetPosition = new Vector3(randomX, transform.position.y, transform.position.z);
    }

    void Shoot()
    {
        if (bulletPrefab != null)
        {
            Vector3 shootDir = Vector3.down;
            if (playerTransform != null) shootDir = (playerTransform.position - transform.position).normalized;
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
                if (deathSounds.Length > 0)
                {
                    
                    AudioClip randomClip = deathSounds[Random.Range(0, deathSounds.Length)];
                    DeathEffect deathScript = fx.GetComponent<DeathEffect>();
                    if (deathScript != null)
                    {
                        deathScript.PlaySound(randomClip);
                        Debug.Log("Playing death sound");
                    }
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
                // Logic Zombie
                if (player.isZombieMode)
                {
                    TakeDamage(10); // Gây 10 damage
                    Debug.Log("Zombie Player hit Cowboy! Dealt 10 DMG.");
                    return;
                }

                // Logic Bất tử
                if (player.IsInvincible())
                {
                    return;
                }
            }

            // Logic thường: Giết Player
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PlayerDied();
            }
            // Boss Cowboy KHÔNG tự hủy
        }
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