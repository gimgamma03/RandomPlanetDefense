using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Resources/Stages 목록을 버튼으로 만들고, 선택 시 TitleFlow로 넘긴다.
/// </summary>
public sealed class StageSelectPanel : MonoBehaviour
{
    [SerializeField]
    private TitleFlow titleFlow;
    [SerializeField]
    private Transform contentRoot;
    [SerializeField]
    private Button stageButtonPrefab;
    [SerializeField]
    private Button backButton;

    [Header("Sci-Fi Skin (atlas)")]
    [SerializeField]
    private Sprite buttonSprite;
    [SerializeField]
    private Sprite buttonPressedSprite;
    [SerializeField]
    private Color buttonColor = Color.white;
    [SerializeField]
    private TMP_FontAsset orbitFont;

    private StageCatalog catalog;

    private void Awake()
    {
        if (titleFlow == null)
        {
            titleFlow = GetComponentInParent<TitleFlow>();
        }

        EnsureOrbitFont();
        EnsureContentRoot();

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(OnBack);
            backButton.onClick.AddListener(OnBack);
            ApplyButtonSkin(backButton);
            TextMeshProUGUI backLabel = backButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (backLabel != null && orbitFont != null)
            {
                backLabel.font = orbitFont;
            }
        }
    }

    public void Refresh()
    {
        EnsureOrbitFont();
        EnsureContentRoot();
        catalog = StageCatalog.LoadFromResources();
        RebuildButtons();
    }

    private void EnsureContentRoot()
    {
        if (contentRoot == null)
        {
            Transform found = transform.Find("ScrollStages/Viewport/ContentStages");
            if (found == null)
            {
                found = transform.Find("ContentStages");
            }

            if (found == null)
            {
                found = FindDeep(transform, "ContentStages");
            }

            contentRoot = found;
        }

        if (contentRoot == null)
        {
            Debug.LogError(
                "[StageSelectPanel] ContentStages / ScrollStages 없음. " +
                "Title 씬에서 ScrollStages를 배치하세요.");
            return;
        }

        EnsureContentLayoutComponents();
        WireScrollRectIfNeeded();
    }

    /// <summary>씬에 둔 ScrollStages 크기를 덮어쓰지 않음. 누락 컴포넌트만 보강.</summary>
    private void EnsureContentLayoutComponents()
    {
        if (contentRoot == null)
        {
            return;
        }

        if (contentRoot.GetComponent<VerticalLayoutGroup>() == null)
        {
            VerticalLayoutGroup layout = contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 16f;
            layout.padding = new RectOffset(24, 24, 12, 12);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
        }

        if (contentRoot.GetComponent<ContentSizeFitter>() == null)
        {
            ContentSizeFitter fitter = contentRoot.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }

    private void WireScrollRectIfNeeded()
    {
        Transform scrollTransform = transform.Find("ScrollStages");
        if (scrollTransform == null)
        {
            return;
        }

        ScrollRect scroll = scrollTransform.GetComponent<ScrollRect>();
        if (scroll == null)
        {
            return;
        }

        RectTransform contentRt = contentRoot as RectTransform;
        if (scroll.content == null && contentRt != null)
        {
            scroll.content = contentRt;
        }

        if (scroll.viewport == null)
        {
            Transform viewport = scrollTransform.Find("Viewport");
            if (viewport != null)
            {
                scroll.viewport = viewport as RectTransform;
            }
        }
    }

    private void RebuildButtons()
    {
        if (contentRoot == null)
        {
            Debug.LogError("[StageSelectPanel] contentRoot 미할당.");
            return;
        }

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(contentRoot.GetChild(i).gameObject);
        }

        if (catalog == null || catalog.All.Count == 0)
        {
            Debug.LogWarning("[StageSelectPanel] StageData 0개 — Resources/Stages 확인.");
            CreateEmptyHint();
            return;
        }

        for (int i = 0; i < catalog.All.Count; i++)
        {
            StageData stage = catalog.All[i];
            if (stage == null || !stage.IsSelectableInCurrentBuild)
            {
                continue;
            }

            CreateStageButton(stage);
        }

        RectTransform contentRt = contentRoot as RectTransform;
        if (contentRt != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRt);
        }

        Canvas.ForceUpdateCanvases();
        Debug.Log($"[StageSelectPanel] 스테이지 버튼 {contentRoot.childCount}개 생성.");
    }

    private void CreateEmptyHint()
    {
        GameObject go = new GameObject("EmptyHint", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
        go.transform.SetParent(contentRoot, false);
        LayoutElement le = go.GetComponent<LayoutElement>();
        le.minHeight = 64f;
        le.preferredHeight = 64f;
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = "스테이지 데이터 없음";
        tmp.fontSize = 28f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        if (orbitFont != null)
        {
            tmp.font = orbitFont;
        }
    }

    private void CreateStageButton(StageData stage)
    {
        Button button;
        if (stageButtonPrefab != null)
        {
            button = Instantiate(stageButtonPrefab, contentRoot);
            button.gameObject.SetActive(true);
        }
        else
        {
            button = CreateRuntimeButton(contentRoot);
        }

        button.name = $"ButtonStage_{stage.stageId}";
        ApplyButtonSkin(button);

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        string display = stage.DisplayName;
        bool locked = IsStageLocked(stage);
        if (locked)
        {
            display += " [Locked]";
        }
        else if (ServiceLocator.TryGet(out IMetaProgressService meta))
        {
            if (meta.IsStageCleared(stage.stageId))
            {
                display += " [Clear]";
            }

            int best = meta.GetStageBestScore(stage.stageId);
            if (best > 0)
            {
                display += $"  Best {best}";
            }
        }

        if (label != null)
        {
            label.text = display;
            if (orbitFont != null)
            {
                label.font = orbitFont;
            }

            label.ForceMeshUpdate(true);
        }
        else
        {
            Text legacy = button.GetComponentInChildren<Text>(true);
            if (legacy != null)
            {
                legacy.text = display;
            }
        }

        int stageId = stage.stageId;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnStageClicked(stageId));

        if (locked)
        {
            button.interactable = false;

            Image lockedImage = button.targetGraphic as Image;
            if (lockedImage != null)
            {
                Color dim = lockedImage.color;
                dim.r *= 0.45f;
                dim.g *= 0.45f;
                dim.b *= 0.45f;
                lockedImage.color = dim;
            }

            if (label != null)
            {
                Color labelDim = label.color;
                labelDim.a *= 0.6f;
                label.color = labelDim;
            }
        }
    }

    /// <summary>Stage 1 클리어 전에는 나머지 스테이지 잠금. 테스트(editorOnly) 스테이지는 제외.</summary>
    private bool IsStageLocked(StageData stage)
    {
        if (stage.stageId <= 1 || stage.editorOnly)
        {
            return false;
        }

        if (!ServiceLocator.TryGet(out IMetaProgressService meta))
        {
            return false;
        }

        return !meta.IsStageCleared(1);
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

    private void OnStageClicked(int stageId)
    {
        if (titleFlow != null)
        {
            titleFlow.OnStageConfirmed(stageId);
            return;
        }

        GameSession.SelectStage(stageId);
        SceneDirector director = FindFirstObjectByType<SceneDirector>();
        if (director != null)
        {
            director.GameStart();
        }
    }

    private void OnBack()
    {
        if (titleFlow != null)
        {
            titleFlow.OnClickBackToTitle();
        }
    }

    private Button CreateRuntimeButton(Transform parent)
    {
        GameObject root = new GameObject(
            "StageButton",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(LayoutElement));

        root.transform.SetParent(parent, false);

        LayoutElement layout = root.GetComponent<LayoutElement>();
        layout.minHeight = 100f;
        layout.preferredHeight = 100f;
        layout.flexibleWidth = 1f;
        layout.minWidth = 400f;

        Image image = root.GetComponent<Image>();
        if (buttonSprite != null)
        {
            image.sprite = buttonSprite;
            image.type = Image.Type.Sliced;
            image.color = buttonColor;
        }
        else
        {
            image.color = new Color(0.12f, 0.18f, 0.28f, 0.95f);
        }

        GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(root.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20f, 10f);
        textRect.offsetMax = new Vector2(-20f, -10f);

        TextMeshProUGUI tmp = textObject.GetComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 34f;
        tmp.color = Color.white;
        tmp.text = "Stage";
        tmp.raycastTarget = false;
        if (orbitFont != null)
        {
            tmp.font = orbitFont;
        }

        Button button = root.GetComponent<Button>();
        button.targetGraphic = image;
        return button;
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

    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
