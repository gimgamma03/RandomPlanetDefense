using System;
using System.Collections.Generic;

/// <summary>아웃게임 진행 저장 루트 (JSON).</summary>
[Serializable]
public class MetaProgressData
{
    public string playerId = string.Empty;
    public int crystals;
    public int bestScore;
    public List<int> clearedStageIds = new List<int>();
    public List<StageBestScoreEntry> stageBestScores = new List<StageBestScoreEntry>();
    public List<WeaponUpgradeEntry> weaponUpgrades = new List<WeaponUpgradeEntry>();
}

[Serializable]
public class StageBestScoreEntry
{
    public int stageId;
    public int bestScore;
}

[Serializable]
public class WeaponUpgradeEntry
{
    /// <summary>WeaponType enum int</summary>
    public int weaponType;
    public int level;
}
