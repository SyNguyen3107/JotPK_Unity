using UnityEngine;

public class VendorVisuals : MonoBehaviour
{
    #region Configuration & Settings
    [Header("Settings")]
    public float lookThreshold = 0.5f;
    #endregion

    #region References
    [Header("References")]
    public Animator animator;
    public SpriteRenderer sr;
    public Transform playerTransform;
    #endregion

    #region Unity Lifecycle
    void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();

        if (playerTransform == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }
    }

    void Update()
    {
        if (animator == null) return;

        bool isMoving = animator.GetBool("IsMoving");

        if (!isMoving)
        {
            HandleIdleLook();
        }
    }
    #endregion

    #region Core Logic
    void HandleIdleLook()
    {
        if (playerTransform == null) return;

        float xDiff = playerTransform.position.x - transform.position.x;
        float lookValue = 0f;

        if (xDiff > lookThreshold) lookValue = 1f;
        else if (xDiff < -lookThreshold) lookValue = -1f;
        else lookValue = 0f;

        animator.SetFloat("LookX", lookValue);
    }

    public void SetWalking(bool isWalking, float directionY)
    {
        if (animator != null)
        {
            animator.SetBool("IsMoving", isWalking);
            animator.SetFloat("MoveY", directionY);
        }
    }
    #endregion
}