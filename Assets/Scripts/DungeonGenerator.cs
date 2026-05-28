using UnityEngine;
using System.Collections.Generic;

public class DungeonGenerator : MonoBehaviour
{
    public GameObject floorTile;
    public GameObject wallTile;
    public GameObject player;

    private HashSet<Vector2> floorPositions =
        new HashSet<Vector2>();

    private List<GameObject> spawnedTiles =
        new List<GameObject>();

    private Vector2 currentRoomPosition =
        Vector2.zero;

    private Vector2 firstRoomCenter;
    private bool firstRoomCreated = false;

    void Start()
    {
        GenerateDungeon();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ClearDungeon();
            GenerateDungeon();
        }
    }

    void GenerateDungeon()
    {
        floorPositions.Clear();

        firstRoomCreated = false;

        currentRoomPosition = Vector2.zero;

        int roomCount = 5;

        for (int i = 0; i < roomCount; i++)
        {
            CreateRoom(currentRoomPosition);

            Vector2 newRoomPosition =
                currentRoomPosition +
                GetRandomDirection() * 35;

            CreateCorridor(
                currentRoomPosition,
                newRoomPosition
            );

            currentRoomPosition =
                newRoomPosition;
        }

        GenerateWalls();

        player.transform.position =
            firstRoomCenter;
    }

    void CreateRoom(Vector2 roomPosition)
    {
        int width = Random.Range(18, 28);
        int height = Random.Range(18, 28);

        Vector2 roomCenter =
            new Vector2(
                roomPosition.x + width / 2,
                roomPosition.y + height / 2
            );

        if (!firstRoomCreated)
        {
            firstRoomCenter = roomCenter;
            firstRoomCreated = true;
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2 pos =
                    new Vector2(
                        x + roomPosition.x,
                        y + roomPosition.y
                    );

                floorPositions.Add(pos);
            }
        }
    }

    void CreateCorridor(
        Vector2 start,
        Vector2 end
    )
    {
        Vector2 position = start;

        // Horizontal corridor
        while ((int)position.x != (int)end.x)
        {
            CreateCorridorWidth(position);

            position.x +=
                Mathf.Sign(end.x - start.x);
        }

        // Vertical corridor
        while ((int)position.y != (int)end.y)
        {
            CreateCorridorWidth(position);

            position.y +=
                Mathf.Sign(end.y - start.y);
        }
    }

    void CreateCorridorWidth(Vector2 position)
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector2 corridorPos =
                    new Vector2(
                        position.x + x,
                        position.y + y
                    );

                floorPositions.Add(corridorPos);
            }
        }
    }

    void GenerateWalls()
    {
        HashSet<Vector2> wallPositions =
            new HashSet<Vector2>();

        Vector2[] directions =
        {
            Vector2.up,
            Vector2.down,
            Vector2.left,
            Vector2.right
        };

        foreach (Vector2 pos in floorPositions)
        {
            SpawnTile(floorTile, pos);

            foreach (Vector2 dir in directions)
            {
                Vector2 neighbor = pos + dir;

                if (!floorPositions.Contains(neighbor))
                {
                    wallPositions.Add(neighbor);
                }
            }
        }

        foreach (Vector2 wallPos in wallPositions)
        {
            SpawnTile(wallTile, wallPos);
        }
    }

    Vector2 GetRandomDirection()
    {
        Vector2[] directions =
        {
            Vector2.up,
            Vector2.down,
            Vector2.left,
            Vector2.right
        };

        return directions[
            Random.Range(0, directions.Length)
        ];
    }

    void SpawnTile(
        GameObject prefab,
        Vector2 position
    )
    {
        GameObject tile =
            Instantiate(
                prefab,
                position,
                Quaternion.identity
            );

        spawnedTiles.Add(tile);
    }

    void ClearDungeon()
    {
        foreach (GameObject tile in spawnedTiles)
        {
            Destroy(tile);
        }

        spawnedTiles.Clear();
    }
}