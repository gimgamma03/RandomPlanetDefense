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

        ResolveButtons();

        Wire(startButton, OnClickStart);
        Wire(upgradeButton, OnClickUpgrade);
        Wire(exitButton, OnClickExit);
        HideExitButtonOnWebGL();
        ShowTitle();
        RefreshCrystalHud();
    }

    /// <summary>WebGL에서는 Application.Quit이 동작하지 않아 Exit 버튼을 숨긴다.</summary>
    private void HideExitButtonOnWebGL()
    {
        if (exitButton == null)
        {
            return;
        }

        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            exitButton.gameObject.SetActive(false);
        }
    }

    private void ResolveButtons()
    {
        Transform searchRoot = panelTitle != null ? panelTitle.transform : transform;

        if (startButton == null)
        {
            startButton = FindButton(searchRoot, "ButtonStage")
                ?? FindButton(searchRoot, "ButtonGameStart")
                ?? FindButton(transform, "ButtonStage")
                ?? FindButton(transform, "ButtonGameStart");
        }

        if (upgradeButton == null)
        {
            upgradeButton = FindButton(searchRoot, "ButtonTowerUpgrade")
                ?? FindButton(transform, "ButtonTowerUpgrade");
        }

        if (exitButton == null)
        {
            exitButton = FindButton(searchRoot, "ButtonGameExit")
                ?? FindButton(transform, "ButtonGameExit");
        }
    }

    private static Button FindButton(Transform root, string name)
    {
        if (root == null)
        {
            return null;
        }

        Transform t = FindDeep(root, name);
        return t != null ? t.GetComponent<Button>() : null;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static void Wire(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
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

        if (stageSelectPanel == null && panelStageSelect != null)
        {
            stageSelectPanel = panelStageSelect.GetComponent<StageSelectPanel>();
        }

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

    private static void SetPanel(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }
}
