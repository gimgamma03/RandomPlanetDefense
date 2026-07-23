using System.Collections;
using UnityEngine;

public sealed class MultiBombBehavior : AttackBehaviorBase
{
    protected override IEnumerator AttackLoop()
    {
        while (true)
        {
            if (!Tower.IsPossibleToAttackTarget())
            {
                yield break;
            }

            SpawnBombsAlongLine();
            yield return new WaitForSeconds(Tower.rate);
        }
    }

    private void SpawnBombsAlongLine()
    {
        if (Tower.AttackTarget == null || Tower.BombPrefab == null)
        {
            return;
        }

        Vector2 direction = ((Vector2)Tower.AttackTarget.position - (Vector2)Tower.transform.position).normalized;
        Vector2 towerPosition = Tower.transform.position;
        const float bombRange = 5.0f;
        const int bombCount = 5;
        Vector2 bombVector = direction * bombRange;

        for (int i = 0; i < bombCount; i++)
        {
            Vector2 pos = towerPosition + bombVector * ((i + 1) * (1.0f / bombCount));
            GameObject bomb = Tower.SpawnPooled(Tower.BombPrefab, pos, Quaternion.identity);
            if (bomb != null)
            {
                bomb.GetComponent<Bomb>().SetUp(Tower.damage);
            }
        }
    }
}