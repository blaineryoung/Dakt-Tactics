using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds the battle grid and provides pathfinding / movement-range queries.
/// Attach this to an empty GameObject in your battle scene and assign a Tile prefab.
/// </summary>
public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Grid Settings")]
    public int width = 10;
    public int depth = 10;
    public float tileSize = 1f;
    public Tile tilePrefab;

    private Tile[,] _tiles;

    private void Awake()
    {
        Instance = this;
        GenerateGrid();
    }

    private void GenerateGrid()
    {
        _tiles = new Tile[width, depth];

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                Vector3 pos = new Vector3(x * tileSize, 0f, z * tileSize);
                Tile tile = Instantiate(tilePrefab, pos, Quaternion.identity, transform);
                tile.x = x;
                tile.z = z;
                tile.name = $"Tile_{x}_{z}";
                _tiles[x, z] = tile;
            }
        }
    }

    public Tile GetTile(int x, int z)
    {
        if (x < 0 || x >= width || z < 0 || z >= depth) return null;
        return _tiles[x, z];
    }

    public Tile GetTile(Vector2Int coord) => GetTile(coord.x, coord.y);

    public Tile WorldToTile(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x / tileSize);
        int z = Mathf.RoundToInt(worldPos.z / tileSize);
        return GetTile(x, z);
    }

    private static readonly Vector2Int[] Directions =
    {
        new Vector2Int(1, 0), new Vector2Int(-1, 0),
        new Vector2Int(0, 1), new Vector2Int(0, -1)
    };

    /// <summary>
    /// Breadth-first search to find every tile reachable within `moveRange` steps,
    /// respecting walkability and occupancy. Jump (height difference tolerance)
    /// is a simple flat check here — extend with real height rules later.
    /// </summary>
    public List<Tile> GetTilesInMoveRange(Tile origin, int moveRange, int jump = 1)
    {
        var result = new List<Tile>();
        var visited = new Dictionary<Vector2Int, int>(); // coord -> steps used
        var frontier = new Queue<(Tile tile, int steps)>();

        visited[origin.Coord] = 0;
        frontier.Enqueue((origin, 0));

        while (frontier.Count > 0)
        {
            var (current, steps) = frontier.Dequeue();
            if (steps > 0) result.Add(current);
            if (steps >= moveRange) continue;

            foreach (var dir in Directions)
            {
                Vector2Int nextCoord = current.Coord + dir;
                Tile next = GetTile(nextCoord);
                if (next == null || !next.walkable) continue;
                if (next.IsOccupied) continue;
                if (Mathf.Abs(next.height - current.height) > jump) continue;

                if (visited.TryGetValue(nextCoord, out int prevSteps) && prevSteps <= steps + 1)
                    continue;

                visited[nextCoord] = steps + 1;
                frontier.Enqueue((next, steps + 1));
            }
        }

        return result;
    }

    /// <summary>Tiles within attack range (Chebyshev/manhattan ring), ignoring occupancy.</summary>
    public List<Tile> GetTilesInAttackRange(Tile origin, int minRange, int maxRange)
    {
        var result = new List<Tile>();
        for (int x = -maxRange; x <= maxRange; x++)
        {
            for (int z = -maxRange; z <= maxRange; z++)
            {
                int dist = Mathf.Abs(x) + Mathf.Abs(z);
                if (dist < minRange || dist > maxRange || dist == 0) continue;
                Tile t = GetTile(origin.Coord + new Vector2Int(x, z));
                if (t != null) result.Add(t);
            }
        }
        return result;
    }

    public void ClearAllHighlights()
    {
        foreach (var tile in _tiles) tile.ClearHighlight();
    }
}
