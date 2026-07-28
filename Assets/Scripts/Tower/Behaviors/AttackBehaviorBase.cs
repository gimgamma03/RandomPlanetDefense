using System.Collections;
using UnityEngine;

/// <summary>
/// 탐색 → 공격 루프를 공유하는 능동 공격 전략 베이스.
/// </summary>
public abstract class AttackBehaviorBase : ITowerBehavior
{
    /// <summary>타겟 없을 때 전수 탐색 간격. 타워마다 스태거로 스파이크 완화.</summary>
    private static readonly WaitForSeconds SearchInterval = new WaitForSeconds(0.05f);

    protected TowerWeapon Tower { get; private set; }

    private Coroutine searchRoutine;
    private Coroutine attackRoutine;

    public virtual void Initialize(TowerWeapon tower)
    {
        Tower = tower;
    }

    public virtual void Activate()
    {
        StopRoutines();
        searchRoutine = Tower.StartCoroutine(SearchTarget());
    }

    public virtual void Deactivate()
    {
        StopRoutines();
        OnAttackStopped();
    }

    public virtual void OnUpgraded() { }

    protected abstract IEnumerator AttackLoop();

    protected virtual void OnAttackStopped() { }

    private IEnumerator SearchTarget()
    {
        // 인스턴스마다 시작 프레임을 어긋내 동시 전수 스캔을 분산
        int staggerFrames = Tower.GetInstanceID() & 3;
        for (int i = 0; i < staggerFrames; i++)
        {
            yield return null;
        }

        while (true)
        {
            Transform target = Tower.FindClosestAttackTarget();
            if (target != null)
            {
                if (attackRoutine != null)
                {
                    Tower.StopCoroutine(attackRoutine);
                }

                attackRoutine = Tower.StartCoroutine(AttackThenSearch());
                yield break;
            }

            yield return SearchInterval;
        }
    }

    private IEnumerator AttackThenSearch()
    {
        yield return AttackLoop();
        attackRoutine = null;
        searchRoutine = Tower.StartCoroutine(SearchTarget());
    }

    private void StopRoutines()
    {
        if (searchRoutine != null)
        {
            Tower.StopCoroutine(searchRoutine);
            searchRoutine = null;
        }

        if (attackRoutine != null)
        {
            Tower.StopCoroutine(attackRoutine);
            attackRoutine = null;
        }
    }
}
