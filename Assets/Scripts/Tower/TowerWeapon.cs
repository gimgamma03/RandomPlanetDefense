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

    /// <summary>골드 업그레이드 횟수. level은 옛 프리팹에 0으로 저장돼 있어 별도 카운터를 쓴다.</summary>
    private int goldUpgradeCount;

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

    private IEnemyRegistry enemyRegistry;

    /// <summary>CollectClosestAttackTargets 스크래치 (GC 방지). 거리는 sqrMagnitude.</summary>
    private float[] collectDistSq;
    private float[] collectHps;
    private bool[] collectBosses;

    public Transform FindClosestAttackTarget()
    {
        if (enemyRegistry == null)
        {
            AttackTarget = null;
            return null;
        }

        TowerTargetPriority priority = EffectiveTargetPriority;
        Enemy bestEnemy = null;
        float bestDistSq = float.PositiveInfinity;
        float bestHp = float.PositiveInfinity;
        float rangeSq = range * range;
        Vector3 origin = transform.position;

        for (int i = 0; i < enemyRegistry.Count; ++i)
        {
            Enemy enemy = enemyRegistry.GetEnemy(i);
            if (!IsValidEnemyTarget(enemy))
            {
                continue;
            }

            Vector3 enemyPos = enemy.transform.position;
            // AABB(축 정렬 박스) 조기 기각 — 사거리 원 밖을 먼저 싼 비용으로 컷 (D3D 충돌/컬링과 같은 개념)
            float dx = enemyPos.x - origin.x;
            float dy = enemyPos.y - origin.y;
            if (dx > range || dx < -range || dy > range || dy < -range)
            {
                continue;
            }

            float distSq = dx * dx + dy * dy;
            if (distSq > rangeSq)
            {
                continue;
            }

            if (IsBetterTarget(enemy, distSq, bestEnemy, bestDistSq, bestHp, priority))
            {
                bestEnemy = enemy;
                bestDistSq = distSq;
                bestHp = GetEnemyCurrentHp(enemy);
            }
        }

        AttackTarget = bestEnemy != null ? bestEnemy.transform : null;
        return AttackTarget;
    }

    /// <summary>
    /// 사거리 안 적을 우선순위·거리 순으로 최대 buffer.Length개 담는다.
    /// AttackTarget은 1순위로 맞춘다.
    /// </summary>
    public int CollectClosestAttackTargets(Transform[] buffer)
    {
        if (buffer == null || buffer.Length == 0 || enemyRegistry == null)
        {
            AttackTarget = null;
            return 0;
        }

        EnsureCollectScratch(buffer.Length);

        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = null;
            collectDistSq[i] = float.PositiveInfinity;
            collectHps[i] = float.PositiveInfinity;
            collectBosses[i] = false;
        }

        TowerTargetPriority priority = EffectiveTargetPriority;
        float rangeSq = range * range;
        Vector3 origin = transform.position;

        for (int i = 0; i < enemyRegistry.Count; ++i)
        {
            Enemy enemy = enemyRegistry.GetEnemy(i);
            if (!IsValidEnemyTarget(enemy))
            {
                continue;
            }

            Vector3 enemyPos = enemy.transform.position;
            float dx = enemyPos.x - origin.x;
            float dy = enemyPos.y - origin.y;
            if (dx > range || dx < -range || dy > range || dy < -range)
            {
                continue;
            }

            float distSq = dx * dx + dy * dy;
            if (distSq > rangeSq)
            {
                continue;
            }

            TryInsertTarget(
                buffer,
                collectDistSq,
                collectHps,
                collectBosses,
                enemy.transform,
                distSq,
                GetEnemyCurrentHp(enemy),
                IsBossEnemy(enemy),
                priority);
        }

        AttackTarget = buffer[0];
        int count = 0;
        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i] != null)
            {
                count++;
            }
        }

        return count;
    }

    private void EnsureCollectScratch(int length)
    {
        if (collectDistSq != null && collectDistSq.Length >= length)
        {
            return;
        }

        collectDistSq = new float[length];
        collectHps = new float[length];
        collectBosses = new bool[length];
    }

    private TowerTargetPriority EffectiveTargetPriority =>
        towerData != null
            ? towerData.GetEffectiveTargetPriority()
            : TowerTargetPriorityDefaults.FromWeapon(weaponType);

    private static bool IsBossEnemy(Enemy enemy)
    {
        return enemy != null && enemy.enemyData != null && enemy.enemyData.isBoss;
    }

    private static float GetEnemyCurrentHp(Enemy enemy)
    {
        if (enemy == null)
        {
            return float.PositiveInfinity;
        }

        EnemyHp hp = enemy.CachedHp;
        return hp != null ? hp.currentHp : float.PositiveInfinity;
    }

    /// <param name="candidateDistSq">제곱 거리. 비교만 하므로 sqrt 불필요.</param>
    private static bool IsBetterTarget(
        Enemy candidate,
        float candidateDistSq,
        Enemy current,
        float currentDistSq,
        float currentHp,
        TowerTargetPriority priority)
    {
        if (current == null)
        {
            return true;
        }

        switch (priority)
        {
            case TowerTargetPriority.BossFirst:
            {
                bool candidateBoss = IsBossEnemy(candidate);
                bool currentBoss = IsBossEnemy(current);
                if (candidateBoss != currentBoss)
                {
                    return candidateBoss;
                }

                return candidateDistSq < currentDistSq;
            }
            case TowerTargetPriority.LowestHp:
            {
                float candidateHp = GetEnemyCurrentHp(candidate);
                if (!Mathf.Approximately(candidateHp, currentHp))
                {
                    return candidateHp < currentHp;
                }

                return candidateDistSq < currentDistSq;
            }
            default:
                return candidateDistSq < currentDistSq;
        }
    }

    private static void TryInsertTarget(
        Transform[] buffer,
        float[] distSq,
        float[] hps,
        bool[] bosses,
        Transform candidate,
        float distanceSq,
        float hp,
        bool boss,
        TowerTargetPriority priority)
    {
        int insertAt = -1;
        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i] == null)
            {
                insertAt = i;
                break;
            }

            if (ShouldRankHigher(distanceSq, hp, boss, distSq[i], hps[i], bosses[i], priority))
            {
                insertAt = i;
                break;
            }
        }

        if (insertAt < 0)
        {
            return;
        }

        for (int i = buffer.Length - 1; i > insertAt; i--)
        {
            buffer[i] = buffer[i - 1];
            distSq[i] = distSq[i - 1];
            hps[i] = hps[i - 1];
            bosses[i] = bosses[i - 1];
        }

        buffer[insertAt] = candidate;
        distSq[insertAt] = distanceSq;
        hps[insertAt] = hp;
        bosses[insertAt] = boss;
    }

    private static bool ShouldRankHigher(
        float distanceSq,
        float hp,
        bool boss,
        float otherDistanceSq,
        float otherHp,
        bool otherBoss,
        TowerTargetPriority priority)
    {
        switch (priority)
        {
            case TowerTargetPriority.BossFirst:
                if (boss != otherBoss)
                {
                    return boss;
                }

                return distanceSq < otherDistanceSq;
            case TowerTargetPriority.LowestHp:
                if (!Mathf.Approximately(hp, otherHp))
                {
                    return hp < otherHp;
                }

                return distanceSq < otherDistanceSq;
            default:
                return distanceSq < otherDistanceSq;
        }
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

        float distSq = (AttackTarget.position - transform.position).sqrMagnitude;
        if (distSq > range * range)
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

        EnemyHp hp = enemy.CachedHp;
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

        // 레지스트리에 없으면 이미 DestroyEnemy 처리된 것
        if (enemyRegistry == null || !enemyRegistry.Contains(enemy))
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
        level = 1;
        goldUpgradeCount = 0;
        EnsureStatsFromData();
        ApplyVisualFromData();
        ResolveProjectilePrefabs();
    }

    public void SetUp(TowerSpawner _)
    {
        // TowerSpawner는 Tower MB가 보관. 여기선 타겟·풀·Behavior만.
        spriteRenderer = GetComponent<SpriteRenderer>();
        ServiceLocator.TryGet(out enemyRegistry);
        poolService = ServiceLocator.Get<IPoolService>();

        EnsureStatsFromData();
        ResolveProjectilePrefabs();

        behavior?.Deactivate();
        behavior = TowerBehaviorFactory.Create(weaponType, towerGrade);
        behavior.Initialize(this);
        behavior.Activate();
    }

    /// <summary>골드 업그레이드 완료 횟수 (0 ~ MaxGoldUpgrades).</summary>
    public int GoldUpgradeCount => goldUpgradeCount;

    public bool CanGoldUpgrade => GoldUpgradeCount < Constants.MaxGoldUpgrades;

    public bool UPGrade()
    {
        if (!CanGoldUpgrade)
        {
            return false;
        }

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
        goldUpgradeCount++;
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
