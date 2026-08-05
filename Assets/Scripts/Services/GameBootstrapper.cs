using UnityEngine;

/// <summary>
/// 게임 단일 진입점.
/// Pure C#(Score/Player)는 씬 Awake 전에 등록하고,
/// Pool(MB)은 Bootstrapper Awake에서 등록한다.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class GameBootstrapper : MonoBehaviour
{
    private static GameBootstrapper instance;
    private static bool pureServicesReady;
    private static bool poolServiceReady;

    [Header("MonoBehaviour 매니저 (비우면 런타임 생성)")]
    [SerializeField] private GameObjectPoolManager poolManagerPrefab;
    [SerializeField] private GameObjectPoolManager poolManagerInstance;

    /// <summary>도메인 리로드 시 static 초기화</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        pureServicesReady = false;
        poolServiceReady = false;
        ServiceLocator.Clear();
    }

    /// <summary>
    /// 씬 오브젝트 Awake보다 먼저 Pure C# 서비스를 올린다.
    /// 이전 AfterSceneLoad 방식이면 Player.Awake에서 IPlayerService 예외 → 골드 미설정 → 벽 설치 실패.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void BootBeforeSceneLoad()
    {
        EnsurePureServices();

        if (instance != null)
        {
            return;
        }

        GameObject go = new GameObject("[GameBootstrapper]");
        Object.DontDestroyOnLoad(go);
        go.AddComponent<GameBootstrapper>();
    }

    private static void EnsurePureServices()
    {
        if (pureServicesReady)
        {
            return;
        }

        ScoreService scoreService = new ScoreService();
        ServiceLocator.Register<IScoreService>(scoreService);
        scoreService.Initialize();

        PlayerService playerService = new PlayerService();
        ServiceLocator.Register<IPlayerService>(playerService);
        playerService.Initialize();

        MetaProgressService metaProgress = new MetaProgressService();
        ServiceLocator.Register<IMetaProgressService>(metaProgress);
        metaProgress.Initialize();

        PlaySessionStatsService playSessionStats = new PlaySessionStatsService();
        ServiceLocator.Register<IPlaySessionStatsService>(playSessionStats);
        playSessionStats.Initialize();

        pureServicesReady = true;
        Debug.Log(
            "[GameBootstrapper] Pure 서비스 등록: IScoreService, IPlayerService, " +
            "IMetaProgressService, IPlaySessionStatsService");
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        EnsurePureServices();
        EnsurePoolService();
        EnsurePlaySessionApiClient();
    }

    private static void EnsurePlaySessionApiClient()
    {
        if (PlaySessionApiClient.Instance != null)
        {
            return;
        }

        GameObject go = new GameObject("[PlaySessionApiClient]");
        DontDestroyOnLoad(go);
        go.AddComponent<PlaySessionApiClient>();
        Debug.Log("[GameBootstrapper] PlaySessionApiClient 등록 (POST → RpdSessionApi)");
    }

    private void EnsurePoolService()
    {
        if (poolServiceReady && ServiceLocator.IsRegistered<IPoolService>())
        {
            return;
        }

        GameObjectPoolManager pool = ResolvePoolManager();
        ServiceLocator.Register<IPoolService>(pool);
        pool.Initialize();
        poolServiceReady = true;
        Debug.Log("[GameBootstrapper] Pool 서비스 등록: IPoolService");
    }

    private GameObjectPoolManager ResolvePoolManager()
    {
        if (poolManagerInstance != null)
        {
            DontDestroyOnLoad(poolManagerInstance.gameObject);
            return poolManagerInstance;
        }

        if (poolManagerPrefab != null)
        {
            GameObjectPoolManager created = Instantiate(poolManagerPrefab);
            created.gameObject.name = "[GameObjectPoolManager]";
            DontDestroyOnLoad(created.gameObject);
            return created;
        }

        GameObject go = new GameObject("[GameObjectPoolManager]");
        DontDestroyOnLoad(go);
        return go.AddComponent<GameObjectPoolManager>();
    }
}
