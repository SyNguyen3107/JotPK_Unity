using UnityEngine;

public class BossLoot : MonoBehaviour
{
    public int livesBonus = 1;

    public Sprite lootSprite;
    void Start()
    {
        // Tự lấy sprite nếu chưa gán
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

            BossManager bm = FindObjectOfType<BossManager>();
            if (bm != null)
            {
                // --- TRUYỀN SPRITE SANG ---
                bm.OnLootCollected(lootSprite);
            }

            gameObject.SetActive(false);
        }
    }
}