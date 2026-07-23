using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SnakeDefender와 같은 중앙 풀 매니저.
/// PoolId로 등록하거나, 프리팹 키로 자동 확장(적·탄 종류 증가 / 이후 Addressables 대비).
/// </summary>
public enum PoolId
{
    EnemyHp = 0,
    Projectile = 1,
    TargetProjectile = 2,
    BombProjectile = 3,
    Bomb = 4
}

[DefaultExecutionOrder(-200)]
public class GameObjectPoolManager : MonoBehaviour, IPoolService
{
    public static GameObjectPoolManager Instance { get; private set; }

    public Transform Root => transform;

    [System.Serializable]
    private class PoolConfig
    {
        public PoolId id = PoolId.EnemyHp;
        public GameObject prefab;
        [Min(0)] public int initialSize = 8;
        public bool canExpand = true;
        public Transform defaultParent;
    }

    private sealed class PoolRuntime
    {
        public GameObject Prefab;
        public PoolId? Id;
        public int InitialSize;
        public bool CanExpand = true;
        public Transform DefaultParent;
        public readonly Queue<PooledObject> Inactive = new Queue<PooledObject>();
    }

    [SerializeField] private List<PoolConfig> poolConfigs = new List<PoolConfig>();
    [SerializeField] private int autoPoolInitialSize = 4;

    private readonly Dictionary<PoolId, PoolRuntime> poolById = new Dictionary<PoolId, PoolRuntime>();
    private readonly Dictionary<int, PoolRuntime> poolByPrefabId = new Dictionary<int, PoolRuntime>();

    public static GameObjectPoolManager EnsureExists()
    {
        if (ServiceLocator.TryGet(out IPoolService pool) && pool is GameObjectPoolManager manager)
        {
            return manager;
        }

        if (Instance != null)
        {
            return Instance;
        }

        GameObjectPoolManager found = FindFirstObjectByType<GameObjectPoolManager>();
        if (found != null)
        {
            return found;
        }

        // Bootstrapper가 아직이면 생성은 Bootstrapper에 맡기고, 최후 수단만 직접 생성
        GameBootstrapper bootstrapper = FindFirstObjectByType<GameBootstrapper>();
        if (bootstrapper == null)
        {
            new GameObject("[GameBootstrapper]").AddComponent<GameBootstrapper>();
        }

        if (ServiceLocator.TryGet(out IPoolService created) && created is GameObjectPoolManager ready)
        {
            return ready;
        }

        GameObject go = new GameObject("[GameObjectPoolManager]");
        return go.AddComponent<GameObjectPoolManager>();
    }

    /// <summary>IService — Awake에서 풀 구성이 끝나므로 추가 작업 없음</summary>
    public void Initialize()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        if (poolById.Count == 0 && poolByPrefabId.Count == 0)
        {
            InitializeConfiguredPools();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InitializeConfiguredPools();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public GameObject Spawn(PoolId id, Transform parentOverride = null)
    {
        if (!poolById.TryGetValue(id, out PoolRuntime pool))
        {
            Debug.LogWarning($"[GameObjectPoolManager] PoolId not registered: {id}", this);
            return null;
        }

        return SpawnFromPool(pool, parentOverride, null, null);
    }

    public GameObject Spawn(PoolId id, Vector3 position, Quaternion rotation, Transform parentOverride = null)
    {
        GameObject go = Spawn(id, parentOverride);
        if (go != null)
        {
            go.transform.SetPositionAndRotation(position, rotation);
        }

        return go;
    }

    /// <summary>프리팹별 풀. 미등록이면 자동 생성(확장용).</summary>
    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parentOverride = null)
    {
        if (prefab == null)
        {
            return null;
        }

        PoolRuntime pool = GetOrCreatePrefabPool(prefab, parentOverride);
        return SpawnFromPool(pool, parentOverride, position, rotation);
    }

    public void EnsurePool(PoolId id, GameObject prefab, Transform defaultParent = null, int initialSize = -1, bool canExpand = true)
    {
        if (prefab == null)
        {
            return;
        }

        if (poolById.ContainsKey(id))
        {
            return;
        }

        if (initialSize < 0)
        {
            initialSize = autoPoolInitialSize;
        }

        PoolRuntime runtime = CreateRuntime(prefab, id, initialSize, canExpand, defaultParent);
        poolById[id] = runtime;
        poolByPrefabId[prefab.GetInstanceID()] = runtime;
        Prewarm(runtime);
    }

