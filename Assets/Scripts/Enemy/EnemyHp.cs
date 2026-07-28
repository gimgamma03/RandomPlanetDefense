using System.Collections;
using UnityEngine;

public class EnemyHp : MonoBehaviour
{
    public float maxHp;
    public float currentHp;
    private float currentShield;
    private bool isDie = false;

    /// <summary>죽었거나 풀에 들어간 상태. 타워 타겟 판정용.</summary>
    public bool IsDead => isDie;

    public bool HasActiveShield => currentShield > 0f;

    private Enemy enemy;
    private SpriteRenderer spriteRenderer;
    private EnemyHpViewer enemyHpViewer;
    private EnemyShieldVisual shieldVisual;
    private Color baseSpriteColor = Color.white;

    public void SetUp(EnemyHpViewer enemyHpViewer)
    {
        this.enemyHpViewer = enemyHpViewer;
    }

    public void PrepareForSpawn(EnemyHpViewer viewer)
    {
        StopAllCoroutines();
        enemy = GetComponent<Enemy>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        maxHp = enemy != null && enemy.enemyData != null ? enemy.enemyData.maxHp : maxHp;
        currentHp = maxHp;
        currentShield = enemy != null && enemy.enemyData != null && enemy.enemyData.HasShield
            ? enemy.enemyData.shieldHp
            : 0f;
        isDie = false;

        if (spriteRenderer != null)
        {
            baseSpriteColor = enemy != null && enemy.enemyData != null
                ? enemy.enemyData.spriteColor
                : spriteRenderer.color;
            baseSpriteColor.a = 1f;
            spriteRenderer.color = baseSpriteColor;
        }

        RefreshShieldVisual();
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
        isDie = true;
        currentShield = 0f;
        if (shieldVisual != null)
        {
            shieldVisual.Clear();
        }

        enemyHpViewer = null;
    }

    public void TakeDamage(float damage)
    {
        if (isDie || !isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            return;
        }

        float remaining = damage;
        bool hadShield = currentShield > 0f;

        if (currentShield > 0f)
        {
            float absorbed = Mathf.Min(currentShield, remaining);
            currentShield -= absorbed;
            remaining -= absorbed;

            if (currentShield <= 0f)
            {
                currentShield = 0f;
                RefreshShieldVisual();
            }
        }

        if (remaining > 0f)
        {
            currentHp -= remaining;
        }

        if (gameObject.activeInHierarchy)
        {
            StopCoroutine("HitAlphaAnimation");
            StartCoroutine("HitAlphaAnimation");
        }

        if (enemyHpViewer != null)
        {
            enemyHpViewer.hpSliderUpdate();
        }

        if (currentHp <= 0f)
        {
            isDie = true;
            if (shieldVisual != null)
            {
                shieldVisual.Clear();
            }

            if (enemy != null)
            {
                enemy.OnDie(EnemyDestroyType.Kill);
            }
        }
        else if (hadShield && !HasActiveShield)
        {
            RefreshShieldVisual();
        }
    }

    private void RefreshShieldVisual()
    {
        if (shieldVisual == null)
        {
            shieldVisual = GetComponent<EnemyShieldVisual>();
            if (shieldVisual == null)
            {
                shieldVisual = gameObject.AddComponent<EnemyShieldVisual>();
            }
        }

        shieldVisual.Refresh(spriteRenderer, HasActiveShield);
    }

    private IEnumerator HitAlphaAnimation()
    {
        if (spriteRenderer == null)
        {
            yield break;
        }

        Color color = baseSpriteColor;
        color.a = 0.4f;
        spriteRenderer.color = color;

        yield return new WaitForSeconds(0.05f);

        color.a = 1.0f;
        spriteRenderer.color = color;
    }
}
