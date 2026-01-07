using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class WaveSpawner : MonoBehaviour
{
    public static WaveSpawner Instance;

    #region --- SETTINGS ---
    [Header("Spawn Settings")]
    public Transform[] spawnPoints;         // Các điểm spawn (Gate)
    public List<WaveData> waves;            // Danh sách các đợt quái
    public float timeBetweenWaves = 3f;     // Thời gian nghỉ giữa các đợt
    public Vector2 mapSize = new Vector2(7.5f, 7.5f); // Kích thước map gốc
    #endregion

    #region --- STATE VARIABLES ---
    private float searchCountdown = 1f;
    private bool isWavePaused = false;
    private Vector3 currentMapOffset = Vector3.zero; // Tâm của map hiện tại (thay đổi khi qua màn)

    // List lưu trữ quái để hồi sinh khi Player chết
    private List<GameObject> enemiesToRespawn = new List<GameObject>();
    #endregion

    #region --- UNITY EVENTS ---
    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        StartCoroutine(RunLevelLogic());
    }

    // Vẽ khung map trong Editor để dễ căn chỉnh
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        // Vẽ khung dựa trên tâm map hiện tại
        Gizmos.DrawWireCube(currentMapOffset, new Vector3(mapSize.x * 2, mapSize.y * 2, 0));
    }
    #endregion

    #region --- LEVEL MANAGEMENT ---
    // Hàm này được GameManager gọi khi chuyển sang map mới
    public void UpdateLevelData(Vector3 newCenter, List<WaveData> newWaves, Transform[] newSpawnPoints)
    {
        // 1. Cập nhật tâm map (để tính toán spawn rìa map)
        currentMapOffset = newCenter;

        // 2. Cập nhật danh sách quái cho màn mới
        waves = newWaves;

        // 3. Cập nhật điểm spawn (gate) của map mới
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

    #region --- PLAYER DEATH & RESPAWN LOGIC ---
    public void OnPlayerDied()
    {
        isWavePaused = true;
        enemiesToRespawn.Clear();

        // Tìm tất cả Enemy đang hoạt động
        GameObject[] activeEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemyObj in activeEnemies)
        {
            Enemy enemyScript = enemyObj.GetComponent<Enemy>();

            // Lưu Prefab gốc để hồi sinh
            if (enemyScript != null && enemyScript.sourcePrefab != null)
            {
                enemiesToRespawn.Add(enemyScript.sourcePrefab);
            }

            Destroy(enemyObj);
        }

        // Xóa Gopher (không lưu)
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

    #region --- SPAWNING LOGIC ---
    IEnumerator RunLevelLogic()
    {
        // Duyệt qua từng Wave trong danh sách
        for (int i = 0; i < waves.Count; i++)
        {
            while (isWavePaused) yield return null;

            Debug.Log("--- WAVE " + (i + 1) + " ---");
            yield return StartCoroutine(SpawnWave(waves[i]));

            // Chờ đến khi dọn sạch quái của Wave này
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
        StopAllCoroutines(); // Dừng mọi thứ cũ kỹ
        StartCoroutine(RunLevelLogic()); // Chạy lại logic từ đầu với danh sách waves mới
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
            // Nếu còn quái KHÔNG phải Spikeball -> Chưa xong
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

        // Quái bay (Butterfly, Imp) -> Spawn rìa map
        if (enemyPrefab.GetComponent<Butterfly>() != null ||
            enemyPrefab.GetComponent<Imp>() != null)
        {
            Vector3 spawnPos = GetRandomEdgePosition();
            newEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        }
        // Quái bộ -> Spawn tại cổng
        else
        {
            int randomIndex = Random.Range(0, spawnPoints.Length);
            Transform spawnPoint = spawnPoints[randomIndex];
            Vector3 randomOffset = Random.insideUnitCircle * 0.5f;

            newEnemy = Instantiate(enemyPrefab, spawnPoint.position + randomOffset, Quaternion.identity);
        }

        // Gán sourcePrefab để phục vụ logic Respawn
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

    #region --- HELPER METHODS ---
    public Vector3 GetRandomEdgePosition()
    {
        int edge = Random.Range(0, 4);
        float x = 0, y = 0;
        float offset = 1.0f; // Offset lớn hơn 1 chút để spawn hẳn bên ngoài

        // Tính toán tọa độ cục bộ (Local) dựa trên mapSize
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

        // Cộng thêm Offset của Map hiện tại để ra tọa độ Thế giới (World) chính xác
        return new Vector3(x, y, 0) + currentMapOffset;
    }
    #endregion
}