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

        Vector3 center = attackTargetPosition - Tower.transform.position;
        int count = Tower.MultiShotCount;
        float spread = Tower.MultiShotSpreadAngle;

        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0.5f : (float)i / (count - 1);
            float angle = Mathf.Lerp(-spread, spread, t);
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.forward) * center;

            GameObject clone = Tower.SpawnPooled(Tower.ProjectilePrefab, spawn.position, Quaternion.identity);
            if (clone != null)
            {
                clone.GetComponent<Projectile>().Setup(direction, Tower.damage);
            }
        }
    }
}
