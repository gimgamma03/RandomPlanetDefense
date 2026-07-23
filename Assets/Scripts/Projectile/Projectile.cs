using UnityEngine;

public class Projectile : MonoBehaviour
{
    private Rigidbody2D rigidbody2d;
    private Vector3 target;
    private float moveSpeed = 4.5f;
    private float damage;

    private void Awake()
    {
        rigidbody2d = GetComponent<Rigidbody2D>();
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

        AddForceToTarget(this.target);
        Invoke(nameof(Release), 3f);
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

        Release();
    }

    private void Release()
    {
        CancelInvoke();
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

        if (GameObjectPoolManager.Instance != null)
        {
            GameObjectPoolManager.Instance.Return(gameObject);
            return;
        }

        Destroy(gameObject);
    }
}
