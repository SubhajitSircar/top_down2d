using UnityEngine;

[System.Serializable]
public enum WavePattern { RingInward, VerticalWall, HorizontalWall }

[System.Serializable]
public struct WaveStep
{
    [Tooltip("Order sequence index for this horde layer.")]
    public int waveIndex;
    public WavePattern pattern;
    public GameObject enemyPrefab;
    public int count;
    public float speedMultiplier;
}

[CreateAssetMenu(fileName = "NewWaveConfig", menuName = "Dungeon/Wave Configuration")]
public class WaveData : ScriptableObject
{
    public WaveStep[] waves;
}