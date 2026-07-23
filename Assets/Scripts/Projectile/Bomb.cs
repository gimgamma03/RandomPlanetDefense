using UnityEngine;

public class Bomb : MonoBehaviour
{
    private float bombTime = 0.5f;
    private float currentTime;
    private float damage;
    private bool isExplode = false;

    [SerializeField]
    private ParticleSystem explodeParticle;

    public void SetUp(float damage)
    {
        CancelInvoke();
        this.damage = damage;
        currentTime = Time.time;
        isExplode = false;
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
        float u = (Time.time - currentTime) / bombTime;
        transform.localScale = new Vector3(u, u, u) * 1.0f;

        if (u >= 1 && !isExplode)
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

            Invoke(nameof(Release), 0.5f);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Enemy"))
        {
            return;
        }

        if (isExplode)
        {
            EnemyHp enemyHp = collision.GetComponent<EnemyHp>();
            if (enemyHp != null)
            {
                enemyHp.TakeDamage(damage);
            }
        }
    }

    private void Release()
    {
        CancelInvoke();
        isExplode = false;

        PooledObject pooled = GetComponent<PooledObject>();
        if (pooled != null)
        {
            pooled.ReturnToPool();
            return;
        }

        if (GameObjectPoolManager.Instance != null)
        {
            GameObjectPoolManager.Instance.Return(gameObject);
            return;
        }

        Destroy(gameObject);
    }
}
