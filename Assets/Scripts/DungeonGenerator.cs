using UnityEngine;
using UnityEngine.Tilemaps; // Required for Tilemaps
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
    [Range(3, 20)] public int roomCount = 5;
    [Range(10, 30)] public int minRoomSize = 18;
    [Range(10, 30)] public int maxRoomSize = 28;
    public int corridorSpacing = 35;

    [Header("Player")]
    public GameObject player;

    private HashSet<Vector2Int> floorPositions = new HashSet<Vector2Int>();
    private Vector2Int currentRoomPosition = Vector2Int.zero;
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
        currentRoomPosition = Vector2Int.zero;

        for (int i = 0; i < roomCount; i++)
        {
            CreateRoom(currentRoomPosition);

            Vector2Int newRoomPosition = currentRoomPosition + GetRandomDirection() * corridorSpacing;
            CreateCorridor(currentRoomPosition, newRoomPosition);
            currentRoomPosition = newRoomPosition;
        }

        DrawDungeonTiles();
        player.transform.position = firstRoomCenter;
    }

    void CreateRoom(Vector2Int roomPosition)
    {
        int width = Random.Range(minRoomSize, maxRoomSize);
        int height = Random.Range(minRoomSize, maxRoomSize);

        Vector2 roomCenter = new Vector2(
            roomPosition.x + width / 2f,
            roomPosition.y + height / 2f
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
                Vector2Int pos = new Vector2Int(x + roomPosition.x, y + roomPosition.y);
                floorPositions.Add(pos);
            }
        }
    }

    void CreateCorridor(Vector2Int start, Vector2Int end)
    {
        Vector2Int position = start;

        while (position.x != end.x)
        {
            CreateCorridorWidth(position);
            position.x += (int)Mathf.Sign(end.x - start.x);
        }

        while (position.y != end.y)
        {
            CreateCorridorWidth(position);
            position.y += (int)Mathf.Sign(end.y - start.y);
        }
    }

    void CreateCorridorWidth(Vector2Int position)
    {
        for (int x = -2; x <= 2; x++)
        {
            for (int y = -2; y <= 2; y++)
            {
                Vector2Int corridorPos = new Vector2Int(position.x + x, position.y + y);
                floorPositions.Add(corridorPos);
            }
        }
    }

    void DrawDungeonTiles()
    {
        HashSet<Vector2Int> wallPositions = new HashSet<Vector2Int>();
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (Vector2Int pos in floorPositions)
        {
            Vector3Int tilePos = new Vector3Int(pos.x, pos.y, 0);
            floorTilemap.SetTile(tilePos, floorTile);

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
            Vector3Int tilePos = new Vector3Int(wallPos.x, wallPos.y, 0);
            wallTilemap.SetTile(tilePos, wallTile);
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
    }
}