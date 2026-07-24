#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// TowerData SO ↔ TowerBalance.csv 에디터 동기화.
/// </summary>
public static class TowerBalanceMenu
{
    private const string BalanceCsvPath = "Balance/TowerBalance.csv";
    private const string ResourcesCsvPath = "Assets/Resources/TowerBalance.csv";
    private const string TowerDataFolder = "Assets/Resources/TowerData";

    private const string CsvHeader =
        "towerId,displayName,grade,damage,rate,range,slowValue,spawnWeight,sell,doubleShot,upgradeDamage,upgradeRate,upgradeRange,upgradeSlow";

    [MenuItem("RPD/Towers/Export TowerData → Balance CSV")]
    private static void ExportTowerDataToCsv()
    {
        TowerData[] towers = LoadAllTowerDataAssets();
        if (towers.Length == 0)
        {
            EditorUtility.DisplayDialog("Export CSV", "Resources/TowerData 에 TowerData가 없습니다.", "OK");
            return;
        }

        System.Array.Sort(towers, (a, b) =>
        {
            int g = ((int)a.grade).CompareTo((int)b.grade);
            return g != 0 ? g : string.CompareOrdinal(a.Id, b.Id);
        });

        var sb = new StringBuilder(512);
        sb.AppendLine(CsvHeader);

        for (int i = 0; i < towers.Length; i++)
        {
            TowerData t = towers[i];
            sb.Append(Escape(t.Id)).Append(',')
                .Append(Escape(t.DisplayName)).Append(',')
                .Append((int)t.grade).Append(',')
                .Append(F(t.weapon.damage)).Append(',')
                .Append(F(t.weapon.rate)).Append(',')
                .Append(F(t.weapon.range)).Append(',')
                .Append(F(t.weapon.slowValue)).Append(',')
                .Append(F(t.spawnWeight)).Append(',')
                .Append(t.weapon.sell).Append(',')
                .Append(t.weapon.doubleShot ? "1" : "0").Append(',')
                .Append(F(t.weaponUpGradeValue.damage)).Append(',')
                .Append(F(t.weaponUpGradeValue.rate)).Append(',')
                .Append(F(t.weaponUpGradeValue.range)).Append(',')
                .Append(F(t.weaponUpGradeValue.slowValue))
                .AppendLine();
        }

        string text = sb.ToString();
        bool balanceOk = TryWriteText(BalanceCsvPath, text, out string balanceError);
        bool resourcesOk = TryWriteText(ResourcesCsvPath, text, out string resourcesError);
        AssetDatabase.Refresh();

        if (balanceOk && resourcesOk)
        {
            Debug.Log($"[RPD] Exported {towers.Length} TowerData → {BalanceCsvPath} (+ Resources copy)");
            EditorUtility.DisplayDialog(
                "Export CSV",
                $"SO {towers.Length}개 → CSV 저장 완료.\n\n{BalanceCsvPath}\n{ResourcesCsvPath}",
                "OK");
            return;
        }

        var msg = new StringBuilder();
        msg.AppendLine($"일부 저장 실패 (SO {towers.Length}개 기준).");
        msg.AppendLine();
        msg.AppendLine(balanceOk ? $"OK: {BalanceCsvPath}" : $"FAIL: {BalanceCsvPath}\n  {balanceError}");
        msg.AppendLine(resourcesOk ? $"OK: {ResourcesCsvPath}" : $"FAIL: {ResourcesCsvPath}\n  {resourcesError}");
        msg.AppendLine();
        msg.AppendLine("Excel/메모장 등으로 CSV를 열어두면 잠깁니다. 닫고 다시 Export 하세요.");

        Debug.LogWarning("[RPD] Export partial failure.\n" + msg);
        EditorUtility.DisplayDialog("Export CSV", msg.ToString(), "OK");
    }

    [MenuItem("RPD/Towers/Import Balance CSV → TowerData")]
    private static void ImportCsvToTowerData()
    {
        string csvText = ReadPreferredCsv(out string usedPath);
        if (string.IsNullOrWhiteSpace(csvText))
        {
            EditorUtility.DisplayDialog(
                "Import CSV",
                $"CSV를 찾지 못했습니다.\n\n우선: {BalanceCsvPath}\n또는: {ResourcesCsvPath}",
                "OK");
            return;
        }

        TowerData[] towers = LoadAllTowerDataAssets();
        if (towers.Length == 0)
        {
            EditorUtility.DisplayDialog("Import CSV", "Resources/TowerData 에 TowerData가 없습니다.", "OK");
            return;
        }

        var catalog = new TowerCatalog(towers);
        TowerBalanceCsv.Apply(catalog, csvText);

        for (int i = 0; i < towers.Length; i++)
        {
            if (towers[i] != null)
            {
                EditorUtility.SetDirty(towers[i]);
            }
        }

        // Balance 폴더를 고쳤을 때 Resources 카피도 맞춤
        if (usedPath.Replace('\\', '/') == BalanceCsvPath)
        {
            if (!TryWriteText(ResourcesCsvPath, csvText, out string copyError))
            {
                Debug.LogWarning($"[RPD] Resources CSV 복사 실패 (SO는 적용됨): {copyError}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[RPD] Imported CSV → TowerData SO ({usedPath})");
        EditorUtility.DisplayDialog(
            "Import CSV",
            $"CSV → SO 적용 완료.\n\n읽은 파일: {usedPath}\n(towerId가 맞는 행만 덮어씀)",
            "OK");
    }

    private static TowerData[] LoadAllTowerDataAssets()
    {
        string[] guids = AssetDatabase.FindAssets("t:TowerData", new[] { TowerDataFolder });
        var list = new List<TowerData>(guids.Length);
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            TowerData data = AssetDatabase.LoadAssetAtPath<TowerData>(path);
            if (data != null)
            {
                list.Add(data);
            }
        }

        return list.ToArray();
    }

    private static string ReadPreferredCsv(out string usedPath)
    {
        if (File.Exists(BalanceCsvPath))
        {
            usedPath = BalanceCsvPath;
            return File.ReadAllText(BalanceCsvPath, Encoding.UTF8);
        }

        if (File.Exists(ResourcesCsvPath))
        {
            usedPath = ResourcesCsvPath;
            return File.ReadAllText(ResourcesCsvPath, Encoding.UTF8);
        }

        usedPath = string.Empty;
        return null;
    }

    private static bool TryWriteText(string path, string text, out string error)
    {
        error = null;
        try
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(path, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return true;
        }
        catch (IOException ex)
        {
            error = ex.Message;
            return false;
        }
        catch (System.UnauthorizedAccessException ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static string F(float value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        // 단순 CSV: 콤마 있으면 시트/파서가 깨지므로 제거
        return value.Replace(',', ' ').Trim();
    }
}
#endif
