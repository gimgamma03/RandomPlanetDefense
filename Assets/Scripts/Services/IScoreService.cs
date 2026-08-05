using TMPro;

/// <summary>점수·하이스코어 (Pure C# 서비스)</summary>
public interface IScoreService : IService
{
    int CurrentScore { get; }

    /// <summary>UI 텍스트 연결 (씬 TMP는 MonoBehaviour 쪽에서 넘김)</summary>
    void BindHud(TMP_Text currentScoreText, TMP_Text bestScoreText);

    void AddScore(int point);

    /// <summary>이번 판 점수를 기록에 반영하고 저장</summary>
    void SaveCurrentRun();

    /// <summary>새 판 시작 시 현재 점수 리셋</summary>
    void ResetRun();
}
