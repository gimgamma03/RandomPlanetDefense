using System.Collections;
using UnityEngine;

public sealed class MultiShotBehavior : AttackBehaviorBase
{
    protected override IEnumerator AttackLoop()
    {
        while (true)
        {
            if (!Tower.IsPossibleToAttackTarget())
            {
                yield break;
            }

            Vector3 aim = Tower.AttackTarget.position;
            SpawnFan(aim);

            if (Tower.DoubleShot)
            {
                yield return new WaitForSeconds(0.2f);
                SpawnFan(aim);
            }

            yield return new WaitForSeconds(Tower.rate);
        }
    }

    private void SpawnFan(Vector3 attackTargetPosition)
    {
        Transform spawn = Tower.SpawnPoint;
        if (spawn == null || Tower.ProjectilePrefab == null)
        {
            return;
        }

        Vector3[] directions = new Vector3[3];
        directions[0] = attackTargetPosition - Tower.transform.position;
        directions[1] = Quaternion.AngleAxis(45f, Vector3.forward) * directions[0];
        directions[2] = Quaternion.AngleAxis(-45f, Vector3.forward) * directions[0];

        for (int i = 0; i < directions.Length; i++)
        {
            GameObject clone = Tower.SpawnPooled(Tower.ProjectilePrefab, spawn.position, Quaternion.identity);
            if (clone != null)
            {
                clone.GetComponent<Projectile>().Setup(directions[i], Tower.damage);
            }
        }
    }
}