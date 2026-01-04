using UnityEngine;

public class Imp : Enemy
{
    protected override void Start()
    {
        // Setup chỉ số
        maxHealth = 3;
        moveSpeed = 2f;

        base.Start();
    }

    // Ghi đè hàm này để trả về FALSE -> Không sợ Zombie
    protected override bool ShouldFlee()
    {
        return false;
    }

    // TODO: Sau này khi có Gopher, ta sẽ override thêm hàm ShouldTargetGopher() và trả về false
}