using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum FectorState
{
    Waiting,
    Intro,
    Idle,
    Moving,
    Attacking,
    Teleporting,
    Dead
}

public class FectorController : BossController
{
    [Header("Fector Settings")]
    public float idleTime = 1f;
    public float moveSpeedCustom = 4f;
    public Vector2 mapBoundsMin;
    public Vector2 mapBoundsMax;

    [Header("Combat")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float fireRate = 0.4f;
    public float wagonWheelChance = 0.5f;
    public float attackDuration = 2f;
    public float attackFrequency = 40f;

    [Header("Abilities - Teleport")]
    public float teleportChance = 20f;
    public GameObject smokeCloudPrefab;
    public LayerMask obstacleLayer;
    public int smokeCount = 5;
    private bool isInvincible = false;

    [Header("Abilities - Summon")]
    public List<GameObject> minionPrefabs;

    // --- THAY ĐỔI: LIST CÁC MỐC MÁU ĐỂ SUMMON (50%, 40%, 30%, 20%, 10%) ---
    private List<float> summonThresholds = new List<float> { 0.5f, 0.4f, 0.3f, 0.2f, 0.1f };

    [Header("Boss Death VFX")]
    public GameObject screenFlashObject;

    // --- State Variables ---
    private FectorState currentState;
    private float stateTimer;
    private Vector3 targetPosition;
    private PlayerController playerScript;

    private float shootTimer;
    private int currentAttackMode;

    void Start()
    {
        base.Start();
        currentHealth = maxHealth;
        moveSpeed = moveSpeedCustom;
        bossName = "FECTOR The Immortal";

        if (currentState != FectorState.Intro)
        {
            currentState = FectorState.Waiting;
        }
    }
    public override void StartBossFight()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            playerScript = playerObj.GetComponent<PlayerController>();

            if (playerScript != null)
            {
                playerScript.isInputEnabled = false;
                if (playerScript.rb != null) playerScript.rb.linearVelocity = Vector2.zero;
                if (playerScript.legsAnimator != null) playerScript.legsAnimator.SetBool("IsMoving", false);
            }
        }

