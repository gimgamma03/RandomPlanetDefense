using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 빌드/상점 모드와 좌클릭 확정을 한곳에서 처리한다.
/// (구 PanelGameManager — 씬 버튼 UnityEvent 메서드 이름 유지)
/// </summary>
public class PanelGameManager : MonoBehaviour
{
    public static PanelGameManager Instance { get; private set; }

    private const string CreateWallObjectName = "CreateWall";

    [SerializeField]
    private TowerSpawner towerSpawner;

    [SerializeField]
    private GameObject randomTowerSpawnerImage;

    private IPlayerService playerService;
    private BuildMode mode = BuildMode.None;
    private Renderer cursorRenderer;
    private Material cursorMaterial;

    private Button createWallButton;
    private Graphic createWallGraphic;
    private Color createWallNormalColor;
    private Selectable.Transition createWallNormalTransition;

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

        if (createWallButton != null)
        {
            createWallButton.onClick.RemoveListener(OnCreateWallClicked);
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

        BindCreateWallGuide();
        RefreshWallGuideVisual();
    }

    private void BindCreateWallGuide()
    {
        GameObject go = GameObject.Find(CreateWallObjectName);
        if (go == null)
        {
            return;
        }

        createWallButton = go.GetComponent<Button>();
        if (createWallButton == null)
        {
            return;
        }

        createWallGraphic = createWallButton.targetGraphic;
        if (createWallGraphic != null)
        {
            createWallNormalColor = createWallGraphic.color;
        }

        createWallNormalTransition = createWallButton.transition;
        createWallButton.onClick.RemoveListener(OnCreateWallClicked);
        createWallButton.onClick.AddListener(OnCreateWallClicked);
    }

    private void OnCreateWallClicked()
    {
        // 안내 패널 다시 누르면 모드 해제
        if (mode == BuildMode.PlaceWall)
        {
            CancelMode();
            return;
        }

        PlaceWallButton();
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

        if (HasActiveMode && mode != BuildMode.PlaceWall && randomTowerSpawnerImage != null)
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

        RefreshWallGuideVisual();
    }

    private void EnterMode(BuildMode next, Color cursorColor)
    {
        CancelMode();
        mode = next;
        RefreshWallGuideVisual();

        // 벽 모드는 셀 주황 테두리(GridHoverOverlay)로 표시 — 고스트 커서 끔
        if (next == BuildMode.PlaceWall || randomTowerSpawnerImage == null)
        {
            return;
        }

        randomTowerSpawnerImage.SetActive(true);
        if (cursorMaterial != null)
        {
            cursorMaterial.color = cursorColor;
        }
    }

    private void RefreshWallGuideVisual()
    {
        if (createWallButton == null || createWallGraphic == null)
        {
            return;
        }

        bool pressed = mode == BuildMode.PlaceWall;
        if (pressed)
        {
            // ColorTint가 덮어쓰지 않게 잠시 끄고 Pressed 색 고정
            createWallButton.transition = Selectable.Transition.None;
            Color pressedColor = createWallButton.colors.pressedColor;
            pressedColor.a = createWallNormalColor.a;
            createWallGraphic.color = pressedColor;
        }
        else
        {
            createWallButton.transition = createWallNormalTransition;
            createWallGraphic.color = createWallNormalColor;
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
