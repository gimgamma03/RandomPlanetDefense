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

    [Header("Projectile (레거시 폴백 — Library 우선)")]
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

    private GameObject resolvedHoming;
    private GameObject resolvedStraight;
    private GameObject resolvedBombShot;
    private GameObject resolvedGroundBomb;

    public Transform AttackTarget { get; set; }

    public Transform SpawnPoint => spawnPoint;
    public GameObject TargetProjectilePrefab => resolvedHoming != null ? resolvedHoming : targetProjectilePrefab;
    public GameObject BombProjectilePrefab => resolvedBombShot != null ? resolvedBombShot : bombProjectilePrefab;
    public GameObject ProjectilePrefab => resolvedStraight != null ? resolvedStraight : projectilePrefab;
    public GameObject BombPrefab => resolvedGroundBomb != null ? resolvedGroundBomb : bombPrefab;
    public LineRenderer LineRenderer => lineRenderer;
    public LineRenderer LineRenderer2 => lineRenderer2;
    public LineRenderer LineRenderer3 => lineRenderer3;
    public bool DoubleShot => doubleShot;

    public TowerGrade towerGrade;
    public Sprite towerSprite => towerData != null ? towerData.sprite : null;
    public Color TowerSpriteColor => towerData != null ? towerData.spriteColor : Color.white;
    public string DisplayName => towerData != null ? towerData.DisplayName : name;
    public TowerData Definition => towerData;
    public int level = 1;
    public int upGradeGold;
    public int useGoldToUpGrade = 0;

    public int MultiShotCount =>
        towerData != null ? Mathf.Max(1, towerData.multiShotCount) : 3;

    public float MultiShotSpreadAngle =>
        towerData != null ? towerData.multiShotSpreadAngle : 45f;

    public int MultiBombCount =>
        towerData != null ? Mathf.Max(1, towerData.multiBombCount) : 5;

    public ProjectileType EffectiveProjectileType
    {
        get
        {
            if (towerData != null)
            {
                return towerData.GetEffectiveProjectileType();
            }

            return ProjectileTypeDefaults.FromWeapon(weaponType);
        }
    }

    /// <summary>카탈로그에서 고른 정의로 등급·타입·스탯·스프라이트·발사체를 맞춘다.</summary>
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
        ApplyVisualFromData();
        ResolveProjectilePrefabs();
    }

    /// <summary>ProjectileBaseLibrary에서 타입 Base를 가져와 슬롯에 올린다. 없으면 프리팹 직렬화 폴백.</summary>
    private void ResolveProjectilePrefabs()
    {
        resolvedHoming = null;
        resolvedStraight = null;
        resolvedBombShot = null;
        resolvedGroundBomb = null;

        ProjectileType type = EffectiveProjectileType;
        if (type == ProjectileType.None || type == ProjectileType.Auto)
        {
            return;
        }

        ProjectileBaseLibrary library = ProjectileBaseLibrary.Load();
        if (library == null)
        {
            return;
        }

        GameObject basePrefab = library.GetBasePrefab(type);
        if (basePrefab == null)
        {
            return;
        }

        switch (type)
        {
            case ProjectileType.Homing:
                resolvedHoming = basePrefab;
                break;
            case ProjectileType.Straight:
                resolvedStraight = basePrefab;
                break;
            case ProjectileType.BombShot:
                resolvedBombShot = basePrefab;
                break;
            case ProjectileType.GroundBomb:
                resolvedGroundBomb = basePrefab;
                break;
        }
    }

    private void ApplyVisualFromData()
    {
        if (towerData == null)
        {
            return;
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (spriteRenderer == null)
        {
            return;
        }

        if (towerData.sprite != null)
        {
            spriteRenderer.sprite = towerData.sprite;
        }

        spriteRenderer.color = towerData.spriteColor;
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
        ResolveProjectilePrefabs();

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