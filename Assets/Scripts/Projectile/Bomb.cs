using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    [SerializeField]
    private float armTime = 0.22f;

    [SerializeField]
    private ParticleSystem explodeParticle;

    [SerializeField]
    [Min(1)]
    private int aoeDamagePerFrame = AoEDamageBatch.DefaultPerFrame;

    private readonly List<EnemyHp> aoeTargets = new List<EnemyHp>(32);

    private float currentTime;
    private float damage;
    private bool isExplode;
    private bool hasDamaged;
    private CircleCollider2D circleCollider;
    private Coroutine damageRoutine;

    private void Awake()
    {
        circleCollider = GetComponent<CircleCollider2D>();
    }

    public void SetUp(float damage)
    {
        CancelInvoke();
        StopAllCoroutines();
        damageRoutine = null;

        this.damage = damage;
        currentTime = Time.time;
        isExplode = false;
        hasDamaged = false;
        transform.localScale = Vector3.zero;
        aoeTargets.Clear();

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        if (explodeParticle != null)
        {
            explodeParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void Update()
    {
        if (isExplode)
        {
            return;
        }

        float u = (Time.time - currentTime) / armTime;
        transform.localScale = Vector3.one * Mathf.Clamp01(u);

        if (u >= 1f)
        {
            Explode();
        }
    }

    private void Explode()
    {
        isExplode = true;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        if (explodeParticle != null)
        {
            explodeParticle.Play();
        }

        BeginAoEDamage();
        StartCoroutine(ReleaseAfterVfx());
    }

    private void BeginAoEDamage()
    {
        if (hasDamaged)
        {
            return;
        }

        hasDamaged = true;

        float radius = 0.5f;
        if (circleCollider != null)
        {
            Vector3 scale = transform.lossyScale;
            float scaleMax = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
            radius = circleCollider.radius * scaleMax;
        }

        AoEDamageBatch.CollectEnemyTargets(transform.position, radius, aoeTargets);
        damageRoutine = StartCoroutine(
            AoEDamageBatch.ApplyOverFrames(aoeTargets, damage, aoeDamagePerFrame));
    }

    private IEnumerator ReleaseAfterVfx()
    {
        yield return new WaitForSeconds(0.45f);
        if (damageRoutine != null)
        {
            yield return damageRoutine;
            damageRoutine = null;
        }

        Release();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 폭발 직후 범위 안으로 들어오는 적 — 스냅샷 분산 적용이 시작된 뒤에는 중복 방지
        if (!isExplode || hasDamaged)
        {
            return;
        }

        if (!collision.CompareTag("Enemy"))
        {
            return;
        }

        EnemyHp enemyHp = collision.GetComponent<EnemyHp>();
        if (enemyHp != null)
        {
            enemyHp.TakeDamage(damage);
        }
    }

    private void Release()
    {
        CancelInvoke();
        StopAllCoroutines();
        damageRoutine = null;
        isExplode = false;
        hasDamaged = false;
        aoeTargets.Clear();
        ProjectileLifecycle.ReturnToPool(gameObject);
    }
}
