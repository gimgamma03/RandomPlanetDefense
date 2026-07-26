using UnityEngine;

/// <summary>
/// 아웃게임에서 고르는 1개 스테이지 = 웨이브 묶음.
/// Resources/Stages 에 두고 StageCatalog로 로드한다.
/// </summary>
[CreateAssetMenu(menuName = "RPD/Stage Data", fileName = "StageData")]
public class StageData : ScriptableObject
{
    [Tooltip("아웃게임 선택·로드 키. 파일 이름과 무관")]
    public int stageId = 1;

    [Tooltip("스테이지 선택 UI용")]
    public string displayName = "Stage 1";

    [Tooltip("전 웨이브 클리어 시 인게임 보너스 골드 (아웃게임 보상과 별개)")]
    public int clearBonusGold;

    public Wave[] waves;

    public string DisplayName =>
        string.IsNullOrEmpty(displayName) ? $"Stage {stageId}" : displayName;

    [System.Serializable]
    public struct Wave
    {
        [Min(0f)]
        [Tooltip("적 한 마리 스폰 간격(초)")]
        public float spawnDelay;

        [Min(1)]
        [Tooltip("이 웨이브에서 스폰할 적 수")]
        public int maxEnemyCount;

        [Tooltip("출현 풀. spawnWeight 상대 비중 (합이 1일 필요 없음)")]
        public WaveEnemy[] enemies;
    }

    [System.Serializable]
    public struct WaveEnemy
    {
        [Tooltip("적 종류 (드롭다운). 프리팹/SO 직접 참조 안 함")]
        public EnemyType enemyType;

        [Min(0f)]
        [Tooltip("상대 가중치. 예: 2와 1이면 약 66% / 33%. 0이면 스폰 안 함")]
        public float spawnWeight;
    }
}
