using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PanelPlayerTMP : MonoBehaviour
{
    [SerializeField]
    private Player player;

    [SerializeField]
    private TextMeshProUGUI playerGold;
    [SerializeField]
    private TextMeshProUGUI playerHp;
    [SerializeField]
    private TextMeshProUGUI currentScoreText;

    void Start()
    {
        currentScoreText.text = "0";
    }

    // Update is called once per frame
    void Update()
    {
        playerGold.text = player.gold.ToString();
        playerHp.text = player.currentHp.ToString();
    }
}
