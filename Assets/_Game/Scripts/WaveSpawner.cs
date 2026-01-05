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

    // LIST MỚI: Dùng để lưu trữ các loại quái đang sống khi Player chết
    private List<GameObject> enemiesToRespawn = new List<GameObject>();

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

    // --- LOGIC MỚI: KHI PLAYER CHẾT ---
    public void OnPlayerDied()
    {
        // 1. Tạm dừng tiến độ Wave
        isWavePaused = true;

        // 2. Dọn sạch danh sách chờ cũ (đề phòng)
        enemiesToRespawn.Clear();

        // 3. Tìm tất cả Enemy đang hoạt động
        GameObject[] activeEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemyObj in activeEnemies)
        {
            Enemy enemyScript = enemyObj.GetComponent<Enemy>();

            // Lưu Prefab gốc của quái vào danh sách để hồi sinh sau này
            if (enemyScript != null && enemyScript.sourcePrefab != null)
            {
                enemiesToRespawn.Add(enemyScript.sourcePrefab);
            }

            // Xóa quái khỏi màn hình
            Destroy(enemyObj);
        }

        // 4. Xóa Gopher (nếu có) - Gopher chết là mất luôn, không lưu lại
        if (Gopher.Instance != null)
        {
            Destroy(Gopher.Instance.gameObject);
        }
    }

    // --- LOGIC MỚI: KHI PLAYER HỒI SINH XONG ---
    public void OnPlayerRespawned()
    {
        StartCoroutine(RespawnSavedEnemies());
    }

    IEnumerator RespawnSavedEnemies()
    {
        // Spawn lại toàn bộ quái đã lưu
        foreach (GameObject enemyPrefab in enemiesToRespawn)
        {
            SpawnEnemy(enemyPrefab);
            // Delay cực ngắn để tránh lag nếu spawn quá nhiều cùng lúc
            yield return new WaitForSeconds(0.1f);
        }

        // Xóa danh sách sau khi đã spawn xong
        enemiesToRespawn.Clear();

        // Tiếp tục chạy Wave
        isWavePaused = false;
    }

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
                // Khi Player chết, isWavePaused = true, vòng lặp này sẽ đứng yên đợi
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

    void SpawnGopher(GameObject gopherPrefab)
    {
        Vector3 spawnPos = GetRandomEdgePosition();
        Instantiate(gopherPrefab, spawnPos, Quaternion.identity);
    }

    // --- CẬP NHẬT: Gán sourcePrefab cho Enemy ---
    void SpawnEnemy(GameObject enemyPrefab)
    {
        GameObject newEnemy = null; // Biến tạm để giữ object mới tạo

        if (spawnPoints.Length == 0) return;

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

        // GÁN SOURCE PREFAB: Để hệ thống biết con này là loại gì (Orc/Mummy...) khi Player chết
        if (newEnemy != null)
        {
            Enemy eScript = newEnemy.GetComponent<Enemy>();
            if (eScript != null)
            {
                eScript.sourcePrefab = enemyPrefab;
            }
        }
    }

    public Vector3 GetRandomEdgePosition()
    {
        int edge = Random.Range(0, 4);
        float x = 0, y = 0;
        float offset = 0.5f;

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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(mapSize.x * 2, mapSize.y * 2, 0));
    }
    public int GetPendingEnemyCount()
    {
        return enemiesToRespawn.Count;
    }
}