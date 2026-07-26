#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ProjectileBaseLibrary 생성·기존 프리팹 등록.
/// </summary>
public static class ProjectileBaseSetup
{
    private const string LibraryPath = "Assets/Resources/ProjectileBaseLibrary.asset";
    private const string ImpactPrefabPath =
        "Assets/Externals/Inguz Media Studio/Free 2D Impact FX/Prefabs/Impact02.prefab";

    private static readonly (ProjectileType type, string path)[] Defaults =
    {
        (ProjectileType.Homing, "Assets/Prefabs/Projectiles/Bases/HomingBase.prefab"),
        (ProjectileType.Straight, "Assets/Prefabs/Projectiles/Bases/StraightBase.prefab"),
        (ProjectileType.BombShot, "Assets/Prefabs/Projectiles/Bases/BombShotBase.prefab"),
        (ProjectileType.GroundBomb, "Assets/Prefabs/Projectiles/Bases/GroundBombBase.prefab"),
    };

    private static readonly string[] FlightVfxPrefabPaths =
    {
        "Assets/Prefabs/Projectiles/Bases/HomingBase.prefab",
        "Assets/Prefabs/Projectiles/Bases/StraightBase.prefab",
    };

    [MenuItem("RPD/Projectiles/1. Create Or Refresh Base Library")]
    private static void CreateOrRefreshLibrary()
    {
        ProjectileBaseLibrary library = AssetDatabase.LoadAssetAtPath<ProjectileBaseLibrary>(LibraryPath);
        if (library == null)
        {
            library = ScriptableObject.CreateInstance<ProjectileBaseLibrary>();
            string dir = Path.GetDirectoryName(LibraryPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            AssetDatabase.CreateAsset(library, LibraryPath);
        }

        var so = new SerializedObject(library);
        SerializedProperty entries = so.FindProperty("entries");
        entries.arraySize = Defaults.Length;

        int missing = 0;
        for (int i = 0; i < Defaults.Length; i++)
        {
            SerializedProperty element = entries.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("projectileType").enumValueIndex = (int)Defaults[i].type;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Defaults[i].path);
            element.FindPropertyRelative("basePrefab").objectReferenceValue = prefab;
            if (prefab == null)
            {
                missing++;
                Debug.LogWarning($"[RPD] Missing projectile prefab: {Defaults[i].path}");
            }
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = library;
        EditorUtility.DisplayDialog(
            "Projectile Library",
            missing == 0
                ? $"등록 완료\n{LibraryPath}\nHoming / Straight / BombShot / GroundBomb"
                : $"등록함 (누락 {missing}개 — 콘솔 확인)\n{LibraryPath}",
            "OK");
    }

    [MenuItem("RPD/Projectiles/2. Attach Flight Vfx (Homing/Straight)")]
    private static void AttachFlightVfx()
    {
        GameObject impact = AssetDatabase.LoadAssetAtPath<GameObject>(ImpactPrefabPath);
        int updated = 0;

        for (int i = 0; i < FlightVfxPrefabPaths.Length; i++)
        {
            string path = FlightVfxPrefabPaths[i];
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning($"[RPD] Missing prefab: {path}");
                continue;
            }

            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);

            if (root.GetComponent<TrailRenderer>() == null)
            {
                root.AddComponent<TrailRenderer>();
            }

            ProjectileVfx vfx = root.GetComponent<ProjectileVfx>();
            if (vfx == null)
            {
                vfx = root.AddComponent<ProjectileVfx>();
            }

            SerializedObject so = new SerializedObject(vfx);
            so.FindProperty("enablePulse").boolValue = true;
            so.FindProperty("enableTrail").boolValue = true;
            so.FindProperty("enableImpact").boolValue = true;
            so.FindProperty("trail").objectReferenceValue = root.GetComponent<TrailRenderer>();
            so.FindProperty("impactPrefab").objectReferenceValue = impact;
            so.FindProperty("trailTime").floatValue = 0.16f;
            so.FindProperty("trailStartWidth").floatValue = 0.14f;
            so.FindProperty("trailEndWidth").floatValue = 0.02f;
            so.FindProperty("impactScale").floatValue = 0.4f;
            so.FindProperty("impactLife").floatValue = 0.7f;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
            updated++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog(
            "Projectile Vfx",
            impact == null
                ? $"Vfx 부착 {updated}개\nImpact 프리팹 없음:\n{ImpactPrefabPath}"
                : $"Vfx 부착 완료 ({updated})\n펄스 + Trail + Impact02",
            "OK");
    }
}
#endif
