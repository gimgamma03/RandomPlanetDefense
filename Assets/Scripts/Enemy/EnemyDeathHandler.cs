/// <summary>
/// 적 사망/골인 시 보상·패널티만 담당.
/// 리스트 제거·풀 반환·스플릿 스폰은 EnemySpawner가 한다.
/// </summary>
public sealed class EnemyDeathHandler
{
    private IPlayerService playerService;
    private IScoreService scoreService;

    public void EnsureServices()
    {
        if (playerService == null)
        {
            ServiceLocator.TryGet(out playerService);
        }

        if (scoreService == null)
        {
            ServiceLocator.TryGet(out scoreService);
        }
    }

    public void ApplyOutcome(EnemyDestroyType type, Enemy enemy)
    {
        if (enemy == null)
        {
            return;
        }

        EnsureServices();

        if (type == EnemyDestroyType.Arrive)
        {
            ApplyArrive(enemy);
            return;
        }

        if (type == EnemyDestroyType.Kill)
        {
            ApplyKill(enemy);
        }
    }

    private void ApplyArrive(Enemy enemy)
    {
        if (playerService == null)
        {
            return;
        }

        // 보스 골인 = 즉시 게임오버 (목숨 -1이 아님)
        bool isBoss = enemy.enemyData != null && enemy.enemyData.isBoss;
        if (isBoss)
        {
            playerService.ForceGameOver();
            return;
        }

        playerService.TakeDamage(Constants.enemyGoalInDamage);
    }

    private void ApplyKill(Enemy enemy)
    {
        if (playerService != null)
        {
            playerService.AddGold(enemy.GetGold());
        }

        if (scoreService != null)
        {
            scoreService.AddScore(enemy.GetScorePoint());
        }
    }
}
