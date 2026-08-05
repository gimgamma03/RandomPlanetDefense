#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

/// <summary>
/// 스프라이트 아틀라스 생성.
/// PackAtlases는 소스 포맷이 꼬이면 Format 0 / Image invalid format 을 도배하므로
/// 아틀라스 등록만 하고, 패킹은 Inspector Pack Preview / 빌드에 맡긴다.
/// </summary>
public static class SpriteAtlasSetup
{
    private const string AtlasFolder = "Assets/Atlases";

    [MenuItem("RPD/Atlas/Create Enemy Atlas")]
    public static void CreateEnemyAtlas()
    {
        CreateOrRefreshAtlas(
            "Atlas_Enemies",
            "Assets/Images/Enemy",
            "적 이미지(Images/Enemy) → Atlas_Enemies");
    }

    [MenuItem("RPD/Atlas/Create Projectiles Atlas")]
    public static void CreateProjectilesAtlas()
    {
        CreateOrRefreshAtlas(
            "Atlas_Projectiles",
            "Assets/Images/Projectiles",
            "발사체 이미지(Images/Projectiles) → Atlas_Projectiles");
    }

    [MenuItem("RPD/Atlas/Create Enemy + Projectiles Atlases")]
    public static void CreateEnemyAndProjectiles()
    {
        CreateEnemyAtlas();
        CreateProjectilesAtlas();
    }

    [MenuItem("RPD/Atlas/1) Force Reimport Enemy+Projectiles (RGBA32)")]
    public static void ForceReimportMenus()
    {
        int n = ForcePrepareTextures("Assets/Images/Enemy");
        n += ForcePrepareTextures("Assets/Images/Projectiles");
        EditorUtility.DisplayDialog(
            "Sprite Atlas",
            $"강제 RGBA32 / Uncompressed 재임포트: {n}개\n" +
            "끝나면 RPD/Atlas/Create Enemy + Projectiles 실행.",
            "OK");
    }

    [MenuItem("RPD/Atlas/How to make Tower Atlas (guide)")]
    public static void ShowTowerGuide()
    {
        EditorUtility.DisplayDialog(
            "타워 아틀라스 만들기 (직접)",
            "1) Create → 2D → Sprite Atlas\n" +
            "2) Assets/Atlases/Atlas_Towers\n" +
            "3) Objects for Packing ← Images/Towers 폴더\n" +
            "4) Pack Preview\n\n" +
            "적/발사체는 RPD/Atlas 메뉴 사용.",
            "OK");
    }

    private static void CreateOrRefreshAtlas(string atlasName, string spritesFolder, string successMessage)
    {
        if (!AssetDatabase.IsValidFolder(spritesFolder))
        {
            EditorUtility.DisplayDialog("Sprite Atlas", $"폴더 없음: {spritesFolder}", "OK");
            return;
        }

        int prepared = ForcePrepareTextures(spritesFolder);
        Debug.Log($"[SpriteAtlasSetup] Force-reimported {prepared} textures in {spritesFolder}");

        EnsureAtlasFolder();

        string atlasPath = $"{AtlasFolder}/{atlasName}.spriteatlas";
        SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
        if (atlas == null)
        {
            atlas = new SpriteAtlas();
            AssetDatabase.CreateAsset(atlas, atlasPath);
        }

        atlas.SetPackingSettings(new SpriteAtlasPackingSettings
        {
            blockOffset = 1,
            enableRotation = false,
            enableTightPacking = false,
            padding = 4,
        });

        atlas.SetTextureSettings(new SpriteAtlasTextureSettings
        {
            readable = false,
            generateMipMaps = false,
            sRGB = true,
            filterMode = FilterMode.Bilinear,
        });

        // 아틀라스 결과물 크기 (타워와 동일 계열)
        var platform = atlas.GetPlatformSettings("DefaultTexturePlatform");
        platform.name = "DefaultTexturePlatform";
        platform.overridden = true;
        platform.maxTextureSize = 4096;
        platform.format = TextureImporterFormat.Automatic;
        platform.textureCompression = TextureImporterCompression.Compressed;
        atlas.SetPlatformSettings(platform);

        Object folder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(spritesFolder);
        if (folder == null)
        {
            EditorUtility.DisplayDialog("Sprite Atlas", $"로드 실패: {spritesFolder}", "OK");
            return;
        }

        Object[] current = atlas.GetPackables();
        if (current != null && current.Length > 0)
        {
            atlas.Remove(current);
        }

        atlas.Add(new Object[] { folder });

        EditorUtility.SetDirty(atlas);
        AssetDatabase.SaveAssets();

        // PackAtlases 호출하지 않음 — Format 0 스팸 원인.
        // Include in Build면 플레이어 빌드 시 패킹. 미리보기는 Inspector Pack Preview.
        LogSourceFormats(spritesFolder);

        Selection.activeObject = atlas;
        EditorGUIUtility.PingObject(atlas);
        EditorUtility.DisplayDialog(
            "Sprite Atlas",
            $"{successMessage}\n" +
            $"강제 재임포트: {prepared}개\n" +
            $"경로: {atlasPath}\n\n" +
            "다음: Project에서 이 아틀라스 선택 → Inspector → Pack Preview\n" +
            "(자동 Pack은 Format 0 오류가 나서 빼 둠)",
            "OK");
    }

