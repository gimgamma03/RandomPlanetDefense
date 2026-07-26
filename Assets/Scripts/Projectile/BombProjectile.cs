using System.Collections;
using UnityEngine;

public class BombProjectile : MonoBehaviour
{
    private Vector3 target;
    private float damage;
    private float moveSpeed = 8.0f;
    private float bombRadius = 2.0f;
    private float bombLifeTime = 0.5f;
    private float bombStartDistance = 0.1f;
    private bool isExploding = false;

    [SerializeField]
    private ParticleSystem bombParticle;

    public void Setup(Transform target, float damage)
    {
        StopAllCoroutines();
        this.target = target.position;
        this.damage = damage;
        isExploding = false;

        ProjectileFacing.FacePoint(transform, this.target);

        if (bombParticle != null)
        {
            bombParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void Update()
    {
        if (!isExploding)
        {
            Vector3 direction = (target - transform.position).normalized;
            ProjectileFacing.FaceDirection(transform, direction);
            transform.position += direction * moveSpeed * Time.deltaTime;

            if (Vector2.Distance(transform.position, target) < bombStartDistance)
            {
                isExploding = true;
                StartCoroutine(Bomb());
            }
        }
    }

    private IEnumerator Bomb()
    {
        if (bombParticle != null)
        {
            bombParticle.Play();
        }

        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, bombRadius * 0.5f);
        foreach (Collider2D collider in colliders)
        {
            if (collider != null && collider.CompareTag("Enemy"))
            {
                EnemyHp enemyHp = collider.GetComponent<EnemyHp>();
                if (enemyHp != null)
                {
                    enemyHp.TakeDamage(damage);
                }
            }
        }

        yield return new WaitForSeconds(bombLifeTime);
        Release();
    }

    private void Release()
    {
        StopAllCoroutines();
        isExploding = false;

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
