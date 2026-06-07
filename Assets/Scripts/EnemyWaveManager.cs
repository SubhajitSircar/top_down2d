using UnityEngine;
using System.Collections;

public class EnemyWaveManager : MonoBehaviour
{
    [Header("Wave Configuration")]
    public WaveData waveConfig;

    [Header("Scaling Progression Engine")]
    public bool loopInfinitely = true;
    public float difficultyScalePerLoop = 0.15f;
    public int countIncreasePerLoop = 5;

    [Header("Runtime Status")]
    public bool initialGuardsCleared = false;

    private int currentWaveStep = 0;
    private int completedLoops = 0;
    private bool isProcessingWave = false;

    private Transform player;
    private Camera mainCam;
    private DungeonGenerator dungeonGenerator;

    void Start()
    {
        mainCam = Camera.main;
        FindAndCacheReferences();
    }

    void Update()
    {
        // Absolute safety fallback: If layout generator script re-instantiated, re-fetch hooks
        if (dungeonGenerator == null || player == null)
        {
            FindAndCacheReferences();
            return;
        }

        if (!initialGuardsCleared || waveConfig == null || waveConfig.waves.Length == 0) return;

        if (!isProcessingWave)
        {
            if (currentWaveStep < waveConfig.waves.Length)
            {
                StartCoroutine(SpawnWaveRoutine(waveConfig.waves[currentWaveStep]));
            }
            else if (loopInfinitely)
            {
                // Progress the threat loop metrics automatically
                completedLoops++;
                currentWaveStep = 0;
                Debug.Log($"💀 Threat Level Escalated! Entering Loop Cycle: {completedLoops}");
            }
        }
    }

