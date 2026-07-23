using System.Collections;
using UnityEngine;

public sealed class LaserBehavior : AttackBehaviorBase
{
    protected override IEnumerator AttackLoop()
    {
        EnableLaser(true);

        while (true)
        {
            if (!Tower.IsPossibleToAttackTarget())
            {
                EnableLaser(false);
                yield return new WaitForSeconds(Tower.rate);
                yield break;
            }

            SpawnLaser();
            yield return null;
        }
    }

    protected override void OnAttackStopped()
    {
        EnableLaser(false);
    }

    private void EnableLaser(bool enabled)
    {
        LineRenderer line = Tower.LineRenderer;
        if (line != null)
        {
            line.gameObject.SetActive(enabled);
        }
    }

    private void SpawnLaser()
    {
        Transform target = Tower.AttackTarget;
        Transform spawn = Tower.SpawnPoint;
        LineRenderer line = Tower.LineRenderer;
        if (target == null || spawn == null || line == null)
        {
            return;
        }

        Vector3 direction = target.position - spawn.position;
        RaycastHit2D[] hits = Physics2D.RaycastAll(spawn.position, direction, Tower.range);

        for (int i = 0; i < hits.Length; ++i)
        {
            if (hits[i].transform != target)
            {
                continue;
            }

            line.SetPosition(0, spawn.position);
            line.SetPosition(1, new Vector3(hits[i].point.x, hits[i].point.y, 0f) + Vector3.back);

            EnemyHp hp = target.GetComponent<EnemyHp>();
            if (hp != null)
            {
                hp.TakeDamage(Tower.damage * Time.deltaTime);
            }

            break;
        }
    }
}