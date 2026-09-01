using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 첫 진입 조작 안내 — 가운데 커서 아이콘에서 각 버튼까지 꺾인 점선 + 화살촉.
/// 아무 키/클릭 또는 닫기 버튼으로 종료. 이후에는 '?' 버튼으로 다시 연다.
/// </summary>
public sealed class StartHintOverlay : MonoBehaviour
{
    public const string DefaultSeenKey = "RPD.GameHintSeen";

    [System.Serializable]
    public sealed class HintTarget
    {
        public RectTransform target;

        [Tooltip("점선 옆에 붙는 짧은 설명. 비우면 표시 안 함")]
        public string label;

        [Tooltip("설명 텍스트 위치 미세 조정")]
        public Vector2 labelOffset;
    }

    [Header("Refs")]
    [Tooltip("실제로 켜고 끄는 오버레이 루트 (비우면 런타임 생성)")]
    [SerializeField]
    private GameObject panel;
    [SerializeField]
    private Image dimmer;
    [SerializeField]
    private UICursorIcon cursorIcon;
    [SerializeField]
    private TextMeshProUGUI titleText;
    [SerializeField]
    private Button closeButton;
    [SerializeField]
    private TMP_FontAsset font;

    [SerializeField]
    private List<HintTarget> targets = new List<HintTarget>();

    [Header("Style")]
    [SerializeField]
    private Color lineColor = new Color(0.68f, 1f, 0.28f, 1f);
    [SerializeField]
    private Color dimColor = new Color(0f, 0f, 0f, 0.62f);
    [SerializeField]
    private float dashLength = 14f;
    [SerializeField]
    private float gapLength = 10f;
    [SerializeField]
    private float lineThickness = 4f;
    [SerializeField]
    private float arrowLength = 26f;
    [SerializeField]
    private float arrowWidth = 22f;
    [Tooltip("커서 아이콘에서 점선이 시작하는 거리")]
    [SerializeField]
    private float cursorClearance = 64f;
    [Tooltip("화살촉과 버튼 사이 여백")]
    [SerializeField]
    private float buttonClearance = 14f;
    [SerializeField]
    private float labelFontSize = 24f;

    [Header("Behaviour")]
    [SerializeField]
    private bool showOnStart = true;
    [Tooltip("켜면 한 번 본 뒤에는 자동으로 뜨지 않음 (브라우저/PC에 기억)")]
    [SerializeField]
    private bool showOnlyOnce = true;
    [SerializeField]
    private string seenKey = DefaultSeenKey;

    private RectTransform panelRect;
    private RectTransform arrowRoot;
    private readonly List<GameObject> generated = new List<GameObject>();
    private bool ignoreInput;

    public bool IsVisible => panel != null && panel.activeSelf;

    private void Awake()
    {
        EnsureBuilt();
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private void Start()
    {
        if (!showOnStart)
        {
            return;
        }

        if (showOnlyOnce && PlayerPrefs.GetInt(seenKey, 0) != 0)
        {
            return;
        }

        Show();
    }

    private void Update()
    {
        if (!IsVisible || ignoreInput)
        {
            return;
        }

        if (Input.anyKeyDown || PointerInput.WasPrimaryPressThisFrame())
        {
            Hide();
        }
    }

    public void Show()
    {
        EnsureBuilt();
        if (panel == null)
        {
            return;
        }

        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        ignoreInput = true;
        StartCoroutine(BuildAfterLayout());
    }

    public void Hide()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }

