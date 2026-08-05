using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// persistentDataPath/MetaProgress.json 저장.
/// </summary>
public sealed class MetaProgressService : IMetaProgressService
{
    private readonly string path;
    private MetaProgressData data = new MetaProgressData();

    public string PlayerId => data.playerId;
    public int Crystals => data.crystals;
    public int BestScore => data.bestScore;

    public event Action OnCrystalsChanged;

    public MetaProgressService()
    {
        path = Path.Combine(Application.persistentDataPath, "MetaProgress.json");
    }

    public void Initialize()
    {
        Load();
        EnsurePlayerId();
        Save();
        Debug.Log(
            $"[MetaProgress] playerId={data.playerId} " +
            $"cleared={data.clearedStageIds.Count} best={data.bestScore} crystals={data.crystals}");
    }

    public bool IsStageCleared(int stageId)
    {
        return data.clearedStageIds != null && data.clearedStageIds.Contains(stageId);
    }

    public int GetStageBestScore(int stageId)
    {
        if (data.stageBestScores == null)
        {
            return 0;
        }

        for (int i = 0; i < data.stageBestScores.Count; i++)
        {
            StageBestScoreEntry entry = data.stageBestScores[i];
            if (entry != null && entry.stageId == stageId)
            {
                return entry.bestScore;
            }
        }

        return 0;
    }

    public void MarkStageCleared(int stageId)
    {
        if (stageId <= 0)
        {
            return;
        }

        if (data.clearedStageIds == null)
        {
            data.clearedStageIds = new List<int>();
        }

        if (data.clearedStageIds.Contains(stageId))
        {
            return;
        }

        data.clearedStageIds.Add(stageId);
        data.clearedStageIds.Sort();
        Save();
    }

    public void RecordScore(int stageId, int score)
    {
        if (score <= 0)
        {
            return;
        }

        bool dirty = false;
        if (score > data.bestScore)
        {
            data.bestScore = score;
            dirty = true;
        }

        if (stageId > 0)
        {
            int current = GetStageBestScore(stageId);
            if (score > current)
            {
                SetStageBestScore(stageId, score);
                dirty = true;
            }
        }

        if (dirty)
        {
            Save();
        }
    }

    public void AddCrystals(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        data.crystals += amount;
        Save();
        OnCrystalsChanged?.Invoke();
    }

    public bool TrySpendCrystals(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (data.crystals < amount)
        {
            return false;
        }

        data.crystals -= amount;
        Save();
        OnCrystalsChanged?.Invoke();
        return true;
    }

    public int GetWeaponUpgradeLevel(WeaponType weaponType)
    {
        EnsureUpgradeList();
        int key = (int)weaponType;
        for (int i = 0; i < data.weaponUpgrades.Count; i++)
        {
            WeaponUpgradeEntry entry = data.weaponUpgrades[i];
            if (entry != null && entry.weaponType == key)
            {
                return Mathf.Max(0, entry.level);
            }
        }

        return 0;
    }

    public bool TryUpgradeWeapon(WeaponType weaponType)
    {
        int level = GetWeaponUpgradeLevel(weaponType);
        if (level >= TowerMetaUpgradeRules.MaxLevel)
        {
            return false;
        }

        int cost = TowerMetaUpgradeRules.GetCostForNextLevel(level);
        if (cost <= 0)
        {
            return false;
        }

        if (data.crystals < cost)
        {
            return false;
        }

        data.crystals -= cost;
        SetWeaponUpgradeLevel(weaponType, level + 1);
        Save();
        OnCrystalsChanged?.Invoke();
        return true;
    }

    /// <summary>에디터/테스트용. 클리어·베스트·playerId는 건드리지 않음.</summary>
    public void DebugSetCrystals(int value)
    {
        data.crystals = Mathf.Max(0, value);
        Save();
        OnCrystalsChanged?.Invoke();
    }

    /// <summary>테스트 경제만 초기화. 클리어·베스트·playerId 유지.</summary>
    public void DebugResetCrystalsAndWeaponUpgrades()
    {
        data.crystals = 0;
        if (data.weaponUpgrades != null)
        {
            data.weaponUpgrades.Clear();
        }

        Save();
        OnCrystalsChanged?.Invoke();
    }

    /// <summary>테스트용. 클리어 스테이지·스테이지별 베스트만 초기화. playerId·크리스탈·강화 유지.</summary>
    public void DebugClearStageProgress()
    {
        if (data.clearedStageIds != null)
        {
            data.clearedStageIds.Clear();
        }

        if (data.stageBestScores != null)
        {
            data.stageBestScores.Clear();
        }

        data.bestScore = 0;
        Save();
    }

    private void SetWeaponUpgradeLevel(WeaponType weaponType, int level)
    {
        EnsureUpgradeList();
        int key = (int)weaponType;
        for (int i = 0; i < data.weaponUpgrades.Count; i++)
        {
            if (data.weaponUpgrades[i] != null && data.weaponUpgrades[i].weaponType == key)
            {
                data.weaponUpgrades[i].level = level;
                return;
            }
        }

        data.weaponUpgrades.Add(new WeaponUpgradeEntry
        {
            weaponType = key,
            level = level,
        });
    }

    private void SetStageBestScore(int stageId, int score)
    {
        if (data.stageBestScores == null)
        {
            data.stageBestScores = new List<StageBestScoreEntry>();
        }

        for (int i = 0; i < data.stageBestScores.Count; i++)
        {
            if (data.stageBestScores[i] != null && data.stageBestScores[i].stageId == stageId)
            {
                data.stageBestScores[i].bestScore = score;
                return;
            }
        }

        data.stageBestScores.Add(new StageBestScoreEntry
        {
            stageId = stageId,
            bestScore = score,
        });
    }

    private void EnsurePlayerId()
    {
        if (!string.IsNullOrEmpty(data.playerId))
        {
            return;
        }

        data.playerId = Guid.NewGuid().ToString("N");
    }

    private void EnsureUpgradeList()
    {
        if (data.weaponUpgrades == null)
        {
            data.weaponUpgrades = new List<WeaponUpgradeEntry>();
        }
    }

    private void Load()
    {
        if (!File.Exists(path))
        {
            data = new MetaProgressData();
            return;
        }

        try
        {
            string json = File.ReadAllText(path);
            MetaProgressData loaded = JsonConvert.DeserializeObject<MetaProgressData>(json);
            data = loaded ?? new MetaProgressData();
            if (data.clearedStageIds == null)
            {
                data.clearedStageIds = new List<int>();
            }

            if (data.stageBestScores == null)
            {
                data.stageBestScores = new List<StageBestScoreEntry>();
            }

            EnsureUpgradeList();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[MetaProgress] Load failed: {e.Message}");
            data = new MetaProgressData();
        }
    }

    private void Save()
    {
        try
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(path, json);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[MetaProgress] Save failed: {e.Message}");
        }
    }
}
