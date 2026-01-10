using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Base Stats")]
    public float moveSpeed = 2f;
    public int maxHealth = 3;
    public int currentHealth;
    public int damageToPlayer = 1;

    [Header("Death Settings")]
    public GameObject deathEffectPrefab;
    public AudioClip[] deathSounds;

    [Header("Visuals")]
    public Sprite whiteSprite;
    protected SpriteRenderer sr;
    protected Animator animator;
    private bool isFlashing = false;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip hitSound;

    [Header("Base References")]
    public Rigidbody2D rb;

    [Header("Drop")]
    public GameObject itemPrefab; // Prefab chung (cái hòm/túi) chứa item

    [Header("Stun Visuals")]
    public GameObject questionMarkObject;
    private float originalSpeed;

    [HideInInspector] public GameObject sourcePrefab;

    protected Transform playerTransform;
    protected bool isDead = false;

    protected virtual void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        currentHealth = maxHealth;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        originalSpeed = moveSpeed;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        if (GameManager.Instance != null && GameManager.Instance.isSmokeBombActive)
        {
            SetStunState(true);
        }
    }

    protected virtual void Update()
    {
        Transform currentTarget = playerTransform;

        if (Gopher.Instance != null && ShouldChaseGopher())
        {
            currentTarget = Gopher.Instance.transform;
        }

        if (currentTarget != null)
        {

            PlayerController playerScript = null;
            if (playerTransform != null) playerScript = playerTransform.GetComponent<PlayerController>();

            if (Gopher.Instance != null && ShouldChaseGopher())
            {
                if (moveSpeed > 0)
                {
                    transform.position = Vector2.MoveTowards(transform.position, currentTarget.position, moveSpeed * Time.deltaTime);
                }
            }
            // --- TRƯỜNG HỢP 2: TƯƠNG TÁC VỚI PLAYER ---
            else
            {
                bool isPlayerZombie = (playerScript != null && playerScript.isZombieMode);

                // Nếu Player là Zombie VÀ quái này biết sợ (ShouldFlee trả về true)
                if (isPlayerZombie && ShouldFlee())
                {
                    // Logic bỏ chạy (Flee)
                    Vector2 fleeDirection = (transform.position - playerTransform.position).normalized;
                    Vector2 targetPos = (Vector2)transform.position + fleeDirection * moveSpeed * Time.deltaTime;

                    // Giới hạn không cho chạy ra khỏi map
                    if (playerScript != null)
                    {
                        float clampX = Mathf.Clamp(targetPos.x, playerScript.mapBoundsMin.x + 0.5f, playerScript.mapBoundsMax.x - 0.5f);
                        float clampY = Mathf.Clamp(targetPos.y, playerScript.mapBoundsMin.y + 0.5f, playerScript.mapBoundsMax.y - 0.5f);
                        transform.position = new Vector2(clampX, clampY);
                    }

                    // Quay mặt ngược hướng Player khi bỏ chạy
                    if (transform.position.x > playerTransform.position.x) sr.flipX = false;
                    else sr.flipX = true;
                }
                // Nếu bình thường -> Đuổi theo Player
                else
                {
                    if (moveSpeed > 0)
                    {
                        transform.position = Vector2.MoveTowards(transform.position, playerTransform.position, moveSpeed * Time.deltaTime);

                        // Quay mặt về phía Player
                        if (playerTransform.position.x < transform.position.x) sr.flipX = true;
                        else sr.flipX = false;
                    }
                }
            }
        }
    }

    protected virtual void FixedUpdate()
    {
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        currentHealth -= damage;

        if (whiteSprite != null)
        {
            StartCoroutine(FlashRoutine());
        }

        if (currentHealth > 0)
        {
            if (audioSource != null && hitSound != null)
            {
                audioSource.PlayOneShot(hitSound);
            }
        }
        else
        {
            Die();
        }
    }

    // --- LOGIC MỚI: RƠI ĐỒ THEO TRỌNG SỐ & BẢO HIỂM ---
    public virtual void Die(bool shouldDrop = true)
    {
        isDead = true;

        if (shouldDrop && GameManager.Instance != null)
        {
            PowerUpData dropData = null;
            float roll = Random.Range(0f, 100f);
            if (roll <= GameManager.Instance.dropChance)
            {
                dropData = GameManager.Instance.GetDropItemLogic();
            }


            // 3. Spawn Item
            if (dropData != null && itemPrefab != null)
            {
                GameObject itemObj = Instantiate(itemPrefab, transform.position, Quaternion.identity);
                ItemPickup pickup = itemObj.GetComponent<ItemPickup>();
                if (pickup != null)
                {
                    pickup.Setup(dropData);
                }

                // Nếu rơi ra Coin -> Báo cáo lại cho Manager
                if (dropData.type == PowerUpType.Coin)
                {
                    GameManager.Instance.RegisterCoinSpawn();
                }
            }
        }

        // --- HẾT LOGIC RƠI ĐỒ ---

        if (deathEffectPrefab != null)
        {
            GameObject fx = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

            if (deathSounds.Length > 0)
            {
                AudioClip randomClip = deathSounds[Random.Range(0, deathSounds.Length)];
                DeathEffect deathScript = fx.GetComponent<DeathEffect>();
                if (deathScript != null)
                {
                    deathScript.PlaySound(randomClip);
                }
            }
        }

        Destroy(gameObject);
    }

    protected virtual bool ShouldFlee()
    {
        return true;
    }
    protected virtual bool ShouldChaseGopher()
    {
        return true;
    }
    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();

            if (player != null && player.isZombieMode)
            {
                player.KillEnemyOnContact();
                Die(true);
                return;
            }

            if (player != null && player.IsInvincible())
            {
                return;
            }
            if (GameManager.Instance != null) GameManager.Instance.PlayerDied();
            Destroy(gameObject);
        }
    }

    IEnumerator FlashRoutine()
    {
        if (isFlashing) yield break;
        isFlashing = true;

        bool wasAnimEnabled = false;
        Sprite originalSprite = sr.sprite;

        if (animator != null)
        {
            wasAnimEnabled = animator.enabled;
            animator.enabled = false;
        }

        sr.sprite = whiteSprite;

        yield return new WaitForSeconds(0.1f);

        if (animator != null)
        {
            animator.enabled = wasAnimEnabled;
        }
        else
        {
            sr.sprite = originalSprite;
        }

        isFlashing = false;
    }

    public void SetStunState(bool isStunned)
    {
        if (isStunned)
        {
            moveSpeed = 0;
            if (animator != null) animator.speed = 0;
            if (questionMarkObject != null) questionMarkObject.SetActive(true);
        }
        else
        {
            moveSpeed = originalSpeed;
            if (animator != null) animator.speed = 1;
            if (questionMarkObject != null) questionMarkObject.SetActive(false);
        }
    }
}