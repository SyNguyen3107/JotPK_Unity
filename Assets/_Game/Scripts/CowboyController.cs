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

public class CowboyController : Enemy
{
    [Header("Settings")]
    public float idleTime = 2f;
    public float minX = -6.5f;
    public float maxX = 6.5f;

    [Header("Combat")]
    public GameObject bulletPrefab;
    public Transform firePoint;
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
        currentHealth = maxHealth;
        currentState = BossState.Waiting;

        if (animator != null) animator.SetBool("IsMoving", false);

    }
    public void setBossDialog(bool isActive)
    {
        if (dialogObject != null)
        {
            dialogObject.SetActive(isActive);
            if (isActive)
            {
                Debug.Log("Bật hộp thoại");
            }
            else
            {
                Debug.Log("Tắt hộp thoại");
            }
        }
    }
    // Hàm này được BossManager gọi khi chuyển cảnh xong
    public void StartBossFight()
    {
        // 1. Tìm và Khóa Player
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

        // 2. CHUYỂN SANG INTRO (BẮT ĐẦU ĐẾM NGƯỢC)
        currentState = BossState.Intro;
        stateTimer = 3f;
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
                setBossDialog(false);
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

    public void TakeDamage(int dmg)
    {
        if (currentState == BossState.Intro || currentState == BossState.Dead || currentState == BossState.Waiting) return;
        currentHealth -= dmg;
        if (!isFlashing) StartCoroutine(FlashRoutine());
        if (currentHealth <= 0) Die();
    }

    IEnumerator FlashRoutine()
    {
        isFlashing = true;
        bool wasAnimEnabled = false;
        if (animator != null) { wasAnimEnabled = animator.enabled; animator.enabled = false; }
        Sprite originalSprite = sr.sprite;
        if (whiteSprite != null) sr.sprite = whiteSprite;
        yield return new WaitForSeconds(0.1f);
        if (animator != null) animator.enabled = wasAnimEnabled;
        else sr.sprite = originalSprite;
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
        if (screenFlashObject != null) { screenFlashObject.SetActive(true); yield return new WaitForSeconds(0.1f); screenFlashObject.SetActive(false); }
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
                    if (deathScript != null) deathScript.PlaySound(randomClip);
                }
            }
            yield return new WaitForSeconds(explosionDuration / smokeCount);
        }
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