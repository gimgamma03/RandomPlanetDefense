#if UNITY_EDITOR
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 메타 진행 테스트 치트.
/// 크리스탈/영구강화만 건드림 — playerId · 클리어 · 베스트 스코어는 유지.
/// </summary>
public static class MetaProgressDebugMenu
{
    private const int TestCrystalAmount = 1_000_000;

    [MenuItem("RPD/Debug/Set Crystals To 1,000,000")]
    public static void SetTestCrystals()
    {
        if (TryApplyInPlayMode(setCrystals: true, resetEconomy: false))
        {
            EditorUtility.DisplayDialog(
                "Meta Debug",
                $"크리스탈 {TestCrystalAmount:N0} 설정.\n\n" +
                "같은 playerId 파일에 저장됨.\n" +
                "클리어/베스트 기록은 안 바뀜.\n" +
                "업글을 사면 그 레벨은 남으니, 끝나면 Reset 메뉴 쓰세요.",
                "OK");
            return;
        }

        MetaProgressData data = LoadOrCreate();
        data.crystals = TestCrystalAmount;
        Save(data);
        EditorUtility.DisplayDialog(
            "Meta Debug",
            $"Play 중이 아니라 JSON에 직접 썼음.\n크리스탈 = {TestCrystalAmount:N0}\n\n" +
            "Play 진입 후 반영.\n클리어/베스트는 유지.",
            "OK");
    }

    [MenuItem("RPD/Debug/Reset Crystals + Weapon Upgrades Only")]
    public static void ResetEconomyOnly()
    {
        if (TryApplyInPlayMode(setCrystals: false, resetEconomy: true))
        {
            EditorUtility.DisplayDialog(
                "Meta Debug",
                "크리스탈·영구 타워 강화만 초기화.\nplayerId / 클리어 / 베스트는 그대로.",
                "OK");
            return;
        }

        MetaProgressData data = LoadOrCreate();
        data.crystals = 0;
        data.weaponUpgrades = new System.Collections.Generic.List<WeaponUpgradeEntry>();
        Save(data);
        EditorUtility.DisplayDialog(
            "Meta Debug",
            "JSON에서 크리스탈·영구강화만 초기화 완료.\n클리어/베스트/playerId 유지.",
            "OK");
    }

    private static bool TryApplyInPlayMode(bool setCrystals, bool resetEconomy)
    {
        if (!Application.isPlaying)
        {
            return false;
        }

        if (!ServiceLocator.TryGet(out IMetaProgressService service) ||
            service is not MetaProgressService meta)
        {
            EditorUtility.DisplayDialog(
                "Meta Debug",
                "Play 중인데 IMetaProgressService를 못 찾음.\nTitle/Game에서 Bootstrap 후 다시.",
                "OK");
            return true;
        }

        if (setCrystals)
        {
            meta.DebugSetCrystals(TestCrystalAmount);
        }

        if (resetEconomy)
        {
            meta.DebugResetCrystalsAndWeaponUpgrades();
        }

        TitleFlow flow = Object.FindFirstObjectByType<TitleFlow>(FindObjectsInactive.Include);
        flow?.RefreshCrystalHud();

        TowerUpgradePanel panel =
            Object.FindFirstObjectByType<TowerUpgradePanel>(FindObjectsInactive.Include);
        if (panel != null && panel.gameObject.activeInHierarchy)
        {
            panel.Refresh();
        }

        return true;
    }

    private static string ProgressPath =>
        Path.Combine(Application.persistentDataPath, "MetaProgress.json");

    private static MetaProgressData LoadOrCreate()
    {
        string path = ProgressPath;
        if (!File.Exists(path))
        {
            return new MetaProgressData();
        }

        try
        {
            MetaProgressData loaded =
                JsonConvert.DeserializeObject<MetaProgressData>(File.ReadAllText(path));
            return loaded ?? new MetaProgressData();
        }
        catch
        {
            return new MetaProgressData();
        }
    }

    private static void Save(MetaProgressData data)
    {
        string path = ProgressPath;
        string dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(path, JsonConvert.SerializeObject(data, Formatting.Indented));
        Debug.Log($"[MetaProgressDebug] saved → {path}");
    }
}
#endif
