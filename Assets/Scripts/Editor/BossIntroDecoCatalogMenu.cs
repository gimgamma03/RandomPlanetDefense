#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// TowerDecoImage 스프라이트를 Resources/BossIntroDecoCatalog 로 묶는다.
/// 플레이어 빌드에는 AssetDatabase 폴백이 없으므로, 빌드 전에도 카탈로그를 갱신한다.
/// </summary>
public static class BossIntroDecoCatalogMenu
{
    private const string DecoFolder = "Assets/Images/Towers/TowerDecoImage";
    private const string CatalogPath = "Assets/Resources/BossIntroDecoCatalog.asset";

    [MenuItem("RPD/Boss/Rebuild Intro Deco Catalog")]
    public static void RebuildCatalog()
    {
        RebuildCatalogInternal(selectAfter: true);
    }

    public static int RebuildCatalogInternal(bool selectAfter)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { DecoFolder });
        List<Sprite> sprites = new List<Sprite>(guids.Length);
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int a = 0; a < assets.Length; a++)
            {
                if (assets[a] is Sprite sprite)
                {
                    sprites.Add(sprite);
                }
            }
        }

        BossIntroDecoCatalog catalog = AssetDatabase.LoadAssetAtPath<BossIntroDecoCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<BossIntroDecoCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        catalog.sprites = sprites.ToArray();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[BossIntro] Catalog rebuilt: {sprites.Count} sprites → {CatalogPath}");
        if (selectAfter)
        {
            Selection.activeObject = catalog;
        }

        return sprites.Count;
    }
}

/// <summary>빌드 시 카탈로그가 비어 있으면 킹 연출이 통째로 스킵되므로 사전 갱신.</summary>
public sealed class BossIntroDecoCatalogBuildPreprocessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        int count = BossIntroDecoCatalogMenu.RebuildCatalogInternal(selectAfter: false);
        if (count <= 0)
        {
            throw new BuildFailedException(
                "[BossIntro] BossIntroDecoCatalog has 0 sprites. Check Assets/Images/Towers/TowerDecoImage.");
        }
    }
}
#endif
