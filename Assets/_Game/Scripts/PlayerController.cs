using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    [Header("State References")]
    public GameObject idleStateObject;   // Kéo GameObject 'IdleState' vào đây
    public GameObject activeStateObject; // Kéo GameObject 'ActiveState' vào đây

    [Header("Active Model References")]
    public Rigidbody2D rb;
    public Animator legsAnimator;       // Kéo GameObject 'Legs' (trong ActiveState)
    public SpriteRenderer bodyRenderer; // Kéo GameObject 'Body' (trong ActiveState)

    [Header("Body Sprites (Gun Mode)")]
    public Sprite bodyUp;    // player_up
    public Sprite bodyDown;  // player_down
    public Sprite bodyLeft;  // player_left
    public Sprite bodyRight; // player_right

    private Vector2 moveInput;
    private Vector2 shootInput;

    void Update()
    {
        // 1. Nhận Input
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(moveX, moveY).normalized;

        // Input bắn (Mũi tên)
        float shootX = 0f; float shootY = 0f;
        if (Input.GetKey(KeyCode.UpArrow)) shootY = 1f;
        else if (Input.GetKey(KeyCode.DownArrow)) shootY = -1f;
        if (Input.GetKey(KeyCode.LeftArrow)) shootX = -1f;
        else if (Input.GetKey(KeyCode.RightArrow)) shootX = 1f;
        shootInput = new Vector2(shootX, shootY);

        // 2. Xác định trạng thái (State Switching)
        bool isMoving = moveInput.magnitude > 0.1f;
        bool isShooting = shootInput != Vector2.zero;

        if (!isMoving && !isShooting)
        {
            // --- TRẠNG THÁI IDLE ---
            SetVisualState(isIdle: true);
        }
        else
        {
            // --- TRẠNG THÁI ACTIVE (Di chuyển hoặc Bắn) ---
            SetVisualState(isIdle: false);
            UpdateActiveModel(isMoving, isShooting);
        }
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }

    // Hàm chuyển đổi bật tắt GameObject
    void SetVisualState(bool isIdle)
    {
        if (idleStateObject.activeSelf != isIdle)
        {
            idleStateObject.SetActive(isIdle);
            activeStateObject.SetActive(!isIdle);
        }
    }

    // Logic xử lý khi đang Active
    void UpdateActiveModel(bool isMoving, bool isShooting)
    {
        // A. Xử lý Chân (Legs)
        // Chỉ chạy animation đi bộ nếu thực sự di chuyển
        legsAnimator.SetBool("IsMoving", isMoving);

        // B. Xử lý Thân trên (Body)
        Vector2 facingDir = Vector2.zero;

        // ƯU TIÊN 1: Hướng bắn (Mũi tên)
        if (isShooting)
        {
            facingDir = shootInput;
        }
        // ƯU TIÊN 2: Nếu không bắn mà đang đi, quay người theo hướng đi (cho tự nhiên)
        else if (isMoving)
        {
            facingDir = moveInput;
        }
        // Mặc định: Giữ nguyên hoặc về Down (ở đây ta giữ logic set sprite)

        UpdateBodySprite(facingDir);
    }

    void UpdateBodySprite(Vector2 dir)
    {
        // Chỉ update nếu có hướng cụ thể (tránh trường hợp vector zero làm mất sprite)
        if (dir.y > 0) bodyRenderer.sprite = bodyUp;
        else if (dir.y < 0) bodyRenderer.sprite = bodyDown;
        else if (dir.x < 0) bodyRenderer.sprite = bodyLeft;
        else if (dir.x > 0) bodyRenderer.sprite = bodyRight;
    }
}