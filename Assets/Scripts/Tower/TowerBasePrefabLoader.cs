using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 계열 Base 프리팹 로드.
/// useAddressables=false → Library 직접 참조 (지금과 동일).
/// useAddressables=true → address로 LoadAssetAsync, 결과는 캐시.
/// </summary>
public class TowerBasePrefabLoader : MonoBehaviour
{
    [SerializeField]
    private bool useAddressables;

    [SerializeField]
    private TowerBaseLibrary library;

    private readonly Dictionary<WeaponType, GameObject> cache = new Dictionary<WeaponType, GameObject>();
    private readonly Dictionary<WeaponType, AsyncOperationHandle<GameObject>> handles =
        new Dictionary<WeaponType, AsyncOperationHandle<GameObject>>();

    public bool UseAddressables
    {
        get => useAddressables;
        set => useAddressables = value;
    }

    public void SetUseAddressables(bool enabled)
    {
        useAddressables = enabled;
    }

    private void Awake()
    {
        if (library == null)
        {
            library = TowerBaseLibrary.Load();
        }
    }

    public void EnsureLibrary()
    {
        if (library == null)
        {
            library = TowerBaseLibrary.Load();
        }
    }

    /// <summary>동기 경로. Addressables 모드에선 캐시에 있을 때만 성공.</summary>
    public bool TryGetCached(WeaponType weaponType, out GameObject prefab)
    {
        return cache.TryGetValue(weaponType, out prefab) && prefab != null;
    }

    public IEnumerator LoadBasePrefab(WeaponType weaponType, Action<GameObject> onLoaded)
    {
        EnsureLibrary();

        if (cache.TryGetValue(weaponType, out GameObject cached) && cached != null)
        {
            onLoaded?.Invoke(cached);
            yield break;
        }

        if (library == null || !library.TryGetEntry(weaponType, out TowerBaseLibrary.Entry entry))
        {
            Debug.LogError($"[TowerBasePrefabLoader] No library entry for {weaponType}");
            onLoaded?.Invoke(null);
            yield break;
        }

        if (!useAddressables || string.IsNullOrEmpty(entry.address))
        {
            if (entry.basePrefab == null)
            {
                Debug.LogError($"[TowerBasePrefabLoader] No prefab/address for {weaponType}");
            }

            cache[weaponType] = entry.basePrefab;
            onLoaded?.Invoke(entry.basePrefab);
            yield break;
        }

        AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(entry.address);
        yield return handle;

        if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
        {
            Debug.LogError($"[TowerBasePrefabLoader] Addressables load failed: {entry.address}. Fallback to direct ref.");
            cache[weaponType] = entry.basePrefab;
            onLoaded?.Invoke(entry.basePrefab);
            yield break;
        }

        handles[weaponType] = handle;
        cache[weaponType] = handle.Result;
        Debug.Log($"[TowerBasePrefabLoader] Loaded via Addressables: {entry.address}");
        onLoaded?.Invoke(handle.Result);
    }

    private void OnDestroy()
    {
        foreach (var pair in handles)
        {
            if (pair.Value.IsValid())
            {
                Addressables.Release(pair.Value);
            }
        }

        handles.Clear();
        cache.Clear();
    }
}