using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using UnityEngine;
using TMPro;

public class ScoreSystem
{
    private List<int> scoreData = new();
    //private int showScoreCount = 8;
    public int currentScore;

    string path = Path.Combine(Application.dataPath, "Resources/Score.json");
    JsonSerializer serializer = new();

    private TextMeshProUGUI textCurrentScore;
    private TextMeshProUGUI textBestScore;
    public void Setup(TextMeshProUGUI textCurrentScore, TextMeshProUGUI textBestScore)
    {
        this.textCurrentScore = textCurrentScore;
        this.textBestScore = textBestScore;
        LoadScore();
    }

    //현재 스코어 점수만큼 증가
    public void AddScore(int point)
    {
        currentScore += point;
        //textCurrentScore.text = "Score : " + currentScore;
        textCurrentScore.text = currentScore.ToString();
    }

    //기록된 스코어 에 현재 스코어 저장
    public void AddThisGameScore()
    {
        if (!scoreData.Contains(currentScore))
        {
            scoreData.Add(currentScore);
        }
        SaveScore();
    }
    public void SaveScore()
    {
        if (File.Exists(path))
        {
            scoreData.Sort(CompareScoreDescending);

            SetTextBestScore();

            string jsonScoreData = JsonConvert.SerializeObject(scoreData);

            File.WriteAllText(path, jsonScoreData);

            foreach (int score in scoreData)
            {
                Debug.Log(score);
            }
        }
    }
    public void LoadScore()
    {
        if (File.Exists (path))
        {
            string jsonString = File.ReadAllText(path);
            scoreData = JsonConvert.DeserializeObject<List<int>>(jsonString);
            scoreData.Sort(CompareScoreDescending);

            SetTextBestScore();
        }

    }

    public void SetTextBestScore()
    {
        if (scoreData.Count > 0)
        {
            textBestScore.text = "Best Score : " + scoreData[0].ToString();
        }
    }

    int CompareScoreDescending(int x, int y)
    {
        return y - x;

        //y - x 는 오름차순이 된다.
        //비교식의 리턴값이 0 보다 클때는 두 요소의 위치를 바꾼다.
        //우리는 내림차순 (왼쪽이 오른쪽보다 크다) 이 필요하므로 
        //왼쪽이 오른쪽 보다 안 클때 (리턴 값이 0 보다 클때) 만 두 위치를 바꾸게 한다.

    }
}
