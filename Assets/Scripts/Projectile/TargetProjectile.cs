using UnityEngine;

public class TargetProjectile : MonoBehaviour
{
    private Transform target;
    private float damage;
    private float moveSpeed = 8.0f;

    public void Setup(Transform target, float damage)
    {
        this.target = target;
        this.damage = damage;
    }

    private void Update()
    {
        if (target != null && target.gameObject.activeInHierarchy)
        {
            Vector3 direction = (target.position - transform.position).normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;
        }
        else
        {
            Release();
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

        Release();
    }

    private void Release()
    {
        target = null;
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
