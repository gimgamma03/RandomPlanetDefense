using System.Collections;
using UnityEngine;

/// <summary>
/// 발사체 없이 타워 → 타겟 방향으로 지면 폭탄을 일렬로 설치한다.
/// 개수·사거리·설치 간격은 TowerData가 정한다.
/// </summary>
public sealed class GroundBombLineBehavior : AttackBehaviorBase
{
    protected override IEnumerator AttackLoop()
    {
        while (true)
        {
            if (!Tower.IsPossibleToAttackTarget())
            {
                yield break;
            }

            yield return SpawnBombsAlongLine();
            yield return new WaitForSeconds(Tower.rate);
        }
    }

    private IEnumerator SpawnBombsAlongLine()
    {
        if (Tower.AttackTarget == null || Tower.BombPrefab == null)
        {
            yield break;
        }

        Vector2 towerPosition = Tower.transform.position;
        Vector2 direction =
            ((Vector2)Tower.AttackTarget.position - towerPosition).normalized;

        int bombCount = Tower.GroundBombCount;
        Vector2 lineVector = direction * Tower.GroundBombLineLength;
        float interval = Tower.GroundBombSpawnInterval;

        for (int i = 0; i < bombCount; i++)
        {
            float t = (i + 1) / (float)bombCount;
            Vector2 position = towerPosition + lineVector * t;

            GameObject bomb = Tower.SpawnPooled(Tower.BombPrefab, position, Quaternion.identity);
            if (bomb != null)
            {
                bomb.GetComponent<Bomb>().SetUp(Tower.damage);
            }

            if (interval > 0f)
            {
                yield return new WaitForSeconds(interval);
            }
        }
    }
}
