/// <summary>
/// 웨이브/스테이지 클리어 시 크리스탈·골드·메타 기록.
/// WaveSystem은 진행 상태만, 보상 규칙은 여기.
/// </summary>
public sealed class WaveRunRewards
{
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
        if (stage != null && stage.clearBonusGold > 0
            && ServiceLocator.TryGet(out IPlayerService player))
        {
            player.AddGold(stage.clearBonusGold);
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
