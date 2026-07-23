using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyDestroyType { Kill = 0, Arrive }

public class Enemy : MonoBehaviour
{
    public List<Vector3> enemyPath;

    private EnemySpawner enemySpawner;

    private int gold;
    private int scorePoint;
    private float moveSpeed;
    private float rotateSpeed;

    private float baseNextNodeMoveTime;
    public EnemyData enemyData;

    public float nextNodeMoveTime = 1.0f;
    private float currentTime;
    private float nodeArriveDistance = 0.1f;

    public bool obstructed = false;

    public void SetUp(EnemySpawner enemySpawner)
    {
        this.enemySpawner = enemySpawner;
    }

    /// <summary>스폰 직전 상태 초기화 (Start 대체).</summary>
    public void PrepareForSpawn(EnemySpawner spawner)
    {
        StopAllCoroutines();
        SetUp(spawner);

        if (enemyPath == null)
        {
            enemyPath = new List<Vector3>();
        }
        else
        {
            enemyPath.Clear();
        }

        gold = enemyData.gold;
        scorePoint = enemyData.scorePoint;
        moveSpeed = enemyData.moveSpeed;
        rotateSpeed = enemyData.rotateSpeed;

        nextNodeMoveTime = 1.0f * (1f / moveSpeed);
        baseNextNodeMoveTime = nextNodeMoveTime;
        obstructed = false;

        SetPath();
    }

    public void ClearForPool()
    {
        StopAllCoroutines();
        if (enemyPath != null)
        {
            enemyPath.Clear();
        }

        enemySpawner = null;
    }

    void Update()
    {
        transform.Rotate(Vector3.forward * -rotateSpeed * (1 - nextNodeMoveTime));
    }

    public int GetGold()
    {
        return gold;
    }

    public int GetScorePoint()
    {
        return scorePoint;
    }

    public void SetPath()
    {
        StopCoroutine("Move");
        enemyPath = MapDirector.Instance.SetPathFromPosition(transform);
        StartCoroutine("Move");
    }

    public IEnumerator Move()
    {
        if (enemyPath == null || enemyPath.Count == 0)
        {
            yield break;
        }

        for (int i = 0; i < enemyPath.Count; i++)
        {
            // 첫 노드는 현재 셀인 경우가 많아 스킵
            if (i == 0)
            {
                continue;
            }

            Vector3 targetPosition = enemyPath[i];
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

    public void ReSetSpeed()
    {
        nextNodeMoveTime = baseNextNodeMoveTime;
    }

    public void OnDie(EnemyDestroyType type)
    {
        if (enemySpawner == null)
        {
            return;
        }

        enemySpawner.DestroyEnemy(type, this);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (enemySpawner == null || !isActiveAndEnabled)
        {
            return;
        }

        if (collision.gameObject.name == "Goal")
        {
            enemySpawner.DestroyEnemy(EnemyDestroyType.Arrive, this);
        }
    }
}