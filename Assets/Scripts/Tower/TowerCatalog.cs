using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 런타임 타워 풀. Resources/TowerData 아래 TowerData SO를 전부 자동 수집.
/// Catalog 에셋에 수동으로 넣을 필요 없음.
/// </summary>
public sealed class TowerCatalog
{
    public const string ResourcesFolder = "TowerData";

    private readonly TowerData[] towers;

    public IReadOnlyList<TowerData> Towers => towers;

    public TowerCatalog(TowerData[] towers)
    {
        this.towers = towers ?? System.Array.Empty<TowerData>();
    }

    public static TowerCatalog LoadFromResources()
    {
        TowerData[] loaded = Resources.LoadAll<TowerData>(ResourcesFolder);
        if (loaded == null || loaded.Length == 0)
        {
            Debug.LogError($"[TowerCatalog] Resources/{ResourcesFolder} 에 TowerData가 없습니다.");
            return new TowerCatalog(System.Array.Empty<TowerData>());
        }

        Debug.Log($"[TowerCatalog] Loaded {loaded.Length} TowerData from Resources/{ResourcesFolder}");
        return new TowerCatalog(loaded);
    }

    public bool TryGet(string id, out TowerData data)
    {
        data = null;
        if (string.IsNullOrEmpty(id))
        {
            return false;
        }

        for (int i = 0; i < towers.Length; i++)
        {
            if (towers[i] != null && towers[i].Id == id)
            {
                data = towers[i];
                return true;
            }
        }

        return false;
    }

    public bool HasAny(TowerGrade grade)
    {
        for (int i = 0; i < towers.Length; i++)
        {
            if (IsSpawnable(towers[i], grade))
            {
                return true;
            }
        }

        return false;
    }

    public TowerData PickRandom(TowerGrade grade)
    {
        float total = 0f;
        for (int i = 0; i < towers.Length; i++)
        {
            if (!IsSpawnable(towers[i], grade))
            {
                continue;
            }

            total += towers[i].spawnWeight;
        }

        if (total <= 0f)
        {
            return null;
        }

        float roll = Random.Range(0f, total);
        float cumulative = 0f;
        TowerData last = null;

        for (int i = 0; i < towers.Length; i++)
        {
            TowerData t = towers[i];
            if (!IsSpawnable(t, grade))
            {
                continue;
            }

            last = t;
            cumulative += t.spawnWeight;
            if (roll <= cumulative)
            {
                return t;
            }
        }

        return last;
    }

    public GameObject ResolvePrefab(TowerData data)
    {
        return ResolvePrefab(data, TowerBaseLibrary.Load());
    }

    public GameObject ResolvePrefab(TowerData data, TowerBaseLibrary baseLibrary)
    {
        if (data == null)
        {
            return null;
        }

        if (baseLibrary == null)
        {
            return null;
        }

        return baseLibrary.GetBasePrefab(data.weaponType);
    }

    private static bool IsSpawnable(TowerData data, TowerGrade grade)
    {
        if (data == null || data.grade != grade || data.spawnWeight <= 0f)
        {
            return false;
        }

        // 프리팹은 TowerBaseLibrary(weaponType)로만 해석
        return true;
    }
}