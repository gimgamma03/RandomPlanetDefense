using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionDamage : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField]
    private float damage;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision != null && collision.CompareTag("Enemy"))
        {
            EnemyHp enemyHp = collision.GetComponent<EnemyHp>();
            enemyHp.TakeDamage(damage);
        }
    }
}
