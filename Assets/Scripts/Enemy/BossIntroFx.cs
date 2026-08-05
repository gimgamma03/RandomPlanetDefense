using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// TowerDeco 조각이 타깃(블랙홀/킹)으로 슈슈슈 빨려 들어가는 연출.
/// </summary>
public sealed class BossIntroFx : MonoBehaviour
{
    private const int SortingOrder = 40;
    private const string CatalogResourcePath = "BossIntroDecoCatalog";

    private const float DefaultWorldRadius = 0.5f;

    [SerializeField]
    private BossIntroDecoCatalog catalog;

    [SerializeField]
    private Sprite[] decoSprites;

    [Min(4)]
    [SerializeField]
    private int shardCount = 22;

    [Min(0.5f)]
    [Tooltip("보스 등장 연출 목표 길이(초)")]
    [SerializeField]
    private float gatherDuration = 2.5f;

    [Tooltip("보스 등장 시 조각 월드 반지름")]
    [SerializeField]
    private float shardWorldRadius = DefaultWorldRadius;

    [SerializeField]
    private Vector2 radiusJitter = new Vector2(0.9f, 1.2f);

    [SerializeField]
    private Vector2 spawnGapRange = new Vector2(0.05f, 0.14f);

    [SerializeField]
    private Vector2 travelTimeRange = new Vector2(0.55f, 1.05f);

    [Header("Summon (lighter)")]
    [SerializeField]
    private int summonShardCount = 11;

    [SerializeField]
    private float summonWorldRadius = 0.35f;

    [SerializeField]
    private Vector2 summonTravelTimeRange = new Vector2(0.4f, 0.75f);

    [SerializeField]
    private Vector2 summonSpawnGapRange = new Vector2(0.04f, 0.12f);

    private readonly List<GameObject> activeShards = new List<GameObject>(32);
    private Coroutine gatherRoutine;

    /// <summary>최종 웨이브 보스 등장용 (풀 연출).</summary>
    public IEnumerator Play(Vector3 blackHoleWorld)
    {
        yield return PlayGather(
            follow: null,
            fixedTarget: blackHoleWorld,
            count: shardCount,
            duration: gatherDuration,
            worldRadius: shardWorldRadius,
            gapRange: spawnGapRange,
            travelRange: travelTimeRange,
            clearExisting: true);
    }

    /// <summary>킹 소환 스킬용 — 킹에게 날아감. 등장보다 짧게·작게.</summary>
    public IEnumerator PlaySummonGather(Transform king, float durationHint)
    {
        float duration = Mathf.Clamp(durationHint, 1.0f, 2.2f);
        yield return PlayGather(
            follow: king,
            fixedTarget: king != null ? king.position : Vector3.zero,
            count: summonShardCount,
            duration: duration,
            worldRadius: summonWorldRadius,
            gapRange: summonSpawnGapRange,
            travelRange: summonTravelTimeRange,
            clearExisting: false);
    }

    /// <summary>소환 연출만 중단 (등장 연출과 공유 Cancel 시 구분용).</summary>
    public void StopGather()
    {
        if (gatherRoutine != null)
        {
            StopCoroutine(gatherRoutine);
            gatherRoutine = null;
        }

        ClearShards();
    }

    public Coroutine StartSummonGather(Transform king, float durationHint)
    {
        StopGather();
        gatherRoutine = StartCoroutine(PlaySummonGatherThenClear(king, durationHint));
        return gatherRoutine;
    }

    private IEnumerator PlaySummonGatherThenClear(Transform king, float durationHint)
    {
        yield return PlaySummonGather(king, durationHint);
        gatherRoutine = null;
    }

    public void Cancel()
    {
        gatherRoutine = null;
        StopAllCoroutines();
        ClearShards();
    }

    private IEnumerator PlayGather(
        Transform follow,
        Vector3 fixedTarget,
        int count,
        float duration,
        float worldRadius,
        Vector2 gapRange,
        Vector2 travelRange,
        bool clearExisting)
    {
        Sprite[] sprites = ResolveSprites();
        if (sprites == null || sprites.Length == 0)
        {
            yield break;
        }

        if (clearExisting)
        {
            ClearShards();
        }

        count = Mathf.Max(4, count);
        float targetDuration = Mathf.Max(0.5f, duration);
        Camera cam = Camera.main;

        float avgGap = (gapRange.x + gapRange.y) * 0.5f;
        float spawnBudget = Mathf.Max(0.35f, targetDuration - travelRange.y);
        float gapScale = count > 1
            ? spawnBudget / (avgGap * (count - 1))
            : 1f;
        gapScale = Mathf.Clamp(gapScale, 0.35f, 2.2f);

        float elapsed = 0f;
        float lastFinish = 0f;

        for (int i = 0; i < count; i++)
        {
            Sprite sprite = sprites[Random.Range(0, sprites.Length)];
            if (sprite == null)
            {
                continue;
            }

            Vector3 end = ResolveTarget(follow, fixedTarget);
            Vector3 start = PickEdgeSpawn(cam, end);
            float travel = Random.Range(travelRange.x, travelRange.y);
            float worldR = worldRadius * Random.Range(radiusJitter.x, radiusJitter.y);
            float startScale = ScaleForWorldRadius(sprite, worldR);
            float spin = Random.Range(-480f, 480f);

            GameObject go = new GameObject($"BossGatherShard_{i}");
            go.transform.position = start;
            go.transform.localScale = Vector3.one * startScale;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = SortingOrder;
            Color c = sr.color;
            c.a = Random.Range(0.85f, 1f);
            sr.color = c;

            activeShards.Add(go);
            StartCoroutine(FlyShard(go, sr, start, follow, end, travel, startScale, spin));
            lastFinish = Mathf.Max(lastFinish, elapsed + travel);

            if (i < count - 1)
            {
                float gap = Random.Range(gapRange.x, gapRange.y) * gapScale;
                yield return new WaitForSeconds(gap);
                elapsed += gap;
            }
        }

        float remain = lastFinish - elapsed;
        if (remain > 0f)
        {
            yield return new WaitForSeconds(remain);
        }

        if (clearExisting)
        {
            ClearShards();
        }
    }

