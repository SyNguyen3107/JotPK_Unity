using UnityEngine;
using System.Collections;

public class Spikeball : Enemy
{
    #region Configuration & Settings
    [Header("Spikeball Settings")]
    public float deployDuration = 1f;
    public int deployedMaxHealth = 7;
    public Sprite deployedWhiteSprite;
    public float maxMoveTime = 3f;
    #endregion

    #region Runtime Variables
    private float moveTimer = 0f;
    private Vector3 targetPosition;
    public bool isDeployed = false;
    private bool isMoving = true;
    #endregion

    #region Unity Lifecycle
    protected override void Start()
    {
        maxHealth = 2;
        base.Start();

        FindTargetPosition();
    }

    void Update()
    {
        if (isMoving && !isDeployed)
        {
            moveTimer += Time.deltaTime;

            if (moveTimer >= maxMoveTime)
            {
                StartCoroutine(DeployRoutine());
                return;
            }

            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            if (targetPosition.x < transform.position.x) sr.flipX = true;
            else sr.flipX = false;

            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                StartCoroutine(DeployRoutine());
            }
        }
    }

    protected override void FixedUpdate()
    {
    }

    protected override void OnCollisionEnter2D(Collision2D collision)
    {
        base.OnCollisionEnter2D(collision);
    }
    #endregion

    #region Core Logic
    void FindTargetPosition()
    {
        int maxAttempts = 20;
        bool found = false;

        float minX = -6f, maxX = 6f, minY = -5f, maxY = 5f;

        for (int i = 0; i < maxAttempts; i++)
        {
            float rX = Random.Range(minX, maxX);
            float rY = Random.Range(minY, maxY);
            Vector2 potentialPos = new Vector2(rX, rY);

            if (!Physics2D.OverlapCircle(potentialPos, 0.5f, LayerMask.GetMask("Default", "Obstacle")))
            {
                targetPosition = potentialPos;
                found = true;
                break;
            }
        }

        if (!found) targetPosition = transform.position;
    }

    public override void TakeDamage(int damage)
    {
        if (isDead) return;
        currentHealth -= damage;
        if (isDeployed)
        {
            if (deployedWhiteSprite != null)
            {
                StartCoroutine(DeployedFlashRoutine());
            }
        }
        else
        {
            if (whiteSprite != null)
            {
                StartCoroutine(FlashRoutine());
            }
        }

        if (currentHealth > 0)
        {
            if (audioSource != null && hitSound != null)
            {
                audioSource.PlayOneShot(hitSound);
            }
        }
        else
        {
            Die();
        }
    }

    public void Weaken()
    {
        if (currentHealth > 1)
        {
            currentHealth = 1;
        }
    }
    #endregion

    #region Coroutines
    IEnumerator DeployRoutine()
    {
        isMoving = false;

        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.SetTrigger("Deploy");

        yield return new WaitForSeconds(deployDuration);

        isDeployed = true;

        int healthDifference = deployedMaxHealth - maxHealth;
        maxHealth = deployedMaxHealth;
        currentHealth += healthDifference;

        if (anim != null) anim.SetBool("IsDeployed", true);

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Static;
        }
    }

    protected IEnumerator DeployedFlashRoutine()
    {
        if (isFlashing) yield break;
        isFlashing = true;

        bool wasAnimEnabled = false;
        Sprite originalSprite = sr.sprite;

        if (animator != null)
        {
            wasAnimEnabled = animator.enabled;
            animator.enabled = false;
        }

        sr.sprite = whiteSprite;

        yield return new WaitForSeconds(0.1f);

        if (animator != null)
        {
            animator.enabled = wasAnimEnabled;
        }
        else
        {
            sr.sprite = originalSprite;
        }

        isFlashing = false;
    }
    #endregion
}