using System.Collections;
using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    #region Configuration & Settings
    public PowerUpData itemData;

    [Header("Lifetime Settings")]
    private float lifeTime = 10f;
    private float warningTime = 3f;
    private float flashSpeed = 0.2f;
    #endregion

    #region Runtime Variables
    private SpriteRenderer sr;
    #endregion

    #region Unity Lifecycle
    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (itemData != null && sr != null)
        {
            sr.sprite = itemData.icon;
        }

        StartCoroutine(LifeCycleRoutine());
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerController player = collision.GetComponent<PlayerController>();
            if (player != null)
            {
                player.PickUpItem(itemData);
                Destroy(gameObject);
            }
        }
    }
    #endregion

    #region Core Logic
    public void Setup(PowerUpData data)
    {
        itemData = data;
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.sprite = itemData.icon;
    }
    #endregion

    #region Coroutines
    IEnumerator LifeCycleRoutine()
    {
        float safeTime = Mathf.Max(0, lifeTime - warningTime);
        yield return new WaitForSeconds(safeTime);

        float timer = 0f;
        while (timer < warningTime)
        {
            if (sr != null) sr.enabled = !sr.enabled;
            yield return new WaitForSeconds(flashSpeed);
            timer += flashSpeed;
        }

        Destroy(gameObject);
    }
    #endregion
}