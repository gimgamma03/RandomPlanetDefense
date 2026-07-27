/// <summary>
/// WeaponType → ITowerBehavior 생성.
/// </summary>
public static class TowerBehaviorFactory
{
    public static ITowerBehavior Create(WeaponType weaponType)
    {
        switch (weaponType)
        {
            case WeaponType.Cannon:
                return new CannonBehavior();
            case WeaponType.Bomb:
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