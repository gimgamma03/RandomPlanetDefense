using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;

/// <summary>
/// Pure C# 점수 매니저 — MonoBehaviour 없음.
/// 디스크 저장/로드는 Initialize, HUD는 BindHud로 연결.
/// </summary>
public class ScoreService : IScoreService
{
    private readonly List<int> scoreData = new List<int>();
    private readonly string path;

    private TMP_Text textCurrentScore;
    private TMP_Text textBestScore;

    public int CurrentScore { get; private set; }

    public ScoreService()
    {
        path = Path.Combine(Application.dataPath, "Resources/Score.json");
    }

    public void Initialize()
    {
        LoadScore();
        CurrentScore = 0;
    }

    public void BindHud(TMP_Text currentScoreText, TMP_Text bestScoreText)
    {
        textCurrentScore = currentScoreText;
        textBestScore = bestScoreText;
        RefreshHud();
    }

    public void ResetRun()
    {
        CurrentScore = 0;
        RefreshHud();
    }

    public void AddScore(int point)
    {
        CurrentScore += point;
        if (textCurrentScore != null)
        {
            textCurrentScore.text = CurrentScore.ToString();
        }
    }

    public void SaveCurrentRun()
    {
        if (!scoreData.Contains(CurrentScore))
        {
            scoreData.Add(CurrentScore);
        }

        SaveScore();
    }

    private void RefreshHud()
    {
        if (textCurrentScore != null)
        {
            textCurrentScore.text = CurrentScore.ToString();
        }

        SetTextBestScore();
    }

    private void SaveScore()
    {
        scoreData.Sort(CompareScoreDescending);
        SetTextBestScore();

        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string jsonScoreData = JsonConvert.SerializeObject(scoreData);
        File.WriteAllText(path, jsonScoreData);
    }

    private void LoadScore()
    {
        if (!File.Exists(path))
        {
            return;
        }

        string jsonString = File.ReadAllText(path);
        List<int> loaded = JsonConvert.DeserializeObject<List<int>>(jsonString);
        scoreData.Clear();
        if (loaded != null)
        {
            scoreData.AddRange(loaded);
        }

        scoreData.Sort(CompareScoreDescending);
        SetTextBestScore();
    }

    private void SetTextBestScore()
    {
        if (textBestScore == null)
        {
            return;
        }

        if (scoreData.Count > 0)
        {
            textBestScore.text = "Best Score : " + scoreData[0];
        }
        else
        {
            textBestScore.text = "Best Score : 0";
        }
    }

    private static int CompareScoreDescending(int x, int y)
    {
        return y - x;
    }
}
