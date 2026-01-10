using UnityEngine;

public class BossLoot : MonoBehaviour
{
    public int livesBonus = 1;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // 1. Cộng mạng
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddLife(livesBonus);
            }

            // 2. Gọi BossManager để bắt đầu Cutscene
            BossManager bm = FindObjectOfType<BossManager>();
            if (bm != null)
            {
                bm.OnLootCollected(); // Hàm này ta sẽ viết ngay sau đây
            }

            // 3. Ẩn vật phẩm đi (để tránh nhặt lại)
            gameObject.SetActive(false);
            // Destroy(gameObject, 2f); // Có thể destroy sau
        }
    }
}