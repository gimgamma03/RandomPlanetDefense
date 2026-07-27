using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

/// <summary>
/// 마우스 아래 그리드 셀 테두리.
/// 기본 흰색 / 벽 주황 / 소환 초록 / 조합 파랑 / 판매 빨강.
/// </summary>
public class GridHoverOverlay : MonoBehaviour
{
    [SerializeField]
    private Color viewColor = Color.white;
    [SerializeField]
    private Color wallBuildColor = new Color(1f, 0.55f, 0.1f, 1f);
    [SerializeField]
    private Color spawnTowerColor = Color.green;
    [SerializeField]
    private Color combineColor = Color.blue;
    [SerializeField]
    private Color sellColor = Color.red;
    [SerializeField]
    private float lineWidth = 0.06f;
    [SerializeField]
    private float inset = 0.04f;

    private LineRenderer line;
    private Tilemap walkableMap;
    private Tilemap wallMap;
    private IBuildModeState buildModeState;
    private Vector3Int lastCell = new Vector3Int(int.MinValue, 0, 0);
    private bool visible;

    public static GridHoverOverlay EnsureExists()
    {
        GridHoverOverlay existing = FindFirstObjectByType<GridHoverOverlay>();
        if (existing != null)
        {
            return existing;
        }

        GameObject go = new GameObject("GridHoverOverlay");
        return go.AddComponent<GridHoverOverlay>();
    }

    private void Awake()
    {
        line = gameObject.AddComponent<LineRenderer>();
        line.loop = true;
        line.positionCount = 4;
        line.useWorldSpace = true;
        line.numCapVertices = 0;
        line.numCornerVertices = 0;
        line.sortingOrder = 50;
        line.widthMultiplier = 1f;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.sharedMaterial = CreateLineMaterial();
        line.enabled = false;
    }

    private void Start()
    {
        BindMaps();
        ResolveBuildModeState();
    }

    private void BindMaps()
    {
        if (MapDirector.Instance == null)
        {
            return;
        }

        walkableMap = MapDirector.Instance.WalkableMap;
        wallMap = MapDirector.Instance.WallMap;
    }

    private void ResolveBuildModeState()
    {
        if (buildModeState != null)
        {
            return;
        }

        if (ServiceLocator.TryGet(out IBuildModeState state))
        {
            buildModeState = state;
            return;
        }

        buildModeState = BuildModeController.Instance;
    }

    private void LateUpdate()
    {
        if (walkableMap == null)
        {
            BindMaps();
            if (walkableMap == null)
            {
                SetVisible(false);
                return;
            }
        }

        if (Camera.main == null)
        {
            SetVisible(false);
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            SetVisible(false);
            return;
        }

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        Vector3Int cell = walkableMap.WorldToCell(mouseWorld);
        ResolveBuildModeState();

        BuildMode mode = buildModeState != null
            ? buildModeState.CurrentMode
            : BuildMode.None;

        bool show;
        if (mode == BuildMode.PlaceWall)
        {
            // 건설 가능 칸만 (빈 walkable)
            show = walkableMap.HasTile(cell);
        }
        else
        {
            // 필드 노드(이동 가능 또는 이미 벽)
            show = walkableMap.HasTile(cell) || (wallMap != null && wallMap.HasTile(cell));
        }

        if (!show)
        {
            SetVisible(false);
            return;
        }

        Color color = GetModeColor(mode);
        if (!visible || cell != lastCell || line.startColor != color)
        {
            DrawCell(cell, color);
            lastCell = cell;
        }

        SetVisible(true);
    }

    private Color GetModeColor(BuildMode mode)
    {
        switch (mode)
        {
            case BuildMode.PlaceWall:
                return wallBuildColor;
            case BuildMode.SpawnTower:
                return spawnTowerColor;
            case BuildMode.Combine:
                return combineColor;
            case BuildMode.Sell:
                return sellColor;
            default:
                return viewColor;
        }
    }

    private void DrawCell(Vector3Int cell, Color color)
    {
        Vector3 center = walkableMap.GetCellCenterWorld(cell);
        center -= walkableMap.cellGap / 2f;
        center.z = 0f;

        Vector3 size = walkableMap.cellSize;
        float halfX = size.x * 0.5f - inset;
        float halfY = size.y * 0.5f - inset;

        line.SetPosition(0, center + new Vector3(-halfX, -halfY, 0f));
        line.SetPosition(1, center + new Vector3(-halfX, halfY, 0f));
        line.SetPosition(2, center + new Vector3(halfX, halfY, 0f));
        line.SetPosition(3, center + new Vector3(halfX, -halfY, 0f));

        line.startColor = color;
        line.endColor = color;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
    }

    private void SetVisible(bool on)
    {
        if (visible == on && line.enabled == on)
        {
            return;
        }

        visible = on;
        line.enabled = on;
        if (!on)
        {
            lastCell = new Vector3Int(int.MinValue, 0, 0);
        }
    }

    private static Material CreateLineMaterial()
    {
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        var mat = new Material(shader);
        mat.color = Color.white;
        return mat;
    }
}
