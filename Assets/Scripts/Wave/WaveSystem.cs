using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스테이지/웨이브 진행·런 종료 상태.
/// UI·보상·일시정지는 협업 클래스로 위임.
/// </summary>
public class WaveSystem : MonoBehaviour
{
    [Header("Stage")]
    [Tooltip("아웃게임에서 고른 스테이지 ID. 지금은 인스펙터/기본값 1")]
    [SerializeField]
    private int stageId = 1;

    [Tooltip("비우면 Resources/Stages 에서 stageId로 로드")]
    [SerializeField]
    private StageData stageOverride;

    [SerializeField]
    private EnemySpawner enemySpawner;
    [SerializeField]
    private TextFadeOut textFadeOut;
    [SerializeField]
    private EndRunOverlay endRunOverlay;
    [SerializeField]
    private TextMeshProUGUI textWaveCount;
    [SerializeField]
    private TextMeshProUGUI textCurrentScore;
    [SerializeField]
    private TextMeshProUGUI textBestScore;

    private StageData stageData;
    private int currentWaveIndex = 0;
    private RunPhase runPhase = RunPhase.Playing;

    private IScoreService scoreService;
    private IMetaProgressService metaProgress;
    private IPlaySessionStatsService sessionStats;

    private readonly WaveStartButtonView waveStartView = new WaveStartButtonView();
    private readonly WaveRunRewards runRewards = new WaveRunRewards();
    private EndRunPresenter endRunPresenter;
    private GamePauseView pauseView;

    public int StageId => stageData != null ? stageData.stageId : stageId;
    public int MaxWave => stageData != null && stageData.waves != null ? stageData.waves.Length : 0;
    public StageData CurrentStage => stageData;
    public RunPhase Phase => runPhase;
    public bool IsRunEnded => runPhase != RunPhase.Playing;

    [SerializeField]
    private Image startGameButton;
    [SerializeField]
    private Image stopGameButton;

    [SerializeField]
    private Sprite startGameBlackButton;
    [SerializeField]
    private Sprite startGameWhiteButton;
    [SerializeField]
    private Sprite stopGameWhiteButton;
    [SerializeField]
    private Sprite stopGameBlackButton;

    [Header("Wave Start Button")]
    [Tooltip("비우면 씬에서 WaveStart 버튼을 찾음")]
    [SerializeField]
    private Button waveStartButton;
    [SerializeField]
    private Color waveStartIdleColor = Color.white;
    [SerializeField]
    private Color waveStartBusyColor = new Color(0.35f, 0.85f, 0.95f, 1f);
    [SerializeField]
    private Vector3 waveStartBusyScale = new Vector3(0.92f, 0.88f, 1f);

    private void Awake()
    {
        if (stageOverride == null)
        {
            stageId = GameSession.SelectedStageId;
        }

        EnsureStageLoaded();

        waveStartView.Bind(
            waveStartButton,
            transform,
            waveStartIdleColor,
            waveStartBusyColor,
            waveStartBusyScale);

        endRunPresenter = new EndRunPresenter(this, textFadeOut, endRunOverlay);
        endRunPresenter.EnsureBuilt();

        pauseView = new GamePauseView(
            startGameButton,
            stopGameButton,
            startGameBlackButton,
            startGameWhiteButton,
            stopGameWhiteButton,
            stopGameBlackButton);
    }

    private void Start()
    {
        scoreService = ServiceLocator.Get<IScoreService>();
        metaProgress = ServiceLocator.Get<IMetaProgressService>();
        ServiceLocator.TryGet(out sessionStats);
        runRewards.Bind(metaProgress, scoreService);

        runPhase = RunPhase.Playing;
        scoreService.ResetRun();
        scoreService.BindHud(textCurrentScore, textBestScore);

        if (textBestScore != null && metaProgress != null && metaProgress.BestScore > 0)
        {
            textBestScore.text = "Best Score : " + metaProgress.BestScore;
        }

        textWaveCount.text = "Wave : " + 1;
        BeginSessionStats();
        waveStartView.SetBusy(false);
    }

    private void OnApplicationQuit()
    {
        EndSessionStats(SessionEndReason.Quit);
    }

    private void OnDestroy()
    {
        if (sessionStats != null && sessionStats.IsRunActive)
        {
            EndSessionStats(SessionEndReason.Quit);
        }
    }

