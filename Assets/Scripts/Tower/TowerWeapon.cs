using UnityEngine;

/// <summary>
/// 공격 방식 구분. 이름은 "어떻게 쏘는가"를 나타낸다.
/// 순서를 바꾸면 기존 SO의 선택이 밀리므로 새 타입은 끝에 추가한다.
/// </summary>
public enum WeaponType
{
    Cannon = 0,
    Laser,
    Slow,
    Buff,
    ChainLightning,
    Bomb,
    MultiWayShooting,

    /// <summary>발사체 없이 지면 폭탄을 일렬 설치 (구 MultiBomb)</summary>
    GroundBombLine,
    MultiLaser,

    /// <summary>차징 후 관통 직진탄 (Behavior 미구현 시 NoOp)</summary>
    ChargePierce,

    /// <summary>궤도 위성 접촉 피해 (Behavior 미구현 시 NoOp)</summary>
    OrbitSatellite
}

/// <summary>
/// 타워 공통 호스트: 스탯, 업그레이드, 타겟 탐색, 풀 스폰.
/// 실제 공격/오라는 ITowerBehavior 전략이 담당한다.
/// </summary>
public class TowerWeapon : MonoBehaviour
{
    #region Identity / Prefab wiring

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

    [Header("GroundBomb")]
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

    public Transform SpawnPoint => spawnPoint;
    public TowerData Definition => towerData;
    public Sprite towerSprite => towerData != null ? towerData.sprite : null;
    public Color TowerSpriteColor => towerData != null ? towerData.spriteColor : Color.white;
    public string DisplayName => towerData != null ? towerData.DisplayName : name;

    public LineRenderer LineRenderer => lineRenderer;
    public LineRenderer LineRenderer2 => lineRenderer2;
    public LineRenderer LineRenderer3 => lineRenderer3;

    #endregion

    #region Runtime stats

    public TowerGrade towerGrade;
    public int level = 1;
    public int upGradeGold;

    /// <summary>판매 환불에 쓰는 누적 업그레이드 골드</summary>
    public int useGoldToUpGrade;

    [HideInInspector] public float damage;
    [HideInInspector] public float range;
    [HideInInspector] public float rate;
    [HideInInspector] public float slowValue;

    private bool doubleShot;
    private bool statsReady;

    public bool DoubleShot => doubleShot;

    public int MultiShotCount =>
        towerData != null ? Mathf.Max(1, towerData.multiShotCount) : 3;

    public float MultiShotSpreadAngle =>
        towerData != null ? towerData.multiShotSpreadAngle : 45f;

    public int GroundBombCount =>
        towerData != null ? Mathf.Max(1, towerData.groundBombCount) : 5;

    public float GroundBombLineLength =>
        towerData != null ? Mathf.Max(0.1f, towerData.groundBombLineLength) : 5f;

    public float GroundBombSpawnInterval =>
        towerData != null ? Mathf.Max(0f, towerData.groundBombSpawnInterval) : 0f;

    /// <summary>0이면 프리팹 LineRenderer 굵기를 그대로 쓴다.</summary>
    public float LaserWidth =>
        towerData != null ? Mathf.Max(0f, towerData.laserWidth) : 0f;

    public int OrbitSatelliteCount =>
        towerData != null ? Mathf.Max(1, towerData.orbitSatelliteCount) : 2;

    #endregion

    #region Projectile resolve (Library → 레거시 폴백)

    private GameObject resolvedHoming;
    private GameObject resolvedStraight;
    private GameObject resolvedBombShot;
    private GameObject resolvedGroundBomb;

    public GameObject TargetProjectilePrefab =>
        resolvedHoming != null ? resolvedHoming : targetProjectilePrefab;

    public GameObject BombProjectilePrefab =>
        resolvedBombShot != null ? resolvedBombShot : bombProjectilePrefab;

    public GameObject ProjectilePrefab =>
        resolvedStraight != null ? resolvedStraight : projectilePrefab;

    public GameObject BombPrefab =>
        resolvedGroundBomb != null ? resolvedGroundBomb : bombPrefab;

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
        if (basePrefab != null)
        {
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

        // Bomb G3+ 라인폭탄 패시브: BombShot과 별도로 GroundBomb Base도 필요
        if (weaponType == WeaponType.Bomb && towerGrade >= TowerBehaviorFactory.PassiveUnlockGrade)
        {
            resolvedGroundBomb = library.GetBasePrefab(ProjectileType.GroundBomb);
        }
    }

    #endregion

    #region Targeting

    public Transform AttackTarget { get; set; }

    private EnemySpawner enemySpawner;

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

