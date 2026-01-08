using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 10f;
    public float lifeTime = 3f;
    public int damage = 1; // --- MỚI: Chỉ số sát thương (Mặc định là 1) ---
    public Rigidbody2D rb;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    public void Setup(Vector2 direction)
    {
        rb.linearVelocity = direction * speed;
        // Xoay đạn theo hướng bắn (nếu cần)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90);
    }

    void OnTriggerEnter2D(Collider2D hitInfo)
    {
        if (hitInfo.CompareTag("Wall"))
        {
            Destroy(gameObject);
            return;
        }

        if (hitInfo.CompareTag("Enemy"))
        {
            // Lấy component Enemy (lớp cha) để gây sát thương
            Enemy enemy = hitInfo.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage); // --- MỚI: Truyền damage của đạn vào ---
            }
            Destroy(gameObject);
        }
    }
}