using System.Collections;
using UnityEngine;

/// <summary>
/// 보스 스킬: 머리 위 차징 → 그 자리 정지 → 쫄을 간격마다 한 마리씩 소환 → 재이동. 반복.
/// </summary>
public sealed class EnemyBossSummonSkill : MonoBehaviour
{
    private Enemy enemy;
    private EnemySpawner spawner;
    private Coroutine loop;

    public void Begin(Enemy host, EnemySpawner enemySpawner)
    {
        enemy = host;
        spawner = enemySpawner;
        StopSkill();

        if (enemy == null || enemy.enemyData == null || !enemy.enemyData.isBoss
            || !enemy.enemyData.enableSummonSkill)
        {
            return;
        }

        if (spawner == null)
        {
            return;
        }

        loop = StartCoroutine(SkillLoop());
    }

    public void StopSkill()
    {
        if (loop != null)
        {
            StopCoroutine(loop);
            loop = null;
        }

        TowerChargeGaugeView.Hide(gameObject);
    }

    private IEnumerator SkillLoop()
    {
        while (isActiveAndEnabled && enemy != null && enemy.enemyData != null
               && enemy.enemyData.enableSummonSkill)
        {
            EnemyData data = enemy.enemyData;
            float chargeDuration = Mathf.Max(0.5f, data.summonChargeDuration);
            float castHold = Mathf.Max(0f, data.summonCastHoldDuration);
            float interval = Mathf.Max(0.05f, data.summonInterval);

            TowerChargeGaugeView.ShowAboveSprite(gameObject);
            float elapsed = 0f;
            while (elapsed < chargeDuration)
            {
                if (!isActiveAndEnabled || enemy == null)
                {
                    TowerChargeGaugeView.Hide(gameObject);
                    yield break;
                }

                elapsed += Time.deltaTime;
                TowerChargeGaugeView.SetFill(gameObject, elapsed / chargeDuration);
                yield return null;
            }

            TowerChargeGaugeView.Hide(gameObject);

            enemy.PauseMovementForSkill();

            int min = Mathf.Max(0, data.summonCountMin);
            int max = Mathf.Max(min, data.summonCountMax);
            int count = max > min ? Random.Range(min, max + 1) : min;

            // 같은 자리에서 같은 간격으로 소환 → 동일 속도면 경로 위 간격이 일정해짐
            // (원형 오프셋은 시작 칸이 달라져 간격이 들쭉날쭉해 보였음)
            Vector3 spawnPos = transform.position;
            WaitForSeconds spawnWait = new WaitForSeconds(interval);

            for (int i = 0; i < count; i++)
            {
                if (!isActiveAndEnabled || enemy == null || spawner == null)
                {
                    yield break;
                }

                spawner.SpawnBossMinion(data.summonMinionType, spawnPos);

                if (i < count - 1)
                {
                    yield return spawnWait;
                    if (!isActiveAndEnabled || enemy == null)
                    {
                        yield break;
                    }
                }
            }

            float hold = 0f;
            while (hold < castHold)
            {
                if (!isActiveAndEnabled || enemy == null)
                {
                    yield break;
                }

                hold += Time.deltaTime;
                yield return null;
            }

            if (enemy != null)
            {
                enemy.ResumeMovementAfterSkill();
            }
        }
    }
}
