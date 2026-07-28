using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static StageData;

public class EnemySpawner : MonoBehaviour, IEnemyRegistry
{
    [Header("Spawn")]
    [Tooltip("적 출현 위치 (블랙홀/게이트). 비우면 이 오브젝트 Transform 사용")]
    [SerializeField]
    private Transform spawnPoint;

    [Tooltip("공통 적 Base 프리팹 (Prefabs/Enemies/EnemyBase)")]
    [SerializeField]
    private GameObject enemyBasePrefab;

    [SerializeField]
    private Transform canvasTransform;
    [SerializeField]
    private WaveSystem waveSystem;

    [SerializeField]
    private GameObject enemyHpSliderPrefab;
    [SerializeField]
    private int enemyHpPoolInitialSize = 16;

    [SerializeField]
    private int enemyPoolInitialSize = 16;

    private IPoolService poolService;
    private IPlayerService playerService;
    private EnemyCatalog enemyCatalog;
    private readonly EnemyDeathHandler deathHandler = new EnemyDeathHandler();

    private Wave currentWave;
    private Coroutine spawnRoutine;
    private StageData pendingBossStage;

    /// <summary>스폰 코루틴이 아직 돌고 있거나, 필드에 적이 남아 웨이브 진행 중</summary>
    private bool waveActive;
    private bool spawnCompleted;

    private readonly List<Enemy> enemies = new List<Enemy>();

    public int Count => enemies.Count;

    public bool IsWaveInProgress => waveActive;

    /// <summary>경로 시작점·스폰에 쓰는 월드 좌표</summary>
    public Vector3 SpawnWorldPosition =>
        spawnPoint != null ? spawnPoint.position : transform.position;

    private void Awake()
    {
        ServiceLocator.Register<IEnemyRegistry>(this);
    }

    public Enemy GetEnemy(int index)
    {
        return enemies[index];
    }

    public bool Contains(Enemy enemy)
    {
        return enemy != null && enemies.Contains(enemy);
    }

    void Start()
    {
        poolService = ServiceLocator.Get<IPoolService>();
        playerService = ServiceLocator.Get<IPlayerService>();
        enemyCatalog = EnemyCatalog.LoadFromResources();
        deathHandler.EnsureServices();

        if (enemyBasePrefab == null)
        {
            Debug.LogError("[EnemySpawner] enemyBasePrefab 미할당. Prefabs/Enemies/EnemyBase를 넣으세요.");
        }
        else
        {
            poolService.EnsurePool(
                PoolId.Enemy,
                enemyBasePrefab,
                poolService.Root,
                enemyPoolInitialSize);
        }

        if (enemyHpSliderPrefab != null)
        {
            poolService.EnsurePool(
                PoolId.EnemyHp,
                enemyHpSliderPrefab,
                canvasTransform,
                enemyHpPoolInitialSize);
        }
    }

    public void CheckPathForAllEnemy()
    {
        for (int i = 0; i < enemies.Count; i++)
        {
            Enemy enemy = enemies[i];
            if (enemy != null)
            {
                enemy.SetPath();
            }
        }
    }

    public void StartWave(Wave wave)
    {
        StartWave(wave, null);
    }

    /// <param name="bossStage">최종 웨이브일 때 StageData를 넘기면 보스 1마리 추가</param>
    public void StartWave(Wave wave, StageData bossStage)
    {
        if (waveSystem != null && waveSystem.IsRunEnded)
        {
            return;
        }

        if (waveActive)
        {
            Debug.LogWarning("[EnemySpawner] Wave already in progress.");
            return;
        }

        if (enemyCatalog == null)
        {
            enemyCatalog = EnemyCatalog.LoadFromResources();
        }

        currentWave = wave;
        pendingBossStage = bossStage;
        waveActive = true;
        spawnCompleted = false;

        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
        }

