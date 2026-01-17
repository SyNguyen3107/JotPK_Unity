using System.Collections;
using UnityEngine;

public enum BossState
{
    Waiting,
    Intro,
    Idle,
    Moving,
    Dead
}

public class CowboyController : BossController
{
    #region Configuration & Settings
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
    #endregion

    #region Runtime Variables
    private BossState currentState = BossState.Waiting;
    private float stateTimer;
    private Vector3 targetPosition;
    private float shootTimer;
    private PlayerController playerScript;
    #endregion

    #region Unity Lifecycle
    protected void Awake()
    {
        currentHealth = maxHealth;
    }

    void Start()
    {
        base.Start();

        if (animator != null) animator.SetBool("IsMoving", false);
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
    #endregion

    #region Core Logic
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
    #endregion

    #region State Machine
    void HandleIntro()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0)
        {
            if (dialogObject != null)
            {
                SetBossDialog(false);
            }
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
        targetPosition = new Vector3(randomX, transform.position.y, transform.position.z);
    }
    #endregion

    #region Combat Logic
    void Shoot()
    {
        if (bulletPrefab != null)
        {
            Vector3 shootDir = Vector3.down;
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
    #endregion

    #region Coroutines
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
                    if (deathScript != null) deathScript.PlaySound(randomClip);
                }
            }
            yield return new WaitForSeconds(explosionDuration / smokeCount);
        }
        Destroy(gameObject);
    }
    #endregion
}