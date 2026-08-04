/// <summary>
/// 웨이브/스테이지 클리어 시 크리스탈·골드·점수 보너스·메타 기록.
/// WaveSystem은 진행 상태만, 보상 규칙은 여기.
/// </summary>
public sealed class WaveRunRewards
{
    /// <summary>보스(스테이지) 클리어 시 남은 목숨 1당 점수 보너스.</summary>
    private const int ClearScoreBonusPerLife = 2;

    private IMetaProgressService metaProgress;
    private IScoreService scoreService;

    public void Bind(IMetaProgressService meta, IScoreService score)
    {
        metaProgress = meta;
        scoreService = score;
    }

    public void OnWaveCleared()
    {
        if (metaProgress == null)
        {
            ServiceLocator.TryGet(out metaProgress);
        }

        metaProgress?.AddCrystals(TowerMetaUpgradeRules.CrystalsPerWave);
    }

    public void OnAllWavesCleared(StageData stage)
    {
        if (!ServiceLocator.TryGet(out IPlayerService player))
        {
            return;
        }

        if (stage != null && stage.clearBonusGold > 0)
        {
            player.AddGold(stage.clearBonusGold);
        }

        // 클리어 생존 보너스: 남은 목숨 × 2점 (세션 저장·하이스코어 전에 반영)
        if (scoreService == null)
        {
            ServiceLocator.TryGet(out scoreService);
        }

        int lives = player.CurrentHp > 0 ? player.CurrentHp : 0;
        int bonus = lives * ClearScoreBonusPerLife;
        if (bonus > 0 && scoreService != null)
        {
            scoreService.AddScore(bonus);
        }
    }

    public void RecordMetaProgress(int stageId, bool stageCleared)
    {
        if (metaProgress == null)
        {
            ServiceLocator.TryGet(out metaProgress);
        }

        if (metaProgress == null)
        {
            return;
        }

        if (scoreService == null)
        {
            ServiceLocator.TryGet(out scoreService);
        }

        int score = scoreService != null ? scoreService.CurrentScore : 0;
        metaProgress.RecordScore(stageId, score);

        if (stageCleared)
        {
            metaProgress.MarkStageCleared(stageId);
            metaProgress.AddCrystals(TowerMetaUpgradeRules.CrystalsStageClearBonus);
        }
    }
}
