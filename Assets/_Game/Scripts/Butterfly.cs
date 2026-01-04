using UnityEngine;

public class Butterfly : Enemy
{
    // Override Start để setup máu giấy (1 HP)
    protected override void Start()
    {
        maxHealth = 1; // Máu cực thấp
        moveSpeed = 3f; // Tốc độ vừa phải (hoặc nhanh tùy bạn chỉnh)

        base.Start();
    }
}