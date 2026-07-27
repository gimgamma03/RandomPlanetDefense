#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Title.unity 패널/참조를 한 번에 깔아 둔다.
/// 메뉴: RPD / Title / Setup Title Flow
///       RPD / Title / Apply Sci-Fi Skin to Stage Select
/// </summary>
public static class TitleFlowSetup
{
    private const string TitleScenePath = "Assets/Scenes/Title.unity";
    private const string AtlasPath = "Assets/Externals/Sci-Fi UI/_SciFi_GUISkin_/atlas.png";

    [MenuItem("RPD/Title/Setup Title Flow")]
    public static void Setup()
    {
        var scene = EditorSceneManager.OpenScene(TitleScenePath);
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Title Flow", "Title 씬에 Canvas가 없습니다.", "OK");
            return;
        }

        SceneDirector director = canvas.GetComponent<SceneDirector>();
        if (director == null)
        {
            director = canvas.gameObject.AddComponent<SceneDirector>();
        }

        TitleFlow flow = canvas.GetComponent<TitleFlow>();
        if (flow == null)
        {
            flow = canvas.gameObject.AddComponent<TitleFlow>();
        }

        Transform canvasTransform = canvas.transform;

        FindOrCreateChild(canvasTransform, "PanelBackGround", stretch: true);

        GameObject panelTitle = FindOrCreateChild(canvasTransform, "PanelTitle", stretch: true);
        GameObject panelStage = FindOrCreateChild(canvasTransform, "PanelStageSelect", stretch: true);

        MoveIfExists(canvasTransform, "ButtonGameStart", panelTitle.transform);
        MoveIfExists(canvasTransform, "ButtonGameExit", panelTitle.transform);
        Transform bg = canvasTransform.Find("PanelBackGround");
        if (bg != null)
        {
            MoveIfExists(bg, "ButtonGameStart", panelTitle.transform);
            MoveIfExists(bg, "ButtonGameExit", panelTitle.transform);
        }

        Button startButton = FindButton(panelTitle.transform, "ButtonGameStart");
        Button exitButton = FindButton(panelTitle.transform, "ButtonGameExit");
        ClearPersistentCalls(startButton);
        ClearPersistentCalls(exitButton);

        StageSelectPanel stagePanel = panelStage.GetComponent<StageSelectPanel>();
        if (stagePanel == null)
        {
            stagePanel = panelStage.AddComponent<StageSelectPanel>();
        }

