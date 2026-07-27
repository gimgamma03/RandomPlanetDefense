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
///       RPD / Title / Setup Tower Upgrade Panel
///       RPD / Title / Apply Sci-Fi Skin to Stage Select
/// </summary>
public static class TitleFlowSetup
{
    private const string TitleScenePath = "Assets/Scenes/Title.unity";
    private const string AtlasPath = "Assets/Externals/Sci-Fi UI/_SciFi_GUISkin_/atlas.png";
    private const string OrbitFontPath = "Assets/Fonts/Orbit-Regular SDF.asset";

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

        Button startButton = FindButton(panelTitle.transform, "ButtonGameStart")
            ?? FindButton(panelTitle.transform, "ButtonStage");
        Button exitButton = FindButton(panelTitle.transform, "ButtonGameExit");
        // Start는 Awake에서 Wire. 영구 리스너도 맞춰 둔다.
        if (startButton != null)
        {
            ClearPersistentCalls(startButton);
            UnityEventTools.AddVoidPersistentListener(startButton.onClick, flow.OnClickStart);
        }

        if (exitButton != null)
        {
            ClearPersistentCalls(exitButton);
            UnityEventTools.AddVoidPersistentListener(exitButton.onClick, flow.OnClickExit);
        }

        StageSelectPanel stagePanel = panelStage.GetComponent<StageSelectPanel>();
        if (stagePanel == null)
        {
            stagePanel = panelStage.AddComponent<StageSelectPanel>();
        }

