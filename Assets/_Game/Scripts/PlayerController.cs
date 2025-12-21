using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    [Header("Combat Settings")]
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float bulletOffset = 0.5f;
    public float fireDelay = 0.4f;
    private float nextFireTime = 0f;

    [Header("Visual References")]
    public GameObject idleStateObject;
    public GameObject activeStateObject;

    // 3 Thành phần hiển thị quan trọng
    public SpriteRenderer bodyRenderer;
    public SpriteRenderer legsRenderer;
    public SpriteRenderer idleRenderer;

    [Header("Active Model References")]
    public Rigidbody2D rb;
    public Animator legsAnimator; // Chứa Animation Die
    public SpriteRenderer bodySpriteDisplay; // (Thường là bodyRenderer ở trên, gán trùng cũng đc)

    // Sprites hướng (Giữ nguyên của bạn)
    public Sprite bodyUp;
    public Sprite bodyDown;
    public Sprite bodyLeft;
    public Sprite bodyRight;

    [Header("Audio")]
    public AudioSource gunAudioSource;
    public AudioSource footstepAudioSource;
    public AudioClip shootClip;
    public AudioClip footstepClip;
    public float stepDelay = 0.3f;
    private float nextStepTime = 0f;

    // State Variables
    private Vector2 moveInput;
    private bool isDead = false;
    private bool isInvincible = false;

    void Update()
    {
        // --- 1. BLOCK INPUT IF DEAD ---
        if (isDead)
        {
            moveInput = Vector2.zero;
            return;
        }

        // --- 2. MOVEMENT ---
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(moveX, moveY).normalized;

        // --- 3. SHOOTING (Logic bắn chéo đã sửa) ---
        float shootX = 0f;
        float shootY = 0f;

        if (Input.GetKey(KeyCode.UpArrow)) shootY = 1f;
        else if (Input.GetKey(KeyCode.DownArrow)) shootY = -1f;

        if (Input.GetKey(KeyCode.LeftArrow)) shootX = -1f;
        else if (Input.GetKey(KeyCode.RightArrow)) shootX = 1f;

        bool attemptedToShoot = (shootX != 0 || shootY != 0);

        if (attemptedToShoot && Time.time >= nextFireTime)
        {
            Vector2 inputShootDir = new Vector2(shootX, shootY);
            Shoot(inputShootDir.normalized);
            nextFireTime = Time.time + fireDelay;

            UpdateActiveModel(moveInput.magnitude > 0.1f, inputShootDir); // Update hướng theo đạn
        }

        // --- 4. VISUAL STATES ---
        bool isMoving = moveInput.magnitude > 0.1f;

        if (!isMoving && !attemptedToShoot)
        {
            SetVisualState(isIdle: true);
        }
        else
        {
            SetVisualState(isIdle: false);

            if (!attemptedToShoot)
                UpdateActiveModel(isMoving, moveInput); // Update hướng theo di chuyển

            HandleFootsteps(isMoving);
        }
    }

    void FixedUpdate()
    {
        // Block Physics logic if dead
        if (isDead) return;

        if (rb != null)
            rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
    }

    // --- DEATH LOGIC ---
    public void TriggerDeathAnimation()
    {
        isDead = true;
        moveInput = Vector2.zero;

        // Dừng vật lý tuyệt đối
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero; // Unity 6 (Dùng rb.velocity = Vector2.zero nếu Unity cũ)
            rb.simulated = false; // Tắt va chạm để không bị đẩy
        }

        if (footstepAudioSource) footstepAudioSource.Stop();

        // Xử lý hiển thị: Bắt buộc hiện ActiveModel để Animator chạy
        if (activeStateObject != null) activeStateObject.SetActive(true);
        if (idleStateObject != null) idleStateObject.SetActive(false);

        // Ẩn thân trên (Body) đi, chỉ để Chân diễn animation
        if (bodyRenderer != null) bodyRenderer.enabled = false;

        // Trigger Animation
        if (legsAnimator != null) legsAnimator.SetTrigger("Die");
    }

    public void ResetState()
    {
        isDead = false;

        // Bật lại vật lý
        if (rb != null) rb.simulated = true;

        // Reset Animation & Body
        if (legsAnimator != null) legsAnimator.Play("Idle");
        if (bodyRenderer != null) bodyRenderer.enabled = true;

        ToggleVisibility(true);
    }

    // --- INVINCIBILITY LOGIC ---
    public bool IsInvincible() => isInvincible;

    public void TriggerRespawnInvincibility(float duration)
    {
        StartCoroutine(InvincibilityRoutine(duration));
    }

    IEnumerator InvincibilityRoutine(float duration)
    {
        isInvincible = true;
        float elapsed = 0f;
        float flashInterval = 0.1f;

        while (elapsed < duration)
        {
            ToggleVisibility(false);
            yield return new WaitForSeconds(flashInterval);
            ToggleVisibility(true);
            yield return new WaitForSeconds(flashInterval);
            elapsed += (flashInterval * 2);
        }
        ToggleVisibility(true);
        isInvincible = false;
    }

    void ToggleVisibility(bool isVisible)
    {
        if (bodyRenderer) bodyRenderer.enabled = isVisible;
        if (legsRenderer) legsRenderer.enabled = isVisible;
        if (idleRenderer) idleRenderer.enabled = isVisible;
    }

    // --- HELPER FUNCTIONS (Giữ nguyên logic cũ) ---
    void Shoot(Vector2 dir)
    {
        Vector3 spawnPos = transform.position + (Vector3)(dir * bulletOffset);
        if (firePoint != null) spawnPos = firePoint.position;
        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);

        // (Giả sử Bullet script của bạn có hàm Setup)
        var bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript) bulletScript.Setup(dir);

        if (gunAudioSource && shootClip) gunAudioSource.PlayOneShot(shootClip);
    }

    void HandleFootsteps(bool isMoving)
    {
        if (isMoving && Time.time >= nextStepTime)
        {
            if (footstepAudioSource && footstepClip) footstepAudioSource.PlayOneShot(footstepClip);
            nextStepTime = Time.time + stepDelay;
        }
    }

    void SetVisualState(bool isIdle)
    {
        if (idleStateObject) idleStateObject.SetActive(isIdle);
        if (activeStateObject) activeStateObject.SetActive(!isIdle);
    }

    void UpdateActiveModel(bool isMoving, Vector2 dir)
    {
        if (legsAnimator) legsAnimator.SetBool("IsMoving", isMoving);

        if (bodySpriteDisplay == null) return;
        if (dir.y > 0) bodySpriteDisplay.sprite = bodyUp;
        else if (dir.y < 0) bodySpriteDisplay.sprite = bodyDown;
        else if (dir.x < 0) bodySpriteDisplay.sprite = bodyLeft;
        else if (dir.x > 0) bodySpriteDisplay.sprite = bodyRight;
    }
}