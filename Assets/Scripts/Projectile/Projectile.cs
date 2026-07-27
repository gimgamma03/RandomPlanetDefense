using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Rigidbody2D rigidbody2d;
    private Vector3 target;
    private float moveSpeed = 4.5f;
    private float damage;
    private ProjectileVfx vfx;

    private bool pierce;
    private bool despawnWhenOffScreen;
    private float maxTravelDistance;
    private float spinSpeed;
    private Vector3 spawnPosition;
    private readonly HashSet<int> piercedEnemyIds = new HashSet<int>();

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
        pierce = false;
        despawnWhenOffScreen = false;
        piercedEnemyIds.Clear();
        spinSpeed = 0f;
        maxTravelDistance = 0f;
        ApplySetup(target, damage, 3f, enableTrail: true);
    }

    /// <summary>직진 관통 — 적마다 1회 피해, 화면 밖으로 나가면 소멸.</summary>
    public void SetupPierce(Vector3 direction, float damage, float spinSpeed = 360f)
    {
        pierce = true;
        despawnWhenOffScreen = true;
        piercedEnemyIds.Clear();
        this.spinSpeed = spinSpeed;
        maxTravelDistance = 0f;
        spawnPosition = transform.position;

        Vector3 normalized = direction.sqrMagnitude > 0.0001f
            ? direction.normalized
            : Vector3.right;

        // 안전망: 카메라 없을 때 무한 비행 방지
        ApplySetup(normalized, damage, 25f, enableTrail: false);
    }

    private void ApplySetup(Vector3 direction, float damage, float lifetime, bool enableTrail = true)
    {
        CancelInvoke();
        target = direction;
        this.damage = damage;

        if (rigidbody2d != null)
        {
            rigidbody2d.linearVelocity = Vector2.zero;
            rigidbody2d.angularVelocity = 0f;
        }

        ProjectileFacing.FaceDirection(transform, direction);
        AddForceToTarget(direction);

        if (vfx != null)
        {
            vfx.SetTrailEnabled(enableTrail);
            vfx.BeginFlight();
        }

        Invoke(nameof(ReleaseMiss), lifetime);
    }

    private void Update()
    {
        if (!pierce)
        {
            return;
        }

        if (spinSpeed != 0f)
        {
            transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
        }

        if (despawnWhenOffScreen && IsOffCamera())
        {
            ReleaseMiss();
            return;
        }

        if (maxTravelDistance > 0f &&
            Vector3.Distance(spawnPosition, transform.position) >= maxTravelDistance)
        {
            ReleaseMiss();
        }
    }

    private bool IsOffCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return false;
        }

        Vector3 viewport = cam.WorldToViewportPoint(transform.position);
        const float margin = 0.12f;
        return viewport.z < 0f
            || viewport.x < -margin
            || viewport.x > 1f + margin
            || viewport.y < -margin
            || viewport.y > 1f + margin;
    }

    public void AddForceToTarget(Vector3 target)
    {
        if (rigidbody2d == null)
        {
            return;
        }

        Vector2 dir = ((Vector2)target).sqrMagnitude > 0.0001f
            ? ((Vector2)target).normalized
            : Vector2.right;
        rigidbody2d.linearVelocity = dir * moveSpeed;
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
            if (pierce)
            {
                int enemyId = collision.GetInstanceID();
                if (!piercedEnemyIds.Add(enemyId))
                {
                    return;
                }

                enemyHp.TakeDamage(damage);
                return;
            }

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
