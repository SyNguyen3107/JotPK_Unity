using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class FectorBossManager : BossManager
{
    #region Configuration & Settings
    [Header("References")]
    public FectorController fectorScript;

    [Header("Transition")]
    public Image fadeScreen;
    public float fadeDuration = 2f;
    #endregion

    #region Unity Lifecycle
    void Update()
    {
        if (isBossActive && fectorScript != null && UIManager.Instance != null)
        {
            UIManager.Instance.UpdateBossHealth(fectorScript.currentHealth, fectorScript.maxHealth);

            if (fectorScript.currentHealth <= 0) HandleVictory();
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.useOverrideRespawn = false;
        if (UIManager.Instance != null) UIManager.Instance.ToggleBossUI(false);
    }
    #endregion

    #region Core Logic
    public override void ActivateBossLevel()
    {
        base.ActivateBossLevel();

        if (fectorScript != null && UIManager.Instance != null)
        {
            UIManager.Instance.SetBossName(fectorScript.bossName);
        }
    }

    void HandleVictory()
    {
        if (victoryTriggered) return;
        victoryTriggered = true;
        isBossActive = false;

        if (GameManager.Instance != null && GameManager.Instance.musicSource != null)
        {
            GameManager.Instance.musicSource.Stop();
        }
        if (UIManager.Instance != null) UIManager.Instance.ToggleBossUI(false);

        if (lootPrefab != null)
        {
            Vector3 spawnPos = (fectorScript != null) ? fectorScript.transform.position : transform.position;
            if (lootSpawnPoint != null) spawnPos = lootSpawnPoint.position;
            Instantiate(lootPrefab, spawnPos, Quaternion.identity);
        }

        if (GameManager.Instance != null && GameManager.Instance.playerObject != null)
        {
            PlayerController pc = GameManager.Instance.playerObject.GetComponent<PlayerController>();
            if (pc != null) pc.SetMapBounds(new Vector2(-7.5f, -7.5f), new Vector2(7.5f, 7.5f));
        }
    }
    #endregion

    #region Event Handlers
    public override void OnLootCollected(Sprite itemSprite)
    {
        StartCoroutine(VictoryPose(itemSprite));
    }
    #endregion

    #region Coroutines
    IEnumerator VictoryPose(Sprite itemSprite)
    {
        GameObject player = GameManager.Instance.playerObject;
        PlayerController pc = null;
        if (player != null) pc = player.GetComponent<PlayerController>();

        if (pc != null)
        {
            pc.isInputEnabled = false;
            if (GameManager.Instance != null && GameManager.Instance.musicSource != null)
            {
                GameManager.Instance.musicSource.Stop();
            }
            pc.PlayVictoryPose(itemSprite);
        }
        yield return new WaitForSeconds(3f);

        if (fadeScreen != null)
        {
            fadeScreen.gameObject.SetActive(true);
            float t = 0;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                float alpha = t / fadeDuration;
                fadeScreen.color = new Color(0, 0, 0, alpha);
                yield return null;
            }
            fadeScreen.color = Color.black;
        }

        yield return new WaitForSeconds(0.5f);

        SceneManager.LoadScene("EndGameCutscene");
    }
    #endregion
}