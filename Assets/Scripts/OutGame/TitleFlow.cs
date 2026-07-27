using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Title 씬 패널 전환: Title(Start/Exit) ↔ StageSelect.
/// Canvas에 SceneDirector와 같이 붙인다.
/// </summary>
public sealed class TitleFlow : MonoBehaviour
{
    [SerializeField]
    private GameObject panelTitle;
    [SerializeField]
    private GameObject panelStageSelect;
    [SerializeField]
    private SceneDirector sceneDirector;
    [SerializeField]
    private StageSelectPanel stageSelectPanel;
    [SerializeField]
    private Button startButton;
    [SerializeField]
    private Button exitButton;
    [Tooltip("PanelBackGround 아래 타이틀 로고/텍스트. 스테이지 선택 중에는 숨김")]
    [SerializeField]
    private GameObject titleBanner;

    private void Awake()
    {
        if (sceneDirector == null)
        {
            sceneDirector = GetComponent<SceneDirector>();
        }

        if (stageSelectPanel == null && panelStageSelect != null)
        {
            stageSelectPanel = panelStageSelect.GetComponent<StageSelectPanel>();
        }

        if (titleBanner == null)
        {
            Transform found = transform.Find("PanelBackGround/TitleText");
            if (found != null)
            {
                titleBanner = found.gameObject;
            }
        }

        Wire(startButton, OnClickStart);
        Wire(exitButton, OnClickExit);
        ShowTitle();
    }

    /// <summary>타이틀 Start 버튼.</summary>
    public void OnClickStart()
    {
        ShowStageSelect();
    }

    /// <summary>타이틀 Exit 버튼.</summary>
    public void OnClickExit()
    {
        if (sceneDirector != null)
        {
            sceneDirector.GameExit();
        }
        else
        {
            Application.Quit();
        }
    }

    /// <summary>스테이지 선택 패널 Back.</summary>
    public void OnClickBackToTitle()
    {
        ShowTitle();
    }

    /// <summary>스테이지 확정 → GameScene.</summary>
    public void OnStageConfirmed(int stageId)
    {
        GameSession.SelectStage(stageId);
        if (sceneDirector == null)
        {
            Debug.LogError("[TitleFlow] SceneDirector 없음.");
            return;
        }

        sceneDirector.GameStart();
    }

    public void ShowTitle()
    {
        SetPanel(panelTitle, true);
        SetPanel(panelStageSelect, false);
        SetPanel(titleBanner, true);
    }

    public void ShowStageSelect()
    {
        SetPanel(panelTitle, false);
        SetPanel(panelStageSelect, true);
        SetPanel(titleBanner, false);
        stageSelectPanel?.Refresh();
    }

    private static void Wire(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private static void SetPanel(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }
}
