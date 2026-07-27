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
    private TextMeshProUGUI textWaveCount;
    [SerializeField]
    private TextMeshProUGUI textCurrentScore;
    [SerializeField]
    private TextMeshProUGUI textBestScore;

    private StageData stageData;
    private int currentWaveIndex = 0;

    /// <summary>ServiceLocator에서 가져온 점수 서비스</summary>
    private IScoreService scoreService;
    private IMetaProgressService metaProgress;

    public int StageId => stageData != null ? stageData.stageId : stageId;
    public int MaxWave => stageData != null && stageData.waves != null ? stageData.waves.Length : 0;
    public StageData CurrentStage => stageData;

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

    private void Awake()
    {
        // 아웃게임에서 고른 스테이지 반영 (인스펙터 기본값은 폴백)
        if (stageOverride == null)
        {
            stageId = GameSession.SelectedStageId;
        }

        EnsureStageLoaded();
    }

    private void Start()
    {
        scoreService = ServiceLocator.Get<IScoreService>();
        metaProgress = ServiceLocator.Get<IMetaProgressService>();
        scoreService.ResetRun();
        scoreService.BindHud(textCurrentScore, textBestScore);

        // HUD Best는 기존 Score.json + 메타 전체 베스트 중 큰 쪽을 쓰도록 메타도 반영
        if (textBestScore != null && metaProgress != null && metaProgress.BestScore > 0)
        {
            textBestScore.text = "Best Score : " + metaProgress.BestScore;
        }

        textWaveCount.text = "Wave : " + 1;
    }

    /// <summary>아웃게임 → 인게임 진입 시 호출할 예정. 지금은 인스펙터 stageId.</summary>
    public void ConfigureStage(int selectedStageId)
    {
        stageId = selectedStageId;
        stageOverride = null;
        stageData = null;
        currentWaveIndex = 0;
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

        if (enemySpawner != null && enemySpawner.IsWaveInProgress)
        {
            return;
        }

        if (stageData == null || stageData.waves == null || currentWaveIndex >= stageData.waves.Length)
        {
            textFadeOut.ShowText("All Waves Clear", 2f);
            return;
        }

        StartGame();
        int currentWave = currentWaveIndex + 1;
        bool isFinalWave = currentWaveIndex >= stageData.waves.Length - 1;
        StageData bossStage = isFinalWave ? stageData : null;

        enemySpawner.StartWave(stageData.waves[currentWaveIndex], bossStage);
        textWaveCount.text = "Wave : " + currentWave;
    }

    public void FinishWave()
    {
        startGameButton.sprite = startGameWhiteButton;
        currentWaveIndex++;

        // 웨이브마다 크리스탈
        if (metaProgress != null)
        {
            metaProgress.AddCrystals(TowerMetaUpgradeRules.CrystalsPerWave);
        }

        if (stageData == null || stageData.waves == null || currentWaveIndex >= stageData.waves.Length)
        {
            if (stageData != null && stageData.clearBonusGold > 0)
            {
                IPlayerService player = ServiceLocator.Get<IPlayerService>();
                player.AddGold(stageData.clearBonusGold);
            }

            RecordMetaProgress(stageCleared: true);
            if (scoreService != null)
            {
                scoreService.SaveCurrentRun();
            }

            textFadeOut.ShowText("All Waves Clear", 2f);
        }
    }

    public void StartGame()
    {
        startGameButton.sprite = startGameBlackButton;
        stopGameButton.sprite = stopGameWhiteButton;
        Time.timeScale = 1.0f;
    }

    public void StopGame()
    {
        startGameButton.sprite = startGameWhiteButton;
        stopGameButton.sprite = stopGameBlackButton;
        Time.timeScale = 0;
    }

    public void FinishGame()
    {
        RecordMetaProgress(stageCleared: false);
        scoreService.SaveCurrentRun();
        textFadeOut.ShowText("Game Over", 3f);
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
