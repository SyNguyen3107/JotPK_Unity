using UnityEngine;

public class Ogre : Enemy
{
    // Override lại hàm Start để thiết lập chỉ số mặc định (nếu muốn)
    // Hoặc bạn có thể chỉnh trực tiếp trong Inspector
    protected override void Start()
    {
        base.Start(); // Bắt buộc gọi base để setup Audio, Player...
    }

    // Xử lý va chạm
    protected override void OnCollisionEnter2D(Collision2D collision)
    {
        base.OnCollisionEnter2D(collision);

        Spikeball spikeball = collision.gameObject.GetComponent<Spikeball>();

        // THÊM ĐIỀU KIỆN: && spikeball.isDeployed
        // Nghĩa là: Phải là Spikeball VÀ Spikeball đó đã biến thành tường rồi
        if (spikeball != null && spikeball.isDeployed)
        {
            spikeball.Die(false);
        }
    }
}