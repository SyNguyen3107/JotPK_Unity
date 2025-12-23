using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Base Stats")]
    public float moveSpeed = 2f;
    public int maxHealth = 3;
    private int currentHealth;
    public int damageToPlayer = 1; // Sát thương gây ra khi chạm vào Player

    [Header("Death Settings")]
    public GameObject deathEffectPrefab; // Kéo Prefab EnemyDeathFX vào đây
    public AudioClip[] deathSounds;

    [Header("Visuals")]
    public Sprite whiteSprite; // Kéo Sprite trắng tương ứng của quái vào đây
    private SpriteRenderer sr;
    private Animator animator;
    private bool isFlashing = false;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip hitSound;

    [Header("Base References")]
    public Rigidbody2D rb;

    [Header("Drop")]
    public GameObject itemPrefab;

    // Protected để các class con (Orc, Imp) có thể truy cập được
    protected Transform playerTransform;
    protected bool isDead = false;

    // Virtual: Cho phép class con ghi đè nếu muốn (ví dụ Boss lúc Start sẽ gầm lên)
    protected virtual void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        currentHealth = maxHealth;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        // Tự động tìm AudioSource nếu quên gán
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
    }

    // Virtual: Để class con tự quyết định cách di chuyển (FixedUpdate)
    // Ở đây ta để trống, vì Orc đi thẳng, Imp đi lượn sóng, Boss đứng yên...
    void Update()
    {
        if (playerTransform != null)
        {
            transform.position = Vector2.MoveTowards(transform.position, playerTransform.position, moveSpeed * Time.deltaTime);
            if (playerTransform.position.x < transform.position.x) sr.flipX = true;
            else sr.flipX = false;
        }
    }
    protected virtual void FixedUpdate()
    {
        // Mặc định không làm gì cả
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        currentHealth -= damage;


        // 1. Kích hoạt Flash Trắng (Nếu có sprite trắng)
        if (whiteSprite != null)
        {
            StartCoroutine(FlashRoutine());
        }

        // 2. Kiểm tra chết
        if (currentHealth > 0)
        {
            // Chỉ phát tiếng Hit nếu chưa chết
            if (audioSource != null && hitSound != null)
            {
                audioSource.PlayOneShot(hitSound);
            }
        }
        else
        {
            // Nếu hết máu -> Chết
            Die();
        }
    }

    public virtual void Die(bool shouldDrop = true)
    {
        isDead = true;
        // Drop Item Logic
        if (shouldDrop)
        {
            float roll = Random.Range(0f, 100f);
            if (GameManager.Instance != null && roll <= GameManager.Instance.dropChance)
            {
                PowerUpData dropData = GameManager.Instance.GetRandomDrop();
                if (dropData != null && itemPrefab != null)
                {
                    GameObject itemObj = Instantiate(itemPrefab, transform.position, Quaternion.identity);
                    ItemPickup pickup = itemObj.GetComponent<ItemPickup>();
                    if (pickup != null) pickup.Setup(dropData);
                }
            }
        }

        // 1. Tạo hiệu ứng chết
        if (deathEffectPrefab != null)
        {
            GameObject fx = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);

            // 2. Phát âm thanh ngẫu nhiên (thông qua object DeathFX vừa tạo)
            if (deathSounds.Length > 0)
            {
                // Chọn ngẫu nhiên 1 clip từ mảng
                AudioClip randomClip = deathSounds[Random.Range(0, deathSounds.Length)];

                // Gọi hàm phát nhạc của DeathFX
                DeathEffect deathScript = fx.GetComponent<DeathEffect>();
                if (deathScript != null)
                {
                    deathScript.PlaySound(randomClip);
                }
            }
        }

        // 3. Xóa Enemy
        Destroy(gameObject);
    }

    // Logic va chạm chung: Cứ chạm Player là Player chết
    protected void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Kiểm tra xem Player có đang bất tử không
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();

            if (player != null && player.IsInvincible())
            {
                // Nếu Player bất tử -> Không làm gì cả (đi xuyên qua hoặc bị đẩy ra)
                return;
            }

            // Nếu không bất tử -> Giết như thường
            if (GameManager.Instance != null) GameManager.Instance.PlayerDied();
            Destroy(gameObject);
        }
    }
    IEnumerator FlashRoutine()
    {
        // Nếu đang flash rồi thì không làm gì (tránh lỗi chồng chéo)
        if (isFlashing) yield break;
        isFlashing = true;

        // A. Lưu trạng thái cũ
        bool wasAnimEnabled = false;
        Sprite originalSprite = sr.sprite; // Lưu sprite hiện tại (để trả lại nếu ko có animator)

        // B. Tắt Animator (quan trọng: nếu không tắt, Animator sẽ ghi đè sprite trắng ngay lập tức)
        if (animator != null)
        {
            wasAnimEnabled = animator.enabled;
            animator.enabled = false;
        }

        // C. Đổi sang Sprite Trắng
        sr.sprite = whiteSprite;

        // D. Chờ 0.1 giây
        yield return new WaitForSeconds(0.1f);

        // E. Khôi phục trạng thái
        if (animator != null)
        {
            animator.enabled = wasAnimEnabled; // Bật lại Animator -> Nó sẽ tự cập nhật sprite đúng
        }
        else
        {
            sr.sprite = originalSprite; // Trả lại hình cũ
        }

        isFlashing = false;
    }
}