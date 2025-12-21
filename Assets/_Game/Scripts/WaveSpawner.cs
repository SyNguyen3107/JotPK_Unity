using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class WaveSpawner : MonoBehaviour
{
    public static WaveSpawner Instance;

    [Header("Spawn Settings")]
    public Transform[] spawnPoints;
    public List<WaveData> waves;
    public float timeBetweenWaves = 3f;

    private bool isWavePaused = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        StartCoroutine(RunLevelLogic());
    }

    public void SetWavePaused(bool paused)
    {
        isWavePaused = paused;
    }

    public void StopSpawning()
    {
        StopAllCoroutines();
    }

    IEnumerator RunLevelLogic()
    {
        for (int i = 0; i < waves.Count; i++)
        {
            // CHỜ NẾU ĐANG PAUSE
            while (isWavePaused) yield return null;

            Debug.Log("--- BẮT ĐẦU WAVE " + (i + 1) + " ---");
            yield return StartCoroutine(SpawnWave(waves[i]));

            // CHỜ DIỆT HẾT QUÁI (VÀ CHỜ NẾU ĐANG PAUSE)
            yield return new WaitUntil(() => GameObject.FindGameObjectsWithTag("Enemy").Length == 0 && !isWavePaused);

            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }

    IEnumerator SpawnWave(WaveData waveData)
    {
        foreach (var group in waveData.enemyGroups)
        {
            for (int i = 0; i < group.count; i++)
            {
                // CHỜ NẾU ĐANG PAUSE
                while (isWavePaused) yield return null;

                SpawnEnemy(group.enemyPrefab);
                yield return new WaitForSeconds(group.rate);
            }
        }
    }

    void SpawnEnemy(GameObject enemyPrefab)
    {
        if (spawnPoints.Length == 0) return;

        int randomIndex = Random.Range(0, spawnPoints.Length);
        Transform spawnPoint = spawnPoints[randomIndex];

        // Random offset để tránh quái bị trùng nhau
        Vector3 randomOffset = Random.insideUnitCircle * 0.5f;

        Instantiate(enemyPrefab, spawnPoint.position + randomOffset, Quaternion.identity);
    }
}