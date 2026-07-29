using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyHpViewer : MonoBehaviour
{
    private static readonly Vector2 NormalSize = new Vector2(50f, 20f);
    private static readonly Vector2 BossSize = new Vector2(84f, 26f);
    private static readonly Vector3 NormalOffset = new Vector3(0f, -20f, 0f);
    private static readonly Vector3 BossOffset = new Vector3(0f, -30f, 0f);
    private static readonly Color NormalFill = new Color(0.792f, 0f, 0f, 1f);
    private static readonly Color BossFill = new Color(0.95f, 0.35f, 0.85f, 1f);
    private static readonly Color NormalBackground = Color.white;
    private static readonly Color BossBackground = new Color(0.12f, 0.05f, 0.18f, 0.92f);
    private static readonly Color BossLabelColor = new Color(1f, 0.85f, 0.35f, 1f);

    private EnemyHp enemyHp;
    private Slider hpSlider;
    private RectTransform rectTransform;
    private Image fillImage;
    private Image backgroundImage;
    private SliderPositionAutoSetter positionSetter;
    private TextMeshProUGUI bossLabel;
    private bool styleCached;
    private Vector2 cachedFillAnchorMin;
    private Vector2 cachedFillAnchorMax;

    public void Setup(EnemyHp enemyHp)
    {
        this.enemyHp = enemyHp;
        hpSlider = GetComponent<Slider>();
        rectTransform = GetComponent<RectTransform>();
        positionSetter = GetComponent<SliderPositionAutoSetter>();
        CacheParts();

        Enemy enemy = null;
        bool isBoss = false;
        if (enemyHp != null && enemyHp.TryGetComponent(out enemy)
            && enemy.enemyData != null
            && enemy.enemyData.isBoss)
        {
            isBoss = true;
        }

        string label = isBoss ? ResolveBossLabel(enemy) : null;
        ApplyVisualStyle(isBoss, label);
        hpSliderUpdate();
    }

    public void ClearForPool()
    {
        enemyHp = null;
        ApplyVisualStyle(isBoss: false, label: null);

        if (positionSetter != null)
        {
            positionSetter.Setup(null);
        }
    }

    public void hpSliderUpdate()
    {
        if (enemyHp == null || hpSlider == null || enemyHp.maxHp <= 0f)
        {
            return;
        }

        hpSlider.value = enemyHp.currentHp / enemyHp.maxHp;
    }

    private void Update()
    {
        if (enemyHp == null)
        {
            return;
        }

        hpSliderUpdate();
    }

    private void CacheParts()
    {
        if (hpSlider == null)
        {
            return;
        }

        if (fillImage == null && hpSlider.fillRect != null)
        {
            fillImage = hpSlider.fillRect.GetComponent<Image>();
        }

        if (backgroundImage == null)
        {
            Transform bg = transform.Find("Background");
            if (bg != null)
            {
                backgroundImage = bg.GetComponent<Image>();
            }
        }

        if (!styleCached && hpSlider.fillRect != null)
        {
            Transform fillArea = hpSlider.fillRect.parent;
            if (fillArea != null)
            {
                RectTransform fillAreaRect = fillArea as RectTransform;
                if (fillAreaRect != null)
                {
                    cachedFillAnchorMin = fillAreaRect.anchorMin;
                    cachedFillAnchorMax = fillAreaRect.anchorMax;
                    styleCached = true;
                }
            }
        }
    }

    private void ApplyVisualStyle(bool isBoss, string label)
    {
        CacheParts();

        if (rectTransform != null)
        {
            rectTransform.sizeDelta = isBoss ? BossSize : NormalSize;
        }

        if (positionSetter != null)
        {
            positionSetter.SetDistance(isBoss ? BossOffset : NormalOffset);
        }

        if (fillImage != null)
        {
            fillImage.color = isBoss ? BossFill : NormalFill;
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = isBoss ? BossBackground : NormalBackground;
        }

        SetFillThickness(isBoss);
        SetBossLabel(isBoss, label);
    }

    private void SetFillThickness(bool isBoss)
    {
        if (hpSlider == null || hpSlider.fillRect == null)
        {
            return;
        }

        RectTransform fillArea = hpSlider.fillRect.parent as RectTransform;
        if (fillArea == null)
        {
            return;
        }

        if (isBoss)
        {
            fillArea.anchorMin = new Vector2(0f, 0.12f);
            fillArea.anchorMax = new Vector2(1f, 0.88f);
            if (backgroundImage != null)
            {
                RectTransform bgRect = backgroundImage.rectTransform;
                bgRect.anchorMin = new Vector2(0f, 0.12f);
                bgRect.anchorMax = new Vector2(1f, 0.88f);
            }
        }
        else if (styleCached)
        {
            fillArea.anchorMin = cachedFillAnchorMin;
            fillArea.anchorMax = cachedFillAnchorMax;
            if (backgroundImage != null)
            {
                RectTransform bgRect = backgroundImage.rectTransform;
                bgRect.anchorMin = new Vector2(0f, 0.25f);
                bgRect.anchorMax = new Vector2(1f, 0.75f);
            }
        }
    }

    private void SetBossLabel(bool isBoss, string label)
    {
        if (!isBoss)
        {
            if (bossLabel != null)
            {
                bossLabel.gameObject.SetActive(false);
            }

            return;
        }

        EnsureBossLabel();
        bossLabel.gameObject.SetActive(true);
        bossLabel.text = string.IsNullOrEmpty(label) ? "BOSS" : label;
    }

    private void EnsureBossLabel()
    {
        if (bossLabel != null)
        {
            return;
        }

        Transform existing = transform.Find("BossLabel");
        GameObject go;
        if (existing != null)
        {
            go = existing.gameObject;
            bossLabel = go.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            go = new GameObject("BossLabel", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            bossLabel = go.AddComponent<TextMeshProUGUI>();
        }

        if (bossLabel == null)
        {
            bossLabel = go.AddComponent<TextMeshProUGUI>();
        }

        RectTransform labelRect = bossLabel.rectTransform;
        labelRect.anchorMin = new Vector2(0.5f, 1f);
        labelRect.anchorMax = new Vector2(0.5f, 1f);
        labelRect.pivot = new Vector2(0.5f, 0f);
        labelRect.anchoredPosition = new Vector2(0f, 2f);
        labelRect.sizeDelta = new Vector2(120f, 18f);

        bossLabel.fontSize = 12f;
        bossLabel.fontStyle = FontStyles.Bold;
        bossLabel.alignment = TextAlignmentOptions.Center;
        bossLabel.color = BossLabelColor;
        bossLabel.raycastTarget = false;
        bossLabel.enableWordWrapping = false;
        bossLabel.overflowMode = TextOverflowModes.Overflow;

        TMP_FontAsset font = ResolveOrbitFont();
        if (font != null)
        {
            bossLabel.font = font;
        }
    }

    private static string ResolveBossLabel(Enemy enemy)
    {
        if (enemy == null || enemy.enemyData == null)
        {
            return "BOSS";
        }

        if (!string.IsNullOrEmpty(enemy.enemyData.displayName))
        {
            return enemy.enemyData.displayName;
        }

        return "BOSS";
    }

    private static TMP_FontAsset ResolveOrbitFont()
    {
#if UNITY_EDITOR
        TMP_FontAsset orbit = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/Fonts/Orbit-Regular SDF.asset");
        if (orbit != null)
        {
            return orbit;
        }
#endif
        TMP_FontAsset[] loaded = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        for (int i = 0; i < loaded.Length; i++)
        {
            TMP_FontAsset candidate = loaded[i];
            if (candidate != null && candidate.name.Contains("Orbit"))
            {
                return candidate;
            }
        }

        return TMP_Settings.defaultFontAsset;
    }
}
