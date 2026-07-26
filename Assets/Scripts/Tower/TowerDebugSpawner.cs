#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 개발용 지정 타워 스폰 패널.
/// F8로 열고 TowerData 선택 후, 패널 밖에서 좌클릭하면 WallMap에 배치한다.
/// </summary>
[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
public sealed class TowerDebugSpawner : MonoBehaviour
{
    private const int WindowId = 82731;
    private const float WindowWidth = 390f;
    private const float WindowHeight = 520f;

    private TowerSpawner towerSpawner;
    private TowerData[] towers = Array.Empty<TowerData>();
    private TowerData selected;
    private Vector2 scroll;
    private bool visible;
    private Rect windowRect = new Rect(20f, 20f, WindowWidth, WindowHeight);

    private void Awake()
    {
        towerSpawner = GetComponent<TowerSpawner>();
        ReloadTowerData();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F8))
        {
            visible = !visible;
        }

        if (!visible || selected == null || towerSpawner == null)
        {
            return;
        }

        if (!Input.GetMouseButtonDown(0))
        {
            return;
        }

        Vector2 guiMouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        if (windowRect.Contains(guiMouse))
        {
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        Vector2 worldPosition = camera.ScreenToWorldPoint(Input.mousePosition);
        int layerMask = ~(1 << LayerMask.NameToLayer("NonRayLayer"));
        RaycastHit2D hit = Physics2D.Raycast(worldPosition, Vector2.zero, Mathf.Infinity, layerMask);
        if (hit.transform == null || !hit.transform.CompareTag("WallMap"))
        {
            Debug.Log("[TowerDebug] WallMap 셀을 클릭하세요.");
            return;
        }

        if (PanelGameManager.Instance != null)
        {
            PanelGameManager.Instance.CancelMode();
        }

        towerSpawner.SpawnTower(worldPosition, selected);
    }

    private void OnGUI()
    {
        if (!visible)
        {
            GUI.Label(new Rect(12f, 12f, 280f, 24f), "Tower Debug: F8");
            return;
        }

        windowRect = GUI.Window(WindowId, windowRect, DrawWindow, "Tower Debug Spawner (F8)");
    }

    private void DrawWindow(int id)
    {
        GUILayout.Label("타워 선택 → 패널 밖 좌클릭으로 WallMap에 배치");
        GUILayout.Label("골드 차감 없음 / 실제 TowerData·Base 로딩 경로 사용");

        string selectedText = selected == null
            ? "선택: 없음"
            : $"선택: G{(int)selected.grade} | {selected.weaponType} | {selected.DisplayName}";
        GUILayout.Box(selectedText, GUILayout.ExpandWidth(true));

        if (GUILayout.Button("TowerData 새로고침"))
        {
            ReloadTowerData();
        }

        scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(400f));
        for (int i = 0; i < towers.Length; i++)
        {
            TowerData data = towers[i];
            if (data == null)
            {
                continue;
            }

            string label =
                $"G{(int)data.grade} | {data.weaponType,-16} | {data.DisplayName} ({data.Id})";
            bool wasEnabled = GUI.enabled;
            GUI.enabled = data != selected;
            if (GUILayout.Button(label))
            {
                selected = data;
                if (PanelGameManager.Instance != null)
                {
                    PanelGameManager.Instance.CancelMode();
                }
            }

            GUI.enabled = wasEnabled;
        }

        GUILayout.EndScrollView();
        GUI.DragWindow(new Rect(0f, 0f, WindowWidth, 24f));
    }

    private void ReloadTowerData()
    {
        towers = Resources.LoadAll<TowerData>(TowerCatalog.ResourcesFolder);
        Array.Sort(towers, CompareTowerData);

        if (selected != null && Array.IndexOf(towers, selected) < 0)
        {
            selected = null;
        }
    }

    private static int CompareTowerData(TowerData a, TowerData b)
    {
        if (ReferenceEquals(a, b))
        {
            return 0;
        }

        if (a == null)
        {
            return 1;
        }

        if (b == null)
        {
            return -1;
        }

        int gradeCompare = a.grade.CompareTo(b.grade);
        if (gradeCompare != 0)
        {
            return gradeCompare;
        }

        int typeCompare = a.weaponType.CompareTo(b.weaponType);
        return typeCompare != 0
            ? typeCompare
            : string.Compare(a.DisplayName, b.DisplayName, StringComparison.Ordinal);
    }
}
#endif