    void FindAndCacheReferences()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
        dungeonGenerator = FindObjectOfType<DungeonGenerator>();
    }

    // 🛠️ GATEWAY CLEAN FIX: Explicitly called by DungeonGenerator to setup new floors completely
    // Called by DungeonGenerator BEFORE clearing assets to lock down the spawners
    public void ResetManagerForNewLevel()
    {
        // Instantly kill active spawning loops so no new entities break through during loading frames
        StopAllCoroutines();

        initialGuardsCleared = false;
        currentWaveStep = 0;
        completedLoops = 0;
        isProcessingWave = false;

        Debug.Log("🔄 Wave System fully stopped, locked down, and flushed for the next stage.");
    }

    public void NotifyInitialGuardsDead()
    {
        Debug.Log("🛡️ Room perimeter cleared! Initiating horde timeline arrays...");
        initialGuardsCleared = true;
        currentWaveStep = 0;
        isProcessingWave = false;
    }

    IEnumerator SpawnWaveRoutine(WaveStep originalStep)
    {
        isProcessingWave = true;

        // Calculate progressive scaling over infinite timelines
        int adjustedCount = originalStep.count + (completedLoops * countIncreasePerLoop);
        float adjustedSpeed = originalStep.speedMultiplier + (completedLoops * difficultyScalePerLoop);

        switch (originalStep.pattern)
        {
            case WavePattern.RingInward:
                SpawnRingPattern(originalStep.enemyPrefab, adjustedCount, adjustedSpeed);
                break;
            case WavePattern.VerticalWall:
                SpawnWallPattern(originalStep.enemyPrefab, adjustedCount, adjustedSpeed, true);
                break;
            case WavePattern.HorizontalWall:
                SpawnWallPattern(originalStep.enemyPrefab, adjustedCount, adjustedSpeed, false);
                break;
        }

        // Wait a window to let instantiation processes clean up frames safely
        yield return new WaitForSeconds(2.0f);

        // Keep polling until every active swarmer on this layer has been neutralized
        while (GameObject.FindGameObjectsWithTag("Enemy").Length > 0)
        {
            yield return new WaitForSeconds(0.7f);
        }

        Debug.Log($"✅ Wave step {currentWaveStep} completely wiped!");
        currentWaveStep++;
        isProcessingWave = false;
    }

    void SpawnRingPattern(GameObject prefab, int count, float speedMult)
    {
        float screenRadius = (mainCam.orthographicSize * mainCam.aspect) + 3f;

        for (int i = 0; i < count; i++)
        {
            float angle = i * (360f / count) * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * screenRadius;
            Vector2 rawSpawnPos = (Vector2)player.position + offset;

            Vector2 safeSpawnPos = GetValidPositionClosestTo(rawSpawnPos);
            SpawnAndInitEnemy(prefab, safeSpawnPos, speedMult);
        }
    }

    void SpawnWallPattern(GameObject prefab, int count, float speedMult, bool isVertical)
    {
        float camHeight = mainCam.orthographicSize;
        float camWidth = camHeight * mainCam.aspect;
        Vector2 camPos = mainCam.transform.position;

        float chooseSide = Random.value > 0.5f ? 1f : -1f;

        if (isVertical)
        {
            float spawnX = camPos.x + (chooseSide * (camWidth + 3f));
            float minY = player.position.y - (camHeight * 1.2f);
            float maxY = player.position.y + (camHeight * 1.2f);

            for (int i = 0; i < count; i++)
            {
                float t = count > 1 ? (float)i / (count - 1) : 0.5f;
                Vector2 rawSpawnPos = new Vector2(spawnX, Mathf.Lerp(minY, maxY, t));

                Vector2 safeSpawnPos = GetValidPositionClosestTo(rawSpawnPos);
                SpawnAndInitEnemy(prefab, safeSpawnPos, speedMult);
            }
        }
        else
        {
            float spawnY = camPos.y + (chooseSide * (camHeight + 3f));
            float minX = player.position.x - (camWidth * 1.2f);
            float maxX = player.position.x + (camWidth * 1.2f);

            for (int i = 0; i < count; i++)
            {
                float t = count > 1 ? (float)i / (count - 1) : 0.5f;
                Vector2 rawSpawnPos = new Vector2(Mathf.Lerp(minX, maxX, t), spawnY);

                Vector2 safeSpawnPos = GetValidPositionClosestTo(rawSpawnPos);
                SpawnAndInitEnemy(prefab, safeSpawnPos, speedMult);
            }
        }
    }

    Vector2 GetValidPositionClosestTo(Vector2 targetPosition)
    {
        if (dungeonGenerator == null || player == null) return targetPosition;

        Vector2Int gridPos = new Vector2Int(Mathf.FloorToInt(targetPosition.x), Mathf.FloorToInt(targetPosition.y));

        // Trace vectors back towards the player
        Vector2 directionToPlayer = ((Vector2)player.position - targetPosition).normalized;

        // 🛠️ WALL PIN FIX: If it hits floor directly, nudge it inward by a 1.2 tile padding allowance
        if (dungeonGenerator.floorPositions.Contains(gridPos))
        {
            Vector2 confirmedPos = new Vector2(gridPos.x + 0.5f, gridPos.y + 0.5f);
            return confirmedPos + (directionToPlayer * 1.2f);
        }

        float totalDistance = Vector2.Distance(targetPosition, player.position);

        for (float step = 0.5f; step < totalDistance; step += 0.7f)
        {
            Vector2 checkPoint = targetPosition + (directionToPlayer * step);
            Vector2Int checkGrid = new Vector2Int(Mathf.FloorToInt(checkPoint.x), Mathf.FloorToInt(checkPoint.y));

            if (dungeonGenerator.floorPositions.Contains(checkGrid))
            {
                // 🛠️ WALL PIN FIX: Force a 1.2-unit inward buffer step away from perimeter seams
                Vector2 safeCandidate = new Vector2(checkGrid.x + 0.5f, checkGrid.y + 0.5f) + (directionToPlayer * 1.2f);

                Vector2Int safetyGridCheck = new Vector2Int(Mathf.FloorToInt(safeCandidate.x), Mathf.FloorToInt(safeCandidate.y));
                if (dungeonGenerator.floorPositions.Contains(safetyGridCheck))
                {
                    return safeCandidate;
                }
            }
        }

        return (Vector2)player.position + (directionToPlayer * 2.5f);
    }

    void SpawnAndInitEnemy(GameObject prefab, Vector2 position, float speedMult)
    {
        GameObject enemy = Instantiate(prefab, position, Quaternion.identity);
        EnemyMovement movement = enemy.GetComponent<EnemyMovement>();

        if (movement != null)
        {
            movement.currentState = EnemyMovement.LeechState.Chasing;
            movement.chaseSpeed *= speedMult;
        }
    }
}