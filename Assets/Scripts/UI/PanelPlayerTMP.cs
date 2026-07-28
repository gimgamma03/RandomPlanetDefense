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
    private int lastCrystalShown = int.MinValue;

    private void Start()
    {
        ServiceLocator.TryGet(out playerService);
        ServiceLocator.TryGet(out metaProgress);

        if (currentScoreText != null)
        {
            currentScoreText.text = "0";
        }

        RefreshCrystal(force: true);
    }

    private void Update()
    {
        if (playerService != null)
        {
            if (playerGold != null)
            {
                playerGold.text = playerService.Gold.ToString();
            }

            if (playerHp != null)
            {
                playerHp.text = playerService.CurrentHp.ToString();
            }
        }

        RefreshCrystal(force: false);
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
