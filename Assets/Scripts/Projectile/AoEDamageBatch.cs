using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AoE 피해를 한 프레임에 몰지 않도록, 대상 스냅샷 후 TakeDamage만 프레임 분산.
/// Overlap은 폭발 순간에 1회, 적용은 코루틴(yield null)으로 이어감.
/// </summary>
public static class AoEDamageBatch
{
    public const int DefaultPerFrame = 8;

    public static void CollectEnemyTargets(Vector2 center, float radius, List<EnemyHp> buffer)
    {
        buffer.Clear();
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, radius);
        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null || !hit.CompareTag("Enemy"))
            {
                continue;
            }

            EnemyHp hp = hit.GetComponent<EnemyHp>();
            if (hp != null)
            {
                buffer.Add(hp);
            }
        }
    }

    public static IEnumerator ApplyOverFrames(
        List<EnemyHp> targets,
        float damage,
        int perFrame = DefaultPerFrame)
    {
        if (targets == null || targets.Count == 0)
        {
            yield break;
        }

        perFrame = Mathf.Max(1, perFrame);
        int index = 0;
        while (index < targets.Count)
        {
            int budget = perFrame;
            while (budget-- > 0 && index < targets.Count)
            {
                EnemyHp hp = targets[index++];
                if (hp != null && hp.isActiveAndEnabled && !hp.IsDead)
                {
                    hp.TakeDamage(damage);
                }
            }

            if (index < targets.Count)
            {
                yield return null;
            }
        }
    }
}
