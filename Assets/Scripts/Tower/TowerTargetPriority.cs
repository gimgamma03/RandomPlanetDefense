/// <summary>타워 타겟 선정 규칙 (SO / 계열 기본값).</summary>
public enum TowerTargetPriority
{
    /// <summary>WeaponType 기본 규칙</summary>
    Auto = 0,
    Nearest = 1,
    /// <summary>사거리 안 보스 우선, 없으면 최단거리</summary>
    BossFirst = 2,
    /// <summary>사거리 안 현재 HP 최저</summary>
    LowestHp = 3,
}

public static class TowerTargetPriorityDefaults
{
    public static TowerTargetPriority FromWeapon(WeaponType weaponType)
    {
        switch (weaponType)
        {
            case WeaponType.Cannon:
            case WeaponType.Laser:
            case WeaponType.MultiLaser:
                return TowerTargetPriority.BossFirst;
            default:
                return TowerTargetPriority.Nearest;
        }
    }
}
