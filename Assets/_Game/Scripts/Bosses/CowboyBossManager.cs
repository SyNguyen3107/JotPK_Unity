using System.Collections;
using UnityEngine;

public class CowboyBossManager : BossManager
{
    #region Configuration & Settings
    [Header("References (Self-Contained in Prefab)")]
    public CowboyController cowboyBossScript;

    [Header("Victory Setup")]
    public GameObject bridgeObject;
    public GameObject riverBlockerObject;

    [Header("Environment")]
    public GameObject levelGridObject;

    [Header("Cutscene - Gopher Squad")]
    public GameObject gopherSquadPrefab;
    public Transform gopherSpawnPoint;
    #endregion

    #region Unity Lifecycle
    void Update()
    {
        if (isBossActive && cowboyBossScript != null && UIManager.Instance != null)
        {
            UIManager.Instance.UpdateBossHealth(cowboyBossScript.currentHealth, cowboyBossScript.maxHealth);
            if (cowboyBossScript.currentHealth <= 0) HandleBossVictory();
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null && playerRespawnPoint != null)
        {
            GameManager.Instance.useOverrideRespawn = true;
            GameManager.Instance.overrideRespawnPosition = playerRespawnPoint.position;
        }
        if (UIManager.Instance != null) UIManager.Instance.ToggleBossUI(false);
    }
    #endregion

    #region Core Logic
    public override void ActivateBossLevel()
    {
        base.ActivateBossLevel();

        if (cowboyBossScript != null && UIManager.Instance != null)
        {
            UIManager.Instance.SetBossName(cowboyBossScript.bossName);
        }
    }
    protected override void HandleBossVictory()
    {
        base.HandleBossVictory();

        if (riverBlockerObject != null) riverBlockerObject.SetActive(false);
        if (bridgeObject != null) bridgeObject.SetActive(true);
    }
    #endregion

    #region Event Handlers
    public override void OnLootCollected(Sprite itemSprite)
    {
        StartCoroutine(GopherCutsceneRoutine(itemSprite));
    }
    #endregion

    #region Coroutines
    IEnumerator GopherCutsceneRoutine(Sprite itemSprite)
    {
        GameObject player = GameManager.Instance.playerObject;
        PlayerController pc = null;
        if (player != null) pc = player.GetComponent<PlayerController>();

        if (pc != null)
        {
            pc.isInputEnabled = false;
            pc.PlayVictoryPose(itemSprite);
        }
        yield return new WaitForSeconds(2f);
        if (pc != null) pc.StopVictoryPose();

        if (UIManager.Instance != null) UIManager.Instance.ToggleHUD(false);
        if (levelGridObject != null) levelGridObject.SetActive(false);
        if (bridgeObject != null) bridgeObject.SetActive(false);
        if (riverBlockerObject != null) riverBlockerObject.SetActive(false);

        yield return new WaitForSeconds(0.5f);

        AudioSource audioSourceToUse = null;
        AudioClip clipToUse = null;
        if (pc != null)
        {
            audioSourceToUse = pc.footstepAudioSource;
            clipToUse = pc.footstepClip;
        }
        float gopherStepRate = 0.25f;
        float nextStepTime = 0f;

        GameObject gophers = null;
        if (gopherSquadPrefab != null && gopherSpawnPoint != null)
        {
            gophers = Instantiate(gopherSquadPrefab, gopherSpawnPoint.position, Quaternion.identity);
        }

        if (gophers != null && player != null)
        {
            float speed = 6f;

            while (Vector3.Distance(gophers.transform.position, player.transform.position) > 0.1f)
            {
                gophers.transform.position = Vector3.MoveTowards(gophers.transform.position, player.transform.position, speed * Time.deltaTime);
                if (Time.time >= nextStepTime && audioSourceToUse != null && clipToUse != null)
                {
                    audioSourceToUse.PlayOneShot(clipToUse);
                    nextStepTime = Time.time + gopherStepRate;
                }
                yield return null;
            }

            if (pc != null) pc.SetPhysicsForCutscene(false);
            player.transform.SetParent(gophers.transform);
            player.transform.localPosition = Vector3.zero;

            yield return new WaitForSeconds(0.5f);

            Vector3 exitPos = gophers.transform.position + Vector3.down * 15f;
            nextStepTime = 0f;

            while (Vector3.Distance(gophers.transform.position, exitPos) > 0.1f)
            {
                gophers.transform.position = Vector3.MoveTowards(gophers.transform.position, exitPos, speed * Time.deltaTime);
                if (Time.time >= nextStepTime && audioSourceToUse != null && clipToUse != null)
                {
                    audioSourceToUse.PlayOneShot(clipToUse);
                    nextStepTime = Time.time + gopherStepRate;
                }
                yield return null;
            }
        }
        yield return new WaitForSeconds(0.5f);

        if (player != null)
        {
            player.transform.SetParent(null);
            DontDestroyOnLoad(player);
            if (pc != null) pc.SetPhysicsForCutscene(true);
        }

        if (GameManager.Instance != null)
        {
            AudioSource gopherFoostep = gophers != null ? gophers.GetComponent<AudioSource>() : null;
            if (gopherFoostep != null) gopherFoostep.Stop();
            if (gophers != null) Destroy(gophers);
            GameManager.Instance.StartLevelTransition();
        }
    }
    #endregion
}