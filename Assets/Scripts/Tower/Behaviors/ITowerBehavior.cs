/// <summary>
/// 타워 전투/오라 전략. TowerWeapon은 스탯·타겟 탐색만 담당하고 실제 동작은 이 모듈이 수행한다.
/// </summary>
public interface ITowerBehavior
{
    void Initialize(TowerWeapon tower);
    void Activate();
    void Deactivate();
    void OnUpgraded();
}