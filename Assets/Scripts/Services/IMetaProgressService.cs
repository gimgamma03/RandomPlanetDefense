using System;

/// <summary>
/// 아웃게임 메타 진행 — 플레이어 식별, 클리어 스테이지, 베스트 스코어, 크리스탈, 영구 타워 강화.
/// </summary>
public interface IMetaProgressService : IService
{
    string PlayerId { get; }
    int Crystals { get; }
    int BestScore { get; }

    /// <summary>크리스탈 값이 바뀌었을 때 (UI 갱신용).</summary>
    event Action OnCrystalsChanged;

    bool IsStageCleared(int stageId);
    int GetStageBestScore(int stageId);

    void MarkStageCleared(int stageId);
    void RecordScore(int stageId, int score);

    void AddCrystals(int amount);
    bool TrySpendCrystals(int amount);

    int GetWeaponUpgradeLevel(WeaponType weaponType);

    /// <summary>크리스탈 소비 후 해당 형 영구 레벨 +1. 실패 시 false.</summary>
    bool TryUpgradeWeapon(WeaponType weaponType);
}
