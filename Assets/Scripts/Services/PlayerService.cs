using System;
using UnityEngine;

/// <summary>
/// Pure C# 플레이어 경제·체력.
/// 피격 연출·게임오버 씬 전환은 Player(MonoBehaviour)가 이벤트로 담당.
/// </summary>
public class PlayerService : IPlayerService
{
    public int Gold { get; private set; }
    public int CurrentHp { get; private set; }
    public int MaxHp { get; private set; }
    public bool IsGameOver { get; private set; }

    public event Action OnDamaged;
    public event Action OnDied;

    public void Initialize()
    {
        // 실제 시작값은 Player.ConfigureRun에서 씬별로 넣음
        IsGameOver = false;
    }

    public void ConfigureRun(int startingGold, int maxHp)
    {
        Gold = Mathf.Max(0, startingGold);
        MaxHp = Mathf.Max(1, maxHp);
        CurrentHp = MaxHp;
        IsGameOver = false;
    }

    public void AddGold(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        Gold += amount;
    }

    public bool TrySpendGold(int amount)
    {
        if (amount < 0 || Gold < amount)
        {
            return false;
        }

        Gold -= amount;
        return true;
    }

    public void TakeDamage(int damage)
    {
        if (IsGameOver || damage <= 0)
        {
            return;
        }

        CurrentHp = Mathf.Max(0, CurrentHp - damage);
        OnDamaged?.Invoke();

        if (CurrentHp <= 0 && !IsGameOver)
        {
            IsGameOver = true;
            OnDied?.Invoke();
        }
    }
}
