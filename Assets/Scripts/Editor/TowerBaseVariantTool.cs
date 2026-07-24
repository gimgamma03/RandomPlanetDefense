#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// TowerBase를 원형으로 두고, 이미 복사해 둔 타입 베이스들을 Variant로 재연결하는 보조 툴.
/// (완전 자동 조립보다는 폴더 구조 + Library가 본체. Variant 관계는 에디터에서 정리용.)
/// </summary>
public static class TowerBaseVariantTool
{
    private const string BasesPath = "Assets/Prefabs/Towers/Bases";

    [MenuItem("RPD/Towers/Open Bases Folder")]
    private static void OpenBasesFolder()
    {
        var obj = AssetDatabase.LoadAssetAtPath<Object>(BasesPath);
        if (obj != null)
        {
            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);
        }
        else
        {
            Debug.LogWarning($"Folder not found: {BasesPath}");
        }
    }

    [MenuItem("RPD/Towers/Log Base Library")]
    private static void LogLibrary()
    {
        var lib = Resources.Load<TowerBaseLibrary>(TowerBaseLibrary.ResourcesName);
        if (lib == null)
        {
            Debug.LogError("TowerBaseLibrary missing in Resources.");
            return;
        }

        Debug.Log($"TowerBase (origin ref): {lib.TowerBasePrefab}", lib.TowerBasePrefab);
        foreach (WeaponType type in System.Enum.GetValues(typeof(WeaponType)))
        {
            var prefab = lib.GetBasePrefab(type);
            if (prefab != null)
            {
                Debug.Log($"  {type} → {prefab.name}", prefab);
            }
        }
    }
}
#endif