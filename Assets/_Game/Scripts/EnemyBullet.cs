using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    public float speed = 8f;
    // public int damage = 1; // Có thể thêm biến damage nếu muốn tùy chỉnh

    private Vector3 direction;

    public void Setup(Vector3 dir)
    {
        direction = dir.normalized;

        // Xoay đạn
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        // Tự hủy sau 5s
        Destroy(gameObject, 5f);
    }

    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }

    // --- LOGIC MỚI: GỌN GÀNG HƠN ---
    void OnTriggerEnter2D(Collider2D collision)
    {
        // Nhờ Matrix, hàm này CHỈ chạy khi va vào: Player hoặc Wall
        // Không bao giờ chạy khi va vào Enemy hay Boss nữa.

        if (collision.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
            {
                Debug.Log("Enemy bullet hit the player!");
                GameManager.Instance.PlayerDied();
            }
            
            Destroy(gameObject); // Trúng người -> Mất đạn
        }
        else
        {
            // Nếu va vào bất cứ thứ gì khác (Tường, Obstacle...) mà Matrix cho phép
            // -> Hủy đạn luôn
            Destroy(gameObject);
        }
    }
}