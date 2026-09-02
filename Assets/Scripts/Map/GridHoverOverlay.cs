using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 마우스 아래 그리드 셀 미리보기.
/// 벽 설치: 파란색 반투명 블록. 벽 해체: 빨간색 반투명 블록.
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
    private Color wallRemoveFill = new Color(0.85f, 0.2f, 0.2f, 0.4f);
    [SerializeField]
    private Color wallRemoveEdge = new Color(1f, 0.35f, 0.35f, 0.9f);
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
    private bool wallFillMode;

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

    private static bool IsPointerActive()
    {
        if (Application.isEditor)
        {
            return true;
        }

        if (Input.touchCount <= 0)
        {
            return false;
        }

        TouchPhase phase = Input.GetTouch(0).phase;
        return phase != TouchPhase.Ended && phase != TouchPhase.Canceled;
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

        if (!IsPointerActive())
        {
            SetVisible(false);
            return;
        }

        if (PointerInput.IsOverUI())
        {
            SetVisible(false);
            return;
        }

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(PointerInput.ScreenPosition());
        mouseWorld.z = 0f;
        Vector3Int cell = walkableMap.WorldToCell(mouseWorld);
        ResolveBuildModeState();

        BuildMode mode = buildModeState != null
            ? buildModeState.CurrentMode
            : BuildMode.None;

        bool show;
        if (mode == BuildMode.PlaceWall)
        {
            show = walkableMap.HasTile(cell);
        }
        else if (mode == BuildMode.RemoveWall)
        {
            show = wallMap != null && wallMap.HasTile(cell);
        }
        else
        {
            show = walkableMap.HasTile(cell) || (wallMap != null && wallMap.HasTile(cell));
        }

        if (!show)
        {
            SetVisible(false);
            return;
        }

        bool useFill = mode == BuildMode.PlaceWall || mode == BuildMode.RemoveWall;
        Color edgeColor;
        Color fillColor;
        if (mode == BuildMode.PlaceWall)
        {
            edgeColor = wallBlueprintEdge;
            fillColor = wallBlueprintFill;
        }
        else if (mode == BuildMode.RemoveWall)
        {
            edgeColor = wallRemoveEdge;
            fillColor = wallRemoveFill;
        }
        else
        {
            edgeColor = GetModeColor(mode);
            fillColor = wallBlueprintFill;
        }

        if (!visible
            || cell != lastCell
            || wallFillMode != useFill
            || line.startColor != edgeColor
            || fill.color != fillColor)
        {
            DrawCell(cell, edgeColor, fillColor, useFill);
            lastCell = cell;
            wallFillMode = useFill;
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

    private void DrawCell(Vector3Int cell, Color edgeColor, Color fillColor, bool useFill)
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

        if (useFill)
        {
            fill.transform.position = center;
            fill.transform.localScale = new Vector3(
                Mathf.Max(0.01f, size.x - inset * 2f),
                Mathf.Max(0.01f, size.y - inset * 2f),
                1f);
            fill.color = fillColor;
        }
    }

    private void SetVisible(bool on)
    {
        bool lineOn = on;
        bool fillOn = on && wallFillMode;

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
            wallFillMode = false;
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
