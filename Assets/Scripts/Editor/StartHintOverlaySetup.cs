#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GameScene에 첫 진입 조작 안내(StartHintOverlay)와 '?' 버튼을 배치한다.
/// 메뉴: RPD / Game / Setup Start Hint Overlay
/// </summary>
public static class StartHintOverlaySetup
{
    private const string GameScenePath = "Assets/Scenes/GameScene.unity";
    private const string OrbitFontPath = "Assets/Fonts/Orbit-Regular SDF.asset";
    private const string OverlayName = "StartHintOverlay";
    private const string HelpButtonName = "ButtonHelp";

    private static readonly (string objectName, string label)[] Targets =
    {
        ("ButtonSpawnTower", "1) 타워 뽑기 (3골드)"),
        ("ButtonCombineTower", "2) 같은 타워 3개 합치기"),
        ("ButtonPlaceWall", "3) 벽으로 적 경로 설정"),
        ("WaveStart", "4) 준비되면 웨이브 시작"),
        ("ButtonSellTower", "타워 되팔기"),
    };

    [MenuItem("RPD/Game/Setup Start Hint Overlay")]
    public static void Setup()
    {
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(GameScenePath);

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Start Hint Overlay", "GameScene에 Canvas가 없습니다.", "OK");
            return;
        }

