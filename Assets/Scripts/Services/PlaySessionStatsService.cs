using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// 세션 통계 수집 + 로컬 큐 저장.
/// 서버 POST는 이후 연결 — 지금은 파일 적재만 (fire-and-forget 전제).
/// </summary>
public sealed class PlaySessionStatsService : IPlaySessionStatsService
{
    private readonly string queueDirectory;
    private PlaySessionStats current;
    private float runStartRealtime;
    private bool ended;

    public bool IsRunActive => current != null && !ended;

    public PlaySessionStatsService()
    {
        queueDirectory = Path.Combine(Application.persistentDataPath, "PlaySessionQueue");
    }

    public void Initialize()
    {
        if (!Directory.Exists(queueDirectory))
        {
            Directory.CreateDirectory(queueDirectory);
        }

        Debug.Log($"[PlaySessionStats] queue dir = {queueDirectory}");
    }

    public void BeginRun(int stageId, string stageName, int maxWaves, string playerId)
    {
        current = new PlaySessionStats
        {
            sessionId = Guid.NewGuid().ToString("N"),
            playerId = playerId ?? string.Empty,
            stageId = stageId,
            stageName = stageName ?? string.Empty,
            maxWaves = Mathf.Max(0, maxWaves),
            clientVersion = Application.version,
            startedAtUtc = DateTime.UtcNow.ToString("o"),
            towerSpawnsByWeapon = new List<WeaponCountEntry>(),
            weaponUpgradeLevels = SnapshotWeaponUpgrades(),
        };
        runStartRealtime = Time.realtimeSinceStartup;
        ended = false;
        Debug.Log(
            $"[PlaySessionStats] Begin session={current.sessionId} stage={stageId} " +
            $"maxWaves={maxWaves} upgrades={current.weaponUpgradeLevels.Count}");
    }

    private static List<WeaponUpgradeEntry> SnapshotWeaponUpgrades()
    {
        var list = new List<WeaponUpgradeEntry>();
        if (!ServiceLocator.TryGet(out IMetaProgressService meta))
        {
            return list;
        }

        foreach (WeaponType type in System.Enum.GetValues(typeof(WeaponType)))
        {
            int level = meta.GetWeaponUpgradeLevel(type);
            if (level <= 0)
            {
                continue;
            }

            list.Add(new WeaponUpgradeEntry
            {
                weaponType = (int)type,
                level = level,
            });
        }

        return list;
    }

    public void RecordWaveStarted(int waveNumber1Based)
    {
        if (!IsRunActive || waveNumber1Based <= 0)
        {
            return;
        }

        if (waveNumber1Based > current.wavesReached)
        {
            current.wavesReached = waveNumber1Based;
        }
    }

    public void RecordWaveCleared(int waveNumber1Based)
    {
        if (!IsRunActive || waveNumber1Based <= 0)
        {
            return;
        }

        current.wavesCleared++;
        if (waveNumber1Based > current.wavesReached)
        {
            current.wavesReached = waveNumber1Based;
        }
    }

    public void RecordTowerSpawned(WeaponType weaponType)
    {
        if (!IsRunActive)
        {
            return;
        }

        current.towersSpawned++;
        IncrementWeaponCount(current.towerSpawnsByWeapon, weaponType);
    }

    public void RecordTowerMerged(WeaponType weaponType)
    {
        if (!IsRunActive)
        {
            return;
        }

        current.towersMerged++;
    }

    public void RecordTowerSold(WeaponType weaponType)
    {
        if (!IsRunActive)
        {
            return;
        }

        current.towersSold++;
    }

    public void EndRun(string endReason, int finalScore)
    {
        if (current == null || ended)
        {
            return;
        }

        ended = true;
        current.endReason = string.IsNullOrEmpty(endReason) ? SessionEndReason.Quit : endReason;
        current.finalScore = Mathf.Max(0, finalScore);
        current.endedAtUtc = DateTime.UtcNow.ToString("o");
        current.durationSeconds = Mathf.Max(0f, Time.realtimeSinceStartup - runStartRealtime);

        try
        {
            SaveToQueue(current);
            Debug.Log(
                $"[PlaySessionStats] End reason={current.endReason} score={current.finalScore} " +
                $"wave={current.wavesReached}/{current.maxWaves} " +
                $"spawn={current.towersSpawned} merge={current.towersMerged} sell={current.towersSold} " +
                $"sec={current.durationSeconds:0.0}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PlaySessionStats] Save failed (ignored): {e.Message}");
        }

        // 이후: HTTP POST. 실패해도 플레이 영향 없음.
        current = null;
    }

    private static void IncrementWeaponCount(List<WeaponCountEntry> list, WeaponType weaponType)
    {
        int key = (int)weaponType;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null && list[i].weaponType == key)
            {
                list[i].count++;
                return;
            }
        }

        list.Add(new WeaponCountEntry { weaponType = key, count = 1 });
    }

    private void SaveToQueue(PlaySessionStats stats)
    {
        if (!Directory.Exists(queueDirectory))
        {
            Directory.CreateDirectory(queueDirectory);
        }

        string fileName = $"{stats.endedAtUtc.Replace(':', '-')}_{stats.sessionId}.json";
        string path = Path.Combine(queueDirectory, fileName);
        string json = JsonConvert.SerializeObject(stats, Formatting.Indented);
        File.WriteAllText(path, json);
    }
}
