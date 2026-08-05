using System.Collections;
using UnityEngine;

public class EnemyHp : MonoBehaviour
{
    /// <summary>레이저 등 틱 피해 플로팅 숫자 합산 창.</summary>
    public const float DamagePopupAggregateSeconds = 0.5f;

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

    private float pendingPopupBody;
    private float pendingPopupShield;
    private Coroutine aggregatePopupRoutine;
    private Coroutine hitFlashRoutine;
    private float nextHitFlashAllowedTime;
    private Vector3 baseLocalScale = Vector3.one;
    private static readonly WaitForSeconds AggregateWait =
        new WaitForSeconds(DamagePopupAggregateSeconds);

    /// <summary>피격: 스케일 펀치만 (색/알파는 건드리지 않음).</summary>
    private const float HitFlashDuration = 0.1f;
    private const float HitFlashPunchScale = 1.15f;
    private const float HitFlashCooldown = 0.5f;

    public void SetUp(EnemyHpViewer enemyHpViewer)
    {
        this.enemyHpViewer = enemyHpViewer;
    }

    public void PrepareForSpawn(EnemyHpViewer viewer)
    {
        StopAllCoroutines();
        aggregatePopupRoutine = null;
        hitFlashRoutine = null;
        nextHitFlashAllowedTime = 0f;
        pendingPopupBody = 0f;
        pendingPopupShield = 0f;

        enemy = GetComponent<Enemy>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        maxHp = enemy != null && enemy.enemyData != null ? enemy.enemyData.maxHp : maxHp;
        currentHp = maxHp;
        currentShield = enemy != null && enemy.enemyData != null && enemy.enemyData.HasShield
            ? enemy.enemyData.shieldHp
            : 0f;
        isDie = false;
        baseLocalScale = transform.localScale;
        if (baseLocalScale.sqrMagnitude < 0.0001f)
        {
            baseLocalScale = Vector3.one;
        }

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
        aggregatePopupRoutine = null;
        hitFlashRoutine = null;
        nextHitFlashAllowedTime = 0f;
        pendingPopupBody = 0f;
        pendingPopupShield = 0f;
        isDie = true;
        currentShield = 0f;
        transform.localScale = baseLocalScale;
        if (spriteRenderer != null)
        {
            Color c = baseSpriteColor;
            c.a = 1f;
            spriteRenderer.color = c;
        }
        if (shieldVisual != null)
        {
            shieldVisual.Clear();
        }

        enemyHpViewer = null;
    }

    /// <param name="aggregatePopup">
    /// true면 플로팅 숫자를 바로 띄우지 않고 <see cref="DamagePopupAggregateSeconds"/> 동안 합산.
    /// 레이저처럼 매 프레임 들어가는 피해용.
    /// </param>
    public void TakeDamage(float damage, bool aggregatePopup = false)
    {
        if (isDie || !isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            return;
        }

        float remaining = damage;
        float shieldHit = 0f;
        float bodyHit = 0f;
        bool hadShield = currentShield > 0f;

        if (currentShield > 0f)
        {
            float absorbed = Mathf.Min(currentShield, remaining);
            currentShield -= absorbed;
            remaining -= absorbed;
            shieldHit = absorbed;

            if (currentShield <= 0f)
            {
                currentShield = 0f;
                RefreshShieldVisual();
            }
        }

        if (remaining > 0f)
        {
            currentHp -= remaining;
            bodyHit = remaining;
        }

        ReportDamagePopup(shieldHit, bodyHit, aggregatePopup);
        TryPlayHitFlash();

        if (enemyHpViewer != null)
        {
            enemyHpViewer.hpSliderUpdate();
        }

        if (currentHp <= 0f)
        {
            isDie = true;
            FlushPendingDamagePopups();
            RestoreHitVisual();
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

    private void TryPlayHitFlash()
    {
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            return;
        }

        if (Time.time < nextHitFlashAllowedTime)
        {
            return;
        }

        nextHitFlashAllowedTime = Time.time + HitFlashCooldown;

        if (hitFlashRoutine != null)
        {
            StopCoroutine(hitFlashRoutine);
            hitFlashRoutine = null;
        }

        hitFlashRoutine = StartCoroutine(HitScalePunch());
    }

    private void RestoreHitVisual()
    {
        if (hitFlashRoutine != null)
        {
            StopCoroutine(hitFlashRoutine);
            hitFlashRoutine = null;
        }

        transform.localScale = baseLocalScale;
    }

    private void ReportDamagePopup(float shieldHit, float bodyHit, bool aggregatePopup)
    {
        if (shieldHit < 0.05f && bodyHit < 0.05f)
        {
            return;
        }

        if (!aggregatePopup)
        {
            Vector3 pos = transform.position;
            if (shieldHit >= 0.05f)
            {
                DamagePopupSpawner.ShowShield(pos, shieldHit);
            }

            if (bodyHit >= 0.05f)
            {
                DamagePopupSpawner.ShowBody(pos + Vector3.up * 0.15f, bodyHit);
            }

            return;
        }

        pendingPopupShield += shieldHit;
        pendingPopupBody += bodyHit;
        if (aggregatePopupRoutine == null && isActiveAndEnabled)
        {
            aggregatePopupRoutine = StartCoroutine(AggregateDamagePopupRoutine());
        }
    }

    private IEnumerator AggregateDamagePopupRoutine()
    {
        yield return AggregateWait;
        aggregatePopupRoutine = null;
        FlushPendingDamagePopups();
    }

    private void FlushPendingDamagePopups()
    {
        if (aggregatePopupRoutine != null)
        {
            StopCoroutine(aggregatePopupRoutine);
            aggregatePopupRoutine = null;
        }

        float shield = pendingPopupShield;
        float body = pendingPopupBody;
        pendingPopupShield = 0f;
        pendingPopupBody = 0f;

        Vector3 pos = transform.position;
        if (shield >= 0.05f)
        {
            DamagePopupSpawner.ShowShield(pos, shield);
        }

        if (body >= 0.05f)
        {
            DamagePopupSpawner.ShowBody(pos + Vector3.up * 0.15f, body);
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

    private IEnumerator HitScalePunch()
    {
        Vector3 punchScale = baseLocalScale * HitFlashPunchScale;
        transform.localScale = punchScale;

        float elapsed = 0f;
        while (elapsed < HitFlashDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / HitFlashDuration);
            float ease = t * t;
            transform.localScale = Vector3.Lerp(punchScale, baseLocalScale, ease);
            yield return null;
        }

        transform.localScale = baseLocalScale;
        hitFlashRoutine = null;
    }
}
