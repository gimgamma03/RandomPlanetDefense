using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 클리어/게임오버 종료 화면 — 문구 고정 + Title 복귀 버튼.
/// Time.timeScale은 건드리지 않음(플레이 중 일시정지와 별개).
/// </summary>
public sealed class EndRunOverlay : MonoBehaviour
{
    [SerializeField]
    private GameObject root;
    [SerializeField]
    private TextMeshProUGUI messageText;
    [SerializeField]
    private Button titleButton;
    [SerializeField]
    private Image dimmer;

    [Header("Layout (선택 — 씬 RectTransform 우선)")]
    [Tooltip("클리어/게임오버 문구 RectTransform.anchoredPosition")]
    [SerializeField]
    private Vector2 messageAnchoredPosition = new Vector2(0f, 7f);
    [Tooltip("Title 버튼 RectTransform.anchoredPosition (문구 오브젝트 기준 상대)")]
    [SerializeField]
    private Vector2 titleButtonAnchoredPosition = new Vector2(0f, -180f);
    [SerializeField]
    private Vector2 titleButtonSize = new Vector2(320f, 72f);
    [Tooltip("켜면 Show 때 Layout 숫자로 Rect를 덮어씀. 씬에서 조절할 땐 끄기")]
    [SerializeField]
    private bool applyLayoutOnShow = false;

    private SceneDirector sceneDirector;
    private bool wired;

    public bool IsVisible => root != null && root.activeSelf;

    public void Show(string message)
    {
        EnsureBuilt();
        if (root == null || messageText == null)
        {
            Debug.LogWarning("[EndRunOverlay] UI 미구성.", this);
            return;
        }

        if (applyLayoutOnShow)
        {
            ApplyLayout();
        }

        messageText.text = message;
        Color c = messageText.color;
        messageText.color = new Color(c.r, c.g, c.b, 1f);
        messageText.raycastTarget = false;
        root.SetActive(true);
        if (dimmer != null)
        {
            dimmer.gameObject.SetActive(true);
        }

        if (titleButton != null)
        {
            titleButton.transform.SetAsLastSibling();
            titleButton.gameObject.SetActive(true);
            titleButton.interactable = true;
        }
    }

    /// <summary>인스펙터 Layout 값을 실제 RectTransform에 반영.</summary>
    public void ApplyLayout()
    {
        if (messageText != null)
        {
            RectTransform messageRt = messageText.rectTransform;
            // ShowWave GO에 TMP가 붙어 있으면 그 Rect를 문구 위치로 씀
            if (messageRt.gameObject == gameObject || root == messageText.gameObject)
            {
                messageRt.anchoredPosition = messageAnchoredPosition;
            }
            else if (root != null)
            {
                RectTransform rootRt = root.transform as RectTransform;
                if (rootRt != null)
                {
                    rootRt.anchoredPosition = messageAnchoredPosition;
                }
            }
        }
        else if (root != null)
        {
            RectTransform rootRt = root.transform as RectTransform;
            if (rootRt != null)
            {
                rootRt.anchoredPosition = messageAnchoredPosition;
            }
        }

        if (titleButton != null)
        {
            RectTransform buttonRt = titleButton.transform as RectTransform;
            if (buttonRt != null)
            {
                buttonRt.anchoredPosition = titleButtonAnchoredPosition;
                buttonRt.sizeDelta = titleButtonSize;
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (applyLayoutOnShow && IsVisible)
        {
            ApplyLayout();
        }
    }
#endif

    public void Hide()
    {
        if (root != null)
        {
            root.SetActive(false);
        }
    }

    private void Awake()
    {
        sceneDirector = FindFirstObjectByType<SceneDirector>();
        EnsureBuilt();
        WireTitleButton();
        if (root != null)
        {
            root.SetActive(false);
        }
    }

    private void WireTitleButton()
    {
        if (wired || titleButton == null)
        {
            return;
        }

        titleButton.onClick.RemoveListener(OnClickTitle);
        titleButton.onClick.AddListener(OnClickTitle);
        wired = true;
    }

    private void OnClickTitle()
    {
        // 안전을 위해 1로 복구 (일시정지 중이었다면)
        Time.timeScale = 1f;

        if (sceneDirector == null)
        {
            sceneDirector = FindFirstObjectByType<SceneDirector>();
        }

        if (sceneDirector != null)
        {
            sceneDirector.TitleScene();
            return;
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(SceneDirector.TitleSceneName);
    }

    /// <summary>씬에 없으면 ShowWave TMP를 활용해 런타임 구성.</summary>
    public void EnsureBuilt()
    {
        if (messageText == null)
        {
            TextFadeOut fade = GetComponent<TextFadeOut>();
            if (fade == null)
            {
                fade = GetComponentInChildren<TextFadeOut>(true);
            }

            if (fade != null && fade.ShowTextTarget != null)
            {
                messageText = fade.ShowTextTarget;
            }
            else
            {
                messageText = GetComponent<TextMeshProUGUI>();
                if (messageText == null)
                {
                    // ButtonTitle 라벨은 제외
                    TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
                    for (int i = 0; i < tmps.Length; i++)
                    {
                        if (tmps[i].transform.parent != null &&
                            tmps[i].transform.parent.name == "ButtonTitle")
                        {
                            continue;
                        }

                        messageText = tmps[i];
                        break;
                    }
                }
            }
        }

        if (root == null)
        {
            root = gameObject;
        }

        if (titleButton == null)
        {
            Transform existing = transform.Find("ButtonTitle");
            if (existing != null)
            {
                titleButton = existing.GetComponent<Button>();
            }
        }

        if (titleButton == null)
        {
            titleButton = CreateTitleButton(
                transform,
                messageText != null ? messageText.font : null,
                titleButtonAnchoredPosition,
                titleButtonSize);
            titleButton.gameObject.SetActive(false);
        }

        if (dimmer == null)
        {
            Transform dim = transform.Find("Dimmer");
            if (dim != null)
            {
                dimmer = dim.GetComponent<Image>();
            }
        }

        WireTitleButton();
    }

    private static Button CreateTitleButton(
        Transform parent,
        TMP_FontAsset font,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        GameObject go = new GameObject(
            "ButtonTitle",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.15f, 0.55f, 0.7f, 0.95f);

        GameObject labelGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(go.transform, false);
        RectTransform labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = labelGo.GetComponent<TextMeshProUGUI>();
        tmp.text = "Title";
        tmp.fontSize = 36f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        if (font != null)
        {
            tmp.font = font;
        }
#if UNITY_EDITOR
        else
        {
            TMP_FontAsset orbit = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/Fonts/Orbit-Regular SDF.asset");
            if (orbit != null)
            {
                tmp.font = orbit;
            }
        }
#endif
        if (tmp.font == null && TMP_Settings.defaultFontAsset != null)
        {
            tmp.font = TMP_Settings.defaultFontAsset;
        }

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        return button;
    }
}
