using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

/// <summary>
/// 마우스 아래 그리드 셀 테두리.
/// 기본: 흰색(현재 보고 있는 노드) / 벽 건설 모드: 주황색(여기에 건설).
/// </summary>
public class GridHoverOverlay : MonoBehaviour
{
    [SerializeField]
    private Color viewColor = Color.white;
    [SerializeField]
    private Color wallBuildColor = new Color(1f, 0.55f, 0.1f, 1f);
    [SerializeField]
    private float lineWidth = 0.06f;
    [SerializeField]
    private float inset = 0.04f;

    private LineRenderer line;
    private Tilemap walkableMap;
    private Tilemap wallMap;
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

        bool wallMode = PanelGameManager.Instance != null
            && PanelGameManager.Instance.CurrentMode == BuildMode.PlaceWall;

        bool show;
        if (wallMode)
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

        Color color = wallMode ? wallBuildColor : viewColor;
        if (!visible || cell != lastCell || line.startColor != color)
        {
            DrawCell(cell, color);
            lastCell = cell;
        }

        SetVisible(true);
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
