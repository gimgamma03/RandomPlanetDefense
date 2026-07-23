using UnityEngine;

/// <summary>
/// 타워 정의(로컬 SO).
/// 구글 시트/엑셀 CSV는 ApplyBalance로 수치·등급·표시이름을 덮어쓴다.
/// </summary>
[CreateAssetMenu(menuName = "RPD/Tower Data", fileName = "TowerData")]
public class TowerData : ScriptableObject
{
    [Tooltip("시트/밸런스 키. 비우면 asset 이름 사용")]
    public string towerId;

    [Tooltip("UI 표시용. 시트 displayName으로 덮을 수 있음")]
    public string displayName;

    public GameObject towerPrefab;
    public GameObject followTowerPrefab;
    public Weapon weapon;
    public WeaponUpGradeValue weaponUpGradeValue;

    [Tooltip("합성 등급 1~5. 이름 접두와 무관 — 시트 grade 컬럼이 우선")]
    public TowerGrade grade;

    public WeaponType weaponType;
    public Sprite sprite;

    [Min(0.01f)]
    [Tooltip("같은 등급 랜덤 스폰 가중치")]
    public float spawnWeight = 1f;

    public string Id => string.IsNullOrEmpty(towerId) ? name : towerId;

    public string DisplayName =>
        string.IsNullOrEmpty(displayName) ? Id : displayName;

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

        [Header("About SlowTower (0.0 ~ 1.0)")]
        public float slowValue;
    }

    [System.Serializable]
    public struct WeaponUpGradeValue
    {
        public float damage;
        public float rate;
        public float range;

        [Header("About SlowTower")]
        public float slowValue;
    }
}