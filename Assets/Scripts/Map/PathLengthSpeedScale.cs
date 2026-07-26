using UnityEngine;

/// <summary>
/// 경로 노드 수가 기준보다 길어질수록 이동 속도 배율 증가.
/// 고정 ETA(벽 양과 무관하게 같은 시간)가 아니라, 일정 길이마다 조금씩 빨라진다.
/// </summary>
public static class PathLengthSpeedScale
{
    /// <summary>이 노드 수까지는 배율 1 (가속 없음).</summary>
    public const int ReferenceNodeCount = 20;

    /// <summary>기준 초과분 이만큼마다 배율 +BoostPerStep.</summary>
    public const int NodesPerBoostStep = 8;

    /// <summary>스텝당 속도 증가 (0.12 = +12%).</summary>
    public const float BoostPerStep = 0.12f;

    /// <summary>최대 배율 상한 (너무 빨라지지 않게).</summary>
    public const float MaxSpeedMultiplier = 2.25f;

    public static float GetSpeedMultiplier(int pathNodeCount)
    {
        if (pathNodeCount <= ReferenceNodeCount)
        {
            return 1f;
        }

        float extra = pathNodeCount - ReferenceNodeCount;
        float steps = extra / NodesPerBoostStep;
        return Mathf.Min(1f + steps * BoostPerStep, MaxSpeedMultiplier);
    }

    /// <summary>노드 간 Lerp 시간 = base / multiplier (배율↑ → 시간↓ → 더 빠름).</summary>
    public static float ScaleNodeMoveTime(float baseNodeMoveTime, int pathNodeCount)
    {
        float mul = GetSpeedMultiplier(pathNodeCount);
        return baseNodeMoveTime / mul;
    }
}
