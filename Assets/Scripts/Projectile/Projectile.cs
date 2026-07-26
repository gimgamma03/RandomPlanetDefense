using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Rigidbody2D rigidbody2d;
    private Vector3 target;
    private float moveSpeed = 4.5f;
    private float damage;
    private ProjectileVfx vfx;

    private void Awake()
    {
        rigidbody2d = GetComponent<Rigidbody2D>();
        vfx = GetComponent<ProjectileVfx>();
        if (vfx != null)
        {
            return;
        }

        vfx = gameObject.AddComponent<ProjectileVfx>();
    }

    public void Setup(Vector3 target, float damage)
    {
        CancelInvoke();
        this.target = target;
        this.damage = damage;

        if (rigidbody2d != null)
        {
            rigidbody2d.linearVelocity = Vector2.zero;
            rigidbody2d.angularVelocity = 0f;
        }

        ProjectileFacing.FaceDirection(transform, target);
        AddForceToTarget(this.target);

        if (vfx != null)
        {
            vfx.BeginFlight();
        }

        Invoke(nameof(ReleaseMiss), 3f);
    }

    public void AddForceToTarget(Vector3 target)
    {
        rigidbody2d.linearVelocity = target * moveSpeed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == null || !collision.CompareTag("Enemy"))
        {
            return;
        }

        if (!collision.gameObject.activeInHierarchy)
        {
            return;
        }

        EnemyHp enemyHp = collision.GetComponent<EnemyHp>();
        if (enemyHp != null)
        {
            enemyHp.TakeDamage(damage);
        }

        Release(hit: true);
    }

    private void ReleaseMiss()
    {
        Release(hit: false);
    }

    private void Release(bool hit)
    {
        CancelInvoke();

        if (vfx != null)
        {
            if (hit)
            {
                vfx.NotifyHit(transform.position);
            }
            else
            {
                vfx.NotifyMiss();
            }
        }

        if (rigidbody2d != null)
        {
            rigidbody2d.linearVelocity = Vector2.zero;
            rigidbody2d.angularVelocity = 0f;
        }

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