        TMP_FontAsset orbitFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OrbitFontPath);

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
        ApplyOrbitFont(titleTmp, orbitFont);

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
        EnsureButtonLabel(backObject.transform, "Back", orbitFont);
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
        if (orbitFont != null)
        {
            stageSo.FindProperty("orbitFont").objectReferenceValue = orbitFont;
        }

        stageSo.ApplyModifiedPropertiesWithoutUndo();

        ApplySciFiSkinInternal(panelStage, stagePanel, backButton);

        SetupTowerUpgradeUi(canvas, flow, panelTitle, director);

        panelTitle.SetActive(true);
        panelStage.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorUtility.DisplayDialog(
            "Title Flow",
            "Setup 완료.\nStart=스테이지 / Upgrade=영구강화 / Exit\nPlay로 확인하세요.",
            "OK");
    }

    [MenuItem("RPD/Title/Setup Tower Upgrade Panel")]
    public static void SetupTowerUpgradeOnly()
    {
        var scene = EditorSceneManager.OpenScene(TitleScenePath);
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        TitleFlow flow = canvas != null ? canvas.GetComponent<TitleFlow>() : null;
        if (canvas == null || flow == null)
        {
            EditorUtility.DisplayDialog("Tower Upgrade", "먼저 Setup Title Flow를 실행하세요.", "OK");
            return;
        }

        GameObject panelTitle = FindOrCreateChild(canvas.transform, "PanelTitle", stretch: true);
        SceneDirector director = canvas.GetComponent<SceneDirector>();
        SetupTowerUpgradeUi(canvas, flow, panelTitle, director);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorUtility.DisplayDialog(
            "Tower Upgrade",
            "Upgrade 패널 재배치 완료.\nHeader / Scroll / Back 고정 + Orbit TMP.\nPlay로 확인하세요.",
            "OK");
    }

    private static void SetupTowerUpgradeUi(
        Canvas canvas,
        TitleFlow flow,
        GameObject panelTitle,
        SceneDirector director)
    {
        Transform canvasTransform = canvas.transform;
        const string CrystalIconPath = "Assets/Images/UI/CrystalIcon.png";
        EnsureSpriteImporter(CrystalIconPath);
        Sprite crystalSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CrystalIconPath);
        if (crystalSprite == null)
        {
            Debug.LogWarning($"[TitleFlowSetup] Crystal icon Sprite 없음: {CrystalIconPath}");
        }

        TryLoadAtlasSprites(out Sprite window, out Sprite button1, out Sprite buttonPushed);

        TMP_FontAsset orbitFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OrbitFontPath);
        if (orbitFont == null)
        {
            Debug.LogWarning($"[TitleFlowSetup] Orbit font 없음: {OrbitFontPath}");
        }

        // Title: Upgrade 버튼
        GameObject upgradeBtnGo = FindOrCreateChild(panelTitle.transform, "ButtonTowerUpgrade", stretch: false);
        RectTransform upgradeRect = upgradeBtnGo.GetComponent<RectTransform>();
        upgradeRect.anchorMin = new Vector2(0.5f, 0.5f);
        upgradeRect.anchorMax = new Vector2(0.5f, 0.5f);
        upgradeRect.pivot = new Vector2(0.5f, 0.5f);
        upgradeRect.anchoredPosition = new Vector2(0f, -20f);
        upgradeRect.sizeDelta = new Vector2(280f, 64f);
        Image upgradeImage = EnsureImage(upgradeBtnGo, Color.white);
        Button upgradeButton = upgradeBtnGo.GetComponent<Button>();
        if (upgradeButton == null)
        {
            upgradeButton = upgradeBtnGo.AddComponent<Button>();
        }

        upgradeButton.targetGraphic = upgradeImage;
        EnsureButtonLabel(upgradeBtnGo.transform, "Upgrade", orbitFont);
        ClearPersistentCalls(upgradeButton);
        UnityEventTools.AddVoidPersistentListener(upgradeButton.onClick, flow.OnClickUpgrade);
        if (button1 != null)
        {
            ApplyButtonAtlas(upgradeButton, button1, buttonPushed);
        }

        // Start 라벨을 Stages로 (있으면)
        Button startButton = FindButton(panelTitle.transform, "ButtonGameStart")
            ?? FindButton(panelTitle.transform, "ButtonStage");
        if (startButton != null)
        {
            EnsureButtonLabel(startButton.transform, "Stages", orbitFont);
            // 아래로 살짝
            RectTransform startRect = startButton.GetComponent<RectTransform>();
            if (startRect != null)
            {
                startRect.anchoredPosition = new Vector2(startRect.anchoredPosition.x, 60f);
            }
        }

        Button exitButton = FindButton(panelTitle.transform, "ButtonGameExit");
        if (exitButton != null)
        {
            EnsureButtonLabel(exitButton.transform, "Exit", orbitFont);
            RectTransform exitRect = exitButton.GetComponent<RectTransform>();
            if (exitRect != null)
            {
                exitRect.anchoredPosition = new Vector2(exitRect.anchoredPosition.x, -100f);
            }
        }

        // Crystal HUD (타이틀 상단)
        GameObject crystalHud = FindOrCreateChild(panelTitle.transform, "CrystalHud", stretch: false);
        RectTransform crystalHudRect = crystalHud.GetComponent<RectTransform>();
        crystalHudRect.anchorMin = new Vector2(1f, 1f);
        crystalHudRect.anchorMax = new Vector2(1f, 1f);
        crystalHudRect.pivot = new Vector2(1f, 1f);
        crystalHudRect.anchoredPosition = new Vector2(-24f, -24f);
        crystalHudRect.sizeDelta = new Vector2(160f, 48f);
        HorizontalLayoutGroup hudLayout = crystalHud.GetComponent<HorizontalLayoutGroup>();
        if (hudLayout == null)
        {
            hudLayout = crystalHud.AddComponent<HorizontalLayoutGroup>();
        }

        hudLayout.childAlignment = TextAnchor.MiddleRight;
        hudLayout.spacing = 8f;
        hudLayout.childControlWidth = false;
        hudLayout.childControlHeight = true;
        hudLayout.childForceExpandWidth = false;

        GameObject iconGo = FindOrCreateChild(crystalHud.transform, "Icon", stretch: false);
        RectTransform iconRect = iconGo.GetComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(40f, 40f);
        Image iconImage = EnsureImage(iconGo, Color.white);
        if (crystalSprite != null)
        {
            iconImage.sprite = crystalSprite;
            iconImage.preserveAspect = true;
        }

        LayoutElement iconLe = iconGo.GetComponent<LayoutElement>();
        if (iconLe == null)
        {
            iconLe = iconGo.AddComponent<LayoutElement>();
        }

        iconLe.preferredWidth = 40f;
        iconLe.preferredHeight = 40f;

        GameObject crystalTextGo = FindOrCreateChild(crystalHud.transform, "TextCrystals", stretch: false);
        TextMeshProUGUI crystalTmp = crystalTextGo.GetComponent<TextMeshProUGUI>();
        if (crystalTmp == null)
        {
            crystalTmp = crystalTextGo.AddComponent<TextMeshProUGUI>();
        }

        crystalTmp.text = "0";
        crystalTmp.fontSize = 28f;
        crystalTmp.alignment = TextAlignmentOptions.MidlineRight;
        crystalTmp.color = Color.white;
        ApplyOrbitFont(crystalTmp, orbitFont);
        RectTransform crystalTextRect = crystalTextGo.GetComponent<RectTransform>();
        crystalTextRect.sizeDelta = new Vector2(100f, 40f);

        // PanelTowerUpgrade — Header / Scroll / Footer 고정
        GameObject panelUpgrade = FindOrCreateChild(canvasTransform, "PanelTowerUpgrade", stretch: true);
        TowerUpgradePanel upgradePanel = panelUpgrade.GetComponent<TowerUpgradePanel>();
        if (upgradePanel == null)
        {
            upgradePanel = panelUpgrade.AddComponent<TowerUpgradePanel>();
        }

        Image panelImage = panelUpgrade.GetComponent<Image>();
        if (panelImage == null)
        {
            panelImage = panelUpgrade.AddComponent<Image>();
        }

        if (window != null)
        {
            panelImage.sprite = window;
            panelImage.type = Image.Type.Sliced;
            panelImage.color = Color.white;
        }
        else
        {
            panelImage.color = new Color(0.05f, 0.07f, 0.12f, 0.96f);
        }

        RectTransform panelRect = panelUpgrade.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(920f, 760f);

        DestroyChildIfExists(panelUpgrade.transform, "ScrollUpgrades");
        DestroyChildIfExists(panelUpgrade.transform, "ContentUpgrades");

        GameObject upgradeTitle = FindOrCreateChild(panelUpgrade.transform, "TextUpgradeTitle", stretch: false);
        RectTransform upgradeTitleRect = upgradeTitle.GetComponent<RectTransform>();
        upgradeTitleRect.anchorMin = new Vector2(0f, 1f);
        upgradeTitleRect.anchorMax = new Vector2(1f, 1f);
        upgradeTitleRect.pivot = new Vector2(0.5f, 1f);
        upgradeTitleRect.anchoredPosition = new Vector2(0f, -32f);
        upgradeTitleRect.sizeDelta = new Vector2(-48f, 52f);
        TextMeshProUGUI upgradeTitleTmp = upgradeTitle.GetComponent<TextMeshProUGUI>();
        if (upgradeTitleTmp == null)
        {
            upgradeTitleTmp = upgradeTitle.AddComponent<TextMeshProUGUI>();
        }

        upgradeTitleTmp.text = "Tower Upgrade";
        upgradeTitleTmp.alignment = TextAlignmentOptions.Center;
        upgradeTitleTmp.fontSize = 44f;
        upgradeTitleTmp.color = Color.white;
        upgradeTitleTmp.raycastTarget = false;
        ApplyOrbitFont(upgradeTitleTmp, orbitFont);

        GameObject panelCrystal = FindOrCreateChild(panelUpgrade.transform, "CrystalRow", stretch: false);
        RectTransform panelCrystalRect = panelCrystal.GetComponent<RectTransform>();
        panelCrystalRect.anchorMin = new Vector2(0.5f, 1f);
        panelCrystalRect.anchorMax = new Vector2(0.5f, 1f);
        panelCrystalRect.pivot = new Vector2(0.5f, 1f);
        panelCrystalRect.anchoredPosition = new Vector2(0f, -90f);
        panelCrystalRect.sizeDelta = new Vector2(260f, 48f);
        HorizontalLayoutGroup pcLayout = panelCrystal.GetComponent<HorizontalLayoutGroup>();
        if (pcLayout == null)
        {
            pcLayout = panelCrystal.AddComponent<HorizontalLayoutGroup>();
        }

        pcLayout.childAlignment = TextAnchor.MiddleCenter;
        pcLayout.spacing = 8f;
        pcLayout.childControlWidth = false;
        pcLayout.childControlHeight = true;
        pcLayout.childForceExpandWidth = false;

        GameObject panelCrystalIcon = FindOrCreateChild(panelCrystal.transform, "Icon", stretch: false);
        Image pci = EnsureImage(panelCrystalIcon, Color.white);
        if (crystalSprite != null)
        {
            pci.sprite = crystalSprite;
            pci.preserveAspect = true;
        }

        panelCrystalIcon.GetComponent<RectTransform>().sizeDelta = new Vector2(36f, 36f);
        LayoutElement pciLe = panelCrystalIcon.GetComponent<LayoutElement>();
        if (pciLe == null)
        {
            pciLe = panelCrystalIcon.AddComponent<LayoutElement>();
        }

        pciLe.preferredWidth = 36f;
        pciLe.preferredHeight = 36f;

        GameObject panelCrystalText = FindOrCreateChild(panelCrystal.transform, "Text", stretch: false);
        TextMeshProUGUI pct = panelCrystalText.GetComponent<TextMeshProUGUI>();
        if (pct == null)
        {
            pct = panelCrystalText.AddComponent<TextMeshProUGUI>();
        }

        pct.text = "0";
        pct.fontSize = 32f;
        pct.alignment = TextAlignmentOptions.MidlineLeft;
        pct.color = Color.white;
        pct.raycastTarget = false;
        ApplyOrbitFont(pct, orbitFont);
        panelCrystalText.GetComponent<RectTransform>().sizeDelta = new Vector2(120f, 36f);

        // Scroll: 헤더(상단 ~120) / Back(하단 ~96) 사이만
        GameObject scrollGo = FindOrCreateChild(panelUpgrade.transform, "ScrollUpgrades", stretch: false);
        RectTransform scrollRectTransform = scrollGo.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = Vector2.zero;
        scrollRectTransform.anchorMax = Vector2.one;
        scrollRectTransform.offsetMin = new Vector2(40f, 108f);
        scrollRectTransform.offsetMax = new Vector2(-40f, -140f);

        ScrollRect scroll = scrollGo.GetComponent<ScrollRect>();
        if (scroll == null)
        {
            scroll = scrollGo.AddComponent<ScrollRect>();
        }

        Image scrollBg = scrollGo.GetComponent<Image>();
        if (scrollBg == null)
        {
            scrollBg = scrollGo.AddComponent<Image>();
        }

        scrollBg.color = new Color(0f, 0f, 0f, 0.25f);
        scrollBg.raycastTarget = true;

        GameObject viewport = FindOrCreateChild(scrollGo.transform, "Viewport", stretch: true);
        Image viewportImage = viewport.GetComponent<Image>();
        if (viewportImage == null)
        {
            viewportImage = viewport.AddComponent<Image>();
        }

        viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
        viewportImage.raycastTarget = true;
        Mask viewportMask = viewport.GetComponent<Mask>();
        if (viewportMask == null)
        {
            viewportMask = viewport.AddComponent<Mask>();
        }

        viewportMask.showMaskGraphic = false;

        GameObject content = FindOrCreateChild(viewport.transform, "ContentUpgrades", stretch: false);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = content.AddComponent<VerticalLayoutGroup>();
        }

        layout.childAlignment = TextAnchor.UpperCenter;
        layout.spacing = 10f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.padding = new RectOffset(12, 12, 8, 8);

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = content.AddComponent<ContentSizeFitter>();
        }

        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = contentRect;
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;

        GameObject backObject = FindOrCreateChild(panelUpgrade.transform, "ButtonBack", stretch: false);
        RectTransform backRect = backObject.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0.5f, 0f);
        backRect.anchorMax = new Vector2(0.5f, 0f);
        backRect.pivot = new Vector2(0.5f, 0f);
        backRect.anchoredPosition = new Vector2(0f, 32f);
        backRect.sizeDelta = new Vector2(280f, 64f);
        Image backImage = EnsureImage(backObject, Color.white);
        Button backButton = backObject.GetComponent<Button>();
        if (backButton == null)
        {
            backButton = backObject.AddComponent<Button>();
        }

        backButton.targetGraphic = backImage;
        EnsureButtonLabel(backObject.transform, "Back", orbitFont);
        ClearPersistentCalls(backButton);
        UnityEventTools.AddVoidPersistentListener(backButton.onClick, flow.OnClickBackToTitle);
        if (button1 != null)
        {
            ApplyButtonAtlas(backButton, button1, buttonPushed);
        }

        upgradeTitle.transform.SetAsFirstSibling();
        panelCrystal.transform.SetSiblingIndex(1);
        scrollGo.transform.SetSiblingIndex(2);
        backObject.transform.SetAsLastSibling();

        SerializedObject upgradeSo = new SerializedObject(upgradePanel);
        upgradeSo.FindProperty("titleFlow").objectReferenceValue = flow;
        upgradeSo.FindProperty("contentRoot").objectReferenceValue = content.transform;
        upgradeSo.FindProperty("backButton").objectReferenceValue = backButton;
        upgradeSo.FindProperty("crystalText").objectReferenceValue = pct;
        upgradeSo.FindProperty("crystalIcon").objectReferenceValue = pci;
        upgradeSo.FindProperty("titleText").objectReferenceValue = upgradeTitleTmp;
        upgradeSo.FindProperty("orbitFont").objectReferenceValue = orbitFont;
        if (button1 != null)
        {
            upgradeSo.FindProperty("buttonSprite").objectReferenceValue = button1;
            upgradeSo.FindProperty("buttonPressedSprite").objectReferenceValue = buttonPushed;
        }

        upgradeSo.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject flowSo = new SerializedObject(flow);
        flowSo.FindProperty("panelTitle").objectReferenceValue = panelTitle;
        flowSo.FindProperty("panelStageSelect").objectReferenceValue =
            FindOrCreateChild(canvasTransform, "PanelStageSelect", stretch: true);
        flowSo.FindProperty("panelTowerUpgrade").objectReferenceValue = panelUpgrade;
        flowSo.FindProperty("sceneDirector").objectReferenceValue = director;
        flowSo.FindProperty("towerUpgradePanel").objectReferenceValue = upgradePanel;
        flowSo.FindProperty("upgradeButton").objectReferenceValue = upgradeButton;
        flowSo.FindProperty("startButton").objectReferenceValue = startButton;
        flowSo.FindProperty("exitButton").objectReferenceValue = exitButton;
        flowSo.FindProperty("crystalHudText").objectReferenceValue = crystalTmp;
        flowSo.FindProperty("crystalHudIcon").objectReferenceValue = iconImage;
        flowSo.ApplyModifiedPropertiesWithoutUndo();

        panelUpgrade.SetActive(false);
        EditorUtility.SetDirty(flow);
        EditorUtility.SetDirty(upgradePanel);
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

    private static void EnsureSpriteImporter(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        bool dirty = false;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            dirty = true;
        }

        if (importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.spriteImportMode = SpriteImportMode.Single;
            dirty = true;
        }

        if (dirty)
        {
            importer.SaveAndReimport();
        }
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
        TMP_FontAsset orbitFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OrbitFontPath);
        if (orbitFont != null)
        {
            stageSo.FindProperty("orbitFont").objectReferenceValue = orbitFont;
        }

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

    private static void DestroyChildIfExists(Transform parent, string childName)
    {
        Transform child = parent.Find(childName);
        if (child == null)
        {
            child = FindDeep(parent, childName);
            if (child == null || child.parent != parent)
            {
                return;
            }
        }

        Object.DestroyImmediate(child.gameObject);
    }

    private static void ApplyOrbitFont(TextMeshProUGUI tmp, TMP_FontAsset font)
    {
        if (tmp == null || font == null)
        {
            return;
        }

        tmp.font = font;
    }

    private static void EnsureButtonLabel(Transform button, string text, TMP_FontAsset font = null)
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
        ApplyOrbitFont(tmp, font);
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
