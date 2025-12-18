using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    [Header("Combat Settings")]
    public GameObject bulletPrefab;
    public Transform firePoint; // Có thể để trống, code sẽ dùng transform.position
    public float bulletOffset = 0.5f;
    [Tooltip("Thời gian nghỉ giữa các lần bắn (giây).")]
    public float fireDelay = 0.4f;
    private float nextFireTime = 0f;

    [Header("Audio Settings")]
    public AudioSource gunAudioSource;
    public AudioSource footstepAudioSource;
    [Space(5)]
    public AudioClip shootClip;
    public AudioClip footstepClip;
    public float stepDelay = 0.3f;
    [Range(0f, 1f)] public float footstepVolume = 0.5f;
    private float nextStepTime = 0f;

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
        bool isShootingFrame = false;
        Vector2 shootDir = Vector2.zero;
        Vector2 inputShootDir = Vector2.zero;
        bool attemptedToShoot = false;

        // Kiểm tra nhấn nút bắn (Single shot logic)
        if (Input.GetKeyDown(KeyCode.UpArrow)) { inputShootDir = Vector2.up; attemptedToShoot = true; }
        else if (Input.GetKeyDown(KeyCode.DownArrow)) { inputShootDir = Vector2.down; attemptedToShoot = true; }
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) { inputShootDir = Vector2.left; attemptedToShoot = true; }
        else if (Input.GetKeyDown(KeyCode.RightArrow)) { inputShootDir = Vector2.right; attemptedToShoot = true; }

        // Logic bắn đạn + Cooldown
        if (attemptedToShoot && Time.time >= nextFireTime)
        {
            Shoot(inputShootDir);
            nextFireTime = Time.time + fireDelay;
            shootDir = inputShootDir;
            isShootingFrame = true;
        }

        // --- 3. STATE & VISUALS ---
        bool isHoldingShootKey = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow) ||
                                 Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow);

        bool isMoving = moveInput.magnitude > 0.1f;

        // Logic chuyển đổi Idle/Active
        if (!isMoving && !isHoldingShootKey)
        {
            SetVisualState(isIdle: true);
        }
        else
        {
            SetVisualState(isIdle: false);

            // --- LOGIC HƯỚNG BODY (Đã chỉnh sửa) ---
            Vector2 targetBodyDir = Vector2.zero;

            // Ưu tiên 1: Nếu vừa bắn -> Hướng theo đạn
            if (isShootingFrame)
            {
                targetBodyDir = shootDir;
            }
            // Ưu tiên 2: Nếu đang giữ nút bắn (nhưng đang cooldown) -> Hướng theo nút giữ
            else if (Input.GetKey(KeyCode.UpArrow)) targetBodyDir = Vector2.up;
            else if (Input.GetKey(KeyCode.DownArrow)) targetBodyDir = Vector2.down;
            else if (Input.GetKey(KeyCode.LeftArrow)) targetBodyDir = Vector2.left;
            else if (Input.GetKey(KeyCode.RightArrow)) targetBodyDir = Vector2.right;

            // QUAN TRỌNG: Đã xóa dòng "else if (isMoving)..."
            // Nếu không bắn, targetBodyDir sẽ là Zero -> Hàm UpdateActiveModel sẽ giữ nguyên sprite cũ.

            UpdateActiveModel(isMoving, targetBodyDir);

            // Xử lý âm thanh bước chân
            HandleFootsteps(isMoving);
        }
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }

    void Shoot(Vector2 dir)
    {
        Vector3 spawnPos = transform.position + (Vector3)(dir * bulletOffset);
        // Nếu firePoint được gán thì dùng vị trí firePoint, không thì dùng logic cộng offset
        if (firePoint != null) spawnPos = firePoint.position;

        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
        bullet.GetComponent<Bullet>().Setup(dir);

        if (gunAudioSource && shootClip)
        {
            gunAudioSource.PlayOneShot(shootClip);
        }
    }

    void HandleFootsteps(bool isMoving)
    {
        if (isMoving && Time.time >= nextStepTime)
        {
            if (footstepAudioSource && footstepClip)
            {
                footstepAudioSource.PlayOneShot(footstepClip);
            }
            nextStepTime = Time.time + stepDelay;
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

        // Chỉ cập nhật Sprite nếu có hướng cụ thể (khác Zero)
        // Nếu dir == Zero (do chỉ di chuyển mà không bắn), sprite cũ sẽ được giữ nguyên.
        if (dir.y > 0) bodyRenderer.sprite = bodyUp;
        else if (dir.y < 0) bodyRenderer.sprite = bodyDown;
        else if (dir.x < 0) bodyRenderer.sprite = bodyLeft;
        else if (dir.x > 0) bodyRenderer.sprite = bodyRight;
    }
}