using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 로드 / 종료만 담당. UI 패널 전환은 TitleFlow.
/// </summary>
public class SceneDirector : MonoBehaviour
{
    public const string TitleSceneName = "Title";
    public const string GameSceneName = "GameScene";

    public void GameStart()
    {
        SceneManager.LoadScene(GameSceneName);
    }

    public void TitleScene()
    {
        SceneManager.LoadScene(TitleSceneName);
    }

    /// <summary>하위 호환 — TitleScene과 동일.</summary>
    public void OpeningScene()
    {
        TitleScene();
    }

    public void GameExit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