    public void Return(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        PooledObject pooled = instance.GetComponent<PooledObject>();
        if (pooled == null || pooled.Owner != this)
        {
            Destroy(instance);
            return;
        }

        if (pooled.IsInPool)
        {
            return;
        }

        PoolRuntime pool = pooled.Runtime as PoolRuntime;
        if (pool == null)
        {
            Destroy(instance);
            return;
        }

        pooled.IsInPool = true;
        instance.SetActive(false);

        Transform defaultParent = pool.DefaultParent != null ? pool.DefaultParent : transform;
        instance.transform.SetParent(defaultParent, false);
        pool.Inactive.Enqueue(pooled);
    }

    private void InitializeConfiguredPools()
    {
        poolById.Clear();
        poolByPrefabId.Clear();

        for (int i = 0; i < poolConfigs.Count; i++)
        {
            PoolConfig config = poolConfigs[i];
            if (config == null || config.prefab == null)
            {
                continue;
            }

            if (poolById.ContainsKey(config.id))
            {
                Debug.LogWarning($"[GameObjectPoolManager] Duplicate PoolId: {config.id}", this);
                continue;
            }

            PoolRuntime runtime = CreateRuntime(
                config.prefab,
                config.id,
                config.initialSize,
                config.canExpand,
                config.defaultParent);

            poolById[config.id] = runtime;
            poolByPrefabId[config.prefab.GetInstanceID()] = runtime;
            Prewarm(runtime);
        }
    }

    private PoolRuntime GetOrCreatePrefabPool(GameObject prefab, Transform preferredParent)
    {
        int key = prefab.GetInstanceID();
        if (poolByPrefabId.TryGetValue(key, out PoolRuntime existing))
        {
            return existing;
        }

        PoolRuntime runtime = CreateRuntime(prefab, null, autoPoolInitialSize, true, preferredParent);
        poolByPrefabId[key] = runtime;
        Prewarm(runtime);
        return runtime;
    }

    private PoolRuntime CreateRuntime(
        GameObject prefab,
        PoolId? id,
        int initialSize,
        bool canExpand,
        Transform defaultParent)
    {
        return new PoolRuntime
        {
            Prefab = prefab,
            Id = id,
            InitialSize = initialSize,
            CanExpand = canExpand,
            DefaultParent = defaultParent
        };
    }

    private void Prewarm(PoolRuntime pool)
    {
        for (int i = 0; i < pool.InitialSize; i++)
        {
            PooledObject pooled = CreatePooledObject(pool);
            if (pooled == null)
            {
                break;
            }

            pool.Inactive.Enqueue(pooled);
        }
    }

    private GameObject SpawnFromPool(
        PoolRuntime pool,
        Transform parentOverride,
        Vector3? position,
        Quaternion? rotation)
    {
        PooledObject pooled = null;
        while (pool.Inactive.Count > 0 && pooled == null)
        {
            pooled = pool.Inactive.Dequeue();
        }

        if (pooled == null)
        {
            if (!pool.CanExpand)
            {
                return null;
            }

            pooled = CreatePooledObject(pool);
            if (pooled == null)
            {
                return null;
            }
        }

        Transform targetParent = parentOverride != null
            ? parentOverride
            : (pool.DefaultParent != null ? pool.DefaultParent : transform);

        if (targetParent != null)
        {
            pooled.transform.SetParent(targetParent, false);
        }

        if (position.HasValue && rotation.HasValue)
        {
            pooled.transform.SetPositionAndRotation(position.Value, rotation.Value);
        }
        else if (position.HasValue)
        {
            pooled.transform.position = position.Value;
        }

        pooled.IsInPool = false;
        pooled.gameObject.SetActive(true);
        return pooled.gameObject;
    }

    private PooledObject CreatePooledObject(PoolRuntime pool)
    {
        Transform parent = pool.DefaultParent != null ? pool.DefaultParent : transform;
        GameObject go = Instantiate(pool.Prefab, parent);
        go.SetActive(false);

        PooledObject pooled = go.GetComponent<PooledObject>();
        if (pooled == null)
        {
            pooled = go.AddComponent<PooledObject>();
        }

        pooled.Owner = this;
        pooled.Runtime = pool;
        pooled.PoolId = pool.Id;
        pooled.IsInPool = true;
        return pooled;
    }
}

public class PooledObject : MonoBehaviour
{
    public GameObjectPoolManager Owner { get; set; }
    public PoolId? PoolId { get; set; }
    public bool IsInPool { get; set; } = true;

    /// <summary>프리팹 키 풀용 런타임 핸들 (매니저 내부).</summary>
    internal object Runtime { get; set; }

    public void ReturnToPool()
    {
        if (Owner != null)
        {
            Owner.Return(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
