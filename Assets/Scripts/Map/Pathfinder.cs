using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pathfinder : MonoBehaviour
{
    private List<Vector3> path;
    private float nodeArriveDistance = 0.1f;
    [SerializeField]
    private float baseNextNodeMoveTime = 0.05f;
    private float nextNodeMoveTime = 0.05f;
    private float currentTime;
    private TrailRenderer trailRenderer;

    void Start()
    {
        path = new List<Vector3>();
        trailRenderer = GetComponent<TrailRenderer>();
        currentTime = Time.time;
        SetPath();
        StartCoroutine(MoveToPath());
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

        foreach (Vector3 waypoint in path)
        {
            Vector3 targetPosition = waypoint;
            Vector3 currentPosition = transform.position;
            currentTime = Time.time;

            while (true)
            {
                float u = (Time.time - currentTime) / nextNodeMoveTime;
                transform.position = Vector3.Lerp(currentPosition, targetPosition, u);

                if (Vector2.Distance(transform.position, targetPosition) < nodeArriveDistance)
                {
                    break;
                }

                yield return null;
            }
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
