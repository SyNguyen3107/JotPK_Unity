using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Bullet Stats")]
    public float speed = 10f;
    public float lifeTime = 3f;
    public int damage = 1; // Sát thương hiện tại của viên đạn
    public Rigidbody2D rb;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    public void Setup(Vector2 direction, int newDamage)
    {
        this.damage = newDamage;

        // Logic di chuyển
        rb.linearVelocity = direction * speed;

        // Logic xoay đạn
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90);
    }

    void OnTriggerEnter2D(Collider2D hitInfo)
    {
        // 1. Va vào tường -> Luôn hủy đạn
        if (hitInfo.CompareTag("Wall") || hitInfo.CompareTag("Obstacle"))
        {
            Destroy(gameObject);
            return;
        }

        // 2. Va vào quái hoặc Boss
        if (hitInfo.CompareTag("Enemy") || hitInfo.CompareTag("Boss"))
        {
            Enemy enemy = hitInfo.GetComponent<Enemy>();

            if (enemy != null)
            {
                // A. Lấy máu hiện tại của kẻ địch (Lưu lại trước khi gây dmg)
                // Lưu ý: Đảm bảo biến 'currentHealth' bên script Enemy là public
                int enemyHealthBeforeHit = enemy.currentHealth;

                // B. Gây sát thương (Kẻ địch sẽ chết nếu damage >= máu)
                enemy.TakeDamage(damage);

                // C. Tính toán damage còn dư
                // Ví dụ: Đạn 10 dmg, Quái 4 máu -> Đạn còn 6 dmg
                // Ví dụ: Đạn 2 dmg, Quái 5 máu -> Đạn còn -3 dmg
                damage -= enemyHealthBeforeHit;

                // D. Kiểm tra số phận viên đạn
                if (damage <= 0)
                {
                    // Đạn đã hết lực -> Hủy
                    Destroy(gameObject);
                }
                else
                {
                    // Đạn vẫn còn lực (damage > 0) -> Bay tiếp xuyên qua
                    // Không gọi Destroy(gameObject) ở đây
                }
            }
            else
            {
                // Trường hợp object có Tag Enemy nhưng quên gắn script -> Hủy cho an toàn
                Destroy(gameObject);
            }
        }
    }
}