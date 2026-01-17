using UnityEngine;

public class Butterfly : Enemy
{
    protected override void Start()
    {
        maxHealth = 1;
        moveSpeed = 3f;

        base.Start();
    }
}