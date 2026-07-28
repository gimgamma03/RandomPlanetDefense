using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    /// <summary>ServiceLocator에서 가져온 점수 서비스</summary>
    private IScoreService scoreService;
    private IMetaProgressService metaProgress;
    private IPlaySessionStatsService sessionStats;

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

    private Sprite waveStartIdleSprite;
    private Sprite waveStartBusySprite;
    private Vector3 waveStartIdleScale = Vector3.one;
    private bool waveStartBusy;

    private void Awake()
    {
        // 아웃게임에서 고른 스테이지 반영 (인스펙터 기본값은 폴백)
        if (stageOverride == null)
        {
            stageId = GameSession.SelectedStageId;
        }

        EnsureStageLoaded();
        ResolveWaveStartButton();
        EnsureEndRunOverlay();
    }

    private void Start()
    {
        scoreService = ServiceLocator.Get<IScoreService>();
        metaProgress = ServiceLocator.Get<IMetaProgressService>();
        ServiceLocator.TryGet(out sessionStats);
        runPhase = RunPhase.Playing;
        scoreService.ResetRun();
        scoreService.BindHud(textCurrentScore, textBestScore);

        // HUD Best는 기존 Score.json + 메타 전체 베스트 중 큰 쪽을 쓰도록 메타도 반영
        if (textBestScore != null && metaProgress != null && metaProgress.BestScore > 0)
        {
            textBestScore.text = "Best Score : " + metaProgress.BestScore;
        }

        textWaveCount.text = "Wave : " + 1;
        BeginSessionStats();
        SetWaveStartBusy(false);
    }

    private void EnsureEndRunOverlay()
    {
        if (endRunOverlay != null)
        {
            endRunOverlay.EnsureBuilt();
            return;
        }

        if (textFadeOut != null)
        {
            endRunOverlay = textFadeOut.GetComponent<EndRunOverlay>();
            if (endRunOverlay == null)
            {
                endRunOverlay = textFadeOut.gameObject.AddComponent<EndRunOverlay>();
            }
        }
        else
        {
            endRunOverlay = GetComponentInChildren<EndRunOverlay>(true);
        }

        endRunOverlay?.EnsureBuilt();
    }

    private void ShowEndRun(string message)
    {
        EnsureEndRunOverlay();

        if (textFadeOut != null)
        {
            textFadeOut.ShowPersistent(message);
        }

        if (endRunOverlay != null)
        {
            endRunOverlay.Show(message);
        }
        else if (textFadeOut == null)
        {
            Debug.LogWarning("[WaveSystem] EndRunOverlay / TextFadeOut 없음: " + message);
        }
    }

    private void ResolveWaveStartButton()
    {
        if (waveStartButton == null)
        {
            Transform found = transform.Find("WaveStart");
            if (found == null)
            {
                found = FindDeep(transform, "WaveStart");
            }

            if (found != null)
            {
                waveStartButton = found.GetComponent<Button>();
            }
        }

        if (waveStartButton == null)
        {
            return;
        }

        waveStartIdleScale = waveStartButton.transform.localScale;
        Image image = waveStartButton.targetGraphic as Image;
        if (image == null)
        {
            image = waveStartButton.GetComponent<Image>();
        }

        if (image != null)
        {
            waveStartIdleSprite = image.sprite;
        }

        Sprite pressed = waveStartButton.spriteState.pressedSprite;
        waveStartBusySprite = pressed != null ? pressed : waveStartIdleSprite;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>웨이브 진행 중 = 눌린 채 + 비활성. 끝나면 다시 누를 수 있게.</summary>
    private void SetWaveStartBusy(bool busy)
    {
        waveStartBusy = busy;
        if (waveStartButton == null)
        {
            return;
        }

        waveStartButton.interactable = !busy;

        Image image = waveStartButton.targetGraphic as Image;
        if (image == null)
        {
            image = waveStartButton.GetComponent<Image>();
        }

        if (image != null)
        {
            if (busy)
            {
                if (waveStartBusySprite != null)
                {
                    image.sprite = waveStartBusySprite;
                }

                image.color = waveStartBusyColor;
            }
            else
            {
                if (waveStartIdleSprite != null)
                {
                    image.sprite = waveStartIdleSprite;
                }

                image.color = waveStartIdleColor;
            }
        }

        waveStartButton.transform.localScale = busy
            ? Vector3.Scale(waveStartIdleScale, waveStartBusyScale)
            : waveStartIdleScale;
    }

    private void OnApplicationQuit()
    {
        EndSessionStats(SessionEndReason.Quit);
    }

    private void OnDestroy()
    {
        // 씬 이탈 시 미종료 세션이 있으면 quit로 마감
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

    public void AddScore(int point)
    {
        scoreService.AddScore(point);
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
        int currentWave = currentWaveIndex + 1;
        bool isFinalWave = currentWaveIndex >= stageData.waves.Length - 1;
        StageData bossStage = isFinalWave ? stageData : null;

        SetWaveStartBusy(true);
        sessionStats?.RecordWaveStarted(currentWave);
        enemySpawner.StartWave(stageData.waves[currentWaveIndex], bossStage);
        textWaveCount.text = "Wave : " + currentWave;
    }

    public void FinishWave()
    {
        if (IsRunEnded)
        {
            return;
        }

        if (startGameButton != null && startGameWhiteButton != null)
        {
            startGameButton.sprite = startGameWhiteButton;
        }

        int clearedWave = currentWaveIndex + 1;
        currentWaveIndex++;

        sessionStats?.RecordWaveCleared(clearedWave);

        if (metaProgress != null)
        {
            metaProgress.AddCrystals(TowerMetaUpgradeRules.CrystalsPerWave);
        }

        if (stageData == null || stageData.waves == null || currentWaveIndex >= stageData.waves.Length)
        {
            if (stageData != null && stageData.clearBonusGold > 0
                && ServiceLocator.TryGet(out IPlayerService player))
            {
                player.AddGold(stageData.clearBonusGold);
            }

            EndRun(RunPhase.Cleared, "All Waves Clear");
            return;
        }

        SetWaveStartBusy(false);
    }

    public void StartGame()
    {
        if (startGameButton != null && startGameBlackButton != null)
        {
            startGameButton.sprite = startGameBlackButton;
        }

        if (stopGameButton != null && stopGameWhiteButton != null)
        {
            stopGameButton.sprite = stopGameWhiteButton;
        }

        Time.timeScale = 1.0f;
    }

    public void StopGame()
    {
        if (startGameButton != null && startGameWhiteButton != null)
        {
            startGameButton.sprite = startGameWhiteButton;
        }

        if (stopGameButton != null && stopGameBlackButton != null)
        {
            stopGameButton.sprite = stopGameBlackButton;
        }

        Time.timeScale = 0;
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
        RecordMetaProgress(stageCleared: cleared);
        if (scoreService != null)
        {
            scoreService.SaveCurrentRun();
        }

        EndSessionStats(cleared ? SessionEndReason.Cleared : SessionEndReason.GameOver);

        SetWaveStartBusy(true);
        if (waveStartButton != null)
        {
            waveStartButton.interactable = false;
        }

        ShowEndRun(message);
    }

    private void RecordMetaProgress(bool stageCleared)
    {
        if (metaProgress == null)
        {
            return;
        }

        int id = StageId;
        int score = scoreService != null ? scoreService.CurrentScore : 0;
        metaProgress.RecordScore(id, score);

        if (stageCleared)
        {
            metaProgress.MarkStageCleared(id);
            metaProgress.AddCrystals(TowerMetaUpgradeRules.CrystalsStageClearBonus);
        }
    }
}
