using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 적 경로를 LineRenderer로 직선 폴리라인 표시. (Trail 번개 느낌 제거)
/// 두께·색·정렬은 인스펙터(ShowPath의 Pathfinder)에서 조절.
/// sortingOrder는 일반 적(1)보다 낮게 두어 적 뒤에 그린다.
/// </summary>
public class Pathfinder : MonoBehaviour
{
    private List<Vector3> path;

    [Header("Move")]
    [SerializeField]
    private float baseNextNodeMoveTime = 0.05f;

    [Header("Line (씬에서 조절)")]
    [SerializeField]
    private LineRenderer lineRenderer;

    [SerializeField]
    private float lineWidth = 0.12f;

    [Tooltip("웨이브 대기/빌드 중 — 찐한 흰색")]
    [SerializeField]
    private Color lineColor = Color.white;

    [Tooltip("웨이브 진행 중 — 조금 연하게")]
    [SerializeField]
    private Color waveLineColor = new Color(1f, 1f, 1f, 0.35f);

    [Tooltip("일반 적 sortingOrder(1)보다 낮아야 적 뒤에 보임")]
    [SerializeField]
    private int sortingOrder = 0;

    private float nextNodeMoveTime = 0.05f;
    private TrailRenderer trailRenderer;
    private bool duringWave;

    void Start()
    {
        path = new List<Vector3>();
        EnsureLineRenderer();
        SyncWaveState(force: true);
        ApplyLineStyle();
        SetPath();
        RebuildLine();
        StartCoroutine(MoveToPath());
    }

    private void LateUpdate()
    {
        SyncWaveState(force: false);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        if (lineRenderer != null)
        {
            ApplyLineStyle();
        }
    }
#endif

    private void SyncWaveState(bool force)
    {
        bool wave = MapDirector.Instance != null && MapDirector.Instance.IsWallPlacementLocked;
        if (!force && wave == duringWave)
        {
            return;
        }

        duringWave = wave;
        ApplyLineStyle();
    }

    private Color CurrentLineColor => duringWave ? waveLineColor : lineColor;

    private void EnsureLineRenderer()
    {
        trailRenderer = GetComponent<TrailRenderer>();
        if (trailRenderer != null)
        {
            trailRenderer.emitting = false;
            trailRenderer.enabled = false;
            trailRenderer.Clear();
        }

        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        lineRenderer.enabled = true;
        lineRenderer.useWorldSpace = true;
        lineRenderer.loop = false;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        lineRenderer.numCornerVertices = 0;
        lineRenderer.numCapVertices = 0;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
        lineRenderer.allowOcclusionWhenDynamic = false;

        if (lineRenderer.sharedMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                lineRenderer.sharedMaterial = new Material(shader);
            }
        }
    }

    private void ApplyLineStyle()
    {
        if (lineRenderer == null)
        {
            return;
        }

        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.sortingOrder = sortingOrder;

        Color c = CurrentLineColor;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(c, 0f),
                new GradientColorKey(c, 1f),
            },
            new[]
            {
                new GradientAlphaKey(c.a, 0f),
                new GradientAlphaKey(c.a, 1f),
            });
        lineRenderer.colorGradient = gradient;
        lineRenderer.startColor = c;
        lineRenderer.endColor = c;
    }

    public void ShowPath()
    {
        StopAllCoroutines();
        CancelInvoke();

        transform.position = MapDirector.Instance.GetEnemySpanwerPosition();
        EnsureLineRenderer();
        SyncWaveState(force: true);
        ApplyLineStyle();
        SetPath();
        RebuildLine();
        StartCoroutine(MoveToPath());
    }

    public void SetPath()
    {
        path = MapDirector.Instance.SetPathFromPosition(transform);
        int count = path != null ? path.Count : 0;
        nextNodeMoveTime = PathLengthSpeedScale.ScaleNodeMoveTime(baseNextNodeMoveTime, count);
    }

    private void RebuildLine()
    {
        if (lineRenderer == null)
        {
            return;
        }

        if (path == null || path.Count == 0)
        {
            lineRenderer.positionCount = 0;
            return;
        }

        // 블랙홀/스폰 월드 좌표는 빼고, A* 맵 노드(셀 중심)만 표시.
        // path[0]이 스폰 셀이면 한 칸 건너뛰어 맵 안 첫 구간부터 시작.
        int startIndex = path.Count > 1 ? 1 : 0;
        int pointCount = path.Count - startIndex;
        lineRenderer.positionCount = pointCount;

        for (int i = 0; i < pointCount; i++)
        {
            Vector3 p = path[startIndex + i];
            p.z = 0f;
            lineRenderer.SetPosition(i, p);
        }
    }

    public IEnumerator MoveToPath()
    {
        if (path == null || path.Count == 0)
        {
            yield break;
        }

        foreach (Vector3 waypoint in path)
        {
            Vector3 targetPosition = waypoint;
            targetPosition.z = transform.position.z;
            Vector3 currentPosition = transform.position;
            float startTime = Time.time;
            float duration = Mathf.Max(0.0001f, nextNodeMoveTime);

            while (true)
            {
                float u = Mathf.Clamp01((Time.time - startTime) / duration);
                transform.position = Vector3.Lerp(currentPosition, targetPosition, u);
                if (u >= 1f)
                {
                    break;
                }

                yield return null;
            }

            transform.position = targetPosition;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.name == "Goal")
        {
            Invoke(nameof(ShowPath), 1f);
        }
    }
}
