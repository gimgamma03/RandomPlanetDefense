using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static WaveData;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn")]
    [Tooltip("적 출현 위치 (블랙홀/게이트). 비우면 이 오브젝트 Transform 사용")]
    [SerializeField]
    private Transform spawnPoint;

    [SerializeField]
    private Transform canvasTransform;
    [SerializeField]
    private WaveSystem waveSystem;

    [SerializeField]
    private GameObject enemyHpSliderPrefab;
    [SerializeField]
    private int enemyHpPoolInitialSize = 16;

    private IPoolService poolService;
    private IPlayerService playerService;

    private Wave currentWave;
    private Coroutine spawnRoutine;

    /// <summary>스폰 코루틴이 아직 돌고 있거나, 필드에 적이 남아 웨이브 진행 중</summary>
    private bool waveActive;
    private bool spawnCompleted;

    public List<Enemy> enemyList;

    public bool IsWaveInProgress => waveActive;

    /// <summary>경로 시작점·스폰에 쓰는 월드 좌표</summary>
    public Vector3 SpawnWorldPosition =>
        spawnPoint != null ? spawnPoint.position : transform.position;

    void Start()
    {
        enemyList = new List<Enemy>();
        poolService = ServiceLocator.Get<IPoolService>();
        playerService = ServiceLocator.Get<IPlayerService>();
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
        foreach (Enemy enemy in enemyList)
        {
            if (enemy != null)
            {
                enemy.SetPath();
            }
        }
    }

    public void StartWave(Wave wave)
    {
        if (waveActive)
        {
            Debug.LogWarning("[EnemySpawner] Wave already in progress.");
            return;
        }

        currentWave = wave;
        waveActive = true;
        spawnCompleted = false;

        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
        }

        spawnRoutine = StartCoroutine(SpawnEnemy());
    }

    private IEnumerator SpawnEnemy()
    {
        int spawnEnemyCount = 0;
        float delay = Mathf.Max(0f, currentWave.spawnDelay);

        while (spawnEnemyCount < currentWave.maxEnemyCount)
        {
            GameObject selectEnemy = PickEnemyPrefab();
            if (selectEnemy == null)
            {
                Debug.LogError("[EnemySpawner] No valid enemy prefab in wave data. Aborting spawn.");
                break;
            }

            if (poolService == null)
            {
                poolService = ServiceLocator.Get<IPoolService>();
            }

            GameObject enemyObject = poolService.Spawn(
                selectEnemy,
                SpawnWorldPosition,
                Quaternion.identity,
                poolService.Root);
            yield return null;

            GameObject enemyHpSlider = SpawnEnemyHpSlider(enemyObject);
            Enemy enemy = enemyObject.GetComponent<Enemy>();
            EnemyHp enemyHp = enemyObject.GetComponent<EnemyHp>();
            EnemyHpViewer enemyHpViewer = enemyHpSlider.GetComponent<EnemyHpViewer>();

            enemy.PrepareForSpawn(this);
            enemyHp.PrepareForSpawn(enemyHpViewer);
            enemyHpViewer.hpSliderUpdate();

            enemyList.Add(enemy);
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

    /// <summary>
    /// 가중치 합이 1이 아니거나 float 오차가 있어도 반드시 유효 프리팹을 고른다.
    /// (예전 로직은 롤 실패 시 selectEnemy==null → while이 끝나지 않아 FinishWave가 영구 미호출됨)
    /// </summary>
    private GameObject PickEnemyPrefab()
    {
        WaveEnemy[] entries = currentWave.enemies;
        if (entries == null || entries.Length == 0)
        {
            return null;
        }

        float totalWeight = 0f;
        GameObject lastValid = null;

        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].enemyPrefab == null)
            {
                continue;
            }

            lastValid = entries[i].enemyPrefab;
            totalWeight += Mathf.Max(0f, entries[i].enemyPercentage);
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
            if (entries[i].enemyPrefab == null)
            {
                continue;
            }

            cumulative += Mathf.Max(0f, entries[i].enemyPercentage);
            if (roll <= cumulative)
            {
                return entries[i].enemyPrefab;
            }
        }

        return lastValid;
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
        if (enemy == null || !enemyList.Contains(enemy))
        {
            return;
        }

        int gold = enemy.GetGold();
        int scorePoint = enemy.GetScorePoint();

        if (type == EnemyDestroyType.Arrive)
        {
            if (playerService == null)
            {
                playerService = ServiceLocator.Get<IPlayerService>();
            }

            playerService.TakeDamage(Constants.enemyGoalInDamage);
        }
        else if (type == EnemyDestroyType.Kill)
        {
            if (playerService == null)
            {
                playerService = ServiceLocator.Get<IPlayerService>();
            }

            playerService.AddGold(gold);
            waveSystem.AddScore(scorePoint);
        }

        enemyList.Remove(enemy);

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
        TryFinishWave();
    }

    /// <summary>스폰이 끝났고 필드 적이 0이면 웨이브 클리어.</summary>
    private void TryFinishWave()
    {
        if (!waveActive || !spawnCompleted)
        {
            return;
        }

        if (enemyList.Count > 0)
        {
            return;
        }

        waveActive = false;
        waveSystem.FinishWave();
    }
}