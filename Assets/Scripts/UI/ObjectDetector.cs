using UnityEngine;

/// <summary>
/// 타워 클릭 → 정보 패널. 합치기/판매 모드 중에는 열지 않는다.
/// 좌클릭 확정(설치/머지/판매/벽)은 BuildModeController가 담당.
/// </summary>
public class ObjectDetector : MonoBehaviour
{
    [SerializeField]
    private PanelTowerDataViewer towerDataViewer;

    [SerializeField]
    private TowerAttackRange towerAttackRangePrefab;

    private TowerAttackRange towerAttackRange;
    private Camera mainCamera;
    private IBuildModeState buildModeState;

    private void Awake()
    {
        mainCamera = Camera.main;
        ResolveBuildModeState();
        EnsureAttackRange();
    }

    private void EnsureAttackRange()
    {
        if (towerAttackRange != null)
        {
            return;
        }

        if (towerAttackRangePrefab == null)
        {
            Debug.LogWarning("[ObjectDetector] towerAttackRangePrefab 미할당.");
            return;
        }

        towerAttackRange = Instantiate(towerAttackRangePrefab);
        towerAttackRange.name = "TowerAttackRange";
        towerAttackRange.gameObject.SetActive(false);
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

    private void Update()
    {
        if (PointerInput.IsOverUI())
        {
            return;
        }

        // 합치기/판매 모드만 타워 정보 클릭을 막는다. 소환·벽 모드에서 타워를 누르면 정보를 연다.
        ResolveBuildModeState();
        if (buildModeState != null)
        {
            BuildMode mode = buildModeState.CurrentMode;
            if (mode == BuildMode.Combine || mode == BuildMode.Sell)
            {
                if (Input.GetMouseButtonDown(1))
                {
                    HideAttackRange();
                }

                return;
            }
        }

        if (PointerInput.WasPrimaryPressThisFrame())
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            Vector2 rayPoint = mainCamera.ScreenToWorldPoint(PointerInput.ScreenPosition());
            Transform tower = FindTowerAt(rayPoint);
            if (tower == null)
            {
                return;
            }

            TowerWeapon towerWeapon = tower.GetComponent<TowerWeapon>();
            if (towerWeapon == null || towerDataViewer == null)
            {
                return;
            }

            if (towerAttackRange != null)
            {
                towerAttackRange.gameObject.SetActive(true);
                towerAttackRange.OnAttackRange(tower.position, towerWeapon.range);
            }

            towerDataViewer.OnPanel(tower);
        }
        else if (Input.GetMouseButtonDown(1))
        {
            HideAttackRange();
        }
    }

    private void HideAttackRange()
    {
        if (towerAttackRange != null)
        {
            towerAttackRange.gameObject.SetActive(false);
        }

        if (towerDataViewer != null)
        {
            towerDataViewer.OffPanel();
        }
    }

    private static Transform FindTowerAt(Vector2 worldPoint)
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(worldPoint);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] != null && hits[i].CompareTag("Tower"))
            {
                return hits[i].transform;
            }
        }

        return null;
    }
}
