using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyDestroyType { Kill = 0, Arrive }

public class Enemy : MonoBehaviour
{
    public List<AStarNode> enemyPath;

    private float LastPathUpdate;
    private EnemySpawner enemySpawner;
    private Transform canvasTransform;

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

    /// <summary>????? ???? ?? ???? ???? (Start ???).</summary>
    public void PrepareForSpawn(EnemySpawner spawner)
    {
        StopAllCoroutines();
        SetUp(spawner);

        if (enemyPath == null)
        {
            enemyPath = new List<AStarNode>();
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

        LastPathUpdate = Time.time;
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
        foreach (var node in enemyPath)
        {
            if (node == enemyPath[0])
            {
                continue;
            }

            Vector2 targetPositon = new Vector2(node.xPos, node.yPos);
            Vector2 currentPosition = transform.position;
            currentTime = Time.time;

            while (true)
            {
                Vector3 move = (targetPositon - currentPosition).normalized * Time.deltaTime;
                transform.position += move;

                float u = (Time.time - currentTime) / nextNodeMoveTime;

                transform.position = Vector3.Lerp(currentPosition, targetPositon, u);

                if (Vector2.Distance(transform.position, targetPositon) < nodeArriveDistance)
                {
                    break;
                }

                yield return new WaitForEndOfFrame();
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
