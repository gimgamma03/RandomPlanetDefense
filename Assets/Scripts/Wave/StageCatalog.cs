using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resources/Stages 아래 StageData를 수집. stageId로 조회.
/// </summary>
public sealed class StageCatalog
{
    public const string ResourcesFolder = "Stages";

    private readonly Dictionary<int, StageData> byId = new Dictionary<int, StageData>();
    private readonly StageData[] all;

    public IReadOnlyList<StageData> All => all;

    public StageCatalog(StageData[] stages)
    {
        all = stages ?? System.Array.Empty<StageData>();

        for (int i = 0; i < all.Length; i++)
        {
            StageData stage = all[i];
            if (stage == null)
            {
                continue;
            }

            if (byId.ContainsKey(stage.stageId))
            {
                Debug.LogWarning(
                    $"[StageCatalog] Duplicate stageId {stage.stageId}: " +
                    $"{byId[stage.stageId].name} vs {stage.name}. Keeping first.");
                continue;
            }

            byId[stage.stageId] = stage;
        }
    }

    public static StageCatalog LoadFromResources()
    {
        StageData[] loaded = Resources.LoadAll<StageData>(ResourcesFolder);
        if (loaded == null || loaded.Length == 0)
        {
            Debug.LogError($"[StageCatalog] Resources/{ResourcesFolder} 에 StageData가 없습니다.");
            return new StageCatalog(System.Array.Empty<StageData>());
        }

        Debug.Log($"[StageCatalog] Loaded {loaded.Length} StageData from Resources/{ResourcesFolder}");
        return new StageCatalog(loaded);
    }

    public bool TryGet(int stageId, out StageData stage)
    {
        return byId.TryGetValue(stageId, out stage) && stage != null;
    }

    public StageData Get(int stageId)
    {
        if (TryGet(stageId, out StageData stage))
        {
            return stage;
        }

        Debug.LogError($"[StageCatalog] Missing StageData for stageId={stageId}");
        return null;
    }
}
