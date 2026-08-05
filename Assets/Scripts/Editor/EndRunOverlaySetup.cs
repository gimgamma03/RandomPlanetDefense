#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// GameScene에 종료 UI(EndRunBanner + Title 버튼)를 씬에 미리 깔아 둔다.
/// 메뉴: RPD / Game / Setup End Run Overlay
/// </summary>
public static class EndRunOverlaySetup
{
    private const string GameScenePath = "Assets/Scenes/GameScene.unity";
    private const string OrbitFontPath = "Assets/Fonts/Orbit-Regular SDF.asset";
    private const string BannerName = "EndRunBanner";
    private const string LegacyBannerName = "ShowWave";

    [MenuItem("RPD/Game/Setup End Run Overlay")]
    public static void Setup()
    {
        var scene = EditorSceneManager.OpenScene(GameScenePath);

        GameObject banner = FindBanner();
        if (banner == null)
        {
            EditorUtility.DisplayDialog(
                "End Run Overlay",
                "GameScene에 EndRunBanner(또는 ShowWave)가 없습니다.",
                "OK");
            return;
        }

        if (banner.name == LegacyBannerName)
        {
            banner.name = BannerName;
        }

        TextFadeOut fade = banner.GetComponent<TextFadeOut>();
        if (fade == null)
        {
            fade = banner.AddComponent<TextFadeOut>();
        }

        TextMeshProUGUI messageTmp = banner.GetComponent<TextMeshProUGUI>();
        if (messageTmp == null)
        {
            EditorUtility.DisplayDialog("End Run Overlay", "EndRunBanner에 TextMeshProUGUI가 없습니다.", "OK");
            return;
        }

        SerializedObject fadeSo = new SerializedObject(fade);
        SerializedProperty showTextProp = fadeSo.FindProperty("showText");
        if (showTextProp != null && showTextProp.objectReferenceValue == null)
        {
            showTextProp.objectReferenceValue = messageTmp;
            fadeSo.ApplyModifiedPropertiesWithoutUndo();
        }

        EndRunOverlay overlay = banner.GetComponent<EndRunOverlay>();
        if (overlay == null)
        {
            overlay = banner.AddComponent<EndRunOverlay>();
        }

        Button titleButton = EnsureTitleButton(banner.transform, messageTmp.font);

        SerializedObject overlaySo = new SerializedObject(overlay);
        overlaySo.FindProperty("root").objectReferenceValue = banner;
        overlaySo.FindProperty("messageText").objectReferenceValue = messageTmp;
        overlaySo.FindProperty("titleButton").objectReferenceValue = titleButton;
        SerializedProperty applyLayout = overlaySo.FindProperty("applyLayoutOnShow");
        if (applyLayout != null)
        {
            applyLayout.boolValue = false;
        }

        overlaySo.ApplyModifiedPropertiesWithoutUndo();

        WaveSystem wave = Object.FindFirstObjectByType<WaveSystem>();
        if (wave != null)
        {
            SerializedObject waveSo = new SerializedObject(wave);
            waveSo.FindProperty("endRunOverlay").objectReferenceValue = overlay;
            waveSo.FindProperty("textFadeOut").objectReferenceValue = fade;
            waveSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(wave);
        }

        banner.SetActive(true);
        Color c = messageTmp.color;
        messageTmp.color = new Color(c.r, c.g, c.b, 1f);
        messageTmp.text = "All Waves Clear";
        messageTmp.raycastTarget = false;
        titleButton.gameObject.SetActive(true);

        EditorUtility.SetDirty(banner);
        EditorUtility.SetDirty(overlay);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Selection.activeGameObject = banner;
        EditorGUIUtility.PingObject(banner);

        Debug.Log("[EndRunOverlaySetup] EndRunBanner + ButtonTitle 씬에 배치 완료. Scene 뷰에서 위치 조절하세요.");

        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog(
                "End Run Overlay",
                "Setup 완료.\n\n" +
                "EndRunBanner / ButtonTitle 을 Scene 뷰에서 드래그해서 위치 맞추세요.\n" +
                "맞춘 뒤 EndRunBanner 체크를 끄면 됩니다 (Play 시작 시에도 자동으로 꺼짐).",
                "OK");
        }
    }

    private static GameObject FindBanner()
    {
        GameObject banner = GameObject.Find(BannerName);
        if (banner == null)
        {
            banner = GameObject.Find(LegacyBannerName);
        }

        if (banner != null)
        {
            return banner;
        }

        Transform[] all = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null || !all[i].gameObject.scene.IsValid())
            {
                continue;
            }

            if (all[i].name == BannerName || all[i].name == LegacyBannerName)
            {
                return all[i].gameObject;
            }
        }

        return null;
    }

    /// <summary>배치모드용.</summary>
    public static void SetupBatch()
    {
        Setup();
        EditorApplication.Exit(0);
    }

    private static Button EnsureTitleButton(Transform parent, TMP_FontAsset font)
    {
        Transform existing = parent.Find("ButtonTitle");
        GameObject go;
        if (existing != null)
        {
            go = existing.gameObject;
        }
        else
        {
            go = new GameObject("ButtonTitle", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(go, "Create ButtonTitle");
        }

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(320f, 72f);
        rt.anchoredPosition = new Vector2(0f, -180f);

        Image img = go.GetComponent<Image>();
        img.color = new Color(0.15f, 0.55f, 0.7f, 0.95f);

        Button button = go.GetComponent<Button>();
        button.targetGraphic = img;

        Transform labelT = go.transform.Find("Text");
        TextMeshProUGUI label;
        if (labelT == null)
        {
            GameObject labelGo = new GameObject("Text", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            label = labelGo.AddComponent<TextMeshProUGUI>();
        }
        else
        {
            label = labelT.GetComponent<TextMeshProUGUI>();
            if (label == null)
            {
                label = labelT.gameObject.AddComponent<TextMeshProUGUI>();
            }
        }

        RectTransform labelRt = label.rectTransform;
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;
        label.text = "Title";
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 36f;
        label.raycastTarget = false;
        if (font != null)
        {
            label.font = font;
        }
        else
        {
            TMP_FontAsset orbit = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OrbitFontPath);
            if (orbit != null)
            {
                label.font = orbit;
            }
        }

        return button;
    }
}
#endif
