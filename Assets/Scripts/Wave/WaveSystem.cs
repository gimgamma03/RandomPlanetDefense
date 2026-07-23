using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;

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

    public ScoreSystem scoreSystem;
    // 웨이브 정보 출력을 위한 Get 프로퍼티 (현재 웨이브, 총 웨이브)
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


    private List<int> scoreData = new List<int>();

    private void Start()
    {
        textWaveCount.text = "Wave : " + 1;
        textCurrentScore.text = "Score : " + 0;
        scoreSystem = new ScoreSystem();
        scoreSystem.Setup(textCurrentScore, textBestScore);
        //LoadWaves();
    }
    public void StartWave()
    {
        if (currentWaveIndex < waveData.waves.Length)
        {
            StartGame();
            int currentWave = currentWaveIndex + 1;

            //textFadeOut.ShowText("Wave : " + currentWave, 1f);

            //현재 웨이브의 정보 에너미 스포너에 넘겨줌
            enemySpawner.StartWave(waveData.waves[currentWaveIndex]);
            textWaveCount.text = "Wave : " + (currentWave);
        }
    }

    public void FinishWave()
    {
        startGameButton.sprite = startGameWhiteButton;
        currentWaveIndex++;
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
        scoreSystem.AddThisGameScore();
        textFadeOut.ShowText("Game Over", 3f);
    }
    
}
