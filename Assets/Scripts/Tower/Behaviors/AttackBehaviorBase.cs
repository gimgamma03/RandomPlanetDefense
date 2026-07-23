using System.Collections;
using UnityEngine;

/// <summary>
/// 탐색 → 공격 루프를 공유하는 능동 공격 전략 베이스.
/// </summary>
public abstract class AttackBehaviorBase : ITowerBehavior
{
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

            yield return null;
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