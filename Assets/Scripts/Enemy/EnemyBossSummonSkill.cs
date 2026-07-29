using System.Collections;
using UnityEngine;

/// <summary>
/// 보스 스킬: 머리 위 차징 → 그 자리 정지 → 쫄을 간격마다 한 마리씩 소환 → 재이동. 반복.
/// 소환 중 TowerDeco 조각이 킹에게 슈슈슈 빨려 들어간다.
/// </summary>
public sealed class EnemyBossSummonSkill : MonoBehaviour
{
    private Enemy enemy;
    private EnemySpawner spawner;
    private BossIntroFx introFx;
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

        introFx = spawner.GetBossIntroFx();
        loop = StartCoroutine(SkillLoop());
    }

    public void StopSkill()
    {
        if (loop != null)
        {
            StopCoroutine(loop);
            loop = null;
        }

        if (introFx != null)
        {
            introFx.StopGather();
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

            Vector3 spawnPos = transform.position;
            WaitForSeconds spawnWait = new WaitForSeconds(interval);

            // 소환과 동시에 킹으로 데코 집결 (등장보다 약하게)
            if (introFx != null && count > 0)
            {
                float gatherHint = (count - 1) * interval + 0.7f;
                introFx.StartSummonGather(transform, gatherHint);
            }

            for (int i = 0; i < count; i++)
            {
                if (!isActiveAndEnabled || enemy == null || spawner == null)
                {
                    if (introFx != null)
                    {
                        introFx.StopGather();
                    }

                    yield break;
                }

                spawnPos = transform.position;
                spawner.SpawnBossMinion(data.summonMinionType, spawnPos);

                if (i < count - 1)
                {
                    yield return spawnWait;
                    if (!isActiveAndEnabled || enemy == null)
                    {
                        if (introFx != null)
                        {
                            introFx.StopGather();
                        }

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
