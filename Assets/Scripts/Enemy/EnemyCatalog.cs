using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resources/EnemyData 아래 EnemyData SO를 전부 수집.
/// Stage 웨이브는 EnemyType + EnemyTier로 정의를 해석한다.
/// </summary>
public sealed class EnemyCatalog
{
    public const string ResourcesFolder = "EnemyData";

    private readonly Dictionary<long, EnemyData> byTypeAndTier =
        new Dictionary<long, EnemyData>();

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

            EnemyTier tier = ClampTier(data.enemyTier);
            long key = MakeKey(data.enemyType, tier);

            if (byTypeAndTier.ContainsKey(key))
            {
                Debug.LogWarning(
                    $"[EnemyCatalog] Duplicate {data.enemyType} T{(int)tier}: " +
                    $"{byTypeAndTier[key].name} vs {data.name}. Keeping first.");
                continue;
            }

            byTypeAndTier[key] = data;
        }
    }

    public static EnemyCatalog LoadFromResources()
    {
        EnemyData[] loaded = Resources.LoadAll<EnemyData>(ResourcesFolder);
        if (loaded == null || loaded.Length == 0)
        {
            Debug.LogError($"[EnemyCatalog] Resources/{EnemyCatalog.ResourcesFolder} 에 EnemyData가 없습니다.");
            return new EnemyCatalog(System.Array.Empty<EnemyData>());
        }

        Debug.Log($"[EnemyCatalog] Loaded {loaded.Length} EnemyData from Resources/{ResourcesFolder}");
        return new EnemyCatalog(loaded);
    }

    public bool TryGet(EnemyType type, out EnemyData data)
    {
        return TryGet(type, EnemyTier.Tier1, out data);
    }

    public bool TryGet(EnemyType type, EnemyTier tier, out EnemyData data)
    {
        // 레거시 RunnerElite 웨이브 슬롯 → Runner T2
        if (type == EnemyType.RunnerElite)
        {
            type = EnemyType.Runner;
            tier = EnemyTier.Tier2;
        }

        tier = ClampTier(tier);
        if (byTypeAndTier.TryGetValue(MakeKey(type, tier), out data) && data != null)
        {
            return true;
        }

        if (tier != EnemyTier.Tier1
            && byTypeAndTier.TryGetValue(MakeKey(type, EnemyTier.Tier1), out data)
            && data != null)
        {
            return true;
        }

        data = null;
        return false;
    }

    public EnemyData Get(EnemyType type, EnemyTier tier = EnemyTier.Tier1)
    {
        if (TryGet(type, tier, out EnemyData data))
        {
            return data;
        }

        Debug.LogError($"[EnemyCatalog] Missing EnemyData for {type} T{(int)ClampTier(tier)}");
        return null;
    }

    private static EnemyTier ClampTier(EnemyTier tier)
    {
        int value = (int)tier;
        if (value < (int)EnemyTier.Tier1)
        {
            return EnemyTier.Tier1;
        }

        if (value > (int)EnemyTier.Tier3)
        {
            return EnemyTier.Tier3;
        }

        return tier;
    }

    private static long MakeKey(EnemyType type, EnemyTier tier)
    {
        return ((long)(int)type << 32) | (uint)(int)tier;
    }
}
