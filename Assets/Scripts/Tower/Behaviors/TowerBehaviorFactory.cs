/// <summary>
/// WeaponType → ITowerBehavior 생성.
/// Grade3+ 임시 패시브: Laser 멀티빔은 LaserBehavior 내부, Bomb → 라인폭탄.
/// </summary>
public static class TowerBehaviorFactory
{
    /// <summary>이 등급부터 계열 패시브(고등급 특수 공격)를 켠다.</summary>
    public const TowerGrade PassiveUnlockGrade = TowerGrade.Grade3;

    public static ITowerBehavior Create(WeaponType weaponType, TowerGrade grade = TowerGrade.Grade1)
    {
        switch (weaponType)
        {
            case WeaponType.Cannon:
                return new CannonBehavior();
            case WeaponType.Bomb:
                if (grade >= PassiveUnlockGrade)
                {
                    return new GroundBombLineBehavior();
                }

                return new BombBehavior();
            case WeaponType.Laser:
            case WeaponType.MultiLaser:
                return new LaserBehavior();
            case WeaponType.ChainLightning:
                return new ChainLightningBehavior();
            case WeaponType.MultiWayShooting:
                return new MultiShotBehavior();
            case WeaponType.GroundBombLine:
                return new GroundBombLineBehavior();
            case WeaponType.Slow:
                return new SlowAuraBehavior();
            case WeaponType.ChargePierce:
                return new ChargePierceBehavior();
            case WeaponType.OrbitSatellite:
                return new OrbitSatelliteBehavior();
            case WeaponType.Buff:
            default:
                return new PassiveNoOpBehavior();
        }
    }
}