using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Title 씬 패널 전환: Title ↔ StageSelect ↔ TowerUpgrade.
/// </summary>
public sealed class TitleFlow : MonoBehaviour
{
    [SerializeField]
    private GameObject panelTitle;
    [SerializeField]
    private GameObject panelStageSelect;
    [SerializeField]
    private GameObject panelTowerUpgrade;
    [SerializeField]
    private SceneDirector sceneDirector;
    [SerializeField]
    private StageSelectPanel stageSelectPanel;
    [SerializeField]
    private TowerUpgradePanel towerUpgradePanel;
    [SerializeField]
    private Button startButton;
    [SerializeField]
    private Button upgradeButton;
    [SerializeField]
    private Button exitButton;
    [Tooltip("PanelBackGround 아래 타이틀 로고/텍스트. 서브 패널 중에는 숨김")]
    [SerializeField]
    private GameObject titleBanner;
    [SerializeField]
    private TextMeshProUGUI crystalHudText;
    [SerializeField]
    private Image crystalHudIcon;

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

        if (towerUpgradePanel == null && panelTowerUpgrade != null)
        {
            towerUpgradePanel = panelTowerUpgrade.GetComponent<TowerUpgradePanel>();
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
        Wire(upgradeButton, OnClickUpgrade);
        Wire(exitButton, OnClickExit);
        ShowTitle();
        RefreshCrystalHud();
    }

    public void OnClickStart()
    {
        ShowStageSelect();
    }

    public void OnClickUpgrade()
    {
        ShowTowerUpgrade();
    }

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

    public void OnClickBackToTitle()
    {
        ShowTitle();
    }

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
        SetPanel(panelTowerUpgrade, false);
        SetPanel(titleBanner, true);
        RefreshCrystalHud();
    }

    public void ShowStageSelect()
    {
        SetPanel(panelTitle, false);
        SetPanel(panelStageSelect, true);
        SetPanel(panelTowerUpgrade, false);
        SetPanel(titleBanner, false);
        stageSelectPanel?.Refresh();
    }

    public void ShowTowerUpgrade()
    {
        SetPanel(panelTitle, false);
        SetPanel(panelStageSelect, false);
        SetPanel(panelTowerUpgrade, true);
        SetPanel(titleBanner, false);
        towerUpgradePanel?.Refresh();
        RefreshCrystalHud();
    }

    public void RefreshCrystalHud()
    {
        if (crystalHudText == null)
        {
            return;
        }

        int crystals = 0;
        if (ServiceLocator.TryGet(out IMetaProgressService meta))
        {
            crystals = meta.Crystals;
        }

        crystalHudText.text = crystals.ToString();
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
