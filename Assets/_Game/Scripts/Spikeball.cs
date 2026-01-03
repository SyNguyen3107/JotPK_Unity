using UnityEngine;
using System.Collections;

public class Spikeball : Enemy
{
    [Header("Spikeball Settings")]
    public float deployDuration = 1f; // Thời gian biến hình
    public int deployedMaxHealth = 7; // Máu sau khi biến hình

    // Các biến sprite để code tự đổi (nếu bạn không muốn dùng Animator phức tạp)
    // Tuy nhiên dùng Animator cho Deploy 3 sprite sẽ mượt hơn. 
    // Ở đây tôi sẽ viết code hỗ trợ Animator vì nó quản lý State tốt nhất.

    private Vector3 targetPosition;
    private bool isDeployed = false;
    private bool isMoving = true;

    // Override lại Start để setup máu ban đầu là 2
    protected override void Start()
    {
        // Setup chỉ số ban đầu (Dạng 1)
        maxHealth = 2;
        base.Start(); // Gọi Start của cha để setup Audio, Player transform...

        // Tìm vị trí ngẫu nhiên để đi tới
        FindTargetPosition();
    }

    // Override Update để KHÔNG đuổi theo Player mà đi đến vị trí chỉ định
    void Update()
    {
        // Nếu đang di chuyển đến điểm deploy
        if (isMoving && !isDeployed)
        {
            // Di chuyển đến targetPosition
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            // Xử lý Flip sprite (nếu cần)
            if (targetPosition.x < transform.position.x) sr.flipX = true;
            else sr.flipX = false;

            // Kiểm tra xem đã đến nơi chưa (sai số nhỏ 0.1f)
            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                StartCoroutine(DeployRoutine());
            }
        }

        // Nếu đã deploy xong (Dạng 2), Spikeball đứng yên -> Không làm gì trong Update
    }

    // Ghi đè FixedUpdate để tắt logic vật lý mặc định nếu có
    protected override void FixedUpdate()
    {
        // Để trống để không bị ảnh hưởng bởi logic đẩy nhau của Enemy gốc nếu không cần thiết
    }

    void FindTargetPosition()
    {
        // Logic tìm vị trí ngẫu nhiên (tránh tường)
        // Ta dùng lại logic tìm vị trí an toàn giống Player (bạn có thể copy hàm FindSafePosition sang 1 class Utils để dùng chung, nhưng giờ ta viết lại cho nhanh)

        int maxAttempts = 20;
        bool found = false;

        // Giả sử mapBounds lấy từ GameManager hoặc hardcode tạm thời theo kích thước map của bạn
        // Tốt nhất là lấy reference từ GameManager nếu có, hoặc dùng giá trị ước lượng
        float minX = -6f, maxX = 6f, minY = -5f, maxY = 5f;

        for (int i = 0; i < maxAttempts; i++)
        {
            float rX = Random.Range(minX, maxX);
            float rY = Random.Range(minY, maxY);
            Vector2 potentialPos = new Vector2(rX, rY);

            // Check xem có đụng tường/gate không (Layer Obstacle)
            // Giả sử Obstacle ở layer "Default" hoặc "Blocking", bạn cần set layer mask phù hợp
            if (!Physics2D.OverlapCircle(potentialPos, 0.5f, LayerMask.GetMask("Default", "Obstacle")))
            {
                targetPosition = potentialPos;
                found = true;
                break;
            }
        }

        if (!found) targetPosition = transform.position; // Không tìm được thì đứng yên tại chỗ
    }

    IEnumerator DeployRoutine()
    {
        isMoving = false; // Dừng di chuyển

        // 1. Chơi Animation Deploying
        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.SetTrigger("Deploy");

        // 2. Chờ 1 giây
        yield return new WaitForSeconds(deployDuration);

        // 3. Chuyển sang Dạng 2 (Deployed)
        isDeployed = true;

        // --- LOGIC CỘNG DỒN MÁU ---
        // Máu cũ (ví dụ max 2, bị đánh 1 -> còn 1)
        // Máu mới max 7. Chênh lệch là 5.
        // Máu hiện tại = 1 + 5 = 6. -> Logic đúng: vẫn mất 1 máu.
        int healthDifference = deployedMaxHealth - maxHealth;
        maxHealth = deployedMaxHealth;
        currentHealth += healthDifference;

        // Cập nhật Animator sang trạng thái Idle (Stationary)
        if (anim != null) anim.SetBool("IsDeployed", true);

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero; // Dừng mọi quán tính cũ (nếu có)
            rb.bodyType = RigidbodyType2D.Static;
        }
    }

    // Hàm đặc biệt để giảm máu về 1 (Gọi từ GameManager)
    public void Weaken()
    {
        if (currentHealth > 1)
        {
            currentHealth = 1;
            // Có thể thêm effect hiển thị bị yếu đi
            Debug.Log("Spikeball weakened!");
        }
    }

    // Xử lý va chạm đặc biệt với Ogre (sau này)
    protected void OnCollisionEnter2D(Collision2D collision)
    {
        base.OnCollisionEnter2D(collision); // Giữ logic va chạm với Player

        // Logic tương lai: Nếu va chạm với Ogre
        /*
        if (collision.gameObject.CompareTag("Enemy") && collision.gameObject.GetComponent<Ogre>() != null)
        {
            Die(false); // Chết không rơi đồ
        }
        */
    }
}