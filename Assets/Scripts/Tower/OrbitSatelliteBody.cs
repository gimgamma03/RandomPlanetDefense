using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 궤도 위성 접촉 피해. OnTriggerStay로 짧은 간격 틱.
/// </summary>
public sealed class OrbitSatelliteBody : MonoBehaviour
{
    private const float HitInterval = 0.25f;

    private float damage;
    private readonly Dictionary<int, float> lastHitTimes = new Dictionary<int, float>();

    public void Configure(float newDamage)
    {
        damage = newDamage;
        lastHitTimes.Clear();
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision == null || !collision.CompareTag("Enemy"))
        {
            return;
        }

        if (!collision.gameObject.activeInHierarchy)
        {
            return;
        }

        int id = collision.GetInstanceID();
        float now = Time.time;
        if (lastHitTimes.TryGetValue(id, out float lastHit) && now - lastHit < HitInterval)
        {
            return;
        }

        lastHitTimes[id] = now;

        EnemyHp enemyHp = collision.GetComponent<EnemyHp>();
        if (enemyHp != null && !enemyHp.IsDead)
        {
            enemyHp.TakeDamage(damage);
        }
    }
}
