using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyDestroyType { Kill = 0, Arrive }

public class Enemy : MonoBehaviour
{
    public List<Vector3> enemyPath;

    private EnemySpawner enemySpawner;
    private SpriteRenderer spriteRenderer;
    private EnemyHp cachedHp;

    /// <summary>타겟 탐색 핫패스용. GetComponent 반복 회피.</summary>
    public EnemyHp CachedHp
    {
        get
        {
            if (cachedHp == null)
            {
                cachedHp = GetComponent<EnemyHp>();
            }

            return cachedHp;
        }
    }

    private int gold;
    private int scorePoint;
    private float moveSpeed;
    private float rotateSpeed;

    private float baseNextNodeMoveTime;
    public EnemyData enemyData;

    /// <summary>0=일반 스폰, 1+=분열 잔해(재분열 안 함)</summary>
    private int splitGeneration;

    public float nextNodeMoveTime = 1.0f;
    private float currentTime;
    private float nodeArriveDistance = 0.1f;

    public bool obstructed = false;

    public EnemyRole Role =>
        enemyData != null ? enemyData.enemyRole : EnemyRole.Swarm;

    public bool CanSplitOnKill =>
        enemyData != null
        && enemyData.CanSplit
        && splitGeneration == 0;

    public void SetUp(EnemySpawner enemySpawner)
    {
        this.enemySpawner = enemySpawner;
    }

    /// <summary>스폰 시 EnemyData로 스탯·비주얼을 맞춘다.</summary>
    public void BindDefinition(EnemyData data)
    {
        if (data == null)
        {
            return;
        }

        enemyData = data;
        ApplyVisualFromData();
    }

    private void ApplyVisualFromData()
    {
        if (enemyData == null)
        {
            return;
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            return;
        }

        if (enemyData.sprite != null)
        {
            spriteRenderer.sprite = enemyData.sprite;
        }

        spriteRenderer.color = enemyData.spriteColor;
        float scale = Mathf.Max(0.1f, enemyData.visualScale);
        transform.localScale = Vector3.one * scale;

        ApplyBossVisual();
    }

    private void ApplyBossVisual()
    {
        EnemyBossVisual bossVisual = GetComponent<EnemyBossVisual>();
        if (enemyData == null || !enemyData.isBoss)
        {
            if (bossVisual != null)
            {
                bossVisual.Clear();
            }
            else if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = EnemyBossVisual.DefaultBodySortingOrder;
            }

            return;
        }

        if (bossVisual == null)
        {
            bossVisual = gameObject.AddComponent<EnemyBossVisual>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        bossVisual.Apply(spriteRenderer, enemyData.bossCrownSprite);
    }

    /// <summary>스폰 직전 상태 초기화. BindDefinition 이후 호출.</summary>
    public void PrepareForSpawn(EnemySpawner spawner, int splitGeneration = 0)
    {
        StopAllCoroutines();
        SetUp(spawner);
        this.splitGeneration = Mathf.Max(0, splitGeneration);

        if (enemyPath == null)
        {
            enemyPath = new List<Vector3>();
        }
        else
        {
            enemyPath.Clear();
        }

        if (enemyData == null)
        {
            Debug.LogError($"[Enemy] {name} has no EnemyData. BindDefinition first.");
            return;
        }

        gold = enemyData.gold;
        scorePoint = enemyData.scorePoint;
        moveSpeed = enemyData.moveSpeed;
        rotateSpeed = enemyData.rotateSpeed;

        if (enemyData.isBoss)
        {
            transform.localRotation = Quaternion.identity;
            rotateSpeed = 0f;
        }

        nextNodeMoveTime = 1.0f * (1f / Mathf.Max(0.01f, moveSpeed));
        baseNextNodeMoveTime = nextNodeMoveTime;
        obstructed = false;

        ApplyVisualFromData();
        SetPath();
        BeginBossSummonSkill();
    }

    private void BeginBossSummonSkill()
    {
        EnemyBossSummonSkill skill = GetComponent<EnemyBossSummonSkill>();
        if (enemyData == null || !enemyData.isBoss || !enemyData.enableSummonSkill)
        {
            if (skill != null)
            {
                skill.StopSkill();
            }

            return;
        }

        if (skill == null)
        {
            skill = gameObject.AddComponent<EnemyBossSummonSkill>();
        }

        skill.Begin(this, enemySpawner);
    }

    public void PauseMovementForSkill()
    {
        StopCoroutine("Move");
    }

    public void ResumeMovementAfterSkill()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        SetPath();
    }

    public void ClearForPool()
    {
        StopAllCoroutines();

        EnemyBossSummonSkill skill = GetComponent<EnemyBossSummonSkill>();
        if (skill != null)
        {
            skill.StopSkill();
        }

        TowerChargeGaugeView.Hide(gameObject);

        if (enemyPath != null)
        {
            enemyPath.Clear();
        }

        enemySpawner = null;
        splitGeneration = 0;
        transform.localScale = Vector3.one;

        EnemyBossVisual bossVisual = GetComponent<EnemyBossVisual>();
        if (bossVisual != null)
        {
            bossVisual.Clear();
        }
    }

    void Update()
    {
        // 보스는 왕관 등 방향 있는 실루엣 — 자전하지 않음
        if (enemyData != null && enemyData.isBoss)
        {
            return;
        }

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
