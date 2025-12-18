using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    [Header("Combat Settings")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletOffset = 0.5f;

    // --- MỚI: Tốc độ bắn ---
    [Tooltip("Thời gian nghỉ giữa các lần bắn (giây). Càng nhỏ bắn càng nhanh.")]
    public float fireDelay = 0.4f;
    private float nextFireTime = 0f; // Biến nội bộ để đếm giờ

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip shootClip;

    [Header("State References")]
    public GameObject idleStateObject;
    public GameObject activeStateObject;

    [Header("Active Model References")]
    public Rigidbody2D rb;
    public Animator legsAnimator;
    public SpriteRenderer bodyRenderer;

    [Header("Body Sprites")]
    public Sprite bodyUp;
    public Sprite bodyDown;
    public Sprite bodyLeft;
    public Sprite bodyRight;

    private Vector2 moveInput;

    void Update()
    {
        // --- 1. MOVEMENT INPUT ---
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(moveX, moveY).normalized;

        // --- 2. SHOOTING INPUT ---
        bool isShootingFrame = false; // Biến xác nhận ĐÃ BẮN ĐƯỢC trong frame này
        Vector2 shootDir = Vector2.zero;

        // Kiểm tra nút bấm
        Vector2 inputShootDir = Vector2.zero;
        bool attemptedToShoot = false;

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            inputShootDir = Vector2.up;
            attemptedToShoot = true;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            inputShootDir = Vector2.down;
            attemptedToShoot = true;
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            inputShootDir = Vector2.left;
            attemptedToShoot = true;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            inputShootDir = Vector2.right;
            attemptedToShoot = true;
        }

        // --- MỚI: Logic kiểm tra Cooldown ---
        // Chỉ bắn nếu CÓ nhấn nút VÀ thời gian hiện tại > thời gian được phép bắn tiếp theo
        if (attemptedToShoot && Time.time >= nextFireTime)
        {
            Shoot(inputShootDir);

            // Cập nhật thời gian được phép bắn lần tới
            nextFireTime = Time.time + fireDelay;

            // Cập nhật visual
            shootDir = inputShootDir;
            isShootingFrame = true;
        }

        // --- 3. STATE MACHINE UPDATE ---
        // Logic hiển thị vẫn giữ nguyên để đảm bảo mượt mà
        bool isHoldingShootKey = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow) ||
                                 Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow);

        bool isMoving = moveInput.magnitude > 0.1f;

        if (!isMoving && !isHoldingShootKey)
        {
            SetVisualState(isIdle: true);
        }
        else
        {
            SetVisualState(isIdle: false);

            Vector2 targetBodyDir = Vector2.zero;

            // Ưu tiên 1: Nếu vừa bắn ra đạn thành công -> Hướng theo đạn
            if (isShootingFrame)
            {
                targetBodyDir = shootDir;
            }
            // Ưu tiên 2: Nếu đang giữ nút bắn (dù chưa bắn được do đang cooldown) -> Hướng theo nút giữ
            // Để người chơi vẫn thấy nhân vật quay súng về phía địch khi đang spam nút
            else if (Input.GetKey(KeyCode.UpArrow)) targetBodyDir = Vector2.up;
            else if (Input.GetKey(KeyCode.DownArrow)) targetBodyDir = Vector2.down;
            else if (Input.GetKey(KeyCode.LeftArrow)) targetBodyDir = Vector2.left;
            else if (Input.GetKey(KeyCode.RightArrow)) targetBodyDir = Vector2.right;
            // Ưu tiên 3: Hướng theo chiều di chuyể

            UpdateActiveModel(isMoving, targetBodyDir);
        }
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }

    void Shoot(Vector2 dir)
    {
        Vector3 spawnPos = transform.position + (Vector3)(dir * bulletOffset);
        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        bullet.GetComponent<Bullet>().Setup(dir);

        if (audioSource && shootClip)
        {
            // Sử dụng PlayOneShot để âm thanh chồng lên nhau tự nhiên nếu bắn nhanh
            audioSource.PlayOneShot(shootClip);
        }
    }

    void SetVisualState(bool isIdle)
    {
        if (idleStateObject.activeSelf != isIdle)
        {
            idleStateObject.SetActive(isIdle);
            activeStateObject.SetActive(!isIdle);
        }
    }

    void UpdateActiveModel(bool isMoving, Vector2 dir)
    {
        legsAnimator.SetBool("IsMoving", isMoving);

        if (dir.y > 0) bodyRenderer.sprite = bodyUp;
        else if (dir.y < 0) bodyRenderer.sprite = bodyDown;
        else if (dir.x < 0) bodyRenderer.sprite = bodyLeft;
        else if (dir.x > 0) bodyRenderer.sprite = bodyRight;
    }
}