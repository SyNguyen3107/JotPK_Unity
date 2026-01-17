using UnityEngine;

public class BossLoot : MonoBehaviour
{
    public int livesBonus = 1;

    public Sprite lootSprite;
    void Start()
    {
        if (lootSprite == null)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null) lootSprite = sr.sprite;
        }
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Boss Loot collected!");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddLife(livesBonus);
            }

            BossManager bm = FindFirstObjectByType<BossManager>();
            if (bm != null)
            {
                bm.OnLootCollected(lootSprite);
            }

            gameObject.SetActive(false);
        }
    }
}