using System.Collections;
using UnityEngine;

public class EnemyHp : MonoBehaviour
{
    public float maxHp;
    public float currentHp;
    private bool isDie = false;

    /// <summary>죽었거나 풀에 들어간 상태. 타워 타겟 판정용.</summary>
    public bool IsDead => isDie;

    private Enemy enemy;
    private SpriteRenderer spriteRenderer;
    private EnemyHpViewer enemyHpViewer;

    public void SetUp(EnemyHpViewer enemyHpViewer)
    {
        this.enemyHpViewer = enemyHpViewer;
    }

    public void PrepareForSpawn(EnemyHpViewer viewer)
    {
        StopAllCoroutines();
        enemy = GetComponent<Enemy>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        maxHp = enemy.enemyData.maxHp;
        currentHp = maxHp;
        isDie = false;

        if (spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a = 1f;
            spriteRenderer.color = color;
        }

        SetUp(viewer);
    }

    public void ReleaseViewerToPool()
    {
        if (enemyHpViewer == null)
        {
            return;
        }

        GameObject viewerObject = enemyHpViewer.gameObject;
        enemyHpViewer.ClearForPool();
        enemyHpViewer = null;

        PooledObject pooled = viewerObject.GetComponent<PooledObject>();
        if (pooled != null)
        {
            pooled.ReturnToPool();
        }
        else if (ServiceLocator.TryGet(out IPoolService pool))
        {
            pool.Return(viewerObject);
        }
        else
        {
            Destroy(viewerObject);
        }
    }

    public void ClearForPool()
    {
        StopAllCoroutines();
        // 풀에 있는 동안은 죽은 상태로 유지 (잔여 탄 히트 방지). PrepareForSpawn에서 false.
        isDie = true;
        enemyHpViewer = null;
    }

    public void TakeDamage(float damage)
    {
        // 이미 죽었거나 풀(비활성)에 들어간 적에는 데미지/코루틴 금지
        if (isDie || !isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            return;
        }

        currentHp -= damage;

        if (gameObject.activeInHierarchy)
        {
            StopCoroutine("HitAlphaAnimation");
            StartCoroutine("HitAlphaAnimation");
        }

        if (enemyHpViewer != null)
        {
            enemyHpViewer.hpSliderUpdate();
        }

        if (currentHp <= 0)
        {
            isDie = true;
            if (enemy != null)
            {
                enemy.OnDie(EnemyDestroyType.Kill);
            }
        }
    }

    private IEnumerator HitAlphaAnimation()
    {
        Color color = spriteRenderer.color;

        color.a = 0.4f;
        spriteRenderer.color = color;

        yield return new WaitForSeconds(0.05f);

        color.a = 1.0f;
        spriteRenderer.color = color;
    }
}