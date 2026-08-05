#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 적 Base + EnemyData 마이그레이션/셋업.
/// </summary>
public static class EnemyBaseSetup
{
    private const string SourcePrefab = "Assets/Prefabs/Enemy 01.prefab";
    private const string BasePrefabPath = "Assets/Prefabs/Enemies/EnemyBase.prefab";
    private const string PrefabFolder = "Assets/Prefabs";
    private const string EnemyDataFolder = "Assets/Resources/EnemyData";

    [MenuItem("RPD/Enemies/1. Create EnemyBase Prefab")]
    private static void CreateEnemyBase()
    {
        if (!File.Exists(SourcePrefab))
        {
            EditorUtility.DisplayDialog("EnemyBase", $"소스 없음:\n{SourcePrefab}", "OK");
            return;
        }

        string dir = Path.GetDirectoryName(BasePrefabPath);
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();
        }

        if (File.Exists(BasePrefabPath))
        {
            AssetDatabase.DeleteAsset(BasePrefabPath);
        }

        if (!AssetDatabase.CopyAsset(SourcePrefab, BasePrefabPath))
        {
            EditorUtility.DisplayDialog("EnemyBase", "CopyAsset 실패", "OK");
            return;
        }

        GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefabPath);
        if (basePrefab == null)
        {
            return;
        }

        Enemy enemy = basePrefab.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.enemyData = null;
            EditorUtility.SetDirty(enemy);
        }

        basePrefab.name = "EnemyBase";
        EditorUtility.SetDirty(basePrefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[RPD] Created {BasePrefabPath}");
        EditorUtility.DisplayDialog("EnemyBase", $"생성 완료\n{BasePrefabPath}\n(EnemyData는 스폰 시 Bind)", "OK");
    }

    [MenuItem("RPD/Enemies/2. Sync Sprites Prefab → EnemyData")]
    private static void SyncSpritesToEnemyData()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder });
        int synced = 0;

        for (int i = 0; i < prefabGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
            string fileName = Path.GetFileNameWithoutExtension(path);
            if (!fileName.StartsWith("Enemy "))
            {
                continue;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                continue;
            }

            Enemy enemy = prefab.GetComponent<Enemy>();
            SpriteRenderer sr = prefab.GetComponent<SpriteRenderer>();
            if (enemy == null || enemy.enemyData == null || sr == null || sr.sprite == null)
            {
                continue;
            }

            EnemyData data = enemy.enemyData;
            data.sprite = sr.sprite;
            if (data.spriteColor.a <= 0f)
            {
                data.spriteColor = Color.white;
            }

            if (string.IsNullOrEmpty(data.enemyId))
            {
                data.enemyId = data.name;
            }

            EditorUtility.SetDirty(data);
            synced++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[RPD] Synced sprite on {synced} EnemyData asset(s).");
        EditorUtility.DisplayDialog("Sync Sprites", $"EnemyData {synced}개에 스프라이트 복사 완료.", "OK");
    }

    [MenuItem("RPD/Enemies/3. Assign EnemyBase To Scene Spawner")]
    private static void AssignBaseToSpawner()
    {
        GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefabPath);
        if (basePrefab == null)
        {
            EditorUtility.DisplayDialog("Assign", "먼저 메뉴 1번으로 EnemyBase를 만드세요.", "OK");
            return;
        }

        EnemySpawner[] spawners = Object.FindObjectsByType<EnemySpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (spawners.Length == 0)
        {
            EditorUtility.DisplayDialog("Assign", "열린 씬에 EnemySpawner 없음.", "OK");
            return;
        }

        SerializedObject so = new SerializedObject(spawners[0]);
        SerializedProperty prop = so.FindProperty("enemyBasePrefab");
        if (prop == null)
        {
            EditorUtility.DisplayDialog("Assign", "enemyBasePrefab 필드 없음 (재컴파일 후 재시도).", "OK");
            return;
        }

        prop.objectReferenceValue = basePrefab;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(spawners[0]);
        Debug.Log($"[RPD] Assigned EnemyBase to {spawners[0].name}");
        EditorUtility.DisplayDialog("Assign", $"EnemySpawner에 EnemyBase 할당:\n{spawners[0].name}", "OK");
    }
}
#endif
