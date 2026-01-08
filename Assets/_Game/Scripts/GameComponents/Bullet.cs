using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("Bullet Stats")]
    public float speed = 10f;
    public float lifeTime = 3f;
    public int damage = 1; // Sát thương mặc định
    public Rigidbody2D rb;

    void Start()
    {
        // Tự hủy sau một khoảng thời gian
        Destroy(gameObject, lifeTime);
    }

    // --- CẬP NHẬT: Thêm tham số newDamage ---
    public void Setup(Vector2 direction, int newDamage)
    {
        // 1. Nạp damage từ Player vào viên đạn này
        this.damage = newDamage;

        // 2. Logic di chuyển (Giữ nguyên)
        // Lưu ý: rb.linearVelocity là cú pháp Unity 6 / 2023.3+. 
        // Nếu dùng bản cũ hơn thì đổi thành rb.velocity
        rb.linearVelocity = direction * speed;

        // 3. Logic xoay đạn (Giữ nguyên)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90);
    }

    void OnTriggerEnter2D(Collider2D hitInfo)
    {
        // Va vào tường -> Mất đạn
        if (hitInfo.CompareTag("Wall") || hitInfo.CompareTag("Obstacle")) // Thêm Obstacle cho chắc
        {
            Destroy(gameObject);
            return;
        }

        // Va vào quái -> Trừ máu
        if (hitInfo.CompareTag("Enemy"))
        {
            Enemy enemy = hitInfo.GetComponent<Enemy>();
            if (enemy != null)
            {
                // Gọi hàm TakeDamage với chỉ số damage đã được nạp ở Setup
                enemy.TakeDamage(damage);
            }
            Destroy(gameObject);
        }
    }
}