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
        
        bool isMoving = moveInput.magnitude > 0.1f;
        // --- 1. MOVEMENT INPUT ---
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(moveX, moveY).normalized;

        // --- 2. SHOOTING INPUT (ĐÃ SỬA) ---
        float shootX = 0f;
        float shootY = 0f;

        // Nhóm 1: Kiểm tra trục Dọc (Y)
        if (Input.GetKey(KeyCode.UpArrow))
        {
            shootY = 1f;
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            shootY = -1f;
        }

        // Nhóm 2: Kiểm tra trục Ngang (X) - Dùng IF mới, không dùng ELSE IF nối tiếp nhóm trên
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            shootX = -1f;
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            shootX = 1f;
        }

        // Kiểm tra xem có đang cố bắn không (Chỉ cần X hoặc Y khác 0)
        bool attemptedToShoot = (shootX != 0 || shootY != 0);
        bool isHoldingShootKey = attemptedToShoot;

        // Logic bắn đạn + Cooldown
        if (attemptedToShoot && Time.time >= nextFireTime)
        {
            // Tạo vector từ 2 trục đã thu thập được
            Vector2 inputShootDir = new Vector2(shootX, shootY);

            // normalized giúp vector chéo (1, 1) trở thành (0.7, 0.7) -> độ dài = 1
            Shoot(inputShootDir.normalized);

            nextFireTime = Time.time + fireDelay;

            // Lưu lại hướng bắn để chỉnh Sprite Body
            // (Lưu ý: inputShootDir ở đây có thể là chéo, nhưng hàm UpdateActiveModel sẽ tự ưu tiên Y)
            UpdateActiveModel(isMoving, inputShootDir);
        }

        // --- 3. STATE & VISUALS ---
        // Logic xác định đang giữ phím


        // Logic chuyển đổi Idle/Active
        if (!isMoving && !isHoldingShootKey)
        {
            SetVisualState(isIdle: true);
        }
        else
        {
            SetVisualState(isIdle: false);

            // Nếu KHÔNG bắn, nhưng ĐANG di chuyển -> Update hướng theo nút di chuyển
            // (Nếu đang bắn thì ở trên đã gọi UpdateActiveModel rồi)
            if (!attemptedToShoot)
            {
                // Bạn có thể dùng moveInput để xoay người khi đi bộ mà không bắn
                UpdateActiveModel(isMoving, moveInput);
            }

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