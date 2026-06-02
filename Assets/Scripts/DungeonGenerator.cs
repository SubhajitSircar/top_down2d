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
    public int roomCount = 6;

    [Range(20, 60)]
    public int minRoomSize = 30;

    [Range(20, 60)]
    public int maxRoomSize = 50;

    public int corridorSpacing = 70;

    [Header("Corridor Settings")]
    [Range(1, 15)]
    public int corridorWidth = 7;

    [Header("Player")]
    public GameObject player;

    // CHANGED TO PUBLIC: Allows your EnemyMovement script to safely inspect 
    // valid coordinates across the entire level for its exploration routine.
    [HideInInspector]
    public HashSet<Vector2Int> floorPositions = new HashSet<Vector2Int>();

    private Vector2Int currentRoomPosition = Vector2Int.zero;
    private Vector2 firstRoomCenter;
    private bool firstRoomCreated;

    [Header("Enemies")]
    public GameObject enemyPrefab;
    public int enemyCount = 10;

    private List<GameObject> spawnedEnemies = new List<GameObject>();

    void Start()
    {
        GenerateDungeon();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ClearEnemies();
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
    }

    void CreateRandomRoom(Vector2Int roomPosition)
    {
        int roomType = Random.Range(0, 5);

        switch (roomType)
        {
            case 0:
                CreateRectangleRoom(roomPosition);
                break;
            case 1:
                CreateLRoom(roomPosition);
                break;
            case 2:
                CreateCrossRoom(roomPosition);
                break;
            case 3:
                CreateHallRoom(roomPosition);
                break;
            case 4:
                CreateArenaRoom(roomPosition);
                break;
        }
    }

    void CreateRectangleRoom(Vector2Int start)
    {
        int width = Random.Range(30, 50);
        int height = Random.Range(30, 50);

        FillRectangle(start, width, height);
        SaveSpawnPoint(start, width, height);
    }

    void CreateLRoom(Vector2Int start)
    {
        int width = Random.Range(25, 40);
        int height = Random.Range(25, 40);

        FillRectangle(start, width, height);

        FillRectangle(
            start + new Vector2Int(width - 10, 0),
            width / 2,
            height
        );

        SaveSpawnPoint(start, width, height);
    }

    void CreateCrossRoom(Vector2Int start)
    {
        int size = Random.Range(30, 45);

        FillRectangle(
            start + new Vector2Int(size / 3, 0),
            size / 3,
            size
        );

        FillRectangle(
            start,
            size,
            size / 3
        );

        SaveSpawnPoint(start, size, size);
    }

    void CreateHallRoom(Vector2Int start)
    {
        int width = Random.Range(50, 80);
        int height = Random.Range(15, 20);

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
                floorPositions.Add(
                    new Vector2Int(
                        start.x + x,
                        start.y + y
                    )
                );
            }
        }
    }

    void SaveSpawnPoint(Vector2Int start, int width, int height)
    {
        if (!firstRoomCreated)
        {
            // Adding half-unit shifts here offsets the center coordinate cleanly 
            // directly into the middle of the central tile.
            firstRoomCenter =
                new Vector2(
                    start.x + (width / 2f),
                    start.y + (height / 2f)
                );

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
                floorPositions.Add(
                    new Vector2Int(
                        center.x + x,
                        center.y + y
                    )
                );
            }
        }
    }

    void DrawDungeonTiles()
    {
        HashSet<Vector2Int> wallPositions = new HashSet<Vector2Int>();

        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        foreach (Vector2Int pos in floorPositions)
        {
            floorTilemap.SetTile(
                new Vector3Int(pos.x, pos.y, 0),
                floorTile
            );

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
            wallTilemap.SetTile(
                new Vector3Int(wallPos.x, wallPos.y, 0),
                wallTile
            );
        }
    }

    void SpawnEnemies()
    {
        List<Vector2Int> floors = new List<Vector2Int>(floorPositions);
        int spawned = 0;

        // Safety switch variables prevent potential infinite loops if your
        // player spawn radius exclusions leave too few valid structural tiles.
        int attempts = 0;
        int maxAttempts = enemyCount * 10;

        while (spawned < enemyCount && attempts < maxAttempts)
        {
            attempts++;
            Vector2Int randomFloor = floors[Random.Range(0, floors.Count)];

            // FIXED: Added an explicit half-unit shift (+0.5f) to both axes.
            // This forces the instantiation point right into the true geometric 
            // center of the chosen floor tile, stopping out-of-bounds wall bleeding.
            Vector2 spawnPos = new Vector2(randomFloor.x + 0.5f, randomFloor.y + 0.5f);

            if (Vector2.Distance(spawnPos, firstRoomCenter) < 25f)
            {
                continue;
            }

            GameObject enemy = Instantiate(
                enemyPrefab,
                spawnPos,
                Quaternion.identity
            );

            spawnedEnemies.Add(enemy);
            spawned++;
        }
    }

    void ClearEnemies()
    {
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }
        spawnedEnemies.Clear();
    }

    Vector2Int GetRandomDirection()
    {
        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        return directions[Random.Range(0, directions.Length)];
    }

    void ClearDungeon()
    {
        floorTilemap.ClearAllTiles();
        wallTilemap.ClearAllTiles();
        floorPositions.Clear();
    }
}