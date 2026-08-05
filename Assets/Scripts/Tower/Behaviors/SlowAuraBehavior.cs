/// <summary>
/// 공격 없이 범위 내 적을 느리게 하는 오라. Slow 컴포넌트에 수치만 넘긴다.
/// </summary>
public sealed class SlowAuraBehavior : ITowerBehavior
{
    private TowerWeapon tower;
    private Slow slow;

    public void Initialize(TowerWeapon tower)
    {
        this.tower = tower;
        slow = tower.GetComponentInChildren<Slow>(true);
    }

    public void Activate()
    {
        Apply();
    }

    public void Deactivate() { }

    public void OnUpgraded()
    {
        Apply();
    }

    private void Apply()
    {
        if (slow == null)
        {
            slow = tower.GetComponentInChildren<Slow>(true);
        }

        if (slow != null)
        {
            slow.SetUp(tower.slowValue, tower.range);
        }
    }
}