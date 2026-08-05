#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 개발용 지정 타워 스폰 패널.
/// F8로 열고 TowerData 선택 후, 패널 밖에서 좌클릭하면 WallMap에 배치한다.
/// G1~G5 전부 선택 가능 (패시브 테스트용 G3+ 포함).
/// </summary>
[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
public sealed class TowerDebugSpawner : MonoBehaviour
{
    private const int WindowId = 82731;
    private const float WindowWidth = 420f;
    private const float WindowHeight = 560f;

    /// <summary>0 = 전체, 1~5 = 해당 등급만</summary>
    private int gradeFilter = 3;

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

        if (ServiceLocator.TryGet(out IBuildModeState buildMode))
        {
            buildMode.CancelMode();
        }

        towerSpawner.SpawnTower(worldPosition, selected);
    }

    private void OnGUI()
    {
        if (!visible)
        {
            return;
        }

        windowRect = GUI.Window(WindowId, windowRect, DrawWindow, "Tower Debug Spawner (F8)");
    }

    private void DrawWindow(int id)
    {
        GUILayout.Label("등급 필터 → 타워 선택 → 패널 밖 좌클릭으로 배치");
        GUILayout.Label("골드 없음 / 실제 TowerData·Base·Behavior 경로 (G3+ 패시브 포함)");

        DrawGradeFilter();

        string selectedText = selected == null
            ? "선택: 없음"
            : $"선택: G{(int)selected.grade} | {selected.weaponType} | {selected.DisplayName}";
        GUILayout.Box(selectedText, GUILayout.ExpandWidth(true));

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("TowerData 새로고침"))
        {
            ReloadTowerData();
        }

        if (selected != null && GUILayout.Button("선택 해제", GUILayout.Width(90f)))
        {
            selected = null;
        }

        GUILayout.EndHorizontal();

        scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(400f));
        int shown = 0;
        for (int i = 0; i < towers.Length; i++)
        {
            TowerData data = towers[i];
            if (data == null)
            {
                continue;
            }

            if (gradeFilter > 0 && (int)data.grade != gradeFilter)
            {
                continue;
            }

            shown++;
            string label =
                $"G{(int)data.grade} | {data.weaponType,-16} | {data.DisplayName} ({data.Id})";
            bool wasEnabled = GUI.enabled;
            GUI.enabled = data != selected;
            if (GUILayout.Button(label))
            {
                selected = data;
                if (ServiceLocator.TryGet(out IBuildModeState buildMode))
                {
                    buildMode.CancelMode();
                }
            }

            GUI.enabled = wasEnabled;
        }

        if (shown == 0)
        {
            GUILayout.Label("이 등급에 TowerData가 없습니다.");
        }

        GUILayout.EndScrollView();
        GUI.DragWindow(new Rect(0f, 0f, WindowWidth, 24f));
    }

    private void DrawGradeFilter()
    {
        GUILayout.BeginHorizontal();
        DrawGradeButton(0, "All");
        for (int g = 1; g <= Constants.MaxTowerGrade; g++)
        {
            DrawGradeButton(g, $"G{g}");
        }

        GUILayout.EndHorizontal();
    }

    private void DrawGradeButton(int grade, string label)
    {
        bool active = gradeFilter == grade;
        Color prev = GUI.backgroundColor;
        if (active)
        {
            GUI.backgroundColor = new Color(0.45f, 0.85f, 0.55f);
        }

        if (GUILayout.Button(label, GUILayout.Height(26f)))
        {
            gradeFilter = grade;
            scroll = Vector2.zero;
        }

        GUI.backgroundColor = prev;
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