        TMP_FontAsset orbit = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OrbitFontPath);

        GameObject overlayGo = FindInScene(OverlayName);
        if (overlayGo == null)
        {
            overlayGo = new GameObject(OverlayName, typeof(RectTransform));
            overlayGo.transform.SetParent(canvas.transform, false);
            Undo.RegisterCreatedObjectUndo(overlayGo, "Create StartHintOverlay");
        }

        StretchFull(overlayGo.GetComponent<RectTransform>());
        overlayGo.transform.SetAsLastSibling();

        StartHintOverlay overlay = overlayGo.GetComponent<StartHintOverlay>();
        if (overlay == null)
        {
            overlay = overlayGo.AddComponent<StartHintOverlay>();
        }

        GameObject panel = EnsureChild(overlayGo.transform, "HintPanel");
        StretchFull(panel.GetComponent<RectTransform>());

        Image dimmer = EnsureImage(panel.transform, "Dimmer", new Color(0f, 0f, 0f, 0.62f));
        dimmer.transform.SetAsFirstSibling();

        UICursorIcon cursor = EnsureCursor(panel.transform);
        TextMeshProUGUI title = EnsureTitle(panel.transform, orbit);
        Button close = EnsureCloseButton(panel.transform, orbit);

        SerializedObject so = new SerializedObject(overlay);
        so.FindProperty("panel").objectReferenceValue = panel;
        so.FindProperty("dimmer").objectReferenceValue = dimmer;
        so.FindProperty("cursorIcon").objectReferenceValue = cursor;
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("closeButton").objectReferenceValue = close;
        so.FindProperty("font").objectReferenceValue = orbit;

        SerializedProperty list = so.FindProperty("targets");
        List<(RectTransform rect, string label)> found = CollectTargets();
        list.arraySize = found.Count;
        for (int i = 0; i < found.Count; i++)
        {
            SerializedProperty element = list.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("target").objectReferenceValue = found[i].rect;
            element.FindPropertyRelative("label").stringValue = found[i].label;
            element.FindPropertyRelative("labelOffset").vector2Value = Vector2.zero;
        }

        so.ApplyModifiedPropertiesWithoutUndo();

        Button help = EnsureHelpButton(canvas.transform, orbit);
        WireHelpButton(help, overlay);

        panel.SetActive(false);

        EditorUtility.SetDirty(overlay);
        EditorUtility.SetDirty(overlayGo);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Selection.activeGameObject = overlayGo;
        EditorGUIUtility.PingObject(overlayGo);

        Debug.Log($"[StartHintOverlaySetup] 배치 완료. 연결된 대상 {found.Count}개.");

        if (!Application.isBatchMode)
        {
            EditorUtility.DisplayDialog(
                "Start Hint Overlay",
                $"배치 완료 (대상 {found.Count}개).\n\n" +
                "· HintPanel 안의 CursorIcon / Title / ButtonClose 위치를 Scene 뷰에서 조절하세요.\n" +
                "· Canvas 밑의 ButtonHelp('?')도 원하는 자리로 드래그하면 됩니다.\n" +
                "· 점선과 화살표는 실행 시 버튼 위치를 읽어 자동으로 그려집니다.",
                "OK");
        }
    }

    private static List<(RectTransform, string)> CollectTargets()
    {
        var result = new List<(RectTransform, string)>();
        for (int i = 0; i < Targets.Length; i++)
        {
            GameObject go = FindInScene(Targets[i].objectName);
            if (go == null)
            {
                Debug.LogWarning($"[StartHintOverlaySetup] '{Targets[i].objectName}' 을(를) 찾지 못했습니다.");
                continue;
            }

            RectTransform rect = go.transform as RectTransform;
            if (rect == null)
            {
                continue;
            }

            result.Add((rect, Targets[i].label));
        }

        return result;
    }

    private static UICursorIcon EnsureCursor(Transform parent)
    {
        GameObject go = EnsureChild(parent, "CursorIcon");
        RectTransform rt = go.GetComponent<RectTransform>();

        UICursorIcon icon = go.GetComponent<UICursorIcon>();
        if (icon == null)
        {
            icon = go.AddComponent<UICursorIcon>();
            // 위치·크기는 최초 생성 때만 잡고, 이후엔 씬에서 조절한 값을 유지한다
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(55f, 55f);
            rt.anchoredPosition = Vector2.zero;
        }

        icon.color = new Color(0.68f, 1f, 0.28f, 1f);
        icon.raycastTarget = false;
        return icon;
    }

    private static TextMeshProUGUI EnsureTitle(Transform parent, TMP_FontAsset font)
    {
        GameObject go = EnsureChild(parent, "HintTitle");
        RectTransform rt = go.GetComponent<RectTransform>();
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp == null)
        {
            tmp = go.AddComponent<TextMeshProUGUI>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(760f, 48f);
            rt.anchoredPosition = new Vector2(0f, -150f);
        }

        tmp.text = "조작 안내 · 아무 키나 누르면 닫힙니다";
        tmp.fontSize = 28f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.85f, 1f, 0.7f, 1f);
        tmp.raycastTarget = false;
        if (font != null)
        {
            tmp.font = font;
        }

        return tmp;
    }

    private static Button EnsureCloseButton(Transform parent, TMP_FontAsset font)
    {
        GameObject go = EnsureChild(parent, "ButtonClose");
        RectTransform rt = go.GetComponent<RectTransform>();

        Image image = go.GetComponent<Image>();
        if (image == null)
        {
            image = go.AddComponent<Image>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(52f, 52f);
            rt.anchoredPosition = new Vector2(-20f, -20f);
        }

        image.color = new Color(0.1f, 0.16f, 0.12f, 0.9f);

        Button button = go.GetComponent<Button>();
        if (button == null)
        {
            button = go.AddComponent<Button>();
        }

        button.targetGraphic = image;
        EnsureLabel(go.transform, "X", 30f, font, new Color(0.68f, 1f, 0.28f, 1f));
        return button;
    }

    private static Button EnsureHelpButton(Transform canvas, TMP_FontAsset font)
    {
        GameObject go = FindInScene(HelpButtonName);
        if (go == null)
        {
            go = new GameObject(HelpButtonName, typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            Undo.RegisterCreatedObjectUndo(go, "Create ButtonHelp");

            RectTransform created = go.GetComponent<RectTransform>();
            created.anchorMin = new Vector2(1f, 1f);
            created.anchorMax = new Vector2(1f, 1f);
            created.pivot = new Vector2(1f, 1f);
            created.sizeDelta = new Vector2(60f, 60f);
            created.anchoredPosition = new Vector2(-366f, -22f);
        }

        Image image = go.GetComponent<Image>();
        if (image == null)
        {
            image = go.AddComponent<Image>();
        }

        image.color = new Color(0.08f, 0.2f, 0.16f, 0.85f);

        Button button = go.GetComponent<Button>();
        if (button == null)
        {
            button = go.AddComponent<Button>();
        }

        button.targetGraphic = image;
        EnsureLabel(go.transform, "?", 28f, font, new Color(0.68f, 1f, 0.28f, 1f));
        return button;
    }

    private static void WireHelpButton(Button button, StartHintOverlay overlay)
    {
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
        {
            UnityEventTools.RemovePersistentListener(button.onClick, i);
        }

        UnityEventTools.AddPersistentListener(button.onClick, overlay.Show);
    }

    private static TextMeshProUGUI EnsureLabel(
        Transform parent,
        string text,
        float fontSize,
        TMP_FontAsset font,
        Color color)
    {
        GameObject go = EnsureChild(parent, "Text");
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp == null)
        {
            tmp = go.AddComponent<TextMeshProUGUI>();
        }

        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.raycastTarget = false;
        if (font != null)
        {
            tmp.font = font;
        }

        return tmp;
    }

    private static Image EnsureImage(Transform parent, string childName, Color color)
    {
        GameObject go = EnsureChild(parent, childName);
        StretchFull(go.GetComponent<RectTransform>());

        Image image = go.GetComponent<Image>();
        if (image == null)
        {
            image = go.AddComponent<Image>();
        }

        image.color = color;
        image.raycastTarget = true;
        return image;
    }

    private static GameObject EnsureChild(Transform parent, string childName)
    {
        Transform existing = parent.Find(childName);
        if (existing != null)
        {
            return existing.gameObject;
        }

        GameObject go = new GameObject(childName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(go, $"Create {childName}");
        return go;
    }

    private static GameObject FindInScene(string name)
    {
        Transform[] all = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null || !all[i].gameObject.scene.IsValid())
            {
                continue;
            }

            if (all[i].name == name)
            {
                return all[i].gameObject;
            }
        }

        return null;
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
#endif
