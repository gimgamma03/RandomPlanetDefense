using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pathfinder : MonoBehaviour
{
    private List<Vector3> path;
    [SerializeField]
    private float baseNextNodeMoveTime = 0.05f;
    private float nextNodeMoveTime = 0.05f;
    private TrailRenderer trailRenderer;

    [SerializeField]
    [Tooltip("작을수록 직선 구간도 촘촘. 코너 정점은 AddPosition으로 별도 고정.")]
    private float minVertexDistance = 0.02f;

    void Start()
    {
        path = new List<Vector3>();
        trailRenderer = GetComponent<TrailRenderer>();
        ConfigureTrail();
        SetPath();
        StartCoroutine(MoveToPath());
    }

    private void ConfigureTrail()
    {
        if (trailRenderer == null)
        {
            return;
        }

        // Editor hitch vs Build 60fps 샘플 밀도 차이를 줄이고,
        // 코너 정점은 MoveToPath에서 AddPosition으로 고정한다.
        trailRenderer.minVertexDistance = minVertexDistance;
        trailRenderer.numCornerVertices = Mathf.Max(trailRenderer.numCornerVertices, 5);
    }

    public void ShowPath()
    {
        trailRenderer.Clear();
        trailRenderer.enabled = false;

        transform.position = MapDirector.Instance.GetEnemySpanwerPosition();

        StopAllCoroutines();
        trailRenderer.enabled = true;
        SetPath();
        StartCoroutine(MoveToPath());
    }

    public void SetPath()
    {
        path = MapDirector.Instance.SetPathFromPosition(transform);
        int count = path != null ? path.Count : 0;
        nextNodeMoveTime = PathLengthSpeedScale.ScaleNodeMoveTime(baseNextNodeMoveTime, count);
    }

    public IEnumerator MoveToPath()
    {
        if (path == null || path.Count == 0)
        {
            yield break;
        }

        // 시작점도 트레일 정점으로 고정 (첫 코너와 동일하게).
        ForceTrailVertex(transform.position);

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

            // 코너를 정확히 찍어야 Build에서도 꺾임(전기)이 살아남음.
            // 거리 조기 탈출이면 대각선으로 컷되어 부드럽게 보임.
            transform.position = targetPosition;
            ForceTrailVertex(targetPosition);
        }
    }

    private void ForceTrailVertex(Vector3 worldPos)
    {
        if (trailRenderer == null || !trailRenderer.enabled || !trailRenderer.emitting)
        {
            return;
        }

        trailRenderer.AddPosition(worldPos);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.name == "Goal")
        {
            Invoke(nameof(ShowPath), 1f);
        }
    }
}
