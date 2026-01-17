using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class EnemyGroup
{
    public GameObject enemyPrefab;
    public int count;
    public float rate;
}

[CreateAssetMenu(fileName = "NewWave", menuName = "JOTPK/Wave Data")]
public class WaveData : ScriptableObject
{
    [Header("Wave Configuration")]
    public List<EnemyGroup> enemyGroups;
}