    /// <summary>
    /// 항상 RGBA32 + Uncompressed로 맞추고 SaveAndReimport.
    /// (이미 Uncompressed여도 Library 캐시가 깨져 Format 0 이 날 수 있음)
    /// </summary>
    private static int ForcePrepareTextures(string folder)
    {
        if (!AssetDatabase.IsValidFolder(folder))
        {
            return 0;
        }

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
        var paths = new List<string>();
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            string ext = Path.GetExtension(path);
            if (string.IsNullOrEmpty(ext))
            {
                continue;
            }

            ext = ext.ToLowerInvariant();
            if (ext != ".png" && ext != ".jpg" && ext != ".jpeg" && ext != ".tga")
            {
                continue;
            }

            paths.Add(path);
        }

        int count = 0;
        try
        {
            AssetDatabase.StartAssetEditing();
            for (int i = 0; i < paths.Count; i++)
            {
                string path = paths[i];
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.mipmapEnabled = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.alphaIsTransparency = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.crunchedCompression = false;

                ApplyUncompressedPlatform(importer, "DefaultTexturePlatform", overridden: true);
                ApplyUncompressedPlatform(importer, "Standalone", overridden: true);

                // StartAssetEditing 안에서는 ImportAsset만 — SaveAndReimport는 밖에서
                AssetDatabase.WriteImportSettingsIfDirty(path);
                count++;
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        for (int i = 0; i < paths.Count; i++)
        {
            AssetDatabase.ImportAsset(paths[i], ImportAssetOptions.ForceUpdate);
        }

        AssetDatabase.Refresh();
        return count;
    }

    private static void ApplyUncompressedPlatform(TextureImporter importer, string platformName, bool overridden)
    {
        TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platformName);
        settings.name = platformName;
        settings.overridden = overridden;
        settings.maxTextureSize = 2048;
        settings.resizeAlgorithm = TextureResizeAlgorithm.Mitchell;
        settings.format = TextureImporterFormat.RGBA32;
        settings.textureCompression = TextureImporterCompression.Uncompressed;
        settings.crunchedCompression = false;
        settings.compressionQuality = 50;
        importer.SetPlatformTextureSettings(settings);
    }

    private static void LogSourceFormats(string folder)
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
        int bad = 0;
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null)
            {
                Debug.LogWarning($"[SpriteAtlasSetup] null texture: {path}");
                bad++;
                continue;
            }

            // format 0 = 미초기화/깨짐 → Pack 시 Unsupported Format 0
            if ((int)tex.format == 0 || tex.width <= 0)
            {
                Debug.LogWarning(
                    $"[SpriteAtlasSetup] BAD format={(int)tex.format} size={tex.width}x{tex.height} path={path}");
                bad++;
            }
        }

        Debug.Log($"[SpriteAtlasSetup] Format check in {folder}: bad={bad} / total={guids.Length}");
    }

    private static void EnsureAtlasFolder()
    {
        if (AssetDatabase.IsValidFolder(AtlasFolder))
        {
            return;
        }

        Directory.CreateDirectory(AtlasFolder.Replace('/', Path.DirectorySeparatorChar));
        AssetDatabase.Refresh();
    }
}
#endif
