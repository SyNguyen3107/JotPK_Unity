using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FectorBossManager : BossManager
{
    [Header("References")]
    public FectorController fectorScript;
    [Header("Transition")]
    public Image fadeScreen; // Kéo cái Panel đen vào đây
    public float fadeDuration = 2f;
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
        yield return new WaitForSeconds(3f);

        Debug.Log("Fading out...");
        if (fadeScreen != null)
        {
            fadeScreen.gameObject.SetActive(true); // Bật tấm màn lên
            float t = 0;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                float alpha = t / fadeDuration;
                // Chỉnh màu đen với độ trong suốt tăng dần từ 0 -> 1
                fadeScreen.color = new Color(0, 0, 0, alpha);
                yield return null;
            }
            // Đảm bảo đen hẳn
            fadeScreen.color = Color.black;
        }
        else
        {
            Debug.LogWarning("Chưa gán Fade Screen trong Inspector!");
        }

        // Chờ thêm 0.5s ở màn hình đen cho lắng đọng
        yield return new WaitForSeconds(0.5f);

        // 3. Chuyển cảnh
        SceneManager.LoadScene("EndGameCutscene");
    }
    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.useOverrideRespawn = false;
        if (UIManager.Instance != null) UIManager.Instance.ToggleBossUI(false);
    }
}