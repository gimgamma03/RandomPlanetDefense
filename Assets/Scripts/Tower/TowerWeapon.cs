using UnityEngine;

public enum WeaponType
{
    Cannon = 0,
    Laser,
    Slow,
    Buff,
    ChainLightning,
    Bomb,
    MultiWayShooting,
    MultiBomb,
    MultiLaser
}

/// <summary>
/// 타워 공통 호스트: 스탯, 업그레이드, 타겟 탐색, 풀 스폰.
/// 실제 공격/오라는 ITowerBehavior 전략이 담당한다.
/// </summary>
public class TowerWeapon : MonoBehaviour
{
    [SerializeField]
    private TowerData towerData;
    [SerializeField]
    private Transform spawnPoint;
    [SerializeField]
    public WeaponType weaponType;

    [Header("Projectile")]
    [SerializeField]
    private GameObject targetProjectilePrefab;

    [Header("BombProjectile")]
    [SerializeField]
    private GameObject bombProjectilePrefab;

    [Header("MultiShoot")]
    [SerializeField]
    private GameObject projectilePrefab;

    [Header("MultiBomb")]
    [SerializeField]
    private GameObject bombPrefab;

    [Header("Laser")]
    [SerializeField]
    private LineRenderer lineRenderer;

    [Header("MultiLaserPlus")]
    [SerializeField]
    private LineRenderer lineRenderer2;
    [SerializeField]
    private LineRenderer lineRenderer3;

    private IPoolService poolService;
    private ITowerBehavior behavior;
    private SpriteRenderer spriteRenderer;
    private TowerSpawner towerSpawner;
    private EnemySpawner enemySpawner;
    private bool statsReady;

    public Transform AttackTarget { get; set; }

    public Transform SpawnPoint => spawnPoint;
    public GameObject TargetProjectilePrefab => targetProjectilePrefab;
    public GameObject BombProjectilePrefab => bombProjectilePrefab;
    public GameObject ProjectilePrefab => projectilePrefab;
    public GameObject BombPrefab => bombPrefab;
    public LineRenderer LineRenderer => lineRenderer;
    public LineRenderer LineRenderer2 => lineRenderer2;
    public LineRenderer LineRenderer3 => lineRenderer3;
    public bool DoubleShot => doubleShot;

    public TowerGrade towerGrade;
    public Sprite towerSprite => towerData != null ? towerData.sprite : null;
    public string DisplayName => towerData != null ? towerData.DisplayName : name;
    public TowerData Definition => towerData;
    public int level = 1;
    public int upGradeGold;
    public int useGoldToUpGrade = 0;

    /// <summary>카탈로그에서 고른 정의로 등급·타입·스탯 소스를 맞춘다.</summary>
    public void BindDefinition(TowerData data)
    {
        if (data == null)
        {
            return;
        }

        towerData = data;
        towerGrade = data.grade;
        weaponType = data.weaponType;
        statsReady = false;
        EnsureStatsFromData();
    }

    [HideInInspector] public float damage;
    [HideInInspector] public float range;
    [HideInInspector] public float rate;
    private bool doubleShot;

    [HideInInspector] public float slowValue;

    private void Start()
    {
        EnsureStatsFromData();
    }

    private void OnDisable()
    {
        behavior?.Deactivate();
    }

    private void OnDestroy()
    {
        behavior?.Deactivate();
    }

    public void SetUp(TowerSpawner towerSpawner, EnemySpawner enemySpawner)
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        this.towerSpawner = towerSpawner;
        this.enemySpawner = enemySpawner;
        poolService = ServiceLocator.Get<IPoolService>();

        EnsureStatsFromData();

        behavior?.Deactivate();
        behavior = TowerBehaviorFactory.Create(weaponType);
        behavior.Initialize(this);
        behavior.Activate();
    }

    public bool UPGrade()
    {
        useGoldToUpGrade += upGradeGold;
        upGradeGold += (int)towerGrade;
        damage += towerData.weaponUpGradeValue.damage;
        range += towerData.weaponUpGradeValue.range;
        rate += towerData.weaponUpGradeValue.rate;

        if (weaponType == WeaponType.Slow)
        {
            slowValue += towerData.weaponUpGradeValue.slowValue;
        }

        level++;
        behavior?.OnUpgraded();
        return true;
    }

    public Transform FindClosestAttackTarget()
    {
        if (enemySpawner == null)
        {
            AttackTarget = null;
            return null;
        }

        float closestDistSqr = Mathf.Infinity;
        Transform closest = null;

        for (int i = 0; i < enemySpawner.enemyList.Count; ++i)
        {
            Enemy enemy = enemySpawner.enemyList[i];
            if (!IsValidEnemyTarget(enemy))
            {
                continue;
            }

            float distance = Vector3.Distance(enemy.transform.position, transform.position);
            if (distance <= range && distance <= closestDistSqr)
            {
                closestDistSqr = distance;
                closest = enemy.transform;
            }
        }

        AttackTarget = closest;
        return AttackTarget;
    }

    public bool IsPossibleToAttackTarget()
    {
        // 풀 반환 후 Transform은 파괴되지 않음 → null이 아니어도 죽은/비활성일 수 있음
        if (!IsValidAttackTarget(AttackTarget))
        {
            AttackTarget = null;
            return false;
        }

        float distance = Vector3.Distance(AttackTarget.position, transform.position);
        if (distance > range)
        {
            AttackTarget = null;
            return false;
        }

        return true;
    }

    private static bool IsValidEnemyTarget(Enemy enemy)
    {
        if (enemy == null || !enemy.isActiveAndEnabled || !enemy.gameObject.activeInHierarchy)
        {
            return false;
        }

        EnemyHp hp = enemy.GetComponent<EnemyHp>();
        if (hp != null && hp.IsDead)
        {
            return false;
        }

        return true;
    }

    private bool IsValidAttackTarget(Transform target)
    {
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            return false;
        }

        Enemy enemy = target.GetComponent<Enemy>();
        if (!IsValidEnemyTarget(enemy))
        {
            return false;
        }

        // 웨이브 목록에 없으면 이미 DestroyEnemy 처리된 것
        if (enemySpawner == null || !enemySpawner.enemyList.Contains(enemy))
        {
            return false;
        }

        return true;
    }

    public GameObject SpawnPooled(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            return null;
        }

        if (poolService == null)
        {
            poolService = ServiceLocator.Get<IPoolService>();
        }

        if (poolService != null)
        {
            return poolService.Spawn(prefab, position, rotation);
        }

        return Instantiate(prefab, position, rotation);
    }

    private void EnsureStatsFromData()
    {
        if (statsReady || towerData == null)
        {
            return;
        }

        damage = towerData.weapon.damage;
        range = towerData.weapon.range;
        rate = towerData.weapon.rate;
        doubleShot = towerData.weapon.doubleShot;

        if (weaponType == WeaponType.Slow)
        {
            slowValue = towerData.weapon.slowValue;
        }

        upGradeGold = (int)towerGrade * Constants.upGradeGoldMulti;
        statsReady = true;
    }
}