        spawnRoutine = StartCoroutine(SpawnEnemy());
    }

    /// <summary>게임오버 시 웨이브 클리어 판정·추가 스폰을 막는다.</summary>
    public void StopWaveForGameOver()
    {
        waveActive = false;
        spawnCompleted = true;
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    private IEnumerator SpawnEnemy()
    {
        if (enemyBasePrefab == null)
        {
            Debug.LogError("[EnemySpawner] No EnemyBase prefab assigned.");
            spawnRoutine = null;
            spawnCompleted = true;
            TryFinishWave();
            yield break;
        }

        // 최종 웨이브: 보스 먼저 1마리
        if (pendingBossStage != null && pendingBossStage.spawnBossOnFinalWave)
        {
            TrySpawnStageBoss(pendingBossStage);
        }

        pendingBossStage = null;

        int spawnEnemyCount = 0;
        float delay = Mathf.Max(0f, currentWave.spawnDelay);

        while (spawnEnemyCount < currentWave.maxEnemyCount)
        {
            EnemyData data = PickEnemyData();
            if (data == null)
            {
                Debug.LogError("[EnemySpawner] No valid EnemyType in wave. Aborting spawn.");
                break;
            }

            SpawnEnemyInstance(data, SpawnWorldPosition, splitGeneration: 0);
            spawnEnemyCount++;

            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }
        }

        spawnRoutine = null;
        spawnCompleted = true;
        TryFinishWave();
    }

    private void TrySpawnStageBoss(StageData stage)
    {
        if (enemyCatalog == null)
        {
            enemyCatalog = EnemyCatalog.LoadFromResources();
        }

        if (!enemyCatalog.TryGet(stage.bossEnemyType, stage.bossEnemyTier, out EnemyData boss)
            || boss == null)
        {
            Debug.LogWarning(
                $"[EnemySpawner] Boss missing: {stage.bossEnemyType} T{(int)stage.bossEnemyTier}");
            return;
        }

        SpawnEnemyInstance(boss, SpawnWorldPosition, splitGeneration: 0);
    }

    /// <summary>
    /// spawnWeight 상대 비중으로 고른다. 합이 1이 아니어도 된다.
    /// </summary>
    private EnemyData PickEnemyData()
    {
        WaveEnemy[] entries = currentWave.enemies;
        if (entries == null || entries.Length == 0 || enemyCatalog == null)
        {
            return null;
        }

        float totalWeight = 0f;
        EnemyData lastValid = null;

        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].spawnWeight <= 0f)
            {
                continue;
            }

            if (!enemyCatalog.TryGet(entries[i].enemyType, ResolveTier(entries[i]), out EnemyData data))
            {
                continue;
            }

            lastValid = data;
            totalWeight += entries[i].spawnWeight;
        }

        if (lastValid == null)
        {
            return null;
        }

        if (totalWeight <= 0f)
        {
            return lastValid;
        }

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].spawnWeight <= 0f)
            {
                continue;
            }

            if (!enemyCatalog.TryGet(entries[i].enemyType, ResolveTier(entries[i]), out EnemyData data))
            {
                continue;
            }

            cumulative += entries[i].spawnWeight;
            if (roll <= cumulative)
            {
                return data;
            }
        }

        return lastValid;
    }

    private static EnemyTier ResolveTier(WaveEnemy entry)
    {
        // 구 Stage SO에 enemyTier 필드가 없으면 0 → Tier1
        // 레거시 RunnerElite 타입은 Runner T2로 취급
        if (entry.enemyType == EnemyType.RunnerElite)
        {
            return EnemyTier.Tier2;
        }

        int value = (int)entry.enemyTier;
        if (value < (int)EnemyTier.Tier1)
        {
            return EnemyTier.Tier1;
        }

        if (value > (int)EnemyTier.Tier3)
        {
            return EnemyTier.Tier3;
        }

        return entry.enemyTier;
    }

    private GameObject SpawnEnemyHpSlider(GameObject enemy)
    {
        GameObject sliderClone = poolService.Spawn(PoolId.EnemyHp, canvasTransform);
        if (sliderClone == null)
        {
            sliderClone = poolService.Spawn(
                enemyHpSliderPrefab,
                Vector3.zero,
                Quaternion.identity,
                canvasTransform);
        }

        sliderClone.transform.SetParent(canvasTransform, false);
        sliderClone.transform.localScale = Vector3.one;

        sliderClone.GetComponent<SliderPositionAutoSetter>().Setup(enemy.transform);
        sliderClone.GetComponent<EnemyHpViewer>().Setup(enemy.GetComponent<EnemyHp>());

        return sliderClone;
    }

    public void DestroyEnemy(EnemyDestroyType type, Enemy enemy)
    {
        if (enemy == null || !enemies.Contains(enemy))
        {
            return;
        }

        Vector3 deathPosition = enemy.transform.position;
        bool shouldSplit = type == EnemyDestroyType.Kill && enemy.CanSplitOnKill;
        EnemyData splitParentData = enemy.enemyData;

        deathHandler.ApplyOutcome(type, enemy);

        enemies.Remove(enemy);
        ReturnEnemyToPool(enemy);

        if (shouldSplit && splitParentData != null)
        {
            SpawnSplitChildren(splitParentData, deathPosition);
        }

        TryFinishWave();
    }

    private void ReturnEnemyToPool(Enemy enemy)
    {
        EnemyHp enemyHp = enemy.GetComponent<EnemyHp>();
        if (enemyHp != null)
        {
            enemyHp.ReleaseViewerToPool();
            enemyHp.ClearForPool();
        }

        enemy.ClearForPool();

        if (poolService == null)
        {
            poolService = ServiceLocator.Get<IPoolService>();
        }

        poolService.Return(enemy.gameObject);
    }

    /// <summary>보스 소환 스킬 — 한 마리. 웨이브 카운트와 무관하게 enemies에 추가.</summary>
    public void SpawnBossMinion(EnemyType minionType, Vector3 position)
    {
        if (enemyCatalog == null)
        {
            enemyCatalog = EnemyCatalog.LoadFromResources();
        }

        if (!enemyCatalog.TryGet(minionType, EnemyTier.Tier1, out EnemyData childData)
            || childData == null)
        {
            Debug.LogWarning($"[EnemySpawner] Boss minion missing: {minionType}");
            return;
        }

        SpawnEnemyInstance(childData, position, splitGeneration: 1);
    }

    /// <summary>보스 소환 — 원형 배치로 여러 마리 한 번에 (레거시/일괄용).</summary>
    public void SpawnBossMinions(EnemyType minionType, Vector3 center, int count)
    {
        if (count <= 0)
        {
            return;
        }

        int n = Mathf.Max(1, count);
        for (int i = 0; i < n; i++)
        {
            float angle = (Mathf.PI * 2f / n) * i;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 0.35f;
            SpawnBossMinion(minionType, center + offset);
        }
    }

    /// <summary>Splitter 사망 시 잔해 스폰. 웨이브 카운트와 무관하게 enemies에 추가.</summary>
    private void SpawnSplitChildren(EnemyData parentData, Vector3 position)
    {
        if (enemyCatalog == null)
        {
            enemyCatalog = EnemyCatalog.LoadFromResources();
        }

        if (!enemyCatalog.TryGet(parentData.splitChildType, EnemyTier.Tier1, out EnemyData childData) || childData == null)
        {
            Debug.LogWarning(
                $"[EnemySpawner] Split child type missing: {parentData.splitChildType}");
            return;
        }

        int count = Mathf.Max(0, parentData.splitCount);
        for (int i = 0; i < count; i++)
        {
            float angle = (Mathf.PI * 2f / Mathf.Max(1, count)) * i;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * 0.25f;
            SpawnEnemyInstance(childData, position + offset, splitGeneration: 1);
        }
    }

    private void SpawnEnemyInstance(EnemyData data, Vector3 position, int splitGeneration)
    {
        if (enemyBasePrefab == null || data == null)
        {
            return;
        }

        if (waveSystem != null && waveSystem.IsRunEnded)
        {
            return;
        }

        if (poolService == null)
        {
            poolService = ServiceLocator.Get<IPoolService>();
        }

        GameObject enemyObject = poolService.Spawn(
            PoolId.Enemy,
            position,
            Quaternion.identity,
            poolService.Root);
        if (enemyObject == null)
        {
            enemyObject = poolService.Spawn(
                enemyBasePrefab,
                position,
                Quaternion.identity,
                poolService.Root);
        }

        if (enemyObject == null)
        {
            return;
        }

        GameObject enemyHpSlider = SpawnEnemyHpSlider(enemyObject);
        Enemy enemy = enemyObject.GetComponent<Enemy>();
        EnemyHp enemyHp = enemyObject.GetComponent<EnemyHp>();
        EnemyHpViewer enemyHpViewer = enemyHpSlider.GetComponent<EnemyHpViewer>();

        enemy.BindDefinition(data);
        enemy.PrepareForSpawn(this, splitGeneration);
        enemyHp.PrepareForSpawn(enemyHpViewer);
        enemyHpViewer.hpSliderUpdate();

        enemies.Add(enemy);
    }

    /// <summary>스폰이 끝났고 필드 적이 0이면 웨이브 클리어.</summary>
    private void TryFinishWave()
    {
        if (waveSystem != null && waveSystem.IsRunEnded)
        {
            return;
        }

        if (playerService == null)
        {
            ServiceLocator.TryGet(out playerService);
        }

        if (playerService != null && playerService.IsGameOver)
        {
            return;
        }

        if (!waveActive || !spawnCompleted)
        {
            return;
        }

        if (enemies.Count > 0)
        {
            return;
        }

        waveActive = false;
        waveSystem.FinishWave();
    }
}
