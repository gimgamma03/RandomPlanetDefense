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
/// 씬 이탈 시 Release로 핸들 수명을 닫는다.
/// </summary>
public class TowerBasePrefabLoader : MonoBehaviour
{
    [SerializeField]
    private bool useAddressables;

    [SerializeField]
    private TowerBaseLibrary library;

    [Tooltip("게임 시작 시 등록된 계열 Base를 미리 로드 (Addressables ON일 때)")]
    [SerializeField]
    private bool preloadAllBasesOnStart;

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

    private void Start()
    {
        if (preloadAllBasesOnStart && useAddressables)
        {
            StartCoroutine(PreloadAllBases());
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
            if (handle.IsValid())
            {
                Addressables.Release(handle);
            }

            cache[weaponType] = entry.basePrefab;
            onLoaded?.Invoke(entry.basePrefab);
            yield break;
        }

        handles[weaponType] = handle;
        cache[weaponType] = handle.Result;
        Debug.Log($"[TowerBasePrefabLoader] Loaded via Addressables: {entry.address}");
        onLoaded?.Invoke(handle.Result);
    }

    /// <summary>Library에 등록된 계열을 미리 로드해 첫 스폰 히치를 줄인다.</summary>
    public IEnumerator PreloadAllBases()
    {
        EnsureLibrary();
        if (library == null || library.Entries == null)
        {
            yield break;
        }

        foreach (TowerBaseLibrary.Entry entry in library.Entries)
        {
            yield return LoadBasePrefab(entry.weaponType, null);
        }
    }

    /// <summary>Addressables 핸들 전부 해제. 타이틀 복귀·씬 파괴 시 호출.</summary>
    public void ReleaseAll()
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

    private void OnDestroy()
    {
        ReleaseAll();
    }
}
