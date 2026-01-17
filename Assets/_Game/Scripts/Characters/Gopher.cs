using UnityEngine;
using UnityEngine.Audio;

public class Gopher : MonoBehaviour
{
    public static Gopher Instance;

    #region Configuration & Settings
    [Header("Settings")]
    public float moveSpeed = 2f;
    public AudioSource audioSource;
    public AudioClip gopherSFX;
    public AudioClip footstepClip;
    public float stepRate = 0.5f;

    public float destroyBound = 7.5f;
    #endregion

    #region Runtime Variables
    private float nextStepTime = 0f;
    private Vector3 moveDirection;
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (audioSource != null & gopherSFX != null)
        {
            audioSource.PlayOneShot(gopherSFX);
        }
        CalculateDirectionAndOrientation();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        transform.position += moveDirection * moveSpeed * Time.deltaTime;
        HandleFootsteps();
        CheckBoundary();
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
    #endregion

    #region Core Logic
    void HandleFootsteps()
    {
        if (audioSource == null || footstepClip == null) return;

        if (Time.time >= nextStepTime)
        {
            audioSource.PlayOneShot(footstepClip);
            nextStepTime = Time.time + stepRate;
        }
    }

    void CalculateDirectionAndOrientation()
    {
        float x = transform.position.x;
        float y = transform.position.y;

        if (Mathf.Abs(y) > Mathf.Abs(x))
        {
            if (y > 0)
            {
                moveDirection = Vector3.down;
            }
            else
            {
                moveDirection = Vector3.up;
            }
        }
        else
        {
            if (x > 0)
            {
                moveDirection = Vector3.left;
            }
            else
            {
                moveDirection = Vector3.right;
            }
        }
    }

    void CheckBoundary()
    {
        if (moveDirection == Vector3.right)
        {
            if (transform.position.x > destroyBound) Destroy(gameObject);
        }
        else if (moveDirection == Vector3.left)
        {
            if (transform.position.x < -destroyBound) Destroy(gameObject);
        }
        else if (moveDirection == Vector3.up)
        {
            if (transform.position.y > destroyBound) Destroy(gameObject);
        }
        else if (moveDirection == Vector3.down)
        {
            if (transform.position.y < -destroyBound) Destroy(gameObject);
        }
    }
    #endregion
}