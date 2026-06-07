using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class DungeonGenerator : MonoBehaviour
{
    [Header("Tilemap References")]
    public Tilemap floorTilemap;
    public Tilemap wallTilemap;

    [Header("Tile Assets")]
    public TileBase floorTile;
    public TileBase wallTile;

    [Header("Dungeon Settings")]
    public int roomCount = 5;

    [Range(30, 150)]
    public int minRoomSize = 60;

    [Range(30, 150)]
    public int maxRoomSize = 100;

    public int corridorSpacing = 150;

    [Header("Corridor Settings")]
    [Range(1, 20)]
    public int corridorWidth = 12;

    [Header("Player")]
    public GameObject player;

    [Header("Door")]
    public GameObject doorPrefab;

    private GameObject currentDoor;

    [HideInInspector]
    public HashSet<Vector2Int> floorPositions = new HashSet<Vector2Int>();

    private Vector2Int currentRoomPosition = Vector2Int.zero;
    private Vector2 firstRoomCenter;
    private bool firstRoomCreated;

    [Header("Enemies")]
    public GameObject enemyPrefab;
    public int enemyCount = 10;

    private List<GameObject> spawnedEnemies = new List<GameObject>();

    // 🛠️ HORDE STATE TRACKING NODES
    private int aliveInitialGuardsCount = 0;

    void Start()
    {
        GenerateDungeon();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // 🛠️ Reset wave manager states on forced generation re-runs
            ResetWaveManagerSystem();

            ClearEnemies();
            ClearDoor();
            ClearDungeon();

            GenerateDungeon();
        }
    }

    void GenerateDungeon()
    {
        floorPositions.Clear();
        firstRoomCreated = false;
        currentRoomPosition = Vector2Int.zero;

        for (int i = 0; i < roomCount; i++)
        {
            CreateRandomRoom(currentRoomPosition);

            Vector2Int nextRoomPosition =
                currentRoomPosition +
                GetRandomDirection() *
                corridorSpacing;

            CreateCorridor(
                currentRoomPosition,
                nextRoomPosition
            );

            currentRoomPosition = nextRoomPosition;
        }

        DrawDungeonTiles();

        if (player != null)
        {
            player.transform.position = firstRoomCenter;
        }

        SpawnEnemies();
        SpawnDoor();
    }

    void CreateRandomRoom(Vector2Int roomPosition)
    {
        int roomType = Random.Range(0, 5);

        switch (roomType)
        {
            case 0: CreateRectangleRoom(roomPosition); break;
            case 1: CreateLRoom(roomPosition); break;
            case 2: CreateCrossRoom(roomPosition); break;
            case 3: CreateHallRoom(roomPosition); break;
            case 4: CreateArenaRoom(roomPosition); break;
        }
    }

    void CreateRectangleRoom(Vector2Int start)
    {
        int width = Random.Range(minRoomSize, maxRoomSize);
        int height = Random.Range(minRoomSize, maxRoomSize);
        FillRectangle(start, width, height);
        SaveSpawnPoint(start, width, height);
    }

    void CreateLRoom(Vector2Int start)
    {
        int width = Random.Range(minRoomSize, maxRoomSize);
        int height = Random.Range(minRoomSize, maxRoomSize);

        FillRectangle(start, width, height);
        FillRectangle(
            start + new Vector2Int(width - width / 4, 0),
            width / 2,
            height
        );
        SaveSpawnPoint(start, width, height);
    }

    void CreateCrossRoom(Vector2Int start)
    {
        int size = Random.Range(minRoomSize, maxRoomSize);

        FillRectangle(start + new Vector2Int(size / 3, 0), size / 3, size);
        FillRectangle(start, size, size / 3);
        SaveSpawnPoint(start, size, size);
    }

    void CreateHallRoom(Vector2Int start)
    {
        int width = Random.Range(maxRoomSize, maxRoomSize + 40);
        int height = Random.Range(minRoomSize / 2, minRoomSize);

        FillRectangle(start, width, height);
        SaveSpawnPoint(start, width, height);
    }

    void CreateArenaRoom(Vector2Int start)
    {
        int size = Random.Range(40, 60);

        FillRectangle(start, size, size);
        FillRectangle(
            start + new Vector2Int(10, 10),
            size - 20,
            size - 20
        );
        SaveSpawnPoint(start, size, size);
    }

    void FillRectangle(Vector2Int start, int width, int height)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                floorPositions.Add(new Vector2Int(start.x + x, start.y + y));
            }
        }
    }

    void SaveSpawnPoint(Vector2Int start, int width, int height)
    {
        if (!firstRoomCreated)
        {
            firstRoomCenter = new Vector2(start.x + (width / 2f), start.y + (height / 2f));
            firstRoomCreated = true;
        }
    }

    void CreateCorridor(Vector2Int start, Vector2Int end)
    {
        Vector2Int position = start;

        while (position.x != end.x)
        {
            PaintCorridor(position);
            position.x += (int)Mathf.Sign(end.x - position.x);
        }

        while (position.y != end.y)
        {
            PaintCorridor(position);
            position.y += (int)Mathf.Sign(end.y - position.y);
        }
    }

    void PaintCorridor(Vector2Int center)
    {
        for (int x = -corridorWidth; x <= corridorWidth; x++)
        {
            for (int y = -corridorWidth; y <= corridorWidth; y++)
            {
                floorPositions.Add(new Vector2Int(center.x + x, center.y + y));
            }
        }
    }

    void DrawDungeonTiles()
    {
        HashSet<Vector2Int> wallPositions = new HashSet<Vector2Int>();
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (Vector2Int pos in floorPositions)
        {
            floorTilemap.SetTile(new Vector3Int(pos.x, pos.y, 0), floorTile);

            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighbor = pos + dir;
                if (!floorPositions.Contains(neighbor))
                {
                    wallPositions.Add(neighbor);
                }
            }
        }

        foreach (Vector2Int wallPos in wallPositions)
        {
            wallTilemap.SetTile(new Vector3Int(wallPos.x, wallPos.y, 0), wallTile);
        }
    }

    void SpawnDoor()
    {
        if (doorPrefab == null) return;
        if (currentDoor != null) Destroy(currentDoor);

        Vector2Int bestPosition = Vector2Int.zero;
        float maxDistance = 0f;

        foreach (Vector2Int pos in floorPositions)
        {
            bool nearWall =
                !floorPositions.Contains(pos + Vector2Int.up) ||
                !floorPositions.Contains(pos + Vector2Int.down) ||
                !floorPositions.Contains(pos + Vector2Int.left) ||
                !floorPositions.Contains(pos + Vector2Int.right);

            if (!nearWall) continue;

            float distance = Vector2.Distance(new Vector2(pos.x, pos.y), firstRoomCenter);
            if (distance > maxDistance)
            {
                maxDistance = distance;
                bestPosition = pos;
            }
        }

        currentDoor = Instantiate(
            doorPrefab,
            new Vector3(bestPosition.x + 0.5f, bestPosition.y + 0.5f, 0),
            Quaternion.identity
        );
    }

    void ClearDoor()
    {
        if (currentDoor != null) Destroy(currentDoor);
    }

    void SpawnEnemies()
    {
        List<Vector2Int> floors = new List<Vector2Int>(floorPositions);
        int spawned = 0;
        int attempts = 0;
        int maxAttempts = enemyCount * 10;

        aliveInitialGuardsCount = enemyCount;

        while (spawned < enemyCount && attempts < maxAttempts)
        {
            attempts++;
            Vector2Int randomFloor = floors[Random.Range(0, floors.Count)];

            // 🛠️ WALL SPAWN FIX: Verify this tile is an inner floor tile and doesn't touch wall borders
            bool isEdgeTile =
                !floorPositions.Contains(randomFloor + Vector2Int.up) ||
                !floorPositions.Contains(randomFloor + Vector2Int.down) ||
                !floorPositions.Contains(randomFloor + Vector2Int.left) ||
                !floorPositions.Contains(randomFloor + Vector2Int.right) ||
                !floorPositions.Contains(randomFloor + new Vector2Int(1, 1)) ||   // Top-Right Corner
                !floorPositions.Contains(randomFloor + new Vector2Int(-1, 1)) ||  // Top-Left Corner
                !floorPositions.Contains(randomFloor + new Vector2Int(1, -1)) ||  // Bottom-Right Corner
                !floorPositions.Contains(randomFloor + new Vector2Int(-1, -1));   // Bottom-Left Corner

            // Skip this tile if it borders a wall area to avoid pinning physics colliders
            if (isEdgeTile) continue;

            Vector2 spawnPos = new Vector2(randomFloor.x + 0.5f, randomFloor.y + 0.5f);

            if (Vector2.Distance(spawnPos, firstRoomCenter) < 25f)
            {
                continue;
            }

            GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
            spawnedEnemies.Add(enemy);
            spawned++;
        }

        // Catch-all safety fallback: Reset tracking count if maxAttempts cut off the loop early
        aliveInitialGuardsCount = spawned;
    }

    // 🛠️ INTERCEPTOR METHOD: Triggered by EnemyHealth.cs when an initial guard dies
    public void TrackGuardDeath()
    {
        // Safety bail out if hordes have already broken lose
        EnemyWaveManager waveMgr = FindObjectOfType<EnemyWaveManager>();
        if (waveMgr != null && waveMgr.initialGuardsCleared) return;

        aliveInitialGuardsCount--;

        if (aliveInitialGuardsCount <= 0)
        {
            if (waveMgr != null) waveMgr.NotifyInitialGuardsDead();
        }
    }

    // 🛠️ UTILITY CLEANUP HANDLER
    // 🛠️ UPDATED RESET ROUTINE INSIDE YOUR DUNGEON GENERATOR
    private void ResetWaveManagerSystem()
    {
        EnemyWaveManager waveMgr = FindObjectOfType<EnemyWaveManager>();
        if (waveMgr != null)
        {
            waveMgr.ResetManagerForNewLevel();
        }
    }

    void ClearEnemies()
    {
        // 1. Purge everything registered in the initial tracked list
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null) Destroy(enemy);
        }
        spawnedEnemies.Clear();

        // 2. GLOBAL SWEEP SYSTEM: Hunt down and eliminate any horde/swarm entities 
        // that were dynamically spawned by the wave manager.
        GameObject[] rogueEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject rogue in rogueEnemies)
        {
            if (rogue != null) Destroy(rogue);
        }
    }

    Vector2Int GetRandomDirection()
    {
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        return directions[Random.Range(0, directions.Length)];
    }

    void ClearDungeon()
    {
        floorTilemap.ClearAllTiles();
        wallTilemap.ClearAllTiles();
        floorPositions.Clear();
    }

    public void NextLevel()
    {
        // 🛠️ Reset engine ahead of natural level progressions
        ResetWaveManagerSystem();

        ClearEnemies();
        ClearDoor();
        ClearDungeon();

        GenerateDungeon();
    }
}