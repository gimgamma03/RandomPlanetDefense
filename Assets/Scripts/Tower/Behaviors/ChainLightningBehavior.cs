using System.Collections;
using UnityEngine;

public sealed class ChainLightningBehavior : AttackBehaviorBase
{
    protected override IEnumerator AttackLoop()
    {
        if (!Tower.IsPossibleToAttackTarget())
        {
            yield break;
        }

        ChainLightning lightning = Tower.GetComponent<ChainLightning>();
        if (lightning != null && !lightning.IsBusy && Tower.AttackTarget != null)
        {
            lightning.Fire(Tower.AttackTarget, Tower.damage);
        }

        yield return new WaitForSeconds(Tower.rate);
    }
}
