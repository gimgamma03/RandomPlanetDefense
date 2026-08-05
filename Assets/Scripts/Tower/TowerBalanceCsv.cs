using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// CSV → TowerData.
/// 컬럼: towerId,displayName,grade,damage,rate,range,slowValue,spawnWeight,sell,doubleShot,upgradeDamage,upgradeRate,upgradeRange,upgradeSlow
/// grade = 합성 등급 1~5 (이름 접두 1Tower 와 무관)
/// </summary>
public static class TowerBalanceCsv
{
    public static void Apply(TowerCatalog catalog, string csvText)
    {
        if (catalog == null || string.IsNullOrWhiteSpace(csvText))
        {
            return;
        }

        Dictionary<string, Row> rows = Parse(csvText);
        if (rows.Count == 0)
        {
            Debug.LogWarning("[TowerBalanceCsv] No data rows parsed.");
            return;
        }

        int applied = 0;
        IReadOnlyList<TowerData> towers = catalog.Towers;
        if (towers == null)
        {
            return;
        }

        for (int i = 0; i < towers.Count; i++)
        {
            TowerData data = towers[i];
            if (data == null)
            {
                continue;
            }

            if (!rows.TryGetValue(data.Id, out Row row))
            {
                continue;
            }

            data.ApplyBalance(
                row.damage,
                row.rate,
                row.range,
                row.slowValue,
                row.spawnWeight,
                row.sell,
                row.doubleShot,
                row.upgradeDamage,
                row.upgradeRate,
                row.upgradeRange,
                row.upgradeSlow,
                row.grade,
                row.displayName);
            applied++;
        }

        Debug.Log($"[TowerBalanceCsv] Applied {applied} tower row(s).");
    }

    private static Dictionary<string, Row> Parse(string csvText)
    {
        var result = new Dictionary<string, Row>(StringComparer.OrdinalIgnoreCase);
        string[] lines = csvText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        if (lines.Length < 2)
        {
            return result;
        }

        string[] header = SplitCsvLine(lines[0]);
        var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < header.Length; i++)
        {
            index[header[i].Trim()] = i;
        }

        if (!index.ContainsKey("towerId"))
        {
            Debug.LogError("[TowerBalanceCsv] Missing towerId column.");
            return result;
        }

        for (int lineIndex = 1; lineIndex < lines.Length; lineIndex++)
        {
            string line = lines[lineIndex].Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
            {
                continue;
            }

            string[] cols = SplitCsvLine(line);
            string id = Get(cols, index, "towerId");
            if (string.IsNullOrEmpty(id))
            {
                continue;
            }

            result[id] = new Row
            {
                displayName = Get(cols, index, "displayName"),
                grade = GetInt(cols, index, "grade", -1),
                damage = GetFloat(cols, index, "damage"),
                rate = GetFloat(cols, index, "rate"),
                range = GetFloat(cols, index, "range"),
                slowValue = GetFloat(cols, index, "slowValue"),
                spawnWeight = GetFloat(cols, index, "spawnWeight", 1f),
                sell = GetInt(cols, index, "sell"),
                doubleShot = GetBool(cols, index, "doubleShot"),
                upgradeDamage = GetFloat(cols, index, "upgradeDamage"),
                upgradeRate = GetFloat(cols, index, "upgradeRate"),
                upgradeRange = GetFloat(cols, index, "upgradeRange"),
                upgradeSlow = GetFloat(cols, index, "upgradeSlow"),
            };
        }

        return result;
    }

    private static string[] SplitCsvLine(string line)
    {
        return line.Split(',');
    }

    private static string Get(string[] cols, Dictionary<string, int> index, string key)
    {
        if (!index.TryGetValue(key, out int i) || i < 0 || i >= cols.Length)
        {
            return string.Empty;
        }

        return cols[i].Trim();
    }

    private static float GetFloat(string[] cols, Dictionary<string, int> index, string key, float fallback = 0f)
    {
        string raw = Get(cols, index, key);
        if (string.IsNullOrEmpty(raw))
        {
            return fallback;
        }

        if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
        {
            return value;
        }

        return fallback;
    }

    private static int GetInt(string[] cols, Dictionary<string, int> index, string key, int fallback = 0)
    {
        string raw = Get(cols, index, key);
        if (string.IsNullOrEmpty(raw))
        {
            return fallback;
        }

        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
        {
            return value;
        }

        return fallback;
    }

    private static bool GetBool(string[] cols, Dictionary<string, int> index, string key)
    {
        string raw = Get(cols, index, key);
        if (string.IsNullOrEmpty(raw))
        {
            return false;
        }

        return raw == "1" || raw.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    private struct Row
    {
        public string displayName;
        public int grade;
        public float damage;
        public float rate;
        public float range;
        public float slowValue;
        public float spawnWeight;
        public int sell;
        public bool doubleShot;
        public float upgradeDamage;
        public float upgradeRate;
        public float upgradeRange;
        public float upgradeSlow;
    }
}