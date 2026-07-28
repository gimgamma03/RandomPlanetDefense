using System;
using System.Collections.Generic;

/// <summary>
/// 플레이 세션 통계 1건 (6번 스키마 — 필드 추가 최소화 위해 여기서 확정).
/// API/DB와 동일 형태를 목표로 한다.
/// </summary>
[Serializable]
public class PlaySessionStats
{
    public string sessionId = string.Empty;
    public string playerId = string.Empty;
    public int stageId;
    public string stageName = string.Empty;
    public int maxWaves;

    public string clientVersion = string.Empty;
    public string startedAtUtc = string.Empty;
    public string endedAtUtc = string.Empty;
    public float durationSeconds;

    /// <summary>시작한 웨이브 중 최대 번호 (1-based). 미시작이면 0.</summary>
    public int wavesReached;

    /// <summary>클리어한 웨이브 수.</summary>
    public int wavesCleared;

    public int finalScore;

    /// <summary>cleared | game_over | quit</summary>
    public string endReason = string.Empty;

    public int towersSpawned;
    public int towersMerged;
    public int towersSold;

    /// <summary>타워 사용 비율용 — 스폰 성공 건수 by WeaponType.</summary>
    public List<WeaponCountEntry> towerSpawnsByWeapon = new List<WeaponCountEntry>();

    /// <summary>세션 시작 시점 영구 강화 레벨 스냅샷 (형별).</summary>
    public List<WeaponUpgradeEntry> weaponUpgradeLevels = new List<WeaponUpgradeEntry>();
}

[Serializable]
public class WeaponCountEntry
{
    /// <summary>WeaponType enum int</summary>
    public int weaponType;
    public int count;
}

/// <summary>세션 종료 원인 문자열 (스키마 고정).</summary>
public static class SessionEndReason
{
    public const string Cleared = "cleared";
    public const string GameOver = "game_over";
    public const string Quit = "quit";
}
