using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;

public class MapDirector : MonoBehaviour
{
    static public MapDirector Instance;

    public AStarGrid aStarGrid;
    public Tilemap WalkableMap;
    public Tilemap WallMap;
    public Tilemap NonWallMap;
    public Tile WallTile;
    public Tile WalkableTile;

    [SerializeField] private GameObject goal;
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private Pathfinder showPath;
    [SerializeField] private TextFadeOut warnintMessage;

    private IPlayerService playerService;

    /// <summary>스폰→골 공유 경로 (월드 좌표). 스네이크 PathRoute처럼 전원이 이 골격을 탄다.</summary>
    public IReadOnlyList<Vector3> SharedWaypoints => sharedWaypoints;

    private List<Vector3> sharedWaypoints = new List<Vector3>();
    private List<AStarNode> sharedNodes = new List<AStarNode>();

    /// <summary>레거시 호환용 (미리보기 등). 공유 노드 목록.</summary>
    public List<AStarNode> StartToEndPath;

    public GameObject Boo;

    private void Awake()
    {
        Instance = this;
        aStarGrid = new AStarGrid();
        aStarGrid.SetUp(WalkableMap, WallMap);
    }

    private void Start()
    {
        playerService = ServiceLocator.Get<IPlayerService>();
        StartToEndPath = new List<AStarNode>();
        RebuildSharedPath();
        GridHoverOverlay.EnsureExists();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F12))
        {
            SceneManager.LoadScene("SampleScene");
        }
    }

    /// <summary>
    /// 마우스 월드 좌표에 벽 설치. 골드·경로 검사 포함.
    /// 입력은 BuildModeController(빌드 모드)가 담당한다.
    /// </summary>
    public bool TryPlaceWallAt(Vector3 worldPos)
    {
        if (playerService == null)
        {
            playerService = ServiceLocator.Get<IPlayerService>();
        }

        if (playerService == null)
        {
            return false;
        }

        if (!playerService.TrySpendGold(Constants.spawnWallGold))
        {
            Debug.Log($"Not enough gold for wall. (gold={playerService.Gold})");
            return false;
        }

        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero, Mathf.Infinity, LayerMask.GetMask("Tile"));
        if (hit.transform == null || !hit.transform.CompareTag("WalkableMap"))
        {
            Debug.Log("Place wall inside the field.");
            playerService.AddGold(Constants.spawnWallGold);
            return false;
        }

        AStarNode wallNode = aStarGrid.GetNodeFromWorld(worldPos);
        Vector3Int cellPosition = WalkableMap.WorldToCell(worldPos);

        if (wallNode == null || !CheckPath(wallNode))
        {
            Debug.Log("Wall blocks the path.");
            if (wallNode != null)
            {
                wallNode.isWalkable = true;
            }

            playerService.AddGold(Constants.spawnWallGold);
            return false;
        }

        WallMap.SetTile(cellPosition, WallTile);
        WalkableMap.SetTile(cellPosition, null);

        RebuildSharedPath();
        enemySpawner.CheckPathForAllEnemy();
        showPath.ShowPath();
        return true;
    }

    /// <summary>
    /// 빈 벽(타워 없음)을 Walkable로 되돌린다. 벽 설치 골드 환불.
    /// </summary>
    public bool TryRemoveWallAt(Vector3 worldPos)
    {
        if (WallMap == null || WalkableMap == null || aStarGrid == null)
        {
            return false;
        }

        Vector3Int cellPosition = WallMap.WorldToCell(worldPos);
        if (!WallMap.HasTile(cellPosition))
        {
            return false;
        }

        AStarNode node = aStarGrid.GetNodeFromWorld(WallMap.GetCellCenterWorld(cellPosition));
        if (node != null && node.isBuildTower)
        {
            Debug.Log("[MapDirector] 타워가 있는 벽은 철거할 수 없습니다.");
            return false;
        }

        WallMap.SetTile(cellPosition, null);
        WalkableMap.SetTile(cellPosition, WalkableTile);

        if (node != null)
        {
            node.isWalkable = true;
            node.isBuildTower = false;
        }

        if (playerService == null)
        {
            playerService = ServiceLocator.Get<IPlayerService>();
        }

        playerService?.AddGold(Constants.spawnWallGold);

        RebuildSharedPath();
        if (enemySpawner != null)
        {
            enemySpawner.CheckPathForAllEnemy();
        }

        if (showPath != null)
        {
            showPath.ShowPath();
        }

        return true;
    }

    public bool CheckPath(AStarNode wallNode)
    {
        wallNode.isWalkable = false;
        AStarNode startNode = aStarGrid.GetNodeFromWorld(enemySpawner.SpawnWorldPosition);
        AStarNode endNode = aStarGrid.GetNodeFromWorld(goal.transform.position);
        return aStarGrid.pathfinder.CreatePath(startNode, endNode) != null;
    }

    public bool CheckPath(Vector3Int cellPosition)
    {
        WallMap.SetTile(cellPosition, WallTile);
        WalkableMap.SetTile(cellPosition, null);

        AStarNode wallNode = aStarGrid.GetNodeFromWorld(WalkableMap.GetCellCenterWorld(cellPosition));
        wallNode.isWalkable = false;

        AStarNode startNode = aStarGrid.GetNodeFromWorld(enemySpawner.SpawnWorldPosition);
        AStarNode endNode = aStarGrid.GetNodeFromWorld(goal.transform.position);
        return aStarGrid.pathfinder.CreatePath(startNode, endNode) != null;
    }

    /// <summary>스폰→골 A*를 한 번만 돌려 공유 경로를 갱신한다.</summary>
    public bool RebuildSharedPath()
    {
        AStarNode startNode = aStarGrid.GetNodeFromWorld(enemySpawner.SpawnWorldPosition);
        AStarNode endNode = aStarGrid.GetNodeFromWorld(goal.transform.position);
        List<AStarNode> path = aStarGrid.pathfinder.CreatePath(startNode, endNode);

        sharedNodes.Clear();
        sharedWaypoints.Clear();

        if (path == null || path.Count == 0)
        {
            StartToEndPath = sharedNodes;
            return false;
        }

        sharedNodes.AddRange(path);
        for (int i = 0; i < path.Count; i++)
        {
            sharedWaypoints.Add(aStarGrid.NodeToWorldCenter(path[i]));
        }

        StartToEndPath = sharedNodes;
        return true;
    }

    /// <summary>
    /// 에이전트용 경로.
    /// 공유 경로 위에 있으면 접미사만 복사.
    /// 벗어나 있으면 합류점까지 짧은 A* + 공유 접미사 (전원 풀 A* 방지).
    /// </summary>
    public List<Vector3> SetPathFromPosition(Transform startPosition)
    {
        return BuildAgentPath(startPosition.position);
    }

    public List<Vector3> BuildAgentPath(Vector3 worldPosition)
    {
        if (sharedWaypoints.Count == 0)
        {
            RebuildSharedPath();
        }

        if (sharedWaypoints.Count == 0)
        {
            return new List<Vector3>();
        }

        AStarNode startNode = aStarGrid.GetNodeFromWorld(worldPosition);
        int onPathIndex = IndexOfSharedNode(startNode);
        if (onPathIndex >= 0)
        {
            return CopySharedFrom(onPathIndex);
        }

        int joinIndex = FindClosestSharedIndex(worldPosition);
        AStarNode joinNode = sharedNodes[joinIndex];
        List<AStarNode> toJoin = aStarGrid.pathfinder.CreatePath(startNode, joinNode);

        List<Vector3> result = new List<Vector3>();
        if (toJoin != null && toJoin.Count > 0)
        {
            for (int i = 0; i < toJoin.Count; i++)
            {
                result.Add(aStarGrid.NodeToWorldCenter(toJoin[i]));
            }

            for (int i = joinIndex + 1; i < sharedWaypoints.Count; i++)
            {
                result.Add(sharedWaypoints[i]);
            }

            return result;
        }

        // 합류 실패 시 골까지 직접 (최후 수단)
        AStarNode goalNode = aStarGrid.GetNodeFromWorld(goal.transform.position);
        List<AStarNode> direct = aStarGrid.pathfinder.CreatePath(startNode, goalNode);
        if (direct == null)
        {
            return CopySharedFrom(joinIndex);
        }

        for (int i = 0; i < direct.Count; i++)
        {
            result.Add(aStarGrid.NodeToWorldCenter(direct[i]));
        }

        return result;
    }

    private int IndexOfSharedNode(AStarNode node)
    {
        for (int i = 0; i < sharedNodes.Count; i++)
        {
            if (sharedNodes[i] == node)
            {
                return i;
            }
        }

        return -1;
    }

    private int FindClosestSharedIndex(Vector3 worldPosition)
    {
        int best = 0;
        float bestSqr = float.MaxValue;
        for (int i = 0; i < sharedWaypoints.Count; i++)
        {
            float sqr = (sharedWaypoints[i] - worldPosition).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = i;
            }
        }

        return best;
    }

    private List<Vector3> CopySharedFrom(int index)
    {
        List<Vector3> result = new List<Vector3>(sharedWaypoints.Count - index);
        for (int i = index; i < sharedWaypoints.Count; i++)
        {
            result.Add(sharedWaypoints[i]);
        }

        return result;
    }

    public Vector3 GetEnemySpanwerPosition()
    {
        return enemySpawner.SpawnWorldPosition;
    }
}