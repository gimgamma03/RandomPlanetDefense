using TMPro;
using UnityEngine;

public class PanelPlayerTMP : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI playerGold;
    [SerializeField]
    private TextMeshProUGUI playerHp;
    [SerializeField]
    private TextMeshProUGUI playerCrystal;
    [SerializeField]
    private TextMeshProUGUI currentScoreText;

    private IPlayerService playerService;
    private IMetaProgressService metaProgress;
    private bool playerSubscribed;
    private bool metaSubscribed;

    private void OnEnable()
    {
        TryBind();
    }

    private void Start()
    {
        // Bootstrap / ConfigureRun 타이밍 대비
        TryBind();

        if (currentScoreText != null)
        {
            currentScoreText.text = "0";
        }
    }

    private void OnDisable()
    {
        Unbind();
    }

    private void TryBind()
    {
        if (playerService == null)
        {
            ServiceLocator.TryGet(out playerService);
        }

        if (metaProgress == null)
        {
            ServiceLocator.TryGet(out metaProgress);
        }

        if (playerService != null && !playerSubscribed)
        {
            playerService.OnGoldChanged += RefreshGold;
            playerService.OnHpChanged += RefreshHp;
            playerSubscribed = true;
        }

        if (metaProgress != null && !metaSubscribed)
        {
            metaProgress.OnCrystalsChanged += RefreshCrystal;
            metaSubscribed = true;
        }

        RefreshGold();
        RefreshHp();
        RefreshCrystal();
    }

    private void Unbind()
    {
        if (playerSubscribed && playerService != null)
        {
            playerService.OnGoldChanged -= RefreshGold;
            playerService.OnHpChanged -= RefreshHp;
        }

        if (metaSubscribed && metaProgress != null)
        {
            metaProgress.OnCrystalsChanged -= RefreshCrystal;
        }

        playerSubscribed = false;
        metaSubscribed = false;
    }

    private void RefreshGold()
    {
        if (playerGold == null)
        {
            return;
        }

        if (playerService == null)
        {
            ServiceLocator.TryGet(out playerService);
            if (playerService == null)
            {
                return;
            }
        }

        playerGold.text = playerService.Gold.ToString();
    }

    private void RefreshHp()
    {
        if (playerHp == null)
        {
            return;
        }

        if (playerService == null)
        {
            ServiceLocator.TryGet(out playerService);
            if (playerService == null)
            {
                return;
            }
        }

        playerHp.text = playerService.CurrentHp.ToString();
    }

    private void RefreshCrystal()
    {
        if (playerCrystal == null)
        {
            return;
        }

        if (metaProgress == null)
        {
            ServiceLocator.TryGet(out metaProgress);
        }

        int crystals = metaProgress != null ? metaProgress.Crystals : 0;
        playerCrystal.text = crystals.ToString();
    }
}
