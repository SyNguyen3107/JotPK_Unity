using UnityEngine;

public abstract class BossController : Enemy
{
    [Header("Boss Common Visuals")]
    public GameObject dialogObject;

    public abstract void StartBossFight();

    public virtual void SetBossDialog(bool isActive)
    {
        if (dialogObject != null)
        {
            dialogObject.SetActive(isActive);
        }
    }
}