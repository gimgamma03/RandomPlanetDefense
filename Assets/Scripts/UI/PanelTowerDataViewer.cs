using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PanelTowerDataViewer : MonoBehaviour
{
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

    private IPlayerService playerService;
    private TowerWeapon currentTowerWeapon;

    private void Awake()
    {
        OffPanel();
    }

    private void Start()
    {
        playerService = ServiceLocator.Get<IPlayerService>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OffPanel();
        }
    }

    public void OnPanel(Transform tower)
    {
        currentTowerWeapon = tower.GetComponent<TowerWeapon>();
        gameObject.SetActive(true);
        UpdateTowerData();
    }

    public void OffPanel()
    {
        gameObject.SetActive(false);
    }

    public void UpdateTowerData()
    {
        towerImage.sprite = currentTowerWeapon.towerSprite;
        towerImage.color = currentTowerWeapon.TowerSpriteColor;
        // Grade = 합성 등급(시트), Level = 골드 업그레이드 단계
        textLevel.text = $"{currentTowerWeapon.DisplayName}  G{(int)currentTowerWeapon.towerGrade}  Lv{currentTowerWeapon.level}";
        textDamage.text = "Damage : " + currentTowerWeapon.damage;
        textRate.text = "Rate : " + currentTowerWeapon.rate;
        textRange.text = "Range : " + currentTowerWeapon.range.ToString("0.00");

        if (currentTowerWeapon.weaponType == WeaponType.Slow)
        {
            textUtility.text = "Slow : " + (currentTowerWeapon.slowValue * 100f).ToString("0") + "%";
        }
        else if (currentTowerWeapon.weaponType == WeaponType.MultiWayShooting)
        {
            textUtility.text = "Shots : " + currentTowerWeapon.MultiShotCount;
        }
        else if (currentTowerWeapon.weaponType == WeaponType.MultiBomb)
        {
            textUtility.text = "Bombs : " + currentTowerWeapon.MultiBombCount;
        }
        else
        {
            textUtility.text = "";
        }

        textUpGradeGold.text = "UpGrade : " + currentTowerWeapon.upGradeGold + " Gold";
    }

    public void UpGradeTowerButton()
    {
        if (playerService == null)
        {
            playerService = ServiceLocator.Get<IPlayerService>();
        }

        int cost = currentTowerWeapon.upGradeGold;
        if (playerService.TrySpendGold(cost))
        {
            currentTowerWeapon.UPGrade();
        }

        UpdateTowerData();
    }
}
