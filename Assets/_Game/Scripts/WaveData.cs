using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class EnemyGroup
{
    public GameObject enemyPrefab; // Loại quái (Orc, Imp...)
    public int count;              // Số lượng cần sinh
    public float rate;             // Tốc độ sinh (giây/con)
}

[CreateAssetMenu(fileName = "NewWave", menuName = "JOTPK/Wave Data")]
public class WaveData : ScriptableObject
{
    [Header("Cấu hình Đợt Quái")]
    public List<EnemyGroup> enemyGroups; // Một wave có thể gồm nhiều nhóm quái
}