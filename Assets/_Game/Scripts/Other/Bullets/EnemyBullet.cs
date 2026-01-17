using UnityEngine;

public class EnemyBullet : MonoBehaviour
{
    #region Configuration & Settings
    public float speed = 8f;
    #endregion

    #region Runtime Variables
    private Vector3 direction;
    #endregion

    #region Unity Lifecycle
    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerController player = collision.GetComponent<PlayerController>();

            if (player != null)
            {
                if (player.IsInvincible() || player.isZombieMode)
                {
                    Destroy(gameObject);
                    return;
                }
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.PlayerDied();
            }

            Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    #endregion

    #region Core Logic
    public void Setup(Vector3 dir)
    {
        direction = dir.normalized;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        Destroy(gameObject, 5f);
    }
    #endregion
}