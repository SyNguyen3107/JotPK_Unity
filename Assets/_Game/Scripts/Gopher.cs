using UnityEngine;

public class Gopher : MonoBehaviour
{
    public static Gopher Instance;

    [Header("Settings")]
    public float moveSpeed = 2f;

    // Giới hạn bản đồ để biến mất. 
    // Lưu ý: Giá trị này nên BẰNG hoặc LỚN HƠN toạ độ spawn một chút.
    // Ví dụ map 7.5, spawn ở 8.5 -> destroyBound nên là 8.5 hoặc 9.0
    public float destroyBound = 7.5f;

    private Vector3 moveDirection;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        CalculateDirectionAndOrientation();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        // 1. DI CHUYỂN
        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        // 2. KIỂM TRA ĐIỀU KIỆN BIẾN MẤT (Logic toạ độ đích)
        CheckBoundary();
    }

    void CalculateDirectionAndOrientation()
    {
        float x = transform.position.x;
        float y = transform.position.y;

        // --- XÁC ĐỊNH HƯỚNG VÀ LẬT HÌNH ---

        // Trường hợp 1: Spawn ở cạnh TRÊN hoặc DƯỚI (Y lớn hơn X)
        if (Mathf.Abs(y) > Mathf.Abs(x))
        {
            if (y > 0)
            {
                // Spawn ở TRÊN -> Đi xuống
                moveDirection = Vector3.down;
            }
            else
            {
                // Spawn ở DƯỚI -> Đi lên
                moveDirection = Vector3.up;
            }
        }
        // Trường hợp 2: Spawn ở cạnh TRÁI hoặc PHẢI (X lớn hơn Y)
        else
        {
            if (x > 0)
            {
                // Spawn ở PHẢI -> Đi trái
                moveDirection = Vector3.left;
            }
            else
            {
                // Spawn ở TRÁI -> Đi phải
                moveDirection = Vector3.right;
            }
        }
    }

    void CheckBoundary()
    {
        // Logic: Chỉ kiểm tra vượt biên theo hướng đang di chuyển

        if (moveDirection == Vector3.right) // Đang đi sang Phải
        {
            // Chỉ hủy khi X vượt quá giới hạn DƯƠNG (bên phải)
            if (transform.position.x > destroyBound) Destroy(gameObject);
        }
        else if (moveDirection == Vector3.left) // Đang đi sang Trái
        {
            // Chỉ hủy khi X vượt quá giới hạn ÂM (bên trái)
            if (transform.position.x < -destroyBound) Destroy(gameObject);
        }
        else if (moveDirection == Vector3.up) // Đang đi lên
        {
            // Chỉ hủy khi Y vượt quá giới hạn DƯƠNG (bên trên)
            if (transform.position.y > destroyBound) Destroy(gameObject);
        }
        else if (moveDirection == Vector3.down) // Đang đi xuống
        {
            // Chỉ hủy khi Y vượt quá giới hạn ÂM (bên dưới)
            if (transform.position.y < -destroyBound) Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.Die(true);
            }
        }
    }
}