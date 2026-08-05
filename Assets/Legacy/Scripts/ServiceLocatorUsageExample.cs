using UnityEngine;

/// <summary>
/// ServiceLocator 사용 예시 (참고용).
/// 실제 게임에서는 WaveSystem / EnemySpawner 등이 같은 방식으로 접근한다.
/// 씬에 붙이지 않아도 되며, 필요하면 빈 오브젝트에 붙여 로그를 확인할 수 있다.
/// </summary>
public class ServiceLocatorUsageExample : MonoBehaviour
{
    private void Start()
    {
        IScoreService score = ServiceLocator.Get<IScoreService>();
        score.AddScore(0);

        IPlayerService player = ServiceLocator.Get<IPlayerService>();
        Debug.Log($"[Example] Gold={player.Gold}, Hp={player.CurrentHp}, Score={score.CurrentScore}");

        if (ServiceLocator.TryGet(out IPoolService pool))
        {
            Debug.Log($"[Example] Pool 서비스 준비됨: {pool.GetType().Name}");
        }
    }
}
