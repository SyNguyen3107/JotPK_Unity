using UnityEngine;

public class Ogre : Enemy
{
    protected override void Start()
    {
        base.Start();
    }
    protected override void OnCollisionEnter2D(Collision2D collision)
    {
        base.OnCollisionEnter2D(collision);

        Spikeball spikeball = collision.gameObject.GetComponent<Spikeball>();
        if (spikeball != null && spikeball.isDeployed)
        {
            spikeball.Die(false);
        }
    }
}