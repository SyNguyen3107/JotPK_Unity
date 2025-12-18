using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class WaveSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public Transform[] spawnPoints;   // Kéo 4 điểm spawn vào đây
    public List<WaveData> waves;      // Danh sách các Wave của Level này (Wave 1, Wave 2...)
    public float timeBetweenWaves = 3f; // Thời gian nghỉ giữa các đợt

    private int currentWaveIndex = 0;
    private bool isSpawning = false;

    void Start()
    {
        // Bắt đầu chạy Wave đầu tiên
        StartCoroutine(RunLevelLogic());
    }

    IEnumerator RunLevelLogic()
    {
        // Duyệt qua từng Wave trong danh sách
        for (int i = 0; i < waves.Count; i++)
        {
            currentWaveIndex = i;
            Debug.Log("--- BẮT ĐẦU WAVE " + (i + 1) + " ---");

            // 1. Sinh quái của Wave hiện tại
            yield return StartCoroutine(SpawnWave(waves[i]));

            // 2. Chờ cho đến khi người chơi giết sạch quái của Wave này
            // (Kiểm tra mỗi 1 giây xem còn quái nào không)
            yield return new WaitUntil(() => GameObject.FindGameObjectsWithTag("Enemy").Length == 0);

            Debug.Log("Wave " + (i + 1) + " Hoàn thành!");

            // 3. Nghỉ một chút trước khi sang Wave mới
            yield return new WaitForSeconds(timeBetweenWaves);
        }

        Debug.Log("CHÚC MỪNG! ĐÃ HOÀN THÀNH LEVEL!");
        // Sau này sẽ gọi GameManager.Instance.Victory(); tại đây
    }

    IEnumerator SpawnWave(WaveData waveData)
    {
        isSpawning = true;

        // Duyệt qua từng nhóm quái trong Wave (Ví dụ: Nhóm Orc xong đến nhóm Imp)
        foreach (var group in waveData.enemyGroups)
        {
            for (int i = 0; i < group.count; i++)
            {
                SpawnEnemy(group.enemyPrefab);
                // Chờ một chút mới sinh con tiếp theo
                yield return new WaitForSeconds(group.rate);
            }
        }

        isSpawning = false;
    }

    void SpawnEnemy(GameObject enemyPrefab)
    {
        // Chọn ngẫu nhiên 1 trong 4 điểm spawn
        int randomIndex = Random.Range(0, spawnPoints.Length);
        Transform spawnPoint = spawnPoints[randomIndex];

        Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);
    }
}