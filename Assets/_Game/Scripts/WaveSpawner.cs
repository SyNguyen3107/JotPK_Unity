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
    public Vector2 mapSize = new Vector2(7.5f, 7.5f);

    private float searchCountdown = 1f;
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
            while (isWavePaused) yield return null;

            Debug.Log("--- WAVE " + (i + 1) + " ---");
            yield return StartCoroutine(SpawnWave(waves[i]));

            // Sử dụng IsWaveCleared để bỏ qua Spikeball còn sót lại
            yield return new WaitUntil(() => IsWaveCleared() && !isWavePaused);

            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }

    bool IsWaveCleared()
    {
        searchCountdown -= Time.deltaTime;
        if (searchCountdown > 0f) return false;
        searchCountdown = 1f;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        if (enemies.Length == 0) return true;

        foreach (GameObject enemy in enemies)
        {
            // Nếu còn bất kỳ quái nào KHÔNG phải Spikeball thì chưa xong wave
            if (enemy.GetComponent<Spikeball>() == null)
            {
                return false;
            }
        }

        return true;
    }

    IEnumerator SpawnWave(WaveData waveData)
    {
        foreach (var group in waveData.enemyGroups)
        {
            for (int i = 0; i < group.count; i++)
            {
                while (isWavePaused) yield return null;

                SpawnEnemy(group.enemyPrefab);
                yield return new WaitForSeconds(group.rate);
            }
        }
    }

    void SpawnEnemy(GameObject enemyPrefab)
    {
        if (spawnPoints.Length == 0) return;

        if (enemyPrefab.GetComponent<Butterfly>() != null)
        {
            Vector3 spawnPos = GetRandomEdgePosition();
            Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            int randomIndex = Random.Range(0, spawnPoints.Length);
            Transform spawnPoint = spawnPoints[randomIndex];
            Vector3 randomOffset = Random.insideUnitCircle * 0.5f;

            Instantiate(enemyPrefab, spawnPoint.position + randomOffset, Quaternion.identity);
        }
    }

    public Vector3 GetRandomEdgePosition()
    {
        int edge = Random.Range(0, 4);
        float x = 0, y = 0;
        float offset = 1f;

        switch (edge)
        {
            case 0: // Top
                x = Random.Range(-mapSize.x, mapSize.x);
                y = mapSize.y + offset;
                break;
            case 1: // Bottom
                x = Random.Range(-mapSize.x, mapSize.x);
                y = -mapSize.y - offset;
                break;
            case 2: // Left
                x = -mapSize.x - offset;
                y = Random.Range(-mapSize.y, mapSize.y);
                break;
            case 3: // Right
                x = mapSize.x + offset;
                y = Random.Range(-mapSize.y, mapSize.y);
                break;
        }

        return new Vector3(x, y, 0);
    }

    // Vẽ khung map trong Scene để dễ căn chỉnh (Optional)
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(mapSize.x * 2, mapSize.y * 2, 0));
    }
}