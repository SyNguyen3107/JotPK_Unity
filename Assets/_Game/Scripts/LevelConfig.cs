using UnityEngine;
using System.Collections.Generic;

[System.Serializable] // Để hiện được trên Inspector
public class LevelConfig
{
    public string levelName = "Level 1";
    public GameObject mapPrefab; // Map hình dáng ra sao? (Đất, Tường, Cổng)
    public List<WaveData> waves; // Quái gì sẽ xuất hiện?
    public float levelDuration = 60f; // Màn này chơi bao lâu?
}