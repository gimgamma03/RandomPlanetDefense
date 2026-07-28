#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// DamagePopup 프리팹 생성 → Prefabs/UI 만 (HP바와 동일 위치).
/// </summary>
public static class DamagePopupSetup
{
    private const string PrefabPath = "Assets/Prefabs/UI/DamagePopup.prefab";
    private const string OrbitFontPath = "Assets/Fonts/Orbit-Regular SDF.asset";

    [MenuItem("RPD/UI/Create DamagePopup Prefab")]
    private static void CreatePrefab()
    {
        EnsureFolder("Assets/Prefabs/UI");

        // 예전 Resources 복제본 정리
        const string legacyResources = "Assets/Resources/UI/DamagePopup.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(legacyResources) != null)
        {
            AssetDatabase.DeleteAsset(legacyResources);
        }

        GameObject root = new GameObject("DamagePopup");
        TextMeshPro tmp = root.AddComponent<TextMeshPro>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = 3.2f;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
        tmp.sortingOrder = 220;
        tmp.text = "0";
        tmp.color = Color.white;

        TMP_FontAsset orbit = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OrbitFontPath);
        if (orbit != null)
        {
            tmp.font = orbit;
        }
        else if (TMP_Settings.defaultFontAsset != null)
        {
            tmp.font = TMP_Settings.defaultFontAsset;
        }

        DamagePopup popup = root.AddComponent<DamagePopup>();
        SerializedObject so = new SerializedObject(popup);
        so.FindProperty("text").objectReferenceValue = tmp;
        so.ApplyModifiedPropertiesWithoutUndo();

        root.SetActive(false);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[RPD] DamagePopup prefab: {PrefabPath}");
        EditorUtility.DisplayDialog(
            "DamagePopup",
            $"생성 완료\n{PrefabPath}\n\n글자 크기 등은 이 프리팹만 수정하면 됩니다.",
            "OK");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            return;
        }

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string name = Path.GetFileName(path);
        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
        {
            return;
        }

        if (!AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        AssetDatabase.CreateFolder(parent, name);
    }
}
#endif
