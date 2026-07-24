using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 빌드/상점 모드와 좌클릭 확정을 한곳에서 처리한다.
/// (구 PanelGameManager — 씬 버튼 UnityEvent 메서드 이름 유지)
/// </summary>
public class PanelGameManager : MonoBehaviour
{
    public static PanelGameManager Instance { get; private set; }

    [SerializeField]
    private TowerSpawner towerSpawner;

    [SerializeField]
    private GameObject randomTowerSpawnerImage;

    private IPlayerService playerService;
    private BuildMode mode = BuildMode.None;
    private Renderer cursorRenderer;
    private Material cursorMaterial;

    public BuildMode CurrentMode => mode;
    public bool HasActiveMode => mode != BuildMode.None;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Start()
    {
        playerService = ServiceLocator.Get<IPlayerService>();
        if (randomTowerSpawnerImage != null)
        {
            cursorRenderer = randomTowerSpawnerImage.GetComponent<Renderer>();
            if (cursorRenderer != null)
            {
                cursorMaterial = cursorRenderer.material;
            }

            randomTowerSpawnerImage.SetActive(false);
        }
    }

    private void Update()
    {
        if (Camera.main == null)
        {
            return;
        }

        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1))
        {
            CancelMode();
        }

        // Space: 벽 모드 진입 (타워 버튼과 같은 흐름). 이미 벽 모드면 즉시 설치.
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (mode == BuildMode.PlaceWall)
            {
                TryPlaceWall(mouseWorld);
            }
            else
            {
                PlaceWallButton();
            }
        }

        if (HasActiveMode && randomTowerSpawnerImage != null)
        {
            randomTowerSpawnerImage.transform.position = mouseWorld;
        }

        if (!Input.GetMouseButtonDown(0) || !HasActiveMode)
        {
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        HandleModeClick(mouseWorld);
    }

    private void HandleModeClick(Vector2 mouseWorld)
    {
        int layerMask = ~(1 << LayerMask.NameToLayer("NonRayLayer"));
        RaycastHit2D hit = Physics2D.Raycast(mouseWorld, Vector2.zero, Mathf.Infinity, layerMask);

        switch (mode)
        {
            case BuildMode.SpawnTower:
                TrySpawnTower(mouseWorld, hit);
                break;
            case BuildMode.Combine:
                if (hit.transform != null && hit.transform.CompareTag("Tower"))
                {
                    towerSpawner.CombineTower(hit.transform.gameObject);
                }

                break;
            case BuildMode.Sell:
                if (hit.transform != null && hit.transform.CompareTag("Tower"))
                {
                    towerSpawner.CellTower(hit.transform.gameObject);
                }

                break;
            case BuildMode.PlaceWall:
                TryPlaceWall(mouseWorld);
                break;
        }
    }

    private void TrySpawnTower(Vector2 mouseWorld, RaycastHit2D hit)
    {
        if (playerService == null)
        {
            playerService = ServiceLocator.Get<IPlayerService>();
        }

        if (!playerService.TrySpendGold(Constants.spawnRandomTowerGold))
        {
            return;
        }

        if (hit.transform == null || !hit.transform.CompareTag("WallMap"))
        {
            playerService.AddGold(Constants.spawnRandomTowerGold);
            return;
        }

        towerSpawner.SpawnTower(mouseWorld, (TowerGrade)Constants.ShopSpawnGrade);
    }

    private void TryPlaceWall(Vector2 mouseWorld)
    {
        if (MapDirector.Instance == null)
        {
            return;
        }

        MapDirector.Instance.TryPlaceWallAt(mouseWorld);
    }

    public void CancelMode()
    {
        mode = BuildMode.None;
        if (randomTowerSpawnerImage != null)
        {
            randomTowerSpawnerImage.SetActive(false);
        }
    }

    private void EnterMode(BuildMode next, Color cursorColor)
    {
        CancelMode();
        mode = next;
        if (randomTowerSpawnerImage == null)
        {
            return;
        }

        randomTowerSpawnerImage.SetActive(true);
        if (cursorMaterial != null)
        {
            cursorMaterial.color = cursorColor;
        }
    }

    /// <summary>씬 버튼 / Space — 벽 설치 모드 (좌클릭으로 설치, 연속 가능).</summary>
    public void PlaceWallButton()
    {
        EnterMode(BuildMode.PlaceWall, new Color(0.85f, 0.85f, 0.2f, 1f));
    }

    public void RandomTowerSpawnerButton()
    {
        EnterMode(BuildMode.SpawnTower, Color.green);
    }

    public void TowerCombinationButton()
    {
        EnterMode(BuildMode.Combine, Color.blue);
    }

    public void TowerCellButton()
    {
        EnterMode(BuildMode.Sell, Color.red);
    }
}