        currentState = FectorState.Intro;
        stateTimer = 3f;
        Debug.Log("FECTOR BOSS FIGHT STARTED!");
    }

    protected override void Update()
    {
        if (currentState == FectorState.Dead) return;

        switch (currentState)
        {
            case FectorState.Waiting: break;
            case FectorState.Intro: HandleIntro(); break;
            case FectorState.Idle: HandleIdle(); break;
            case FectorState.Moving: HandleMoving(); break;
            case FectorState.Attacking: HandleAttacking(); break;
            case FectorState.Teleporting: break;
        }
    }

    // --- STATE HANDLERS ---

    void HandleIntro()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0)
        {
            if (dialogObject != null)
            {
                SetBossDialog(false);
                //Tắt lại nếu chưa tắt
            }
            if (playerScript != null) playerScript.isInputEnabled = true;
            EnterIdleState();
        }
    }

    void HandleIdle()
    {
        stateTimer -= Time.deltaTime;
        if (base.animator != null) base.animator.SetBool("IsMoving", false);

        if (stateTimer <= 0)
        {
            PickNextAction();
        }
    }

    void HandleMoving()
    {
        if (base.animator != null) base.animator.SetBool("IsMoving", true);
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        shootTimer -= Time.deltaTime;
        if (shootTimer <= 0)
        {
            ShootLinear();
            shootTimer = fireRate;
        }

        if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
        {
            EnterIdleState();
        }
    }

    void HandleAttacking()
    {
        if (base.animator != null) base.animator.SetBool("IsMoving", false);

        shootTimer -= Time.deltaTime;
        if (shootTimer <= 0)
        {
            if (currentAttackMode == 0) ShootLinear();
            else Shoot8Way();
            shootTimer = fireRate;
        }

        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0)
        {
            EnterIdleState();
        }
    }

    // --- AI DECISION ---

    void EnterIdleState()
    {
        currentState = FectorState.Idle;
        stateTimer = idleTime;
    }

    void PickNextAction()
    {
        float chance = Random.Range(0f, 100f);

        if (chance < teleportChance)
        {
            TeleportAndSmoke();
        }
        else if (chance < teleportChance + attackFrequency)
        {
            PerformRandomAttack();
        }
        else
        {
            currentState = FectorState.Moving;
            float randX = Random.Range(mapBoundsMin.x, mapBoundsMax.x);
            float randY = Random.Range(mapBoundsMin.y, mapBoundsMax.y);
            targetPosition = new Vector3(randX, randY, 0);
            shootTimer = 0.5f;
        }
    }

    void PerformRandomAttack()
    {
        currentState = FectorState.Attacking;
        stateTimer = attackDuration;
        if (Random.value > wagonWheelChance) currentAttackMode = 0;
        else currentAttackMode = 1;
        shootTimer = 0f;
    }

    // --- ABILITIES: TELEPORT ---

    void TeleportAndSmoke()
    {
        currentState = FectorState.Teleporting;
        StartCoroutine(SpawnMultipleSmokes(transform.position));
        Vector3 targetPos = FindSafePosition();
        transform.position = targetPos;
        StartCoroutine(SpawnMultipleSmokes(targetPos));

        // Gọi hàm Tàng hình thay vì Bất tử
        StartCoroutine(StealthRoutine(3f));

        EnterIdleState();
    }

    Vector3 FindSafePosition()
    {
        int maxAttempts = 20;
        for (int i = 0; i < maxAttempts; i++)
        {
            float randX = Random.Range(mapBoundsMin.x, mapBoundsMax.x);
            float randY = Random.Range(mapBoundsMin.y, mapBoundsMax.y);
            Vector2 candidatePos = new Vector2(randX, randY);

            Collider2D hitWall = Physics2D.OverlapCircle(candidatePos, 1f, obstacleLayer);
            float distToPlayer = 999f;
            if (playerTransform != null) distToPlayer = Vector2.Distance(candidatePos, playerTransform.position);

            if (hitWall == null && distToPlayer > 3f)
            {
                return candidatePos;
            }
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
            yield return new WaitForSeconds(0.05f);
        }
    }
    IEnumerator StealthRoutine(float duration)
    {
        // Hiệu ứng "Tàng hình" (Ghost): Mờ đi
        if (sr != null) sr.color = new Color(1f, 1f, 1f, 0.4f); // 40% độ rõ

        yield return new WaitForSeconds(duration);

        // Hết giờ -> Hiện rõ lại
        if (sr != null) sr.color = Color.white;
    }
    // --- ABILITIES: SUMMON ---

    void SummonMinions()
    {
        if (minionPrefabs == null || minionPrefabs.Count == 0) return;

        // Số lượng quái mỗi lần summon (2-4 con)
        int count = Random.Range(2, 5);

        // Hiệu ứng summon
        StartCoroutine(SpawnMultipleSmokes(transform.position));

        for (int i = 0; i < count; i++)
        {
            Vector2 spawnOffset = Random.insideUnitCircle.normalized * 2f;
            Vector3 spawnPos = transform.position + (Vector3)spawnOffset;

            spawnPos.x = Mathf.Clamp(spawnPos.x, mapBoundsMin.x, mapBoundsMax.x);
            spawnPos.y = Mathf.Clamp(spawnPos.y, mapBoundsMin.y, mapBoundsMax.y);

            GameObject prefabToSpawn = minionPrefabs[Random.Range(0, minionPrefabs.Count)];
            GameObject minion = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
            Debug.Log("Summoned Minion at " + spawnPos);
            if (smokeCloudPrefab != null) Instantiate(smokeCloudPrefab, spawnPos, Quaternion.identity);
        }
    }

    // --- COMBAT & DAMAGE ---

    void ShootLinear()
    {
        if (bulletPrefab == null) return;
        Vector3 shootDir = Vector3.down;
        if (playerTransform != null) shootDir = (playerTransform.position - transform.position).normalized;
        SpawnBullet(shootDir);
    }

    void Shoot8Way()
    {
        if (bulletPrefab == null) return;
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            Vector2 wheelDir = Quaternion.Euler(0, 0, angle) * Vector2.up;
            SpawnBullet(wheelDir);
        }
    }

    void SpawnBullet(Vector3 direction)
    {
        Vector3 spawnPos = (firePoint != null) ? firePoint.position : transform.position;
        GameObject bulletObj = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        EnemyBullet bulletScript = bulletObj.GetComponent<EnemyBullet>();
        if (bulletScript != null) bulletScript.Setup(direction);
    }

    protected override void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();

            if (player != null)
            {
                // --- LOGIC ZOMBIE MODE CHO BOSS ---
                if (player.isZombieMode)
                {
                    // 1. Gây 10 sát thương cho Boss
                    TakeDamage(10);

                    // 2. Có thể thêm hiệu ứng đẩy lùi Player nhẹ (tuỳ chọn)
                    // Hoặc tạo hiệu ứng va chạm tại đây
                    Debug.Log("Zombie Player hit Fector! Dealt 10 DMG.");

                    // 3. Quan trọng: Return ngay để Player KHÔNG chết và Boss KHÔNG bị hủy
                    return;
                }

                // --- LOGIC BẤT TỬ ---
                if (player.IsInvincible())
                {
                    return;
                }
            }

            // --- LOGIC THƯỜNG ---
            // Nếu không phải Zombie và không bất tử -> Giết Player
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PlayerDied();
            }

            // QUAN TRỌNG: KHÔNG gọi Destroy(gameObject) ở đây như Enemy thường.
        }
    }

    public override void TakeDamage(int dmg)
    {
        // Vẫn chặn nếu chưa vào trận hoặc đã chết
        if (currentState == FectorState.Waiting || currentState == FectorState.Intro || currentState == FectorState.Dead) return;

        currentHealth -= dmg;
        if (whiteSprite != null)
        {
            StartCoroutine(FlashRoutine());
        }
        // --- CHECK SUMMON THEO MỐC MÁU ---
        float healthPercent = (float)currentHealth / maxHealth;

        // Duyệt ngược danh sách để có thể xóa phần tử an toàn
        for (int i = summonThresholds.Count - 1; i >= 0; i--)
        {
            if (healthPercent <= summonThresholds[i])
            {
                SummonMinions();

                summonThresholds.RemoveAt(i);
            }
        }

        if (currentHealth <= 0) Die();
    }

    public override void Die(bool dropLoot = true)
    {
        if (currentState == FectorState.Dead) return;
        currentState = FectorState.Dead;

        // 1. Tắt va chạm và di chuyển ngay lập tức
        if (GetComponent<Collider2D>() != null) GetComponent<Collider2D>().enabled = false;
        if (rb != null) rb.linearVelocity = Vector2.zero; // Hoặc rb.velocity

        // 2. Thay vì Destroy ngay, hãy chạy chuỗi hành động chết (Hiệu ứng nổ)
        StartCoroutine(DeathSequence());

        Debug.Log("Fector Defeated! (Death Sequence Started)");
    }

    IEnumerator DeathSequence()
    {
        // A. Ẩn hình ảnh Boss
        if (sr != null) sr.enabled = false;

        for (int i = 0; i < 3; i++)
        {
            StartCoroutine(SpawnMultipleSmokes(transform.position));
            AudioClip randomClip = deathSounds[Random.Range(0, deathSounds.Length)];
            audioSource.PlayOneShot(randomClip);
            yield return new WaitForSeconds(0.3f);
        }

        // C. Chờ thêm một chút để chắc chắn FectorBossManager đã kịp gọi HandleVictory
        yield return new WaitForSeconds(1f);

        // D. Giờ mới hủy Object thật sự
        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3((mapBoundsMin.x + mapBoundsMax.x) / 2, (mapBoundsMin.y + mapBoundsMax.y) / 2, 0);
        Vector3 size = new Vector3(mapBoundsMax.x - mapBoundsMin.x, mapBoundsMax.y - mapBoundsMin.y, 0);
        Gizmos.DrawWireCube(center, size);
    }
}