    private void BeginSessionStats()
    {
        if (sessionStats == null)
        {
            return;
        }

        EnsureStageLoaded();
        string stageName = stageData != null ? stageData.DisplayName : $"Stage {stageId}";
        string playerId = metaProgress != null ? metaProgress.PlayerId : string.Empty;
        sessionStats.BeginRun(StageId, stageName, MaxWave, playerId);
    }

    private void EndSessionStats(string endReason)
    {
        if (sessionStats == null || !sessionStats.IsRunActive)
        {
            return;
        }

        int score = scoreService != null ? scoreService.CurrentScore : 0;
        sessionStats.EndRun(endReason, score);
    }

    /// <summary>아웃게임 → 인게임 진입 시 호출할 예정. 지금은 인스펙터 stageId.</summary>
    public void ConfigureStage(int selectedStageId)
    {
        stageId = selectedStageId;
        stageOverride = null;
        stageData = null;
        currentWaveIndex = 0;
        runPhase = RunPhase.Playing;
        EnsureStageLoaded();
        textWaveCount.text = "Wave : " + 1;
    }

    private void EnsureStageLoaded()
    {
        if (stageData != null)
        {
            return;
        }

        if (stageOverride != null)
        {
            stageData = stageOverride;
            return;
        }

        StageCatalog catalog = StageCatalog.LoadFromResources();
        stageData = catalog.Get(stageId);
        if (stageData == null)
        {
            Debug.LogError($"[WaveSystem] StageId {stageId} 로드 실패.");
        }
    }

    public void StartWave()
    {
        EnsureStageLoaded();

        if (IsRunEnded)
        {
            return;
        }

        if (enemySpawner != null && enemySpawner.IsWaveInProgress)
        {
            return;
        }

        if (stageData == null || stageData.waves == null || currentWaveIndex >= stageData.waves.Length)
        {
            EndRun(RunPhase.Cleared, "All Waves Clear");
            return;
        }

        StartGame();
        CancelWallBuildModeIfActive();

        int currentWave = currentWaveIndex + 1;
        bool isFinalWave = currentWaveIndex >= stageData.waves.Length - 1;
        StageData bossStage = isFinalWave ? stageData : null;

        waveStartView.SetBusy(true);
        sessionStats?.RecordWaveStarted(currentWave);
        enemySpawner.StartWave(stageData.waves[currentWaveIndex], bossStage);
        textWaveCount.text = "Wave : " + currentWave;
    }

    private static void CancelWallBuildModeIfActive()
    {
        if (!ServiceLocator.TryGet(out IBuildModeState buildMode))
        {
            return;
        }

        if (buildMode.CurrentMode == BuildMode.PlaceWall)
        {
            buildMode.CancelMode();
        }
    }

    public void FinishWave()
    {
        if (IsRunEnded)
        {
            return;
        }

        pauseView?.ShowIdleStart();

        int clearedWave = currentWaveIndex + 1;
        currentWaveIndex++;

        sessionStats?.RecordWaveCleared(clearedWave);
        runRewards.OnWaveCleared();

        if (stageData == null || stageData.waves == null || currentWaveIndex >= stageData.waves.Length)
        {
            runRewards.OnAllWavesCleared(stageData);
            EndRun(RunPhase.Cleared, "All Waves Clear");
            return;
        }

        waveStartView.SetBusy(false);
    }

    public void StartGame()
    {
        pauseView?.Play();
    }

    /// <summary>웨이브 클리어와 같은 중앙 문구 UI에 페이드 인/아웃 배너.</summary>
    public void ShowCenterBanner(string message, float fadeIn, float hold, float fadeOut)
    {
        if (textFadeOut == null)
        {
            return;
        }

        textFadeOut.ShowTextFadeInOut(message, fadeIn, hold, fadeOut);
    }

    public void StopGame()
    {
        pauseView?.Pause();
    }

    public void FinishGame()
    {
        EndRun(RunPhase.GameOver, "Game Over");
    }

    /// <summary>클리어/오버 공통 종료. 한 번만 실행된다.</summary>
    private void EndRun(RunPhase phase, string message)
    {
        if (phase == RunPhase.Playing || IsRunEnded)
        {
            return;
        }

        runPhase = phase;

        if (enemySpawner != null)
        {
            enemySpawner.StopWaveForGameOver();
        }

        bool cleared = phase == RunPhase.Cleared;
        runRewards.RecordMetaProgress(StageId, stageCleared: cleared);
        scoreService?.SaveCurrentRun();
        EndSessionStats(cleared ? SessionEndReason.Cleared : SessionEndReason.GameOver);

        waveStartView.Lock();
        endRunPresenter?.Show(message);
    }
}
