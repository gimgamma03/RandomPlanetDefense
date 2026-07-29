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
    private int lastGoldShown = int.MinValue;
    private int lastHpShown = int.MinValue;
    private int lastCrystalShown = int.MinValue;

    private void Start()
    {
        ServiceLocator.TryGet(out playerService);
        ServiceLocator.TryGet(out metaProgress);

        if (currentScoreText != null)
        {
            currentScoreText.text = "0";
        }

        RefreshGold(force: true);
        RefreshHp(force: true);
        RefreshCrystal(force: true);
    }

    private void Update()
    {
        RefreshGold(force: false);
        RefreshHp(force: false);
        RefreshCrystal(force: false);
    }

    private void RefreshGold(bool force)
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

        int gold = playerService.Gold;
        if (!force && gold == lastGoldShown)
        {
            return;
        }

        lastGoldShown = gold;
        playerGold.text = gold.ToString();
    }

    private void RefreshHp(bool force)
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

        int hp = playerService.CurrentHp;
        if (!force && hp == lastHpShown)
        {
            return;
        }

        lastHpShown = hp;
        playerHp.text = hp.ToString();
    }

    private void RefreshCrystal(bool force)
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
        if (!force && crystals == lastCrystalShown)
        {
            return;
        }

        lastCrystalShown = crystals;
        playerCrystal.text = crystals.ToString();
    }
}
