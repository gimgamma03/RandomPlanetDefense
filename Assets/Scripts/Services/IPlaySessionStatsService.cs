/// <summary>
/// 한 판 플레이 세션 통계 수집. 종료 시 로컬 저장 + API POST 시도.
/// 실패해도 플레이에 영향 없음.
/// </summary>
public interface IPlaySessionStatsService : IService
{
    bool IsRunActive { get; }

    void BeginRun(int stageId, string stageName, int maxWaves, string playerId);

    void RecordWaveStarted(int waveNumber1Based);
    void RecordWaveCleared(int waveNumber1Based);

    void RecordTowerSpawned(WeaponType weaponType);
    void RecordTowerMerged(WeaponType weaponType);
    void RecordTowerSold(WeaponType weaponType);

    /// <summary>세션 마감·로컬 저장·전송 시도. 중복 호출 안전.</summary>
    void EndRun(string endReason, int finalScore);
}
