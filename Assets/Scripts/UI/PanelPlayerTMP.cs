using TMPro;
using UnityEngine;

public class PanelPlayerTMP : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI playerGold;
    [SerializeField]
    private TextMeshProUGUI playerHp;
    [SerializeField]
    private TextMeshProUGUI currentScoreText;

    private IPlayerService playerService;

    private void Start()
    {
        playerService = ServiceLocator.Get<IPlayerService>();
        if (currentScoreText != null)
        {
            currentScoreText.text = "0";
        }
    }

    private void Update()
    {
        if (playerService == null)
        {
            return;
        }

        playerGold.text = playerService.Gold.ToString();
        playerHp.text = playerService.CurrentHp.ToString();
    }
}
