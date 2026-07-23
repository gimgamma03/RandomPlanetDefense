using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PanelTowerDataViewer : MonoBehaviour
{
    [SerializeField]
    private Player player;

    [SerializeField]
    private Image towerImage;
    [SerializeField]
    private TextMeshProUGUI textDamage;
    [SerializeField]
    private TextMeshProUGUI textRate;
    [SerializeField]
    private TextMeshProUGUI textRange;
    [SerializeField]
    private TextMeshProUGUI textLevel;
    [SerializeField]
    private TextMeshProUGUI textUpGradeGold;
    [SerializeField]
    private TextMeshProUGUI textUtility;

    private TowerWeapon currentTowerWeapon;

    //bool showSpecialData = false;

    private void Awake()
    {
        OffPanel();
    }
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OffPanel();
        }
    }

    public void OnPanel(Transform tower)
    {
        currentTowerWeapon = tower.GetComponent<TowerWeapon>();
        this.gameObject.SetActive(true);
        UpdateTowerData();

    }
    public void OffPanel()
    {
        this.gameObject.SetActive(false);
        //TowerAttackRange.OffAttackRange();
    }

    public void UpdateTowerData()
    {
        towerImage.sprite = currentTowerWeapon.towerSprite;
        textLevel.text = "Level : " + (currentTowerWeapon.level + 1);
        textDamage.text = "Damage : " + currentTowerWeapon.damage;
        textRate.text = "Rate : " + currentTowerWeapon.rate;
        textRange.text = "Range : " + currentTowerWeapon.range.ToString("0.00");

        if (currentTowerWeapon.weaponType == WeaponType.Slow)
        {
            textUtility.text = "Slow : " + (currentTowerWeapon.slowValue * 100f).ToString("0") + "%";
        }
        else
        {
            textUtility.text = "";
        }

        textUpGradeGold.text = "UpGrade : " + currentTowerWeapon.upGradeGold + " Gold";

    }

    public void UpGradeTowerButton()
    {
        if (player.gold >= currentTowerWeapon.upGradeGold)
        {
            currentTowerWeapon.UPGrade();
            player.gold -= currentTowerWeapon.upGradeGold;
        }
        UpdateTowerData();
    }
}
