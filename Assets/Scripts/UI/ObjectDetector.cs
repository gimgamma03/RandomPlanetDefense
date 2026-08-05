using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 빌드 모드가 아닐 때만 타워 클릭 → 정보 패널.
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
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // 빌드 모드 중에는 타워 정보/사거리 클릭 무시
        ResolveBuildModeState();
        if (buildModeState != null && buildModeState.HasActiveMode)
        {
            if (Input.GetMouseButtonDown(1))
            {
                HideAttackRange();
            }

            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            Vector2 rayPoint = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(rayPoint, Vector2.zero);
            if (hit.transform == null)
            {
                return;
            }

            if (!hit.transform.CompareTag("Tower"))
            {
                return;
            }

            TowerWeapon towerWeapon = hit.transform.GetComponent<TowerWeapon>();
            if (towerWeapon == null || towerAttackRange == null || towerDataViewer == null)
            {
                return;
            }

            towerAttackRange.gameObject.SetActive(true);
            towerAttackRange.OnAttackRange(hit.transform.position, towerWeapon.range);
            towerDataViewer.OnPanel(hit.transform);
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
}