        GameObject titleLabel = FindOrCreateChild(panelStage.transform, "TextStageSelectTitle", stretch: false);
        RectTransform titleRect = titleLabel.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -80f);
        titleRect.sizeDelta = new Vector2(480f, 60f);
        TextMeshProUGUI titleTmp = titleLabel.GetComponent<TextMeshProUGUI>();
        if (titleTmp == null)
        {
            titleTmp = titleLabel.AddComponent<TextMeshProUGUI>();
        }

        titleTmp.text = "Select Stage";
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.fontSize = 42f;
        titleTmp.color = Color.white;

        GameObject content = FindOrCreateChild(panelStage.transform, "ContentStages", stretch: false);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0.5f, 0.5f);
        contentRect.anchorMax = new Vector2(0.5f, 0.5f);
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.anchoredPosition = new Vector2(0f, 20f);
        contentRect.sizeDelta = new Vector2(400f, 280f);

        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = content.AddComponent<VerticalLayoutGroup>();
        }

        layout.childAlignment = TextAnchor.UpperCenter;
        layout.spacing = 14f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.padding = new RectOffset(24, 24, 12, 12);

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = content.AddComponent<ContentSizeFitter>();
        }

        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject backObject = FindOrCreateChild(panelStage.transform, "ButtonBack", stretch: false);
        RectTransform backRect = backObject.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.5f, 0f);
        backRect.anchorMax = new Vector2(0.5f, 0f);
        backRect.pivot = new Vector2(0.5f, 0f);
        backRect.anchoredPosition = new Vector2(0f, 72f);
        backRect.sizeDelta = new Vector2(240f, 64f);

        Image backImage = EnsureImage(backObject, Color.white);
        Button backButton = backObject.GetComponent<Button>();
        if (backButton == null)
        {
            backButton = backObject.AddComponent<Button>();
        }

        backButton.targetGraphic = backImage;
        EnsureButtonLabel(backObject.transform, "Back");
        ClearPersistentCalls(backButton);
        UnityEventTools.AddVoidPersistentListener(backButton.onClick, flow.OnClickBackToTitle);

        SerializedObject flowSo = new SerializedObject(flow);
        flowSo.FindProperty("panelTitle").objectReferenceValue = panelTitle;
        flowSo.FindProperty("panelStageSelect").objectReferenceValue = panelStage;
        flowSo.FindProperty("sceneDirector").objectReferenceValue = director;
        flowSo.FindProperty("stageSelectPanel").objectReferenceValue = stagePanel;
        flowSo.FindProperty("startButton").objectReferenceValue = startButton;
        flowSo.FindProperty("exitButton").objectReferenceValue = exitButton;
        flowSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject stageSo = new SerializedObject(stagePanel);
        stageSo.FindProperty("titleFlow").objectReferenceValue = flow;
        stageSo.FindProperty("contentRoot").objectReferenceValue = content.transform;
        stageSo.FindProperty("stageButtonPrefab").objectReferenceValue = null;
        stageSo.FindProperty("backButton").objectReferenceValue = backButton;
        stageSo.ApplyModifiedPropertiesWithoutUndo();

        ApplySciFiSkinInternal(panelStage, stagePanel, backButton);

        panelTitle.SetActive(true);
        panelStage.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorUtility.DisplayDialog(
            "Title Flow",
            "Setup 완료.\nPlay 모드에서 Start → Stage01 → GameScene 흐름을 확인하세요.",
            "OK");
    }

    [MenuItem("RPD/Title/Apply Sci-Fi Skin to Stage Select")]
    public static void ApplySciFiSkinOnly()
    {
        var scene = EditorSceneManager.OpenScene(TitleScenePath);
        StageSelectPanel stagePanel = Object.FindFirstObjectByType<StageSelectPanel>(FindObjectsInactive.Include);
        if (stagePanel == null)
        {
            EditorUtility.DisplayDialog(
                "Sci-Fi Skin",
                "PanelStageSelect / StageSelectPanel이 없습니다.\n먼저 Setup Title Flow를 실행하세요.",
                "OK");
            return;
        }

        Button backButton = null;
        SerializedObject so = new SerializedObject(stagePanel);
        SerializedProperty backProp = so.FindProperty("backButton");
        if (backProp != null)
        {
            backButton = backProp.objectReferenceValue as Button;
        }

        ApplySciFiSkinInternal(stagePanel.gameObject, stagePanel, backButton);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorUtility.DisplayDialog(
            "Sci-Fi Skin",
            "스테이지 패널에 atlas window / button1 적용 완료.",
            "OK");
    }

    private static void ApplySciFiSkinInternal(
        GameObject panelStage,
        StageSelectPanel stagePanel,
        Button backButton)
    {
        if (!TryLoadAtlasSprites(out Sprite window, out Sprite button1, out Sprite buttonPushed))
        {
            Debug.LogError($"[TitleFlowSetup] atlas 로드 실패: {AtlasPath}");
            return;
        }

        // 패널 배경 = window (9-slice)
        Image panelImage = panelStage.GetComponent<Image>();
        if (panelImage == null)
        {
            panelImage = panelStage.AddComponent<Image>();
        }

        panelImage.sprite = window;
        panelImage.type = Image.Type.Sliced;
        panelImage.color = Color.white;
        panelImage.raycastTarget = true;

        // 풀스크린 대신 창 프레임처럼 안쪽 여백
        RectTransform panelRect = panelStage.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(720f, 560f);

        if (backButton != null)
        {
            ApplyButtonAtlas(backButton, button1, buttonPushed);
        }

        SerializedObject stageSo = new SerializedObject(stagePanel);
        stageSo.FindProperty("buttonSprite").objectReferenceValue = button1;
        stageSo.FindProperty("buttonPressedSprite").objectReferenceValue = buttonPushed;
        stageSo.FindProperty("buttonColor").colorValue = Color.white;
        stageSo.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(panelStage);
        EditorUtility.SetDirty(stagePanel);
    }

    private static bool TryLoadAtlasSprites(out Sprite window, out Sprite button1, out Sprite buttonPushed)
    {
        window = null;
        button1 = null;
        buttonPushed = null;

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(AtlasPath);
        if (assets == null || assets.Length == 0)
        {
            return false;
        }

        Sprite[] sprites = assets.OfType<Sprite>().ToArray();
        window = sprites.FirstOrDefault(s => s.name == "window");
        button1 = sprites.FirstOrDefault(s => s.name == "button1");
        buttonPushed = sprites.FirstOrDefault(s => s.name == "button_pushed");
        return window != null && button1 != null;
    }

    private static void ApplyButtonAtlas(Button button, Sprite normal, Sprite pressed)
    {
        Image image = button.targetGraphic as Image;
        if (image == null)
        {
            image = button.GetComponent<Image>();
        }

        if (image == null)
        {
            image = button.gameObject.AddComponent<Image>();
        }

        image.sprite = normal;
        image.type = Image.Type.Sliced;
        image.color = Color.white;
        button.targetGraphic = image;
        button.transition = Selectable.Transition.SpriteSwap;

        SpriteState state = button.spriteState;
        state.highlightedSprite = normal;
        state.pressedSprite = pressed != null ? pressed : normal;
        state.selectedSprite = normal;
        button.spriteState = state;
        EditorUtility.SetDirty(button);
    }

    private static void ClearPersistentCalls(Button button)
    {
        if (button == null)
        {
            return;
        }

        while (button.onClick.GetPersistentEventCount() > 0)
        {
            UnityEventTools.RemovePersistentListener(button.onClick, 0);
        }

        EditorUtility.SetDirty(button);
    }

    private static GameObject FindOrCreateChild(Transform parent, string name, bool stretch)
    {
        Transform existing = FindDeep(parent, name);
        if (existing != null && existing.parent == parent)
        {
            return existing.gameObject;
        }

        if (existing != null)
        {
            return existing.gameObject;
        }

        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        if (stretch)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        return go;
    }

    private static Image EnsureImage(GameObject go, Color color)
    {
        Image image = go.GetComponent<Image>();
        if (image == null)
        {
            image = go.AddComponent<Image>();
        }

        image.color = color;
        return image;
    }

    private static void EnsureButtonLabel(Transform button, string text)
    {
        TextMeshProUGUI tmp = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp == null)
        {
            GameObject label = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(button, false);
            RectTransform rect = label.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            tmp = label.GetComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 28f;
            tmp.color = Color.white;
        }

        tmp.text = text;
    }

    private static void MoveIfExists(Transform searchRoot, string childName, Transform newParent)
    {
        Transform child = FindDeep(searchRoot, childName);
        if (child == null || child.parent == newParent)
        {
            return;
        }

        child.SetParent(newParent, true);
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

    private static Button FindButton(Transform root, string name)
    {
        Transform t = FindDeep(root, name);
        return t != null ? t.GetComponent<Button>() : null;
    }
}
#endif
