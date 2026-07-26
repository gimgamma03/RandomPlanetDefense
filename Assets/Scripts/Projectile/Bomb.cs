using UnityEngine;

public class Bomb : MonoBehaviour
{
    [SerializeField]
    private float armTime = 0.22f;

    [SerializeField]
    private ParticleSystem explodeParticle;

    private float currentTime;
    private float damage;
    private bool isExplode;
    private bool hasDamaged;
    private CircleCollider2D circleCollider;

    private void Awake()
    {
        circleCollider = GetComponent<CircleCollider2D>();
    }

    public void SetUp(float damage)
    {
        CancelInvoke();
        this.damage = damage;
        currentTime = Time.time;
        isExplode = false;
        hasDamaged = false;
        transform.localScale = Vector3.zero;

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

        ApplyAoEDamage();
        Invoke(nameof(Release), 0.45f);
    }

    private void ApplyAoEDamage()
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

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null || !hit.CompareTag("Enemy"))
            {
                continue;
            }

            EnemyHp enemyHp = hit.GetComponent<EnemyHp>();
            if (enemyHp != null)
            {
                enemyHp.TakeDamage(damage);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 폭발 직후 범위 안으로 들어오는 적 (이미 맞춘 폭발은 hasDamaged로 중복 방지)
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
        isExplode = false;
        hasDamaged = false;

        PooledObject pooled = GetComponent<PooledObject>();
        if (pooled != null)
        {
            pooled.ReturnToPool();
            return;
        }

        if (ServiceLocator.TryGet(out IPoolService pool))
        {
            pool.Return(gameObject);
            return;
        }

        Destroy(gameObject);
    }
}
