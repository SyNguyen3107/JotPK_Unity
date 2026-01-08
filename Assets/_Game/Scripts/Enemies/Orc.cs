using UnityEngine;

// Kế thừa từ class Enemy thay vì MonoBehaviour
public class Orc : Enemy
{
    // Ghi đè hàm FixedUpdate của cha để định nghĩa cách di chuyển riêng
    protected override void FixedUpdate()
    {
        // Gọi hàm của cha (nếu cha có logic gì đó). Ở đây cha rỗng nên ko cần base.FixedUpdate();

        if (isDead || playerTransform == null) return;

        // --- LOGIC DI CHUYỂN CỦA ORC: Đi thẳng về phía Player ---
        Vector2 direction = (playerTransform.position - transform.position).normalized;
        rb.MovePosition(rb.position + direction * moveSpeed * Time.fixedDeltaTime);
    }
}