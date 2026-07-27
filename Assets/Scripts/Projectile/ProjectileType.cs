using UnityEngine;

/// <summary>
/// 발사체 구조 타입. TowerData에서 지정하거나 Auto면 WeaponType 기본값.
/// </summary>
public enum ProjectileType
{
    /// <summary>WeaponType 기본 매핑 사용</summary>
    Auto = 0,
    /// <summary>발사체 없음 (레이저·슬로우 등)</summary>
    None = 1,
    /// <summary>유도탄 (구 TargetProjectile)</summary>
    Homing = 2,
    /// <summary>직진탄 (구 Projectile / MultiShot)</summary>
    Straight = 3,
    /// <summary>날아가는 폭탄 (구 BombProjectile)</summary>
    BombShot = 4,
    /// <summary>지점 설치형 폭탄 (구 Bomb / GroundBombLine)</summary>
    GroundBomb = 5,
}

/// <summary>WeaponType → 기본 ProjectileType</summary>
public static class ProjectileTypeDefaults
{
    public static ProjectileType FromWeapon(WeaponType weaponType)
    {
        switch (weaponType)
        {
            case WeaponType.Cannon:
                return ProjectileType.Homing;
            case WeaponType.Bomb:
                return ProjectileType.BombShot;
            case WeaponType.MultiWayShooting:
            case WeaponType.ChargePierce:
                return ProjectileType.Straight;
            case WeaponType.GroundBombLine:
                return ProjectileType.GroundBomb;
            case WeaponType.Laser:
            case WeaponType.MultiLaser:
            case WeaponType.Slow:
            case WeaponType.Buff:
            case WeaponType.ChainLightning:
            case WeaponType.OrbitSatellite:
            default:
                return ProjectileType.None;
        }
    }

    public static bool UsesProjectile(ProjectileType type)
    {
        return type != ProjectileType.Auto && type != ProjectileType.None;
    }
}
