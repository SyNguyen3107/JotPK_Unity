using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class WaveSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public Transform[] spawnPoints;
    public List<WaveData> waves;
    public float timeBetweenWaves = 3f;

    // Biến tham chiếu để GameManager có thể gọi
    public static WaveSpawner Instance;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        StartCoroutine(RunLevelLogic());
    }

    IEnumerator RunLevelLogic()
    {
        // ... (Giữ nguyên logic lặp qua các waves)
        for (int i = 0; i < waves.Count; i++)
        {
            // ... Logic spawn ...
            yield return StartCoroutine(SpawnWave(waves[i]));

            // ... Logic chờ quái chết ...
            yield return new WaitUntil(() => GameObject.FindGameObjectsWithTag("Enemy").Length == 0);

            yield return new WaitForSeconds(timeBetweenWaves);
        }
        // Lưu ý: Logic "Victory" ở đây sẽ không dùng nữa, 
        // vì GameManager sẽ quyết định thắng thua dựa trên thời gian.
    }

    IEnumerator SpawnWave(WaveData waveData)
    {
        foreach (var group in waveData.enemyGroups)
        {
            for (int i = 0; i < group.count; i++)
            {
                SpawnEnemy(group.enemyPrefab);
                yield return new WaitForSeconds(group.rate);
            }
        }
    }

    void SpawnEnemy(GameObject enemyPrefab)
    {
        int randomIndex = Random.Range(0, spawnPoints.Length);
        Transform spawnPoint = spawnPoints[randomIndex];
        Vector3 randomOffset = Random.insideUnitCircle * 0.5f;
        Instantiate(enemyPrefab, spawnPoint.position + randomOffset, Quaternion.identity);
    }

    // --- HÀM MỚI: DỪNG SPAWN NGAY LẬP TỨC ---
    public void StopSpawning()
    {
        // Dừng tất cả các Coroutine đang chạy (RunLevelLogic, SpawnWave...)
        StopAllCoroutines();
        Debug.Log("WaveSpawner: Đã dừng spawn quái do hết giờ!");
    }
}