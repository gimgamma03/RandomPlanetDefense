using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WaveSystem : MonoBehaviour
{
    [SerializeField]
    private WaveData waveData;
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

    private int currentWaveIndex = 0;

    /// <summary>ServiceLocator에서 가져온 점수 서비스</summary>
    private IScoreService scoreService;

    public int MaxWave => waveData.waves.Length;

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

    private void Start()
    {
        scoreService = ServiceLocator.Get<IScoreService>();
        scoreService.ResetRun();
        scoreService.BindHud(textCurrentScore, textBestScore);

        textWaveCount.text = "Wave : " + 1;
    }

    public void AddScore(int point)
    {
        scoreService.AddScore(point);
    }

    public void StartWave()
    {
        if (enemySpawner != null && enemySpawner.IsWaveInProgress)
        {
            return;
        }

        if (currentWaveIndex >= waveData.waves.Length)
        {
            textFadeOut.ShowText("All Waves Clear", 2f);
            return;
        }

        StartGame();
        int currentWave = currentWaveIndex + 1;

        enemySpawner.StartWave(waveData.waves[currentWaveIndex]);
        textWaveCount.text = "Wave : " + currentWave;
    }

    public void FinishWave()
    {
        startGameButton.sprite = startGameWhiteButton;
        currentWaveIndex++;

        if (currentWaveIndex >= waveData.waves.Length)
        {
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
        scoreService.SaveCurrentRun();
        textFadeOut.ShowText("Game Over", 3f);
    }
}