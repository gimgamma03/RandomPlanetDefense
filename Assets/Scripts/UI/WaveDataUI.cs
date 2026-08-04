using TMPro;
using UnityEngine;

/// <summary>
/// 웨이브 번호 + 소환 진행(소환됨/총 소환 예정).
/// </summary>
public sealed class WaveDataUI : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI text;

    private int waveNumber = 1;
    private int spawned;
    private int total;

    private void Awake()
    {
        if (text == null)
        {
            text = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        Refresh();
    }

    public void SetWave(int wave)
    {
        waveNumber = Mathf.Max(1, wave);
        Refresh();
    }

    public void SetSpawnProgress(int spawnedCount, int totalCount)
    {
        spawned = Mathf.Max(0, spawnedCount);
        total = Mathf.Max(0, totalCount);
        Refresh();
    }

    /// <summary>다음 웨이브 대기: 0/총원 미리 표시.</summary>
    public void PreviewWave(int wave, int totalCount)
    {
        waveNumber = Mathf.Max(1, wave);
        spawned = 0;
        total = Mathf.Max(0, totalCount);
        Refresh();
    }

    private void Refresh()
    {
        if (text == null)
        {
            return;
        }

        text.text = $"웨이브 {waveNumber}\n{spawned}/{total}";
    }
}
