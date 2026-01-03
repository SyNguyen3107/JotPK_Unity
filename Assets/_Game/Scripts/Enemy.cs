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

    [Header("Stun Visuals")]
    public GameObject questionMarkObject; // Kéo GameObject chứa Text "?" vào đây
    private float originalSpeed;

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
        originalSpeed = moveSpeed;
        // Tự động tìm AudioSource nếu quên gán
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

    // Virtual: Để class con tự quyết định cách di chuyển (FixedUpdate)
    // Ở đây ta để trống, vì Orc đi thẳng, Imp đi lượn sóng, Boss đứng yên...
    void Update()
    {
        if (playerTransform != null)
        {
            PlayerController playerScript = playerTransform.GetComponent<PlayerController>();
            bool isPlayerZombie = (playerScript != null && playerScript.isZombieMode);
            if (isPlayerZombie)
            {
                // --- LOGIC BỎ CHẠY (FLEE) ---

                // 1. Tính hướng TỪ Player ĐẾN Enemy (Hướng chạy trốn)
                Vector2 fleeDirection = (transform.position - playerTransform.position).normalized;

                // 2. Tính vị trí đích muốn đến
                Vector2 targetPos = (Vector2)transform.position + fleeDirection * moveSpeed * Time.deltaTime;

                // 3. KẸP VỊ TRÍ (CLAMP) TRONG BẢN ĐỒ
                // Enemy cần truy cập mapBounds của Player để biết tường ở đâu
                if (playerScript != null)
                {
                    // Lấy biên map từ Player (cộng trừ 1 chút để không đứng sát sạt tường)
                    float clampX = Mathf.Clamp(targetPos.x, playerScript.mapBoundsMin.x + 0.5f, playerScript.mapBoundsMax.x - 0.5f);
                    float clampY = Mathf.Clamp(targetPos.y, playerScript.mapBoundsMin.y + 0.5f, playerScript.mapBoundsMax.y - 0.5f);

                    // Cập nhật vị trí mới đã được giới hạn
                    transform.position = new Vector2(clampX, clampY);
                }

                // Visual: Lật mặt ngược lại hướng chạy (để trông như đang ngoái lại nhìn hoặc đơn giản là quay đầu chạy)
                // Nếu chạy sang phải (x > player.x) -> flipX = false (mặt hướng phải)
                if (transform.position.x > playerTransform.position.x) sr.flipX = false;
                else sr.flipX = true;
            }
            else
            {
                // --- LOGIC BÌNH THƯỜNG (CHASE) ---
                // Chỉ di chuyển nếu KHÔNG BỊ STUN (Code cũ của bạn)
                if (moveSpeed > 0)
                {
                    transform.position = Vector2.MoveTowards(transform.position, playerTransform.position, moveSpeed * Time.deltaTime);
                    if (playerTransform.position.x < transform.position.x) sr.flipX = true;
                    else sr.flipX = false;
                }
            }
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

            if (player != null && player.isZombieMode)
            {
                player.KillEnemyOnContact(); // Gọi hàm bên player nếu cần FX
                Die(true); // true = Vẫn rơi đồ như thường
                return; // Kết thúc hàm, không gây dame cho player
            }

            // CASE 2: Player đang bất tử (Invincible) -> Bỏ qua
            if (player != null && player.IsInvincible())
            {
                return;
            }

            // CASE 3: Player thường -> Player chết
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
    public void SetStunState(bool isStunned)
    {
        if (isStunned)
        {
            moveSpeed = 0; // Đứng im
            if (animator != null) animator.speed = 0; // Dừng animation (đóng băng hình ảnh)
            if (questionMarkObject != null) questionMarkObject.SetActive(true); // Hiện dấu hỏi
        }
        else
        {
            moveSpeed = originalSpeed; // Trả lại tốc độ cũ (nhớ là Enemy phải có biến originalSpeed lưu ở Start)
            if (animator != null) animator.speed = 1; // Animation chạy lại
            if (questionMarkObject != null) questionMarkObject.SetActive(false); // Ẩn dấu hỏi
        }
    }
}