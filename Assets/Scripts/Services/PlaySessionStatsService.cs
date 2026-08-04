using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// 세션 통계 수집 + 로컬 큐 저장 + (가능하면) API POST.
/// 전송 실패해도 플레이에 영향 없음.
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
        current.livesRemaining = 0;
        if (ServiceLocator.TryGet(out IPlayerService player))
        {
            current.livesRemaining = Mathf.Max(0, player.CurrentHp);
        }

        PlaySessionStats finished = current;
        try
        {
            SaveToQueue(finished);
            Debug.Log(
                $"[PlaySessionStats] End reason={finished.endReason} score={finished.finalScore} " +
                $"lives={finished.livesRemaining} wave={finished.wavesReached}/{finished.maxWaves} " +
                $"spawn={finished.towersSpawned} merge={finished.towersMerged} sell={finished.towersSold} " +
                $"sec={finished.durationSeconds:0}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[PlaySessionStats] Save failed (ignored): {e.Message}");
        }

        // API POST (PlaySessionApiClient가 있을 때만). 실패해도 무시.
        if (PlaySessionApiClient.Instance != null)
        {
            PlaySessionApiClient.Instance.PostSession(finished);
        }

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
