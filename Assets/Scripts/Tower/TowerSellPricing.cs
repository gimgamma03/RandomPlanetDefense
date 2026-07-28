/// <summary>타워 판매 환급 골드 계산.</summary>
public static class TowerSellPricing
{
    public static int CalculateRefund(TowerGrade grade, int goldSpentOnUpgrades)
    {
        int refund = Constants.spawnRandomTowerGold;
        for (int i = 1; i < (int)grade; i++)
        {
            refund *= Constants.towerCombineCount;
        }

        refund += goldSpentOnUpgrades;
        return (int)(refund * Constants.cellTowerReturnGoldMulti);
    }
}
