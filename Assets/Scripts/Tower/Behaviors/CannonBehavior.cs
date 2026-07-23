using System.Collections;
using UnityEngine;

public sealed class CannonBehavior : AttackBehaviorBase
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
                Tower.TargetProjectilePrefab,
                Tower.SpawnPoint.position,
                Quaternion.identity);

            if (clone != null)
            {
                clone.GetComponent<TargetProjectile>().Setup(Tower.AttackTarget, Tower.damage);
            }

            yield return new WaitForSeconds(Tower.rate);
        }
    }
}