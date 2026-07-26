using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resources/EnemyData 아래 EnemyData SO를 전부 수집.
/// Stage 웨이브는 EnemyType만 들고, 여기서 정의로 해석한다.
/// </summary>
public sealed class EnemyCatalog
{
    public const string ResourcesFolder = "EnemyData";

    private readonly Dictionary<EnemyType, EnemyData> byType =
        new Dictionary<EnemyType, EnemyData>();

    public IReadOnlyDictionary<EnemyType, EnemyData> ByType => byType;

    public EnemyCatalog(EnemyData[] enemies)
    {
        if (enemies == null)
        {
            return;
        }

        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyData data = enemies[i];
            if (data == null)
            {
                continue;
            }

            if (byType.ContainsKey(data.enemyType))
            {
                Debug.LogWarning(
                    $"[EnemyCatalog] Duplicate EnemyType {data.enemyType}: " +
                    $"{byType[data.enemyType].name} vs {data.name}. Keeping first.");
                continue;
            }

            byType[data.enemyType] = data;
        }
    }

    public static EnemyCatalog LoadFromResources()
    {
        EnemyData[] loaded = Resources.LoadAll<EnemyData>(ResourcesFolder);
        if (loaded == null || loaded.Length == 0)
        {
            Debug.LogError($"[EnemyCatalog] Resources/{ResourcesFolder} 에 EnemyData가 없습니다.");
            return new EnemyCatalog(System.Array.Empty<EnemyData>());
        }

        Debug.Log($"[EnemyCatalog] Loaded {loaded.Length} EnemyData from Resources/{ResourcesFolder}");
        return new EnemyCatalog(loaded);
    }

    public bool TryGet(EnemyType type, out EnemyData data)
    {
        return byType.TryGetValue(type, out data) && data != null;
    }

    public EnemyData Get(EnemyType type)
    {
        if (TryGet(type, out EnemyData data))
        {
            return data;
        }

        Debug.LogError($"[EnemyCatalog] Missing EnemyData for {type}");
        return null;
    }
}
