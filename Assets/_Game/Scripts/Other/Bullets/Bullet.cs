using UnityEngine;

public class Bullet : MonoBehaviour
{
    #region Configuration & Settings
    [Header("Bullet Stats")]
    public float speed = 10f;
    public float lifeTime = 3f;
    public int damage = 1;
    public Rigidbody2D rb;
    #endregion

    #region Unity Lifecycle
    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter2D(Collider2D hitInfo)
    {
        if (hitInfo.CompareTag("Wall"))
        {
            Destroy(gameObject);
            return;
        }

        if (hitInfo.CompareTag("Enemy") || hitInfo.CompareTag("Boss"))
        {
            Enemy enemy = hitInfo.GetComponent<Enemy>();

            if (enemy != null)
            {
                int enemyHealthBeforeHit = enemy.currentHealth;

                enemy.TakeDamage(damage);

                damage -= enemyHealthBeforeHit;

                if (damage <= 0)
                {
                    Destroy(gameObject);
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
    #endregion

    #region Core Logic
    public void Setup(Vector2 direction, int newDamage)
    {
        this.damage = newDamage;

        rb.linearVelocity = direction * speed;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle - 90);
    }
    #endregion
}