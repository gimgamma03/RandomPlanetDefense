using System;

/// <summary>플레이어 골드·HP (Pure C# 서비스)</summary>
public interface IPlayerService : IService
{
    int Gold { get; }
    int CurrentHp { get; }
    int MaxHp { get; }
    bool IsGameOver { get; }

    /// <summary>씬 Player가 인스펙터 시작값으로 한 판을 세팅</summary>
    void ConfigureRun(int startingGold, int maxHp);

    void AddGold(int amount);

    /// <summary>골드가 충분하면 차감하고 true</summary>
    bool TrySpendGold(int amount);

    void TakeDamage(int damage);

    event Action OnDamaged;
    event Action OnDied;
}
