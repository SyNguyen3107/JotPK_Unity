using System.Collections;
using UnityEngine;

public class FectorBossManager : BossManager
{
    [Header("References")]
    public FectorController fectorScript;

    public override void ActivateBossLevel()
    {
        base.ActivateBossLevel();
    }

    void Update()
    {
        if (isBossActive && fectorScript != null && UIManager.Instance != null)
        {
            UIManager.Instance.UpdateBossHealth(fectorScript.currentHealth, fectorScript.maxHealth);

            // Kiểm tra máu boss để xử lý thắng
            if (fectorScript.currentHealth <= 0) HandleVictory();
        }
    }

    void HandleVictory()
    {
        if (victoryTriggered) return;
        victoryTriggered = true;
        isBossActive = false;

        Debug.Log("VICTORY AGAINST FECTOR!");

        // 1. Tắt nhạc & UI
        if (GameManager.Instance != null && GameManager.Instance.musicSource != null)
        {
            GameManager.Instance.musicSource.Stop();
        }
        if (UIManager.Instance != null) UIManager.Instance.ToggleBossUI(false);

        // 2. Rơi vật phẩm (Item cuối cùng)
        if (lootPrefab != null)
        {
            Vector3 spawnPos = (fectorScript != null) ? fectorScript.transform.position : transform.position;
            if (lootSpawnPoint != null) spawnPos = lootSpawnPoint.position;
            Instantiate(lootPrefab, spawnPos, Quaternion.identity);
        }

        // 3. Mở rộng map bounds (để player đi nhặt đồ thoải mái)
        if (GameManager.Instance != null && GameManager.Instance.playerObject != null)
        {
            PlayerController pc = GameManager.Instance.playerObject.GetComponent<PlayerController>();
            if (pc != null) pc.SetMapBounds(new Vector2(-1000, -1000), new Vector2(1000, 1000));
        }

        // CHƯA CÓ LOGIC CHUYỂN MÀN
    }

    public override void OnLootCollected(Sprite itemSprite)
    {
        StartCoroutine(VictoryPose(itemSprite));
    }
    IEnumerator VictoryPose(Sprite itemSprite)
    {
        Debug.Log("Starting Victory Sequence...");

        GameObject player = GameManager.Instance.playerObject;
        PlayerController pc = null;
        if (player != null) pc = player.GetComponent<PlayerController>();

        // Player tạo dáng
        if (pc != null)
        {
            pc.isInputEnabled = false;
            pc.PlayVictoryPose(itemSprite);
        }
        yield return new WaitForSeconds(2f);
        if (pc != null) pc.StopVictoryPose();
    }
    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.overrideRespawnPosition = null;
        if (UIManager.Instance != null) UIManager.Instance.ToggleBossUI(false);
    }
}