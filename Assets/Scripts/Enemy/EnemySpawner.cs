using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static WaveData;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField]
    private Player player;
    [SerializeField]
    private Transform canvasTransform;
    [SerializeField]
    private WaveSystem waveSystem;

    [SerializeField]
    private GameObject enemyHpSliderPrefab;
    [SerializeField]
    private GameObjectPoolManager poolManager;
    [SerializeField]
    private int enemyHpPoolInitialSize = 16;

    private float LastSpawnTime;
    private int currentEnemyCount;
    private Wave currentWave;
    private int currentWaveCount;
    public List<Enemy> enemyList;

    void Start()
    {
        enemyList = new List<Enemy>();
        poolManager = GameObjectPoolManager.EnsureExists();
        if (enemyHpSliderPrefab != null)
        {
            poolManager.EnsurePool(
                PoolId.EnemyHp,
                enemyHpSliderPrefab,
                canvasTransform,
                enemyHpPoolInitialSize);
        }
    }

    void Update()
    {
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
        currentWave = wave;
        currentWaveCount = wave.maxEnemyCount;
        StartCoroutine("SpawnEnemy");
    }

    private IEnumerator SpawnEnemy()
    {
        int spawnEnemyCount = 0;
        while (spawnEnemyCount < currentWave.maxEnemyCount)
        {
            float randomSpawnRoll = Random.value;
            GameObject selectEnemy = null;

            foreach (var enemyInWave in currentWave.enemies)
            {
                if (randomSpawnRoll <= enemyInWave.enemyPercentage)
                {
                    selectEnemy = enemyInWave.enemyPrefab;
                    break;
                }

                randomSpawnRoll -= enemyInWave.enemyPercentage;
            }

            if (selectEnemy != null)
            {
                if (poolManager == null)
                {
                    poolManager = GameObjectPoolManager.EnsureExists();
                }

                GameObject enemyObject = poolManager.Spawn(
                    selectEnemy,
                    transform.position,
                    Quaternion.identity,
                    transform);
                yield return new WaitForEndOfFrame();

                GameObject enemyHpSlider = SpawnEnemyHpSlider(enemyObject);
                Enemy enemy = enemyObject.GetComponent<Enemy>();
                EnemyHp enemyHp = enemyObject.GetComponent<EnemyHp>();
                EnemyHpViewer enemyHpViewer = enemyHpSlider.GetComponent<EnemyHpViewer>();

                enemy.PrepareForSpawn(this);
                enemyHp.PrepareForSpawn(enemyHpViewer);
                enemyHpViewer.hpSliderUpdate();

                enemyList.Add(enemy);
                currentEnemyCount++;

                spawnEnemyCount++;
                yield return new WaitForSeconds(currentWave.spawnDelay);
            }
        }

        waveSystem.FinishWave();
    }

    private GameObject SpawnEnemyHpSlider(GameObject enemy)
    {
        GameObject sliderClone = poolManager.Spawn(PoolId.EnemyHp, canvasTransform);
        if (sliderClone == null)
        {
            sliderClone = poolManager.Spawn(
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
            player.TakeDamage(Constants.enemyGoalInDamage);
        }
        else if (type == EnemyDestroyType.Kill)
        {
            player.gold += gold;
            waveSystem.scoreSystem.AddScore(scorePoint);
        }

        currentEnemyCount--;
        enemyList.Remove(enemy);

        EnemyHp enemyHp = enemy.GetComponent<EnemyHp>();
        if (enemyHp != null)
        {
            enemyHp.ReleaseViewerToPool();
            enemyHp.ClearForPool();
        }

        enemy.ClearForPool();

        if (poolManager == null)
        {
            poolManager = GameObjectPoolManager.EnsureExists();
        }

        poolManager.Return(enemy.gameObject);
    }
}
