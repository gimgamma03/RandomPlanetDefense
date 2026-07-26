using UnityEngine;

public class TargetProjectile : MonoBehaviour
{
    private Transform target;
    private float damage;
    private float moveSpeed = 8.0f;
    private ProjectileVfx vfx;

    private void Awake()
    {
        vfx = GetComponent<ProjectileVfx>();
        if (vfx == null)
        {
            vfx = gameObject.AddComponent<ProjectileVfx>();
        }
    }

    public void Setup(Transform target, float damage)
    {
        this.target = target;
        this.damage = damage;

        if (target != null)
        {
            ProjectileFacing.FacePoint(transform, target.position);
        }

        if (vfx != null)
        {
            vfx.BeginFlight();
        }
    }

    private void Update()
    {
        if (target != null && target.gameObject.activeInHierarchy)
        {
            Vector3 direction = (target.position - transform.position).normalized;
            ProjectileFacing.FaceDirection(transform, direction);
            transform.position += direction * moveSpeed * Time.deltaTime;
        }
        else
        {
            Release(hit: false);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Enemy"))
        {
            return;
        }

        if (collision.transform != target)
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

    private void Release(bool hit)
    {
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

        target = null;
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
