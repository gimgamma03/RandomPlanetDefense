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
        if (lightning != null && Tower.AttackTarget != null)
        {
            lightning.SetUp(Tower.AttackTarget.gameObject, Tower.damage);
            lightning.ChainLightningStart();
        }

        yield return new WaitForSeconds(Tower.rate);
    }
}