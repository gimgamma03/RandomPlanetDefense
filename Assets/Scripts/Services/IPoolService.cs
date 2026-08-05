using UnityEngine;

/// <summary>오브젝트 풀 서비스 (MonoBehaviour 구현체는 GameObjectPoolManager)</summary>
public interface IPoolService : IService
{
    GameObject Spawn(PoolId id, Transform parentOverride = null);

    GameObject Spawn(PoolId id, Vector3 position, Quaternion rotation, Transform parentOverride = null);

    GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parentOverride = null);

    void Return(GameObject instance);

    /// <summary>풀에서 꺼낸 오브젝트를 붙일 기본 루트 (보통 PoolManager Transform)</summary>
    Transform Root { get; }

    void EnsurePool(        PoolId id,
        GameObject prefab,
        Transform defaultParent = null,
        int initialSize = -1,
        bool canExpand = true);
}
