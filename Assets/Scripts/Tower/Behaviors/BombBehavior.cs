using System.Collections;
using UnityEngine;

public sealed class BombBehavior : AttackBehaviorBase
{
    protected override IEnumerator AttackLoop()
    {
        while (true)
        {
            if (!Tower.IsPossibleToAttackTarget())
            {
                yield break;
            }

            GameObject clone = Tower.SpawnPooled(
                Tower.BombProjectilePrefab,
                Tower.SpawnPoint.position,
                Quaternion.identity);

            if (clone != null)
            {
                clone.GetComponent<BombProjectile>().Setup(Tower.AttackTarget, Tower.damage);
            }

            yield return new WaitForSeconds(Tower.rate);
        }
    }
}