using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class LevelConfig
{
    public string levelName = "Level 1";
    public GameObject mapPrefab;
    public List<WaveData> waves;
    public float levelDuration = 60f;
}