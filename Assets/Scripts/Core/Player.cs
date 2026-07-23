using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 씬 전용 플레이어 뷰.
/// 골드/HP 데이터는 IPlayerService, 여기선 피격 UI·게임오버 연출만.
/// </summary>
[DefaultExecutionOrder(-100)]
public class Player : MonoBehaviour
{
    [Tooltip("한 판 시작 골드 (인스펙터)")]
    public int gold;
    public int maxHp;

    [SerializeField] private WaveSystem waveSystem;
    [SerializeField] private Image hitRedImage;

    private IPlayerService playerService;
    private SceneDirector sceneDirector;
    private bool configured;

    private void Awake()
    {
        sceneDirector = GetComponent<SceneDirector>();
        TryConfigureRun();
    }

    private void Start()
    {
        // Bootstrap 타이밍 대비 — Awake에서 못 했으면 Start에서 재시도
        TryConfigureRun();
    }

    private void TryConfigureRun()
    {
        if (configured)
        {
            return;
        }

        if (!ServiceLocator.TryGet(out playerService))
        {
            Debug.LogWarning("[Player] IPlayerService 아직 없음 — Start에서 재시도합니다.", this);
            return;
        }

        playerService.ConfigureRun(gold, maxHp);
        playerService.OnDamaged += PlayHitAnimation;
        playerService.OnDied += HandleGameOver;
        configured = true;
    }

    private void OnDestroy()
    {
        if (playerService == null || !configured)
        {
            return;
        }

        playerService.OnDamaged -= PlayHitAnimation;
        playerService.OnDied -= HandleGameOver;
    }

    public int currentHp => playerService != null ? playerService.CurrentHp : 0;

    public void TakeDamage(int damage)
    {
        if (!configured)
        {
            TryConfigureRun();
        }

        playerService?.TakeDamage(damage);
    }

    private void PlayHitAnimation()
    {
        if (!isActiveAndEnabled || hitRedImage == null)
        {
            return;
        }

        StopCoroutine(nameof(HitAnimation));
        StartCoroutine(HitAnimation());
    }

    private void HandleGameOver()
    {
        waveSystem.FinishGame();
        Invoke(nameof(GameOverScene), 3f);
    }

    public void GameOverScene()
    {
        sceneDirector.OpeningScene();
    }

    private IEnumerator HitAnimation()
    {
        Color color = hitRedImage.color;
        color.a = 0.4f;
        hitRedImage.color = color;

        while (color.a >= 0.0f)
        {
            color.a -= Time.deltaTime;
            hitRedImage.color = color;
            yield return null;
        }
    }
}