using UnityEngine;

[CreateAssetMenu]
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
        public GameObject enemyPrefab;
        [Range(0f, 1f)]
        public float enemyPercentage;
    }

    private void OnValidate()
    {
        float totalPercentage = 0f;

        for (int i = 0; i < waves.Length; i++)
        {
            totalPercentage = 0f;

            for (int j = 0; j < waves[i].enemies.Length; j++)
            {
                totalPercentage += waves[i].enemies[j].enemyPercentage;
            }

            float adjustment = 1f / totalPercentage;

            //적 출현 확률 합이 1이 되도록 조정
            if (totalPercentage != 1f)
            {
                for (int j = 0; j < waves[i].enemies.Length; j++)
                {
                    waves[i].enemies[j].enemyPercentage *= adjustment;
                }
            }
        }
    }
}
