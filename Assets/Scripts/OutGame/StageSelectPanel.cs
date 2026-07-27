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
            BuildScrollContentRuntime();
        }
        else if (contentRoot.parent == transform)
        {
            PromoteContentIntoScroll();
        }

        EnsureContentLayoutComponents();
    }

    private void PromoteContentIntoScroll()
    {
        if (contentRoot == null || contentRoot.parent != transform)
        {
            return;
        }

        if (transform.Find("ScrollStages") != null)
        {
            return;
        }

        Transform oldContent = contentRoot;
        BuildScrollContentRuntime();
        if (contentRoot == null || contentRoot == oldContent)
        {
            return;
        }

        // 새 ContentStages로 컴포넌트/참조 유지 — 옛 오브젝트 제거
        Destroy(oldContent.gameObject);
    }

    private void BuildScrollContentRuntime()
    {
        RectTransform panelRt = transform as RectTransform;

        GameObject scrollGo = new GameObject("ScrollStages", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollGo.transform.SetParent(transform, false);
        RectTransform scrollRt = scrollGo.GetComponent<RectTransform>();
        scrollRt.anchorMin = Vector2.zero;
        scrollRt.anchorMax = Vector2.one;
        scrollRt.offsetMin = new Vector2(48f, 110f);
        scrollRt.offsetMax = new Vector2(-48f, -120f);
        Image scrollBg = scrollGo.GetComponent<Image>();
        scrollBg.color = new Color(0f, 0f, 0f, 0.2f);
        scrollBg.raycastTarget = true;

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(scrollGo.transform, false);
        RectTransform vpRt = viewport.GetComponent<RectTransform>();
        vpRt.anchorMin = Vector2.zero;
        vpRt.anchorMax = Vector2.one;
        vpRt.offsetMin = Vector2.zero;
        vpRt.offsetMax = Vector2.zero;
        Image vpImage = viewport.GetComponent<Image>();
        vpImage.color = new Color(1f, 1f, 1f, 0.01f);
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        GameObject content = new GameObject(
            "ContentStages",
            typeof(RectTransform),
            typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        contentRoot = content.transform;

        ScrollRect scroll = scrollGo.GetComponent<ScrollRect>();
        scroll.content = content.GetComponent<RectTransform>();
        scroll.viewport = vpRt;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        // Back 버튼은 맨 앞(아래)에 두기 위해 스크롤을 title 뒤로
        Transform title = transform.Find("TextStageSelectTitle");
        if (title != null)
        {
            scrollGo.transform.SetSiblingIndex(title.GetSiblingIndex() + 1);
        }

        if (panelRt != null)
        {
            // keep offsets relative to panel size
        }
    }

    private void EnsureContentLayoutComponents()
    {
        if (contentRoot == null)
        {
            return;
        }

        RectTransform contentRt = contentRoot as RectTransform;
        VerticalLayoutGroup layout = contentRoot.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        layout.childAlignment = TextAnchor.UpperCenter;
        layout.spacing = 16f;
        layout.padding = new RectOffset(24, 24, 12, 12);
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        ContentSizeFitter fitter = contentRoot.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = contentRoot.gameObject.AddComponent<ContentSizeFitter>();
        }

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 구형 ContentStages(높이 40 고정)면 스크롤 영역으로 재배치
        if (contentRoot.parent == transform)
        {
            contentRt.anchorMin = new Vector2(0.5f, 0.5f);
            contentRt.anchorMax = new Vector2(0.5f, 0.5f);
            contentRt.pivot = new Vector2(0.5f, 1f);

            RectTransform panelRt = transform as RectTransform;
            float width = 640f;
            if (panelRt != null)
            {
                width = Mathf.Clamp(panelRt.rect.width - 120f, 480f, 1100f);
            }

            contentRt.sizeDelta = new Vector2(width, 0f);
            contentRt.anchoredPosition = new Vector2(0f, 180f);
        }
        else
        {
            // Scroll viewport 안: 상단 스트레치
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 0f);
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
            if (stage == null)
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
        if (ServiceLocator.TryGet(out IMetaProgressService meta))
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
