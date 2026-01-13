using System.Collections;
using UnityEngine;

public class CowboyBossManager : BossManager
{
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


    //// --- HÀM KÍCH HOẠT CHÍNH ---
    //public void ActivateBossLevel()
    //{
    //    Debug.Log("BOSS LEVEL ACTIVATED!");
    //    // 1. Setup UI Boss & Tắt Timer
    //    if (GameManager.Instance != null)
    //    {
    //        GameManager.Instance.SetTimerRunning(false);
    //        if (playerRespawnPoint != null)
    //            GameManager.Instance.overrideRespawnPosition = playerRespawnPoint.position;
    //    }

    //    if (UIManager.Instance != null)
    //    {
    //        UIManager.Instance.ToggleBossUI(true);
    //        UIManager.Instance.ToggleHUD(true);
    //    }
    //    if (cowboyBossScript != null)
    //    {
    //        cowboyBossScript.SetBossDialog(true);
    //        // Bật lại nếu chưa bật.
    //    }
    //    // 2. Phát nhạc Boss
    //    if (GameManager.Instance != null && bossMusic != null)
    //    {
    //        GameManager.Instance.musicSource.Stop();
    //        GameManager.Instance.musicSource.clip = bossMusic;
    //        GameManager.Instance.musicSource.Play();
    //    }

    //    // 3. Giới hạn vùng di chuyển
    //    if (playerZoneLimit != null)
    //    {
    //        Vector2 zoneMin = playerZoneLimit.bounds.min;
    //        Vector2 zoneMax = playerZoneLimit.bounds.max;

    //        if (GameManager.Instance != null && GameManager.Instance.playerObject != null)
    //        {
    //            PlayerController pc = GameManager.Instance.playerObject.GetComponent<PlayerController>();
    //            if (pc != null) pc.SetMapBounds(zoneMin, zoneMax);
    //        }
    //    }

    //    // 4. Kích hoạt Boss Script
    //    if (cowboyBossScript != null)
    //    {
    //        isBossActive = true;
    //        cowboyBossScript.StartBossFight();
    //    }
    //}

    void Update()
    {
        if (isBossActive && cowboyBossScript != null && UIManager.Instance != null)
        {
            UIManager.Instance.UpdateBossHealth(cowboyBossScript.currentHealth, cowboyBossScript.maxHealth);
            if (cowboyBossScript.currentHealth <= 0) HandleBossVictory();
        }
    }

    protected override void HandleBossVictory()
    {
        base.HandleBossVictory();

        // Xử lý Cầu / Sông
        if (riverBlockerObject != null) riverBlockerObject.SetActive(false);
        if (bridgeObject != null) bridgeObject.SetActive(true);
    }

    public override void OnLootCollected(Sprite itemSprite)
    {
        StartCoroutine(GopherCutsceneRoutine(itemSprite));
    }

    IEnumerator GopherCutsceneRoutine(Sprite itemSprite)
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
        // Màn hình đen (Tắt hết Grid)
        if (UIManager.Instance != null) UIManager.Instance.ToggleHUD(false);
        if (levelGridObject != null) levelGridObject.SetActive(false);
        if (bridgeObject != null) bridgeObject.SetActive(false);
        if (riverBlockerObject != null) riverBlockerObject.SetActive(false);

        yield return new WaitForSeconds(0.5f);

        // Setup Audio cho Gopher
        AudioSource audioSourceToUse = null;
        AudioClip clipToUse = null;
        if (pc != null)
        {
            audioSourceToUse = pc.footstepAudioSource;
            clipToUse = pc.footstepClip;
        }
        float gopherStepRate = 0.25f;
        float nextStepTime = 0f;

        // Spawn Gopher
        GameObject gophers = null;
        if (gopherSquadPrefab != null && gopherSpawnPoint != null)
        {
            gophers = Instantiate(gopherSquadPrefab, gopherSpawnPoint.position, Quaternion.identity);
        }

        if (gophers != null && player != null)
        {
            float speed = 6f;

            // Gopher chạy đến Player
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

            // Khiêng Player
            if (pc != null) pc.SetPhysicsForCutscene(false);
            player.transform.SetParent(gophers.transform);
            player.transform.localPosition = Vector3.zero;

            yield return new WaitForSeconds(0.5f);

            // Gopher chạy đi
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

        // Trả Player về
        if (player != null)
        {
            player.transform.SetParent(null);
            DontDestroyOnLoad(player);
            if (pc != null) pc.SetPhysicsForCutscene(true);
        }

        // Chuyển màn
        if (GameManager.Instance != null)
        {
            AudioSource gopherFoostep = gophers != null ? gophers.GetComponent<AudioSource>() : null;
            if (gopherFoostep != null) gopherFoostep.Stop();
            if (gophers != null) Destroy(gophers);
            GameManager.Instance.StartLevelTransition();
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.overrideRespawnPosition = null;
        }
        if (UIManager.Instance != null) UIManager.Instance.ToggleBossUI(false);
    }
}