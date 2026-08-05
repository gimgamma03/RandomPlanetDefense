using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 타이틀 영구 타워 강화. 헤더/스크롤 목록/Back 고정 배치. TMP는 Orbit.
/// </summary>
public sealed class TowerUpgradePanel : MonoBehaviour
{
    [SerializeField]
    private TitleFlow titleFlow;
    [SerializeField]
    private Transform contentRoot;
    [SerializeField]
    private Button backButton;
    [SerializeField]
    private TextMeshProUGUI crystalText;
    [SerializeField]
    private Image crystalIcon;
    [SerializeField]
    private TextMeshProUGUI titleText;
    [SerializeField]
    private TMP_FontAsset orbitFont;

    [Header("Sci-Fi Skin")]
    [SerializeField]
    private Sprite buttonSprite;
    [SerializeField]
    private Sprite buttonPressedSprite;
    [SerializeField]
    private Color buttonColor = Color.white;

    private void Awake()
    {
        if (titleFlow == null)
        {
            titleFlow = GetComponentInParent<TitleFlow>();
        }

        EnsureOrbitFont();
        ApplyFont(titleText);
        ApplyFont(crystalText);
        ApplyHeaderScale();

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(OnBack);
            backButton.onClick.AddListener(OnBack);
            ApplyButtonSkin(backButton);
            ApplyFont(backButton.GetComponentInChildren<TextMeshProUGUI>(true));
        }
    }

    public void Refresh()
    {
        EnsureOrbitFont();
        ApplyFont(titleText);
        ApplyFont(crystalText);
        ApplyHeaderScale();
        RefreshCrystalLabel();
        RebuildRows();
    }

    private void ApplyHeaderScale()
    {
        if (titleText != null)
        {
            titleText.fontSize = 44f;
        }

        if (crystalText != null)
        {
            crystalText.fontSize = 32f;
        }

        if (backButton != null)
        {
            TextMeshProUGUI backLabel = backButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (backLabel != null)
            {
                backLabel.fontSize = 28f;
            }
        }
    }

    private void RefreshCrystalLabel()
    {
        if (crystalText == null)
        {
            return;
        }

        int crystals = 0;
        if (ServiceLocator.TryGet(out IMetaProgressService meta))
        {
            crystals = meta.Crystals;
        }

        crystalText.text = crystals.ToString();
    }

    private void RebuildRows()
    {
        if (contentRoot == null)
        {
            Debug.LogError("[TowerUpgradePanel] contentRoot 미할당.");
            return;
        }

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(contentRoot.GetChild(i).gameObject);
        }

        List<TowerData> entries = CollectG1ByWeaponType();
        for (int i = 0; i < entries.Count; i++)
        {
            CreateRow(entries[i]);
        }

        RectTransform contentRt = contentRoot as RectTransform;
        if (contentRt != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);
        }

        Canvas.ForceUpdateCanvases();
    }

    private static List<TowerData> CollectG1ByWeaponType()
    {
        var result = new List<TowerData>();
        var seen = new HashSet<WeaponType>();
        TowerCatalog catalog = TowerCatalog.LoadFromResources();
        if (catalog == null)
        {
            return result;
        }

        for (int i = 0; i < catalog.Towers.Count; i++)
        {
            TowerData data = catalog.Towers[i];
            if (data == null || data.grade != TowerGrade.Grade1)
            {
                continue;
            }

            if (!seen.Add(data.weaponType))
            {
                continue;
            }

            result.Add(data);
        }

        result.Sort((a, b) => a.weaponType.CompareTo(b.weaponType));
        return result;
    }

    private void CreateRow(TowerData data)
    {
        WeaponType type = data.weaponType;
        int level = 0;
        int crystals = 0;
        if (ServiceLocator.TryGet(out IMetaProgressService meta))
        {
            level = meta.GetWeaponUpgradeLevel(type);
            crystals = meta.Crystals;
        }

        int nextCost = TowerMetaUpgradeRules.GetCostForNextLevel(level);
        bool maxed = level >= TowerMetaUpgradeRules.MaxLevel;
        string towerName = string.IsNullOrEmpty(data.DisplayName) ? type.ToString() : data.DisplayName;

        GameObject row = new GameObject(
            $"Upgrade_{type}",
            typeof(RectTransform),
            typeof(Image),
            typeof(LayoutElement));
        row.transform.SetParent(contentRoot, false);

        LayoutElement le = row.GetComponent<LayoutElement>();
        le.minHeight = 110f;
        le.preferredHeight = 110f;
        le.flexibleWidth = 1f;

        Image bg = row.GetComponent<Image>();
        bg.color = new Color(0.08f, 0.1f, 0.16f, 0.92f);
        bg.raycastTarget = false;

        // Icon — 왼쪽 고정
        GameObject iconBox = new GameObject(
            "IconBox",
            typeof(RectTransform),
            typeof(RectMask2D));
        iconBox.transform.SetParent(row.transform, false);
        RectTransform iconBoxRt = iconBox.GetComponent<RectTransform>();
        iconBoxRt.anchorMin = new Vector2(0f, 0.5f);
        iconBoxRt.anchorMax = new Vector2(0f, 0.5f);
        iconBoxRt.pivot = new Vector2(0f, 0.5f);
        iconBoxRt.anchoredPosition = new Vector2(16f, 0f);
        iconBoxRt.sizeDelta = new Vector2(64f, 64f);

        GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(iconBox.transform, false);
        RectTransform iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.anchorMin = Vector2.zero;
        iconRt.anchorMax = Vector2.one;
        iconRt.offsetMin = Vector2.zero;
        iconRt.offsetMax = Vector2.zero;
        Image icon = iconGo.GetComponent<Image>();
        icon.sprite = data.sprite;
        icon.color = data.spriteColor.a > 0.01f ? data.spriteColor : Color.white;
        icon.preserveAspect = true;
        icon.type = Image.Type.Simple;
        icon.raycastTarget = false;

        // 왼쪽 — 이름 / Lv·비용
        TextMeshProUGUI metaLabel = CreateRowLabel(
            row.transform,
            "Meta",
            new Vector2(0f, 0f),
            new Vector2(0.42f, 1f),
            new Vector2(92f, 10f),
            new Vector2(-8f, -10f),
            TextAlignmentOptions.MidlineLeft,
            22f);
        metaLabel.text = TowerMetaUpgradeRules.FormatRowMeta(towerName, level, nextCost, maxed);
        metaLabel.ForceMeshUpdate(true);

        // 가운데 — 효과 설명
        TextMeshProUGUI effect = CreateRowLabel(
            row.transform,
            "Effect",
            new Vector2(0.42f, 0f),
            new Vector2(1f, 1f),
            new Vector2(8f, 10f),
            new Vector2(-160f, -10f),
            TextAlignmentOptions.MidlineLeft,
            24f);
        effect.color = new Color(0.65f, 0.95f, 0.88f, 1f);
        effect.text = TowerMetaUpgradeRules.GetEffectSummary(type);
        effect.ForceMeshUpdate(true);

        // Up 버튼 — 오른쪽 끝 + 패딩
        GameObject btnGo = new GameObject(
            "ButtonUpgrade",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button));
        btnGo.transform.SetParent(row.transform, false);
        RectTransform btnRt = btnGo.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(1f, 0.5f);
        btnRt.anchorMax = new Vector2(1f, 0.5f);
        btnRt.pivot = new Vector2(1f, 0.5f);
        btnRt.anchoredPosition = new Vector2(-20f, 0f);
        btnRt.sizeDelta = new Vector2(132f, 56f);

        Image btnImage = btnGo.GetComponent<Image>();
        Button button = btnGo.GetComponent<Button>();
        button.targetGraphic = btnImage;
        ApplyButtonSkin(button);
        EnsureButtonLabel(btnGo.transform, maxed ? "MAX" : "Up");
        button.interactable = !maxed && crystals >= nextCost;
        button.onClick.AddListener(() => OnUpgradeClicked(type));
    }

    private TextMeshProUGUI CreateRowLabel(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax,
        TextAlignmentOptions alignment,
        float fontSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        EnsureOrbitFont();
        if (orbitFont != null)
        {
            tmp.font = orbitFont;
        }

        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Normal;
        tmp.color = Color.white;
        tmp.alignment = alignment;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;
        tmp.richText = false;
        return tmp;
    }

    private void OnUpgradeClicked(WeaponType type)
    {
        if (!ServiceLocator.TryGet(out IMetaProgressService meta))
        {
            return;
        }

        if (!meta.TryUpgradeWeapon(type))
        {
            Refresh();
            return;
        }

        Refresh();
        titleFlow?.RefreshCrystalHud();
    }

    private void OnBack()
    {
        titleFlow?.OnClickBackToTitle();
    }

    private void ApplyButtonSkin(Button button)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.targetGraphic as Image;
        if (image == null)
        {
            image = button.GetComponent<Image>();
        }

        if (image != null && buttonSprite != null)
        {
            image.sprite = buttonSprite;
            image.type = Image.Type.Sliced;
            image.color = buttonColor;
            button.targetGraphic = image;
        }

        if (buttonPressedSprite != null)
        {
            SpriteState state = button.spriteState;
            state.pressedSprite = buttonPressedSprite;
            state.highlightedSprite = buttonSprite != null ? buttonSprite : state.highlightedSprite;
            state.selectedSprite = buttonSprite != null ? buttonSprite : state.selectedSprite;
            button.spriteState = state;
            button.transition = Selectable.Transition.SpriteSwap;
        }
    }

    private void EnsureButtonLabel(Transform button, string text)
    {
        TextMeshProUGUI tmp = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp == null)
        {
            GameObject label = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(button, false);
            RectTransform rt = label.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(4f, 2f);
            rt.offsetMax = new Vector2(-4f, -2f);
            tmp = label.GetComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.fontSize = 24f;
            tmp.raycastTarget = false;
        }

        tmp.text = text;
        tmp.fontSize = 24f;
        ApplyFont(tmp);
        tmp.ForceMeshUpdate(true);
    }

    private void EnsureOrbitFont()
    {
        if (orbitFont != null)
        {
            return;
        }

#if UNITY_EDITOR
        orbitFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/Fonts/Orbit-Regular SDF.asset");
#endif
        if (orbitFont == null && TMP_Settings.defaultFontAsset != null)
        {
            orbitFont = TMP_Settings.defaultFontAsset;
        }
    }

    private void ApplyFont(TextMeshProUGUI tmp)
    {
        if (tmp == null)
        {
            return;
        }

        EnsureOrbitFont();
        if (orbitFont != null)
        {
            tmp.font = orbitFont;
        }
    }
}
