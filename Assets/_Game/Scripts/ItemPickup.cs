using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public PowerUpData itemData;
    private SpriteRenderer sr;
    private float lifeTime = 10f; // Item tự biến mất sau 10s nếu không ai nhặt

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (itemData != null && sr != null)
        {
            sr.sprite = itemData.icon;
        }

        // Tự hủy sau một thời gian để tránh rác game
        Destroy(gameObject, lifeTime);
    }

    // Hàm này để Enemy gọi khi khởi tạo
    public void Setup(PowerUpData data)
    {
        itemData = data;
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.sprite = itemData.icon;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerController player = collision.GetComponent<PlayerController>();
            if (player != null)
            {
                player.PickUpItem(itemData);
                Destroy(gameObject); // Biến mất sau khi nhặt
            }
        }
    }
}