using UnityEngine;

[CreateAssetMenu(menuName = "RPD/Wave Data", fileName = "WaveData")]
public class WaveData : ScriptableObject
{
    public Wave[] waves;

    [System.Serializable]
    public struct Wave
    {
        public int spawnDelay;
        public int maxEnemyCount;
        public WaveEnemy[] enemies;
    }

    [System.Serializable]
    public struct WaveEnemy
    {
        [Tooltip("적 정의 SO (권장)")]
        public EnemyData enemyData;

        [Tooltip("레거시. enemyData 비어 있을 때만 사용 (마이그레이션용)")]
        public GameObject enemyPrefab;

        [Range(0f, 1f)]
        public float enemyPercentage;
    }

    private void OnValidate()
    {
        if (waves == null)
        {
            return;
        }

        for (int i = 0; i < waves.Length; i++)
        {
            if (waves[i].enemies == null || waves[i].enemies.Length == 0)
            {
                continue;
            }

            float totalPercentage = 0f;
            for (int j = 0; j < waves[i].enemies.Length; j++)
            {
                totalPercentage += waves[i].enemies[j].enemyPercentage;
            }

            if (totalPercentage <= 0f || Mathf.Approximately(totalPercentage, 1f))
            {
                continue;
            }

            float adjustment = 1f / totalPercentage;
            for (int j = 0; j < waves[i].enemies.Length; j++)
            {
                waves[i].enemies[j].enemyPercentage *= adjustment;
            }
        }
    }
}
