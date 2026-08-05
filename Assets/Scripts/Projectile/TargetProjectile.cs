using UnityEngine;

public class TargetProjectile : MonoBehaviour
{
    private const float Lifetime = 2f;
    private const float MaxTravelDistance = 10f;

    private Transform target;
    private float damage;
    private float moveSpeed = 8.0f;
    private ProjectileVfx vfx;
    private Vector3 spawnPosition;

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
        CancelInvoke();
        this.target = target;
        this.damage = damage;
        spawnPosition = transform.position;

        if (target != null)
        {
            ProjectileFacing.FacePoint(transform, target.position);
        }

        if (vfx != null)
        {
            vfx.BeginFlight();
        }

        Invoke(nameof(ReleaseMiss), Lifetime);
    }

    private void Update()
    {
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            Release(hit: false);
            return;
        }

        Vector3 direction = (target.position - transform.position).normalized;
        ProjectileFacing.FaceDirection(transform, direction);
        transform.position += direction * moveSpeed * Time.deltaTime;

        if (Vector3.Distance(spawnPosition, transform.position) >= MaxTravelDistance)
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

    private void ReleaseMiss()
    {
        Release(hit: false);
    }

    private void Release(bool hit)
    {
        CancelInvoke();
        target = null;
        ProjectileLifecycle.Release(gameObject, vfx, hit);
    }
}