    private static Vector3 ResolveTarget(Transform follow, Vector3 fixedTarget)
    {
        if (follow != null)
        {
            Vector3 p = follow.position;
            p.z = 0f;
            return p;
        }

        fixedTarget.z = 0f;
        return fixedTarget;
    }

    private static float ScaleForWorldRadius(Sprite sprite, float worldRadius)
    {
        float ext = Mathf.Max(sprite.bounds.extents.x, sprite.bounds.extents.y);
        if (ext < 0.0001f)
        {
            return worldRadius * 2f;
        }

        return worldRadius / ext;
    }

    private IEnumerator FlyShard(
        GameObject go,
        SpriteRenderer sr,
        Vector3 start,
        Transform follow,
        Vector3 fixedEnd,
        float travel,
        float startScale,
        float spinDegrees)
    {
        if (go == null)
        {
            yield break;
        }

        float t = 0f;
        while (t < travel && go != null)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / travel);
            float eased = u * u * u;

            Vector3 end = ResolveTarget(follow, fixedEnd);
            go.transform.position = Vector3.LerpUnclamped(start, end, eased);
            go.transform.Rotate(0f, 0f, spinDegrees * Time.deltaTime);

            float scaleMul = Mathf.Lerp(1f, 0.12f, eased);
            go.transform.localScale = Vector3.one * (startScale * scaleMul);

            if (sr != null)
            {
                Color c = sr.color;
                c.a = Mathf.Lerp(0.95f, 0f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 1f, eased)));
                sr.color = c;
            }

            yield return null;
        }

        if (go != null)
        {
            Destroy(go);
            activeShards.Remove(go);
        }
    }

    private void ClearShards()
    {
        for (int i = 0; i < activeShards.Count; i++)
        {
            if (activeShards[i] != null)
            {
                Destroy(activeShards[i]);
            }
        }

        activeShards.Clear();
    }

    private Sprite[] ResolveSprites()
    {
        if (decoSprites != null && decoSprites.Length > 0)
        {
            return decoSprites;
        }

        if (catalog != null && catalog.sprites != null && catalog.sprites.Length > 0)
        {
            return catalog.sprites;
        }

        BossIntroDecoCatalog loaded = Resources.Load<BossIntroDecoCatalog>(CatalogResourcePath);
        if (loaded != null && loaded.sprites != null && loaded.sprites.Length > 0)
        {
            catalog = loaded;
            return loaded.sprites;
        }

#if UNITY_EDITOR
        Sprite[] editorSprites = LoadSpritesFromDecoFolderEditor();
        if (editorSprites != null && editorSprites.Length > 0)
        {
            Debug.LogWarning(
                "[BossIntro] Resources/BossIntroDecoCatalog missing or empty — using Editor AssetDatabase fallback. " +
                "Run menu RPD/Boss/Rebuild Intro Deco Catalog before Player builds.");
            return editorSprites;
        }
#endif

        Debug.LogError(
            "[BossIntro] No deco sprites. Build will skip king gather FX. " +
            "Ensure Assets/Resources/BossIntroDecoCatalog.asset exists (RPD/Boss/Rebuild Intro Deco Catalog).");
        return null;
    }

#if UNITY_EDITOR
    private static Sprite[] LoadSpritesFromDecoFolderEditor()
    {
        string[] guids = UnityEditor.AssetDatabase.FindAssets(
            "t:Texture2D",
            new[] { "Assets/Images/Towers/TowerDecoImage" });
        List<Sprite> list = new List<Sprite>(guids.Length);
        for (int i = 0; i < guids.Length; i++)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
            Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            for (int a = 0; a < assets.Length; a++)
            {
                if (assets[a] is Sprite sprite)
                {
                    list.Add(sprite);
                }
            }
        }

        return list.Count > 0 ? list.ToArray() : null;
    }
#endif

    private static Vector3 PickEdgeSpawn(Camera cam, Vector3 target)
    {
        if (cam == null || !cam.orthographic)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Random.Range(4.5f, 7.5f);
            return target + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
        }

        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        Vector3 center = cam.transform.position;
        center.z = 0f;

        float margin = Random.Range(0.6f, 1.8f);
        int side = Random.Range(0, 4);
        switch (side)
        {
            case 0:
                return new Vector3(center.x - halfW - margin, center.y + Random.Range(-halfH, halfH), 0f);
            case 1:
                return new Vector3(center.x + halfW + margin, center.y + Random.Range(-halfH, halfH), 0f);
            case 2:
                return new Vector3(center.x + Random.Range(-halfW, halfW), center.y - halfH - margin, 0f);
            default:
                return new Vector3(center.x + Random.Range(-halfW, halfW), center.y + halfH + margin, 0f);
        }
    }

    private void OnDisable()
    {
        Cancel();
    }
}
