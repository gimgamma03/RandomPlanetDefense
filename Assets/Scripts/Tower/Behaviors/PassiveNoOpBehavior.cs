/// <summary>
/// Buff 등 전투 루프가 없는 타입용.
/// </summary>
public sealed class PassiveNoOpBehavior : ITowerBehavior
{
    public void Initialize(TowerWeapon tower) { }
    public void Activate() { }
    public void Deactivate() { }
    public void OnUpgraded() { }
}