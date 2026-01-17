using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class WaveSpawner : MonoBehaviour
{
    public static WaveSpawner Instance;

    #region Configuration & Settings
    [Header("Spawn Settings")]
    public Transform[] spawnPoints;
    public List<WaveData> waves;
    public float timeBetweenWaves = 3f;
    public Vector2 mapSize = new Vector2(7.5f, 7.5f);
    #endregion

    #region Runtime Variables
    private float searchCountdown = 1f;
    private bool isWavePaused = false;
    private Vector3 currentMapOffset = Vector3.zero;
    private List<GameObject> enemiesToRespawn = new List<GameObject>();
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        StartCoroutine(RunLevelLogic());
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(currentMapOffset, new Vector3(mapSize.x * 2, mapSize.y * 2, 0));
    }
    #endregion

    #region Level Management
    public void UpdateLevelData(Vector3 newCenter, List<WaveData> newWaves, Transform[] newSpawnPoints)
    {
        currentMapOffset = newCenter;
        waves = newWaves;
        spawnPoints = newSpawnPoints;
    }

    public void SetWavePaused(bool paused)
    {
        isWavePaused = paused;
    }

    public void StopSpawning()
    {
        StopAllCoroutines();
    }
    #endregion

    #region Player Death & Respawn Logic
    public void OnPlayerDied()
    {
        isWavePaused = true;
        enemiesToRespawn.Clear();

        GameObject[] activeEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemyObj in activeEnemies)
        {
            Enemy enemyScript = enemyObj.GetComponent<Enemy>();

            if (enemyScript != null && enemyScript.sourcePrefab != null)
            {
                enemiesToRespawn.Add(enemyScript.sourcePrefab);
            }

            Destroy(enemyObj);
        }

        if (Gopher.Instance != null)
        {
            Destroy(Gopher.Instance.gameObject);
        }
    }

    public void OnPlayerRespawned()
    {
        StartCoroutine(RespawnSavedEnemies());
    }

    IEnumerator RespawnSavedEnemies()
    {
        foreach (GameObject enemyPrefab in enemiesToRespawn)
        {
            SpawnEnemy(enemyPrefab);
            yield return new WaitForSeconds(0.1f);
        }
        enemiesToRespawn.Clear();
        isWavePaused = false;
    }

    public int GetPendingEnemyCount()
    {
        return enemiesToRespawn.Count;
    }
    #endregion

    #region Spawning Logic
    IEnumerator RunLevelLogic()
    {
        for (int i = 0; i < waves.Count; i++)
        {
            while (isWavePaused) yield return null;

            Debug.Log("--- WAVE " + (i + 1) + " ---");
            yield return StartCoroutine(SpawnWave(waves[i]));

            yield return new WaitUntil(() => IsWaveCleared() && !isWavePaused);

            yield return new WaitForSeconds(timeBetweenWaves);
        }
    }

    IEnumerator SpawnWave(WaveData waveData)
    {
        foreach (var group in waveData.enemyGroups)
        {
            for (int i = 0; i < group.count; i++)
            {
                while (isWavePaused) yield return null;

                if (group.enemyPrefab.GetComponent<Gopher>() != null)
                {
                    SpawnGopher(group.enemyPrefab);
                }
                else
                {
                    SpawnEnemy(group.enemyPrefab);
                }

                yield return new WaitForSeconds(group.rate);
            }
        }
    }

    public void StartNextLevelWaves()
    {
        StopAllCoroutines();
        StartCoroutine(RunLevelLogic());
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
            if (enemy.GetComponent<Spikeball>() == null)
                return false;
        }

        return true;
    }

    void SpawnGopher(GameObject gopherPrefab)
    {
        Vector3 spawnPos = GetRandomEdgePosition();
        Instantiate(gopherPrefab, spawnPos, Quaternion.identity);
    }

    void SpawnEnemy(GameObject enemyPrefab)
    {
        if (spawnPoints.Length == 0) return;

        GameObject newEnemy = null;

        if (enemyPrefab.GetComponent<Butterfly>() != null ||
            enemyPrefab.GetComponent<Imp>() != null)
        {
            Vector3 spawnPos = GetRandomEdgePosition();
            newEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            int randomIndex = Random.Range(0, spawnPoints.Length);
            Transform spawnPoint = spawnPoints[randomIndex];
            Vector3 randomOffset = Random.insideUnitCircle * 0.5f;

            newEnemy = Instantiate(enemyPrefab, spawnPoint.position + randomOffset, Quaternion.identity);
        }

        if (newEnemy != null)
        {
            Enemy eScript = newEnemy.GetComponent<Enemy>();
            if (eScript != null)
            {
                eScript.sourcePrefab = enemyPrefab;
            }
        }
    }
    #endregion

    #region Helpers
    public Vector3 GetRandomEdgePosition()
    {
        int edge = Random.Range(0, 4);
        float x = 0, y = 0;
        float offset = 1.0f;

        switch (edge)
        {
            case 0:
                x = Random.Range(-mapSize.x, mapSize.x);
                y = mapSize.y + offset;
                break;
            case 1:
                x = Random.Range(-mapSize.x, mapSize.x);
                y = -mapSize.y - offset;
                break;
            case 2:
                x = -mapSize.x - offset;
                y = Random.Range(-mapSize.y, mapSize.y);
                break;
            case 3:
                x = mapSize.x + offset;
                y = Random.Range(-mapSize.y, mapSize.y);
                break;
        }

        return new Vector3(x, y, 0) + currentMapOffset;
    }
    #endregion
}