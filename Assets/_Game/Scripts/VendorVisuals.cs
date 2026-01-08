using UnityEngine;

public class VendorVisuals : MonoBehaviour
{
    [Header("References")]
    public Animator animator;       // Kéo Animator vào đây
    public SpriteRenderer sr;       // Vẫn cần để Flip nếu muốn (tùy animation)
    public Transform playerTransform;

    [Header("Settings")]
    public float lookThreshold = 0.5f; // Ngưỡng để quyết định quay đầu

    void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();

        // Tự tìm Player nếu chưa gán
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }
    }

    void Update()
    {
        // Nếu đang đi bộ, ta không cần tính toán hướng nhìn theo Player
        // Animator sẽ tự lo việc chạy animation đi bộ dựa trên parameter MoveY
        bool isMoving = animator.GetBool("IsMoving");

        if (!isMoving)
        {
            HandleIdleLook();
        }
    }

    // Logic: Tính toán vị trí Player để set thông số LookX cho Blend Tree
    void HandleIdleLook()
    {
        if (playerTransform == null) return;

        float xDiff = playerTransform.position.x - transform.position.x;
        float lookValue = 0f;

        if (xDiff > lookThreshold) lookValue = 1f;       // Phải
        else if (xDiff < -lookThreshold) lookValue = -1f; // Trái
        else lookValue = 0f;                             // Giữa

        // Cập nhật Animator
        animator.SetFloat("LookX", lookValue);
    }

    // --- CÁC HÀM ĐỂ CONTROLLER GỌI ---

    // Gọi hàm này khi NPC bắt đầu đi
    // directionY: -1 là đi xuống (vào), 1 là đi lên (về)
    public void SetWalking(bool isWalking, float directionY)
    {
        if (animator != null)
        {
            animator.SetBool("IsMoving", isWalking);
            animator.SetFloat("MoveY", directionY);
        }
    }
}