        PlayerPrefs.SetInt(seenKey, 1);
        PlayerPrefs.Save();
    }

    /// <summary>최초 안내 표시 기록을 지운다 (에디터 테스트용).</summary>
    public void ResetSeenFlag()
    {
        PlayerPrefs.DeleteKey(seenKey);
        PlayerPrefs.Save();
    }

    private IEnumerator BuildAfterLayout()
    {
        // 레이아웃이 잡힌 뒤에 버튼 위치를 읽어야 한다
        yield return null;
        Canvas.ForceUpdateCanvases();
        BuildArrows();
        ignoreInput = false;
    }

    private void BuildArrows()
    {
        ClearGenerated();

        if (panelRect == null || arrowRoot == null)
        {
            return;
        }

        Vector2 cursorCenter = cursorIcon != null
            ? cursorIcon.rectTransform.anchoredPosition
            : Vector2.zero;

        for (int i = 0; i < targets.Count; i++)
        {
            HintTarget hint = targets[i];
            if (hint == null || hint.target == null || !hint.target.gameObject.activeInHierarchy)
            {
                continue;
            }

            Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(panelRect, hint.target);
            Vector2 center = bounds.center;
            bool onRight = center.x >= cursorCenter.x;

            float tipX = onRight
                ? center.x - bounds.extents.x - buttonClearance
                : center.x + bounds.extents.x + buttonClearance;

            Vector2 tip = new Vector2(tipX, center.y);
            Vector2 start = cursorCenter + new Vector2(onRight ? cursorClearance : -cursorClearance, 0f);

            List<Vector2> path = new List<Vector2>(4);
            path.Add(start);

            Vector2 elbow;
            if (Mathf.Abs(tip.y - start.y) < 12f)
            {
                elbow = Vector2.Lerp(start, tip, 0.5f);
            }
            else
            {
                float midX = Mathf.Lerp(start.x, tip.x, 0.5f);
                path.Add(new Vector2(midX, start.y));
                path.Add(new Vector2(midX, tip.y));
                elbow = new Vector2(midX, Mathf.Lerp(start.y, tip.y, 0.5f));
            }

            path.Add(tip);
            CreateArrow(path, i);

            if (!string.IsNullOrEmpty(hint.label))
            {
                CreateLabel(hint.label, elbow + hint.labelOffset, onRight, i);
            }
        }
    }

    private void CreateArrow(List<Vector2> path, int index)
    {
        GameObject go = new GameObject($"HintArrow_{index}", typeof(RectTransform));
        go.transform.SetParent(arrowRoot, false);
        StretchFull(go.GetComponent<RectTransform>());

        UIDashedArrow arrow = go.AddComponent<UIDashedArrow>();
        arrow.color = lineColor;
        arrow.raycastTarget = false;
        arrow.SetStyle(dashLength, gapLength, lineThickness, arrowLength, arrowWidth);
        arrow.SetPoints(path);

        generated.Add(go);
    }

    private void CreateLabel(string text, Vector2 position, bool onRight, int index)
    {
        GameObject go = new GameObject($"HintLabel_{index}", typeof(RectTransform));
        go.transform.SetParent(arrowRoot, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(onRight ? 1f : 0f, 0.5f);
        rt.sizeDelta = new Vector2(300f, 40f);
        rt.anchoredPosition = position + new Vector2(onRight ? -12f : 12f, 0f);

        TextMeshProUGUI label = go.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = labelFontSize;
        label.color = lineColor;
        label.alignment = onRight ? TextAlignmentOptions.Right : TextAlignmentOptions.Left;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        if (font != null)
        {
            label.font = font;
        }

        generated.Add(go);
    }

    private void ClearGenerated()
    {
        for (int i = 0; i < generated.Count; i++)
        {
            if (generated[i] != null)
            {
                Destroy(generated[i]);
            }
        }

        generated.Clear();
    }

    private void EnsureBuilt()
    {
        if (panel == null)
        {
            GameObject go = new GameObject("HintPanel", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            StretchFull(go.GetComponent<RectTransform>());
            panel = go;
        }

        panelRect = panel.transform as RectTransform;

        if (dimmer == null)
        {
            dimmer = CreateChildImage("Dimmer", dimColor, raycast: true);
        }

        if (cursorIcon == null)
        {
            GameObject go = new GameObject("CursorIcon", typeof(RectTransform));
            go.transform.SetParent(panel.transform, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(96f, 96f);
            rt.anchoredPosition = Vector2.zero;

            cursorIcon = go.AddComponent<UICursorIcon>();
            cursorIcon.color = lineColor;
            cursorIcon.raycastTarget = false;
        }

        if (arrowRoot == null)
        {
            Transform existing = panel.transform.Find("Arrows");
            if (existing != null)
            {
                arrowRoot = existing as RectTransform;
            }
            else
            {
                GameObject go = new GameObject("Arrows", typeof(RectTransform));
                go.transform.SetParent(panel.transform, false);
                arrowRoot = go.GetComponent<RectTransform>();
                StretchFull(arrowRoot);
            }
        }

        if (font == null && titleText != null)
        {
            font = titleText.font;
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Hide);
            closeButton.onClick.AddListener(Hide);
        }

        // 커서 아이콘과 화살표가 딤 위에 오도록
        if (dimmer != null)
        {
            dimmer.transform.SetAsFirstSibling();
        }
    }

    private Image CreateChildImage(string childName, Color imageColor, bool raycast)
    {
        GameObject go = new GameObject(childName, typeof(RectTransform));
        go.transform.SetParent(panel.transform, false);
        StretchFull(go.GetComponent<RectTransform>());

        Image image = go.AddComponent<Image>();
        image.color = imageColor;
        image.raycastTarget = raycast;
        return image;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
