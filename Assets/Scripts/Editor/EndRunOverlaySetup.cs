#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// GameScene에 종료 UI(ShowWave + Title 버튼)를 씬에 미리 깔아 둔다.
/// 메뉴: RPD / Game / Setup End Run Overlay
/// </summary>
public static class EndRunOverlaySetup
{
    private const string GameScenePath = "Assets/Scenes/GameScene.unity";
    private const string OrbitFontPath = "Assets/Fonts/Orbit-Regular SDF.asset";

    [MenuItem("RPD/Game/Setup End Run Overlay")]
    public static void Setup()
    {
        var scene = EditorSceneManager.OpenScene(GameScenePath);

        GameObject showWave = GameObject.Find("ShowWave");
        if (showWave == null)
        {
            // inactive 포함 검색
            Transform[] all = Resources.FindObjectsOfTypeAll<Transform>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name == "ShowWave" && all[i].gameObject.scene.IsValid())
                {
                    showWave = all[i].gameObject;
                    break;
                }
            }
        }

        if (showWave == null)
        {
            EditorUtility.DisplayDialog("End Run Overlay", "GameScene에 ShowWave가 없습니다.", "OK");
            return;
        }

        TextFadeOut fade = showWave.GetComponent<TextFadeOut>();
        if (fade == null)
        {
            fade = showWave.AddComponent<TextFadeOut>();
        }

        TextMeshProUGUI messageTmp = showWave.GetComponent<TextMeshProUGUI>();
        if (messageTmp == null)
        {
            EditorUtility.DisplayDialog("End Run Overlay", "ShowWave에 TextMeshProUGUI가 없습니다.", "OK");
            return;
        }

        SerializedObject fadeSo = new SerializedObject(fade);
        SerializedProperty showTextProp = fadeSo.FindProperty("showText");
        if (showTextProp != null && showTextProp.objectReferenceValue == null)
        {
            showTextProp.objectReferenceValue = messageTmp;
            fadeSo.ApplyModifiedPropertiesWithoutUndo();
        }

        EndRunOverlay overlay = showWave.GetComponent<EndRunOverlay>();
        if (overlay == null)
        {
            overlay = showWave.AddComponent<EndRunOverlay>();
        }

        Button titleButton = EnsureTitleButton(showWave.transform, messageTmp.font);

        SerializedObject overlaySo = new SerializedObject(overlay);
        overlaySo.FindProperty("root").objectReferenceValue = showWave;
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

        // 씬에서 위치 잡기 쉽게: ShowWave 켠 상태로 두고 알파 1
        showWave.SetActive(true);
        Color c = messageTmp.color;
        messageTmp.color = new Color(c.r, c.g, c.b, 1f);
        messageTmp.text = "All Waves Clear";
        messageTmp.raycastTarget = false;
        titleButton.gameObject.SetActive(true);

        EditorUtility.SetDirty(showWave);
        EditorUtility.SetDirty(overlay);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Selection.activeGameObject = showWave;
        EditorGUIUtility.PingObject(showWave);

        Debug.Log("[EndRunOverlaySetup] ShowWave + ButtonTitle 씬에 배치 완료. Scene 뷰에서 위치 조절하세요.");

        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog(
                "End Run Overlay",
                "Setup 완료.\n\n" +
                "ShowWave / ButtonTitle 을 Scene 뷰에서 드래그해서 위치 맞추세요.\n" +
                "맞춘 뒤 ShowWave 체크를 끄면 됩니다 (Play 시작 시에도 자동으로 꺼짐).",
                "OK");
        }
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
        if (existing == null)
        {
            rt.anchoredPosition = new Vector2(0f, -180f);
            rt.sizeDelta = new Vector2(320f, 72f);
        }

        Image image = go.GetComponent<Image>();
        if (image.color.a < 0.1f)
        {
            image.color = new Color(0.15f, 0.55f, 0.7f, 0.95f);
        }

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;

        Transform labelTf = go.transform.Find("Text");
        GameObject labelGo;
        if (labelTf != null)
        {
            labelGo = labelTf.gameObject;
        }
        else
        {
            labelGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(go.transform, false);
        }

        RectTransform labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = labelGo.GetComponent<TextMeshProUGUI>();
        if (tmp == null)
        {
            tmp = labelGo.AddComponent<TextMeshProUGUI>();
        }

        tmp.text = "Title";
        tmp.fontSize = 36f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        TMP_FontAsset orbit = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OrbitFontPath);
        if (orbit != null)
        {
            tmp.font = orbit;
        }
        else if (font != null)
        {
            tmp.font = font;
        }

        return button;
    }
}
#endif
