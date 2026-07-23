using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class AStarGrid
{
    public Tilemap walkableMap;
    public Tilemap wallmap;

    [Header("씬에 그리드를 표시")]
    [SerializeField] bool ShowTestGrid;
    [Header("대각선 탐색")]
    [SerializeField] bool Diagonal;

    public AStarNode[,] grid;
    public AStarPathfind pathfinder;

    private AStarNode startNode;
    private AStarNode endNode;
    public AStarNode GoalNode;
    public Tile WallTile;
    public List<AStarNode> StartToEndPath;

    public void SetUp(Tilemap WalkableMap, Tilemap WallMap)
    {
        walkableMap = WalkableMap;
        wallmap = WallMap;
        CreateGrid();
        pathfinder = new AStarPathfind(this);
    }

    public void CreateGrid()
    {
        walkableMap.CompressBounds();
        BoundsInt bounds = walkableMap.cellBounds;
        grid = new AStarNode[bounds.size.y, bounds.size.x];

        for (int y = bounds.yMin, i = 0; i < bounds.size.y; y++, i++)
        {
            for (int x = bounds.xMin, j = 0; j < bounds.size.x; x++, j++)
            {
                AStarNode node = new AStarNode();
                Vector3 cell = walkableMap.CellToWorld(new Vector3Int(x, y, 0));

                node.xPos = cell.x;
                node.yPos = cell.y;
                node.yIndex = i;
                node.xIndex = j;
                node.gCost = int.MaxValue;
                node.isWalkable = walkableMap.HasTile(new Vector3Int(x, y, 0));

                grid[i, j] = node;
            }
        }
    }

    public void ResetNode()
    {
        if (grid == null)
        {
            return;
        }

        foreach (AStarNode node in grid)
        {
            node.Reset();
        }
    }

    public AStarNode GetNodeFromWorld(Vector3 worldPosition)
    {
        Vector3Int cellPos = walkableMap.WorldToCell(worldPosition);
        int y = cellPos.y + Mathf.Abs(walkableMap.cellBounds.yMin);
        int x = cellPos.x + Mathf.Abs(walkableMap.cellBounds.xMin);
        return grid[y, x];
    }

    public Vector3 NodeToWorldCenter(AStarNode node)
    {
        Vector3Int cellPos = new Vector3Int(
            walkableMap.cellBounds.xMin + node.xIndex,
            walkableMap.cellBounds.yMin + node.yIndex,
            0);
        Vector3 center = walkableMap.GetCellCenterWorld(cellPos);
        center -= walkableMap.cellGap / 2f;
        return center;
    }

    public List<AStarNode> GetNeighborNodes(AStarNode node, bool diagonal = false)
    {
        List<AStarNode> neighbors = new List<AStarNode>();
        int height = grid.GetUpperBound(0);
        int width = grid.GetUpperBound(1);
        int y = node.yIndex;
        int x = node.xIndex;

        if (y < height)
        {
            neighbors.Add(grid[y + 1, x]);
        }

        if (y > 0)
        {
            neighbors.Add(grid[y - 1, x]);
        }

        if (x < width)
        {
            neighbors.Add(grid[y, x + 1]);
        }

        if (x > 0)
        {
            neighbors.Add(grid[y, x - 1]);
        }

        if (!diagonal)
        {
            return neighbors;
        }

        if (x > 0 && y > 0)
        {
            neighbors.Add(grid[y - 1, x - 1]);
        }

        if (x < width && y > 0)
        {
            neighbors.Add(grid[y - 1, x + 1]);
        }

        if (x > 0 && y < height)
        {
            neighbors.Add(grid[y + 1, x - 1]);
        }

        if (x < width && y < height)
        {
            neighbors.Add(grid[y + 1, x + 1]);
        }

        return neighbors;
    }
}