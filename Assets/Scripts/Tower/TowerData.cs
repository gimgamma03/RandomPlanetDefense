using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 타워 정의(로컬 SO).
/// 프리팹은 TowerBaseLibrary(weaponType)가 담당 — SO에는 두지 않는다.
/// 구글 시트/엑셀 CSV는 ApplyBalance로 수치·등급·표시이름을 덮어쓴다.
/// </summary>
[CreateAssetMenu(menuName = "RPD/Tower Data", fileName = "TowerData")]
public class TowerData : ScriptableObject
{
    [Tooltip("시트/밸런스 키. 비우면 asset 이름 사용")]
    public string towerId;

    [Tooltip("UI 표시용. 시트 displayName으로 덮을 수 있음")]
    public string displayName;

    public Weapon weapon;
    public WeaponUpGradeValue weaponUpGradeValue;

    [Tooltip("합성 등급 1~5. 이름 접두와 무관 — 시트 grade 컬럼이 우선")]
    public TowerGrade grade;

    public WeaponType weaponType;

    [Tooltip("Auto = WeaponType 기본 발사체. None/Homing/Straight…로 덮어쓰기 가능")]
    public ProjectileType projectileType = ProjectileType.Auto;

    [Tooltip("Auto = 계열 기본(캐논/레이저=보스 우선). 그 외는 SO에서 지정")]
    public TowerTargetPriority targetPriority = TowerTargetPriority.Auto;

    public Sprite sprite;

    [Tooltip("같은 스프라이트도 색으로 구분")]
    public Color spriteColor = Color.white;

    [Min(0.01f)]
    [Tooltip("같은 등급 랜덤 스폰 가중치")]
    public float spawnWeight = 1f;

    // 인스펙터는 TowerDataEditor가 weaponType에 따라 표시
    [HideInInspector]
    [Min(1)]
    public int multiShotCount = 3;

    [HideInInspector]
    public float multiShotSpreadAngle = 45f;

    [HideInInspector]
    [Min(1)]
    [FormerlySerializedAs("multiBombCount")]
    public int groundBombCount = 5;

    [HideInInspector]
    [Min(0.1f)]
    public float groundBombLineLength = 5f;

    [HideInInspector]
    [Min(0f)]
    public float groundBombSpawnInterval = 0f;

    [HideInInspector]
    [Min(0f)]
    [Tooltip("Laser/MultiLaser 굵기. 0이면 프리팹 LineRenderer 값 그대로")]
    public float laserWidth = 0f;

    [HideInInspector]
    [Min(1)]
    [Tooltip("OrbitSatellite 궤도 위성 개수")]
    public int orbitSatelliteCount = 2;

    public string Id => string.IsNullOrEmpty(towerId) ? name : towerId;

    public string DisplayName =>
        string.IsNullOrEmpty(displayName) ? Id : displayName;

    /// <summary>Auto면 WeaponType 기본값, 아니면 SO에 지정한 타입.</summary>
    public ProjectileType GetEffectiveProjectileType()
    {
        if (projectileType == ProjectileType.Auto)
        {
            return ProjectileTypeDefaults.FromWeapon(weaponType);
        }

        return projectileType;
    }

    /// <summary>Auto면 계열 기본 타겟 규칙, 아니면 SO 지정.</summary>
    public TowerTargetPriority GetEffectiveTargetPriority()
    {
        if (targetPriority == TowerTargetPriority.Auto)
        {
            return TowerTargetPriorityDefaults.FromWeapon(weaponType);
        }

        return targetPriority;
    }

    public void ApplyBalance(
        float damage,
        float rate,
        float range,
        float slowValue,
        float weight,
        int sell = 0,
        bool doubleShot = false,
        float upgradeDamage = 0f,
        float upgradeRate = 0f,
        float upgradeRange = 0f,
        float upgradeSlow = 0f,
        int gradeNumber = -1,
        string newDisplayName = null)
    {
        weapon.damage = damage;
        weapon.rate = rate;
        weapon.range = range;
        weapon.slowValue = slowValue;
        weapon.sell = sell;
        weapon.doubleShot = doubleShot;

        if (weight > 0f)
        {
            spawnWeight = weight;
        }

        weaponUpGradeValue.damage = upgradeDamage;
        weaponUpGradeValue.rate = upgradeRate;
        weaponUpGradeValue.range = upgradeRange;
        weaponUpGradeValue.slowValue = upgradeSlow;

        if (gradeNumber >= (int)TowerGrade.Grade1 && gradeNumber <= Constants.MaxTowerGrade)
        {
            grade = (TowerGrade)gradeNumber;
        }

        if (!string.IsNullOrEmpty(newDisplayName))
        {
            displayName = newDisplayName;
        }
    }

    [System.Serializable]
    public struct Weapon
    {
        public float damage;
        public float rate;
        public float range;
        public int sell;
        public bool doubleShot;

        [Tooltip("Slow 타입만 사용 (0.0 ~ 1.0)")]
        public float slowValue;
    }

    [System.Serializable]
    public struct WeaponUpGradeValue
    {
        public float damage;
        public float rate;
        public float range;

        [Tooltip("Slow 타입만 사용")]
        public float slowValue;
    }
}
