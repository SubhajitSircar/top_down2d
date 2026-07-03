using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class NEWDungeonGenerator : MonoBehaviour
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

    [Header("Top Wall Architecture")]
    [Tooltip("Drag your PLAIN, solid massive wall tiles here!")]
    public TileBase[] plainTopWalls;

    [Tooltip("Drag your massive DOORS and GATES here!")]
    public TileBase[] doorTopWalls;

    [Tooltip("How many grid spaces wide is your massive artwork?")]
    public int wallWidthSpacing = 6;

    [Tooltip("Minimum grid spaces required between two doors!")]
    public float minDistanceBetweenDoors = 15f;

    [Tooltip("Percentage chance (0.0 to 1.0) a door will spawn instead of a wall.")]
    [Range(0f, 1f)]
    public float doorSpawnChance = 0.15f;

    [Header("Enemies")]
    [Tooltip("Drag all 5 of your elemental slime prefabs into this array!")]
    public GameObject[] enemyPrefabs;
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

        // 1. Paint all floors and locate where the walls belong
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

        HashSet<Vector2Int> blockedPositions = new HashSet<Vector2Int>();
        List<Vector2Int> placedDoorLocations = new List<Vector2Int>();

        // 🚨 THE FIX: Sort the wall positions from Left to Right!
        // This stops the random HashSet from painting a wall in a space that a door is about to block.
        List<Vector2Int> sortedWalls = new List<Vector2Int>(wallPositions);
        sortedWalls.Sort((a, b) => {
            if (a.y != b.y) return a.y.CompareTo(b.y);
            return a.x.CompareTo(b.x);
        });

        // 2. Paint the walls reading Left-to-Right
        foreach (Vector2Int wallPos in sortedWalls)
        {
            bool isTopWallEdge = floorPositions.Contains(wallPos + Vector2Int.down);

            if (isTopWallEdge && !blockedPositions.Contains(wallPos))
            {
                bool canSpawnDoor = false;

                if (doorTopWalls.Length > 0 && Random.value <= doorSpawnChance)
                {
                    canSpawnDoor = true;

                    // Check corners safely
                    for (int xOffset = -2; xOffset < wallWidthSpacing + 2; xOffset++)
                    {
                        Vector2Int checkPos = new Vector2Int(wallPos.x + xOffset, wallPos.y);
                        bool neighborIsTopWall = wallPositions.Contains(checkPos) && floorPositions.Contains(checkPos + Vector2Int.down);

                        if (!neighborIsTopWall)
                        {
                            canSpawnDoor = false;
                            break;
                        }
                    }

                    // Check distance between existing doors
                    if (canSpawnDoor)
                    {
                        foreach (Vector2Int existingDoor in placedDoorLocations)
                        {
                            if (Vector2.Distance(wallPos, existingDoor) < minDistanceBetweenDoors)
                            {
                                canSpawnDoor = false;
                                break;
                            }
                        }
                    }
                }

                TileBase tileToPlace = null;

                if (canSpawnDoor)
                {
                    tileToPlace = doorTopWalls[Random.Range(0, doorTopWalls.Length)];
                    placedDoorLocations.Add(wallPos);
                }
                else if (plainTopWalls.Length > 0)
                {
                    tileToPlace = plainTopWalls[Random.Range(0, plainTopWalls.Length)];
                }

                if (tileToPlace != null)
                {
                    wallTilemap.SetTile(new Vector3Int(wallPos.x, wallPos.y, 0), tileToPlace);

                    // Block the exact width of the door so no plain walls spawn over it
                    for (int i = 0; i < wallWidthSpacing; i++)
                    {
                        blockedPositions.Add(new Vector2Int(wallPos.x + i, wallPos.y));
                    }
                }
                continue;
            }

            // Paint standard wall blocks on unblocked spaces
            if (!blockedPositions.Contains(wallPos))
            {
                wallTilemap.SetTile(new Vector3Int(wallPos.x, wallPos.y, 0), wallTile);
            }
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

        // --- 1. DYNAMIC ENEMY COUNT ---
        // Calculates the size of the level based on walkable floor tiles.
        // A smaller level gets ~10 enemies, a massive level caps out at 25.
        int calculatedCount = Mathf.Clamp(floors.Count / 300, 10, 25);
        enemyCount = calculatedCount; // Update the global count for the wave manager tracking

        aliveInitialGuardsCount = enemyCount;

        int spawned = 0;
        int attempts = 0;
        int maxAttempts = enemyCount * 10;

        // --- 2. SELECT EXACTLY 2 RANDOM ENEMY TYPES ---
        List<GameObject> currentLevelEnemyTypes = new List<GameObject>();
        if (enemyPrefabs != null && enemyPrefabs.Length > 0)
        {
            // Pick the first random enemy type
            int index1 = Random.Range(0, enemyPrefabs.Length);
            currentLevelEnemyTypes.Add(enemyPrefabs[index1]);

            // Pick a second random enemy type, ensuring it is NOT the same as the first one
            if (enemyPrefabs.Length > 1)
            {
                int index2 = Random.Range(0, enemyPrefabs.Length);
                while (index2 == index1)
                {
                    index2 = Random.Range(0, enemyPrefabs.Length);
                }
                currentLevelEnemyTypes.Add(enemyPrefabs[index2]);
            }
        }

        // --- 3. SPAWN THE SELECTED ENEMIES ---
        while (spawned < enemyCount && attempts < maxAttempts)
        {
            attempts++;
            Vector2Int randomFloor = floors[Random.Range(0, floors.Count)];

            // Verify this tile is an inner floor tile and doesn't touch wall borders
            bool isEdgeTile =
                !floorPositions.Contains(randomFloor + Vector2Int.up) ||
                !floorPositions.Contains(randomFloor + Vector2Int.down) ||
                !floorPositions.Contains(randomFloor + Vector2Int.left) ||
                !floorPositions.Contains(randomFloor + Vector2Int.right) ||
                !floorPositions.Contains(randomFloor + new Vector2Int(1, 1)) ||
                !floorPositions.Contains(randomFloor + new Vector2Int(-1, 1)) ||
                !floorPositions.Contains(randomFloor + new Vector2Int(1, -1)) ||
                !floorPositions.Contains(randomFloor + new Vector2Int(-1, -1));

            // Skip this tile if it borders a wall area to avoid pinning physics colliders
            if (isEdgeTile) continue;

            Vector2 spawnPos = new Vector2(randomFloor.x + 0.5f, randomFloor.y + 0.5f);

            // Keep enemies away from the player's immediate spawn room
            if (Vector2.Distance(spawnPos, firstRoomCenter) < 25f)
            {
                continue;
            }

            if (currentLevelEnemyTypes.Count > 0)
            {
                // Randomly pick between the 2 specific enemy types chosen for this level
                GameObject randomSlime = currentLevelEnemyTypes[Random.Range(0, currentLevelEnemyTypes.Count)];

                GameObject enemy = Instantiate(randomSlime, spawnPos, Quaternion.identity);
                spawnedEnemies.Add(enemy);
                spawned++;
            }
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


