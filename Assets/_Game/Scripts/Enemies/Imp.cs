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
    protected override bool ShouldChaseGopher()
    {
        return false;
    }
}