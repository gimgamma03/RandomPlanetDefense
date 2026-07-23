using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

public class AStarGrid
{

    public Tilemap walkableMap;
    public Tilemap wallmap;

    //위의 타일맵 두개는 일단 해놓고 나중에 private


    [Header("씬에 그리드를 표시")][SerializeField] bool ShowTestGrid;
    [Header("대각선 탐색")][SerializeField] bool Diagonal;

    public AStarNode[,] grid; // [y,x] 그리드

    public AStarPathfind pathfinder;

    // for TEST
    private AStarNode startNode;
    private AStarNode endNode;
    public AStarNode GoalNode;

    //임시
    public Tile WallTile;
    public List<AStarNode> StartToEndPath;


    public void SetUp(Tilemap WalkableMap, Tilemap WallMap)
    {
        walkableMap = WalkableMap;
        wallmap = WallMap;

        CreateGrid();
        pathfinder = new AStarPathfind(this);

    }

    public List<AStarNode> SetPath(Transform Start, Transform End)
    {
        AStarNode StartNode = GetNodeFromWorld(Start.position);
        AStarNode EndNode = GetNodeFromWorld(End.position);

        StartToEndPath = pathfinder.CreatePath(StartNode, EndNode);

        for (int i = 0; i < StartToEndPath.Count; i++)
        {
            Vector3Int NodePosition = walkableMap.WorldToCell(new Vector3(StartToEndPath[i].xPos, StartToEndPath[i].yPos));
            Vector3 CenterPositon = walkableMap.GetCellCenterWorld(NodePosition);
            CenterPositon -= walkableMap.cellGap / 2;


            StartToEndPath[i].xPos = CenterPositon.x;
            StartToEndPath[i].yPos = CenterPositon.y;
        }

        return StartToEndPath;
    }

    public void CreateGrid()
    {
        walkableMap.CompressBounds();
        BoundsInt bounds = walkableMap.cellBounds;
        grid = new AStarNode[bounds.size.y, bounds.size.x];

        Debug.Log(bounds.size);
        Debug.Log(bounds.yMin);
        Debug.Log(bounds.xMin);

        for (int y = bounds.yMin, i = 0; i < bounds.size.y; y++, i++)
        {
            for (int x = bounds.xMin, j = 0; j < bounds.size.x; x++, j++)
            {
                AStarNode node = new AStarNode();
                Vector3 cell = walkableMap.CellToWorld(new Vector3Int(x, y));

                node.xPos = cell.x;
                node.yPos = cell.y;

                node.yIndex = i;
                node.xIndex = j;
                node.gCost = int.MaxValue;

                // walkable Tilemap에 타일이 있으면 이동 가능한 노드, 타일이 없으면 이동 불가능한 노드이다.
                if (walkableMap.HasTile(new Vector3Int(x, y)))
                {
                    node.isWalkable = true;
                }
                else
                {
                    node.isWalkable = false;
                }

                grid[i, j] = node;
                /*                else if (wallmap.HasTile(new Vector3Int(x, y)))
                                {
                                    node.isWalkable = false;
                                }
                                else
                                {
                                    node.isWalkable = false;
                                }*/
                // walkableMap, wallMap 둘 다 타일이 없으면 이동 가능한 노드가 아님
            }
        }
    }

    public void ResetNode()
    {
        foreach (AStarNode node in grid)
        {
            node.Reset();
        }
    }

    public AStarNode GetNodeFromWorld(Vector3 worldPosition)
    {
        // 월드 좌표로 해당 좌표의 AStarNode 인스턴스를 얻는다.
        Vector3Int cellPos = walkableMap.WorldToCell(worldPosition);
        int y = cellPos.y + Mathf.Abs(walkableMap.cellBounds.yMin);
        int x = cellPos.x + Mathf.Abs(walkableMap.cellBounds.xMin);

        AStarNode node = grid[y, x];
        return node;
    }
    public List<AStarNode> GetNeighborNodes(AStarNode node, bool diagonal = false)
    {
        List<AStarNode> neighbors = new List<AStarNode>();
        int height = grid.GetUpperBound(0);
        int width = grid.GetUpperBound(1);

        int y = node.yIndex;
        int x = node.xIndex;
        // 상하
        if (y < height)
            neighbors.Add(grid[y + 1, x]); 
        if (y > 0)
            neighbors.Add(grid[y - 1, x]); 
        // 좌우
        if (x < width)
            neighbors.Add(grid[y, x + 1]); 
        if (x > 0)
            neighbors.Add(grid[y, x - 1]);

        if (!diagonal) return neighbors;

        // 대각선
        if (x > 0 && y > 0)
            neighbors.Add(grid[y - 1, x - 1]);
        if (x < width && y > 0)
            neighbors.Add(grid[y - 1, x + 1]);
        if (x > 0 && y < height)
            neighbors.Add(grid[y + 1, x - 1]);
        if (x < width && y < height)
            neighbors.Add(grid[y + 1, x + 1]);

        return neighbors;
    }

    public void OnDrawGizmos()
    {
        // 그리드가 잘 생성되었는지 확인해보기 위해서 에디터에 그려본다.
        if (grid != null && ShowTestGrid)
        {
            foreach (var node in grid)
            {
                Gizmos.color = Color.red;
                Vector3Int cellPos = walkableMap.WorldToCell(new Vector3(node.xPos, node.yPos));
                Vector3 drawPos = walkableMap.GetCellCenterWorld(cellPos);
                drawPos -= walkableMap.cellGap / 2;
                Vector3 drawSize = walkableMap.cellSize;
                Gizmos.DrawWireCube(drawPos, drawSize);
            }
        }
    }

    public void TestPathfind(bool diagonal)
    {
        List<AStarNode> path = pathfinder.CreatePath(startNode, endNode, diagonal);
        if (path != null)
        {
            for (int i = 0; i < path.Count - 1; i++)
            {
                Vector3Int startCellPos = walkableMap.WorldToCell(new Vector3(path[i].xPos, path[i].yPos));
                Vector3 startCenterPos = walkableMap.GetCellCenterWorld(startCellPos);
                startCenterPos -= walkableMap.cellGap / 2;

                Vector3Int endCellPos = walkableMap.WorldToCell(new Vector3(path[i+1].xPos, path[i+1].yPos));
                Vector3 endCenterPos = walkableMap.GetCellCenterWorld(endCellPos);
                endCenterPos -= walkableMap.cellGap / 2;

                Debug.DrawLine(new Vector3(path[i].xPos, path[i].yPos), new Vector3(path[i+1].xPos, path[i+1].yPos), Color.black, 2f);
                Debug.DrawLine(startCenterPos, endCenterPos, Color.white, 2f);
            }
            Debug.Log("path");
        }
    }
}
