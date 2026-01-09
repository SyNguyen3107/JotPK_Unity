using System.Collections;
using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public PowerUpData itemData;
    private SpriteRenderer sr;

    [Header("Lifetime Settings")]
    private float lifeTime = 10f;        // Tổng thời gian tồn tại
    private float warningTime = 3f;      // Thời gian nhấp nháy cảnh báo
    private float flashSpeed = 0.2f;     // Tốc độ nhấp nháy (càng nhỏ càng nhanh)

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (itemData != null && sr != null)
        {
            sr.sprite = itemData.icon;
        }

        StartCoroutine(LifeCycleRoutine());
    }
    IEnumerator LifeCycleRoutine()
    {
        // 1. Giai đoạn chờ bình thường
        // (Tổng 10s - 3s cảnh báo = 7s đứng yên)
        float safeTime = Mathf.Max(0, lifeTime - warningTime);
        yield return new WaitForSeconds(safeTime);

        // 2. Giai đoạn nhấp nháy (3s cuối)
        float timer = 0f;
        while (timer < warningTime)
        {
            if (sr != null) sr.enabled = !sr.enabled; // Bật/Tắt hình ảnh
            yield return new WaitForSeconds(flashSpeed);
            timer += flashSpeed;
        }

        // 3. Hết giờ -> Tự hủy
        Destroy(gameObject);
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