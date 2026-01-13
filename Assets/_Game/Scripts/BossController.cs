using UnityEngine;

public abstract class BossController : Enemy
{
    [Header("Boss Common Visuals")]
    public GameObject dialogObject;

    // Hàm abstract bắt buộc các Boss con phải tự định nghĩa logic intro của mình
    public abstract void StartBossFight();

    public virtual void SetBossDialog(bool isActive)
    {
        if (dialogObject != null)
        {
            dialogObject.SetActive(isActive);
        }
    }
}