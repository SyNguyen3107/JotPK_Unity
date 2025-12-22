using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Base Stats")]
    public float moveSpeed = 2f;
    public int health = 3;
    public int damageToPlayer = 1; // Sát thương gây ra khi chạm vào Player

    [Header("Death Settings")]
    public GameObject deathEffectPrefab; // Kéo Prefab EnemyDeathFX vào đây
    public AudioClip[] deathSounds;

    [Header("Base References")]
    public Rigidbody2D rb;

    [Header("Drop")]
    public GameObject itemPrefab;

    // Protected để các class con (Orc, Imp) có thể truy cập được
    protected Transform playerTarget;
    protected bool isDead = false;

    // Virtual: Cho phép class con ghi đè nếu muốn (ví dụ Boss lúc Start sẽ gầm lên)
    protected virtual void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
        }
    }

    // Virtual: Để class con tự quyết định cách di chuyển (FixedUpdate)
    // Ở đây ta để trống, vì Orc đi thẳng, Imp đi lượn sóng, Boss đứng yên...
    protected virtual void FixedUpdate()
    {
        // Mặc định không làm gì cả
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        health -= damage;
        if (health <= 0)
        {
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
}