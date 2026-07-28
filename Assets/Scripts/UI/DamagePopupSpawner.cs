using TMPro;
using UnityEngine;

/// <summary>
/// 데미지 플로팅 숫자 — <see cref="IPoolService"/> / <see cref="PoolId.DamagePopup"/>.
/// 레이저 틱 피해는 EnemyHp에서 0.5초 합산 후 Show를 호출한다.
/// </summary>
public sealed class DamagePopupSpawner : MonoBehaviour
{
    public static readonly Color BodyColor = new Color(1f, 0.97f, 0.82f, 1f);
    public static readonly Color ShieldColor = new Color(0.45f, 0.85f, 1f, 1f);

    private const int DefaultPoolSize = 16;

    private static DamagePopupSpawner instance;

    [SerializeField]
    [Tooltip("비우면 Prefabs/UI/DamagePopup 또는 런타임 템플릿")]
    private GameObject damagePopupPrefab;

    [SerializeField]
    private int poolInitialSize = DefaultPoolSize;

    private IPoolService poolService;
    private GameObject runtimeTemplate;
    private bool poolReady;

    public static DamagePopupSpawner EnsureExists()
    {
        if (instance != null)
        {
            return instance;
        }

        instance = FindFirstObjectByType<DamagePopupSpawner>();
        if (instance != null)
        {
            return instance;
        }

        GameObject go = new GameObject("DamagePopupSpawner");
        instance = go.AddComponent<DamagePopupSpawner>();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void Start()
    {
        EnsurePoolRegistered();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public static void ShowBody(Vector3 worldPosition, float amount)
    {
        Show(worldPosition, amount, BodyColor);
    }

    public static void ShowShield(Vector3 worldPosition, float amount)
    {
        Show(worldPosition, amount, ShieldColor);
    }

    public static void Show(Vector3 worldPosition, float amount, Color color)
    {
        if (amount < 0.05f)
        {
            return;
        }

        DamagePopupSpawner spawner = EnsureExists();
        if (!spawner.EnsurePoolRegistered())
        {
            return;
        }

        Vector3 spawnPos = worldPosition + new Vector3(0f, 0.25f, 0f);
        GameObject go = spawner.poolService.Spawn(
            PoolId.DamagePopup,
            spawnPos,
            Quaternion.identity,
            spawner.poolService.Root);
        if (go == null)
        {
            return;
        }

        DamagePopup popup = go.GetComponent<DamagePopup>();
        if (popup == null)
        {
            spawner.poolService.Return(go);
            return;
        }

        popup.Play(amount, color, worldPosition);
    }

    public static void Release(DamagePopup popup)
    {
        if (popup == null)
        {
            return;
        }

        popup.ClearForPool();

        if (ServiceLocator.TryGet(out IPoolService pool))
        {
            pool.Return(popup.gameObject);
            return;
        }

        Destroy(popup.gameObject);
    }

    public static string FormatAmount(float amount)
    {
        if (amount < 0.05f)
        {
            return null;
        }

        if (amount < 10f)
        {
            return amount.ToString("0.#");
        }

        return Mathf.RoundToInt(amount).ToString();
    }

    private bool EnsurePoolRegistered()
    {
        if (poolReady)
        {
            return true;
        }

        if (!ServiceLocator.TryGet(out poolService) || poolService == null)
        {
            Debug.LogWarning("[DamagePopupSpawner] IPoolService 없음.");
            return false;
        }

        GameObject prefab = ResolvePrefab();
        if (prefab == null)
        {
            Debug.LogWarning("[DamagePopupSpawner] DamagePopup 프리팹/템플릿 없음.");
            return false;
        }

        poolService.EnsurePool(
            PoolId.DamagePopup,
            prefab,
            poolService.Root,
            Mathf.Max(1, poolInitialSize));
        poolReady = true;
        return true;
    }

    private GameObject ResolvePrefab()
    {
        if (damagePopupPrefab != null)
        {
            return damagePopupPrefab;
        }

#if UNITY_EDITOR
        GameObject fromAssets = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/UI/DamagePopup.prefab");
        if (fromAssets != null)
        {
            damagePopupPrefab = fromAssets;
            return damagePopupPrefab;
        }
#endif

        return GetOrCreateRuntimeTemplate();
    }

    private GameObject GetOrCreateRuntimeTemplate()
    {
        if (runtimeTemplate != null)
        {
            return runtimeTemplate;
        }

        runtimeTemplate = new GameObject("DamagePopup");
        runtimeTemplate.transform.SetParent(transform, false);
        runtimeTemplate.SetActive(false);
        DamagePopup popup = runtimeTemplate.AddComponent<DamagePopup>();
        popup.EnsureText(ResolveOrbitFont());
        damagePopupPrefab = runtimeTemplate;
        return runtimeTemplate;
    }

    private static TMP_FontAsset ResolveOrbitFont()
    {
#if UNITY_EDITOR
        TMP_FontAsset orbit = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/Fonts/Orbit-Regular SDF.asset");
        if (orbit != null)
        {
            return orbit;
        }
#endif
        TMP_FontAsset[] loaded = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        for (int i = 0; i < loaded.Length; i++)
        {
            TMP_FontAsset candidate = loaded[i];
            if (candidate != null && candidate.name.Contains("Orbit"))
            {
                return candidate;
            }
        }

        return TMP_Settings.defaultFontAsset;
    }
}
