using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// 타워 정보 패널 — 이름 / 스탯 / 업그레이드 버튼 3단.
/// </summary>
public class PanelTowerDataViewer : MonoBehaviour
{
    [SerializeField]
    private Image towerImage;

    [Tooltip("이름 · 등급 · 레벨 (한 줄)")]
    [SerializeField]
    private TextMeshProUGUI textLevel;

    [Tooltip("Damage / Rate / Range / 유틸 묶음")]
    [FormerlySerializedAs("textDamage")]
    [SerializeField]
    private TextMeshProUGUI textStats;

    [SerializeField]
    private TextMeshProUGUI textUpGradeGold;

    [Header("Legacy (비활성 권장)")]
    [SerializeField]
    private TextMeshProUGUI textRate;
    [SerializeField]
    private TextMeshProUGUI textRange;
    [SerializeField]
    private TextMeshProUGUI textUtility;

    private IPlayerService playerService;
    private TowerWeapon currentTowerWeapon;
    private Slow visibleSlowRange;
    private readonly StringBuilder statsBuilder = new StringBuilder(128);

    private void Awake()
    {
        HideLegacyStatTexts();
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
        HideSlowRangeVisual();
        currentTowerWeapon = tower.GetComponent<TowerWeapon>();
        gameObject.SetActive(true);
        UpdateTowerData();
        ShowSlowRangeIfNeeded();
    }

    public void OffPanel()
    {
        HideSlowRangeVisual();
        currentTowerWeapon = null;
        gameObject.SetActive(false);
    }

    private void ShowSlowRangeIfNeeded()
    {
        if (currentTowerWeapon == null || currentTowerWeapon.weaponType != WeaponType.Slow)
        {
            return;
        }

        visibleSlowRange = currentTowerWeapon.GetComponentInChildren<Slow>(true);
        if (visibleSlowRange != null)
        {
            visibleSlowRange.SetVisualVisible(true);
        }
    }

    private void HideSlowRangeVisual()
    {
        if (visibleSlowRange != null)
        {
            visibleSlowRange.SetVisualVisible(false);
            visibleSlowRange = null;
        }
    }

    public void UpdateTowerData()
    {
        if (currentTowerWeapon == null)
        {
            return;
        }

        if (towerImage != null)
        {
            towerImage.sprite = currentTowerWeapon.towerSprite;
            towerImage.color = currentTowerWeapon.TowerSpriteColor;
        }

        // 1) 이름
        if (textLevel != null)
        {
            textLevel.enableWordWrapping = false;
            textLevel.overflowMode = TextOverflowModes.Ellipsis;
            textLevel.text = currentTowerWeapon.DisplayName;
        }

        // 2) 스탯
        if (textStats != null)
        {
            textStats.enableWordWrapping = true;
            textStats.overflowMode = TextOverflowModes.Overflow;
            textStats.text = BuildStatsText();
        }

        // 3) 업그레이드 버튼 라벨
        if (textUpGradeGold != null)
        {
            textUpGradeGold.text = "UpGrade:" + currentTowerWeapon.upGradeGold + " Gold";
        }
    }

    private string BuildStatsText()
    {
        statsBuilder.Clear();
        // Orbit은 스페이스가 넓어서 " : " 대신 ":" 로 붙인다
        statsBuilder.Append("Damage:").Append(currentTowerWeapon.damage);
        statsBuilder.Append('\n').Append("Rate:").Append(currentTowerWeapon.rate);
        statsBuilder.Append('\n').Append("Range:").Append(currentTowerWeapon.range.ToString("0.00"));

        string utility = GetUtilityLine();
        if (!string.IsNullOrEmpty(utility))
        {
            statsBuilder.Append('\n').Append(utility);
        }

        return statsBuilder.ToString();
    }

    private string GetUtilityLine()
    {
        switch (currentTowerWeapon.weaponType)
        {
            case WeaponType.Slow:
                return "Slow:" + (currentTowerWeapon.slowValue * 100f).ToString("0") + "%";
            case WeaponType.MultiWayShooting:
                return "Shots:" + currentTowerWeapon.MultiShotCount;
            case WeaponType.GroundBombLine:
                return "Bombs:" + currentTowerWeapon.GroundBombCount;
            case WeaponType.OrbitSatellite:
                return "Satellites:" + currentTowerWeapon.OrbitSatelliteCount;
            default:
                return string.Empty;
        }
    }

    private void HideLegacyStatTexts()
    {
        DisableTextObject(textRate);
        DisableTextObject(textRange);
        DisableTextObject(textUtility);
    }

    private static void DisableTextObject(TextMeshProUGUI text)
    {
        if (text != null)
        {
            text.gameObject.SetActive(false);
        }
    }

    public void UpGradeTowerButton()
    {
        if (currentTowerWeapon == null)
        {
            return;
        }

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
