using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Resources/Stages 목록을 버튼으로 만들고, 선택 시 TitleFlow로 넘긴다.
/// Sci-Fi atlas 스프라이트(window / button1)를 쓰면 타이틀 UI와 톤이 맞는다.
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

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(OnBack);
            backButton.onClick.AddListener(OnBack);
            ApplyButtonSkin(backButton);
        }
    }

    public void Refresh()
    {
        catalog = StageCatalog.LoadFromResources();
        RebuildButtons();
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

        RectTransform rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(560f, 96f);

        LayoutElement layout = root.GetComponent<LayoutElement>();
        layout.minHeight = 96f;
        layout.preferredHeight = 96f;
        layout.minWidth = 560f;
        layout.preferredWidth = 560f;

        Image image = root.GetComponent<Image>();
        image.color = buttonColor;
        if (buttonSprite != null)
        {
            image.sprite = buttonSprite;
            image.type = Image.Type.Sliced;
        }
        else
        {
            image.color = new Color(0.15f, 0.18f, 0.25f, 0.92f);
        }

        GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(root.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(16f, 8f);
        textRect.offsetMax = new Vector2(-16f, -8f);

        TextMeshProUGUI tmp = textObject.GetComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 36f;
        tmp.color = Color.white;
        tmp.text = "Stage";
        if (orbitFont != null)
        {
            tmp.font = orbitFont;
        }

        Button button = root.GetComponent<Button>();
        button.targetGraphic = image;
        return button;
    }
}
