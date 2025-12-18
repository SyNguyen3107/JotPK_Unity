using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 10f;
    public float lifeTime = 3f; // Đạn tự hủy sau 
    public Rigidbody2D rb;

    void Start()
    {
        // Tự hủy để tránh đầy bộ nhớ
        Destroy(gameObject, lifeTime);
    }

    // Hàm này sẽ được Player gọi khi bắn
    public void Setup(Vector2 direction)
    {
        // Gán vận tốc cho đạn
        rb.linearVelocity = direction * speed;

        // (Tùy chọn) Xoay viên đạn theo hướng bắn nếu đạn hình dài
        // transform.up = direction; 
    }

    // Xử lý va chạm (chúng ta sẽ viết kỹ hơn khi làm Enemy)
    void OnTriggerEnter2D(Collider2D hitInfo)
    {
        // Tạm thời: Nếu trúng Tường (Wall) hoặc Kẻ thù (Enemy) thì hủy đạn
        if (hitInfo.CompareTag("Wall") || hitInfo.CompareTag("Enemy"))
        {
            Destroy(gameObject);
        }
    }
}