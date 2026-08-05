using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

/// <summary>
/// 마우스 아래 그리드 셀 미리보기.
/// 벽 모드: 파란색 반투명 블록(블루프린트).
/// 그 외: 테두리(기본 흰 / 소환 초록 / 조합 파랑 / 판매 빨강).
/// </summary>
public class GridHoverOverlay : MonoBehaviour
{
    [SerializeField]
    private Color viewColor = Color.white;
    [SerializeField]
    private Color wallBlueprintFill = new Color(0.25f, 0.65f, 1f, 0.4f);
    [SerializeField]
    private Color wallBlueprintEdge = new Color(0.45f, 0.85f, 1f, 0.9f);
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
    private SpriteRenderer fill;
    private Tilemap walkableMap;
    private Tilemap wallMap;
    private IBuildModeState buildModeState;
    private Vector3Int lastCell = new Vector3Int(int.MinValue, 0, 0);
    private bool visible;
    private bool wallBlueprintMode;

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
        line.sharedMaterial = CreateUnlitMaterial();
        line.enabled = false;

        GameObject fillGo = new GameObject("BlueprintFill");
        fillGo.transform.SetParent(transform, false);
        fill = fillGo.AddComponent<SpriteRenderer>();
        fill.sprite = CreateWhiteSprite();
        fill.sharedMaterial = CreateUnlitMaterial();
        fill.sortingOrder = 49;
        fill.enabled = false;
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
        }
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

        bool useBlueprint = mode == BuildMode.PlaceWall;
        Color edgeColor = useBlueprint ? wallBlueprintEdge : GetModeColor(mode);
        if (!visible
            || cell != lastCell
            || wallBlueprintMode != useBlueprint
            || line.startColor != edgeColor)
        {
            DrawCell(cell, edgeColor, useBlueprint);
            lastCell = cell;
            wallBlueprintMode = useBlueprint;
        }

        SetVisible(true);
    }

    private Color GetModeColor(BuildMode mode)
    {
        switch (mode)
        {
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

    private void DrawCell(Vector3Int cell, Color edgeColor, bool blueprint)
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

        line.startColor = edgeColor;
        line.endColor = edgeColor;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;

        if (blueprint)
        {
            fill.transform.position = center;
            fill.transform.localScale = new Vector3(
                Mathf.Max(0.01f, size.x - inset * 2f),
                Mathf.Max(0.01f, size.y - inset * 2f),
                1f);
            fill.color = wallBlueprintFill;
        }
    }

    private void SetVisible(bool on)
    {
        bool lineOn = on;
        bool fillOn = on && wallBlueprintMode;

        if (visible == on && line.enabled == lineOn && fill.enabled == fillOn)
        {
            return;
        }

        visible = on;
        line.enabled = lineOn;
        fill.enabled = fillOn;
        if (!on)
        {
            lastCell = new Vector3Int(int.MinValue, 0, 0);
            wallBlueprintMode = false;
        }
    }

    private static Material CreateUnlitMaterial()
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

    private static Sprite CreateWhiteSprite()
    {
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }
}
