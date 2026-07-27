using UnityEngine;

/// <summary>영구 타워 강화 수치·비용 (아웃게임 크리스탈).</summary>
public static class TowerMetaUpgradeRules
{
    public const int MaxLevel = 10;
    public const int BaseCrystalCost = 5;

    /// <summary>레벨당 피해 +1%</summary>
    public const float DamagePerLevel = 0.01f;

    /// <summary>레벨당 사거리 +1%</summary>
    public const float RangePerLevel = 0.01f;

    /// <summary>레벨당 공격 주기(쿨) 1% 감소 (빨라짐)</summary>
    public const float RateShrinkPerLevel = 0.01f;

    /// <summary>레벨당 슬로우 강도 +1% (Slow 전용)</summary>
    public const float SlowPerLevel = 0.01f;

    public const float MinRateFactor = 0.5f;

    /// <summary>웨이브 클리어마다 크리스탈</summary>
    public const int CrystalsPerWave = 1;

    /// <summary>스테이지 올클리어 보너스 크리스탈</summary>
    public const int CrystalsStageClearBonus = 3;

    public static int GetCostForNextLevel(int currentLevel)
    {
        if (currentLevel >= MaxLevel)
        {
            return 0;
        }

        return BaseCrystalCost * (currentLevel + 1);
    }

    public static string GetEffectSummary(WeaponType type)
    {
        int pct = Mathf.RoundToInt(DamagePerLevel * 100f);
        if (type == WeaponType.Slow)
        {
            return $"업당 데미지/사거리/공속/슬로우 +{pct}%";
        }

        return $"업당 데미지/사거리/공속 +{pct}%";
    }

    public static string FormatRowMeta(string towerName, int level, int nextCost, bool maxed)
    {
        string levelLine = maxed
            ? $"Lv {level}/{MaxLevel} MAX"
            : $"Lv {level}/{MaxLevel}";
        string costLine = maxed ? "비용 -" : $"비용 {nextCost}";
        return $"{towerName}\n{levelLine}  {costLine}";
    }

    public static void ApplyToStats(
        int level,
        ref float damage,
        ref float range,
        ref float rate,
        ref float slowValue,
        bool applySlow)
    {
        if (level <= 0)
        {
            return;
        }

        damage *= 1f + level * DamagePerLevel;
        range *= 1f + level * RangePerLevel;
        float rateFactor = Mathf.Max(MinRateFactor, 1f - level * RateShrinkPerLevel);
        rate *= rateFactor;

        if (applySlow)
        {
            slowValue *= 1f + level * SlowPerLevel;
        }
    }
}