    /// <summary>
    /// 사거리 안 적을 가까운 순으로 최대 buffer.Length개 담는다.
    /// AttackTarget은 1순위(가장 가까운)로 맞춘다.
    /// </summary>
    public int CollectClosestAttackTargets(Transform[] buffer)
    {
        if (buffer == null || buffer.Length == 0 || enemySpawner == null)
        {
            AttackTarget = null;
            return 0;
        }

        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = null;
        }

        // 거리 병행 버퍼(고정 크기 — 레이저 빔 수와 맞춤)
        float d0 = float.PositiveInfinity;
        float d1 = float.PositiveInfinity;
        float d2 = float.PositiveInfinity;

        for (int i = 0; i < enemySpawner.enemyList.Count; ++i)
        {
            Enemy enemy = enemySpawner.enemyList[i];
            if (!IsValidEnemyTarget(enemy))
            {
                continue;
            }

            float distance = Vector3.Distance(enemy.transform.position, transform.position);
            if (distance > range)
            {
                continue;
            }

            Transform t = enemy.transform;
            if (buffer.Length >= 1 && (buffer[0] == null || distance < d0))
            {
                if (buffer.Length >= 3)
                {
                    buffer[2] = buffer[1];
                    d2 = d1;
                }

                if (buffer.Length >= 2)
                {
                    buffer[1] = buffer[0];
                    d1 = d0;
                }

                buffer[0] = t;
                d0 = distance;
            }
            else if (buffer.Length >= 2 && (buffer[1] == null || distance < d1))
            {
                if (buffer.Length >= 3)
                {
                    buffer[2] = buffer[1];
                    d2 = d1;
                }

                buffer[1] = t;
                d1 = distance;
            }
            else if (buffer.Length >= 3 && (buffer[2] == null || distance < d2))
            {
                buffer[2] = t;
                d2 = distance;
            }
        }

        int count = 0;
        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i] != null)
            {
                count++;
            }
        }

        AttackTarget = buffer[0];
        return count;
    }

    /// <summary>
    /// MultiLaser / G3+ 레이저용. LaserEffect를 복제해 lineRenderer2·3을 채운다.
    /// </summary>
    public void EnsureMultiLaserLines()
    {
        if (lineRenderer == null)
        {
            return;
        }

        if (lineRenderer2 == null)
        {
            lineRenderer2 = CloneLaserLineRenderer("LaserEffect_2");
        }

        if (lineRenderer3 == null)
        {
            lineRenderer3 = CloneLaserLineRenderer("LaserEffect_3");
        }
    }

    private LineRenderer CloneLaserLineRenderer(string objectName)
    {
        Transform parent = lineRenderer.transform.parent;
        GameObject clone = Object.Instantiate(lineRenderer.gameObject, parent);
        clone.name = objectName;
        return clone.GetComponent<LineRenderer>();
    }

    public LineRenderer GetLaserLine(int index)
    {
        switch (index)
        {
            case 0:
                return lineRenderer;
            case 1:
                return lineRenderer2;
            case 2:
                return lineRenderer3;
            default:
                return null;
        }
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

    #endregion

    #region Pool

    private IPoolService poolService;

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

    #endregion

    #region Behavior lifecycle

    private ITowerBehavior behavior;
    private SpriteRenderer spriteRenderer;

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

    public void SetUp(TowerSpawner _, EnemySpawner enemySpawner)
    {
        // TowerSpawner는 Tower MB가 보관. 여기선 타겟·풀·Behavior만.
        spriteRenderer = GetComponent<SpriteRenderer>();
        this.enemySpawner = enemySpawner;
        poolService = ServiceLocator.Get<IPoolService>();

        EnsureStatsFromData();
        ResolveProjectilePrefabs();

        behavior?.Deactivate();
        behavior = TowerBehaviorFactory.Create(weaponType, towerGrade);
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

        TowerGradeRingView.Attach(gameObject, towerGrade);
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

        ApplyPermanentMetaUpgrades();

        upGradeGold = (int)towerGrade * Constants.upGradeGoldMulti;
        statsReady = true;
    }

    /// <summary>아웃게임 크리스탈 강화 — 같은 weaponType 전원에게 적용.</summary>
    private void ApplyPermanentMetaUpgrades()
    {
        if (!ServiceLocator.TryGet(out IMetaProgressService meta))
        {
            return;
        }

        int metaLevel = meta.GetWeaponUpgradeLevel(weaponType);
        if (metaLevel <= 0)
        {
            return;
        }

        float slow = slowValue;
        TowerMetaUpgradeRules.ApplyToStats(
            metaLevel,
            ref damage,
            ref range,
            ref rate,
            ref slow,
            applySlow: weaponType == WeaponType.Slow);
        slowValue = slow;
    }

    #endregion
}
