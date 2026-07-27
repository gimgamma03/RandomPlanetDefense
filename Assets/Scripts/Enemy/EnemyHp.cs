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
    private Color baseSpriteColor = Color.white;
    private static readonly Color ShieldTint = new Color(0.55f, 0.85f, 1f, 1f);

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
            ApplyDisplayColor();
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
        isDie = true;
        currentShield = 0f;
        enemyHpViewer = null;
    }

    public void TakeDamage(float damage)
    {
        if (isDie || !isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            return;
        }

        float remaining = damage;

        if (currentShield > 0f)
        {
            float absorbed = Mathf.Min(currentShield, remaining);
            currentShield -= absorbed;
            remaining -= absorbed;

            if (currentShield <= 0f)
            {
                currentShield = 0f;
                ApplyDisplayColor();
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
            if (enemy != null)
            {
                enemy.OnDie(EnemyDestroyType.Kill);
            }
        }
    }

    private void ApplyDisplayColor()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.color = HasActiveShield
            ? Color.Lerp(baseSpriteColor, ShieldTint, 0.55f)
            : baseSpriteColor;
    }

    private IEnumerator HitAlphaAnimation()
    {
        Color color = HasActiveShield
            ? Color.Lerp(baseSpriteColor, ShieldTint, 0.55f)
            : baseSpriteColor;

        color.a = 0.4f;
        spriteRenderer.color = color;

        yield return new WaitForSeconds(0.05f);

        color.a = 1.0f;
        spriteRenderer.color = color;
        ApplyDisplayColor();
    }
}
