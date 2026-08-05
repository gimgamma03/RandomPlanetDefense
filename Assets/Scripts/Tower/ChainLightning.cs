using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 체인 라이트닝 연출 + 피해.
/// 아웃라인(어두운 밑선) + 글로우 + 코어. 회색 맵에서도 대비가 나게 한다.
/// </summary>
public class ChainLightning : MonoBehaviour
{
    private const int DefaultSegments = 10;
    private const float CoreWidth = 0.12f;
    private const float GlowWidth = 0.52f;
    private const float OutlineWidth = 0.72f;
    private const float NoisePerUnit = 0.18f;
    private const float MinNoise = 0.06f;
    private const float MaxNoise = 0.35f;
    private const float LiveJitter = 0.045f;
    private const float FlickerSpeed = 42f;

    [SerializeField]
    private LayerMask layerMask;
    [SerializeField]
    private LineRenderer lineRenderer;
    [SerializeField]
    private LineRenderer glowLineRenderer;
    [SerializeField]
    private LineRenderer outlineLineRenderer;

    [SerializeField]
    private int chainCount = 3;
    [SerializeField]
    private float searchRadius = 3f;
    [SerializeField]
    private float hopDuration = 0.22f;
    [SerializeField]
    private int segments = DefaultSegments;

    private readonly HashSet<int> hitIds = new HashSet<int>();
    private readonly List<Vector3> baseHopPoints = new List<Vector3>(16);
    private readonly List<Vector3> liveHopPoints = new List<Vector3>(16);
    private float damage = 1f;
    private Coroutine running;
    private Vector2 hopPerp;

    // 회색 노드 대비: 채도 높은 전기색 + 어두운 밑선
    private static readonly Color CoreColor = new Color(1f, 0.98f, 0.75f, 1f);
    private static readonly Color GlowColor = new Color(0.2f, 0.75f, 1f, 0.85f);
    private static readonly Color OutlineColor = new Color(0.02f, 0.08f, 0.18f, 0.9f);

    public bool IsBusy => running != null;

    private void Awake()
    {
        EnsureRenderers();
        ConfigureRenderers();
        SetLinesEnabled(false);
    }

    /// <summary>첫 타겟부터 체인을 시작한다. 진행 중이면 무시.</summary>
    public void Fire(Transform firstTarget, float damageValue)
    {
        EnsureRenderers();
        if (firstTarget == null || IsBusy || lineRenderer == null)
        {
            return;
        }

        damage = damageValue;
        running = StartCoroutine(ChainRoutine(firstTarget));
    }

    private IEnumerator ChainRoutine(Transform firstTarget)
    {
        hitIds.Clear();
        SetLinesEnabled(true);

        Vector3 from = transform.position;
        Transform current = firstTarget;

        for (int hop = 0; hop < chainCount; hop++)
        {
            if (current == null)
            {
                break;
            }

            Vector3 to = current.position;
            int id = current.GetInstanceID();
            hitIds.Add(id);

            yield return AnimateHop(from, to);
            ApplyDamage(current);

            from = to;
            current = FindNext(from);
            if (current == null)
            {
                break;
            }
        }

        ClearLines();
        running = null;
    }

    private void ApplyDamage(Transform target)
    {
        if (target == null)
        {
            return;
        }

        EnemyHp hp = target.GetComponent<EnemyHp>();
        if (hp != null)
        {
            hp.TakeDamage(damage);
        }
    }

    private Transform FindNext(Vector3 from)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(from, searchRadius, layerMask);
        Transform best = null;
        float bestSqr = float.PositiveInfinity;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D col = hits[i];
            if (col == null)
            {
                continue;
            }

            Transform t = col.transform;
            if (hitIds.Contains(t.GetInstanceID()))
            {
                continue;
            }

            EnemyHp hp = t.GetComponent<EnemyHp>();
            if (hp == null || hp.currentHp <= 0f)
            {
                continue;
            }

            float sqr = (t.position - from).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = t;
            }
        }

        return best;
    }

    private IEnumerator AnimateHop(Vector3 source, Vector3 target)
    {
        BuildBaseHopPoints(source, target);
        float elapsed = 0f;

        while (elapsed < hopDuration)
        {
            float normalized = elapsed / hopDuration;
            ApplyLiveJitter(elapsed);
            ApplyFlicker(elapsed, normalized);
            ApplyHopPointsToLines();

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void BuildBaseHopPoints(Vector3 source, Vector3 target)
    {
        baseHopPoints.Clear();
        int count = Mathf.Max(2, segments);
        float distance = Vector3.Distance(source, target);
        float noise = Mathf.Clamp(distance * NoisePerUnit, MinNoise, MaxNoise);

        Vector2 dir = target - source;
        if (dir.sqrMagnitude < 0.0001f)
        {
            hopPerp = Vector2.up;
            baseHopPoints.Add(source);
            baseHopPoints.Add(target);
            return;
        }

        dir.Normalize();
        hopPerp = new Vector2(-dir.y, dir.x);

        baseHopPoints.Add(source);
        for (int i = 1; i < count - 1; i++)
        {
            float t = (float)i / (count - 1);
            Vector3 pos = Vector3.Lerp(source, target, t);
            float envelope = Mathf.Sin(t * Mathf.PI);
            float offset = (Random.value * 2f - 1f) * noise * envelope;
            pos.x += hopPerp.x * offset;
            pos.y += hopPerp.y * offset;
            baseHopPoints.Add(pos);
        }

        baseHopPoints.Add(target);
    }

    private void ApplyLiveJitter(float elapsed)
    {
        liveHopPoints.Clear();
        int count = baseHopPoints.Count;
        if (count == 0)
        {
            return;
        }

        liveHopPoints.Add(baseHopPoints[0]);
        for (int i = 1; i < count - 1; i++)
        {
            float t = (float)i / (count - 1);
            float envelope = Mathf.Sin(t * Mathf.PI);

            // 고주파 sin + 약한 랜덤 = 전류가 타는 느낌 (실루엣은 base 유지)
            float wave = Mathf.Sin((elapsed * FlickerSpeed) + (i * 1.7f));
            float crackle = (Random.value * 2f - 1f) * 0.35f;
            float offset = (wave * 0.65f + crackle) * LiveJitter * envelope;

            Vector3 pos = baseHopPoints[i];
            pos.x += hopPerp.x * offset;
            pos.y += hopPerp.y * offset;
            liveHopPoints.Add(pos);
        }

        liveHopPoints.Add(baseHopPoints[count - 1]);
    }

    private void ApplyFlicker(float elapsed, float hopNormalized)
    {
        // 밝기/두께가 빠르게 떨리며, 점프 끝으로 갈수록 살짝 약해짐
        float pulse = 0.72f + 0.28f * Mathf.Abs(Mathf.Sin(elapsed * FlickerSpeed * 1.35f));
        float fade = Mathf.Lerp(1f, 0.55f, hopNormalized * hopNormalized);
        float strength = pulse * fade;

        if (outlineLineRenderer != null)
        {
            float outlineW = OutlineWidth * Mathf.Lerp(0.95f, 1.1f, pulse);
            outlineLineRenderer.startWidth = outlineW;
            outlineLineRenderer.endWidth = outlineW;
            SetLineColor(outlineLineRenderer, ScaleAlpha(OutlineColor, Mathf.Lerp(0.75f, 1f, strength)));
        }

        if (glowLineRenderer != null)
        {
            float glowW = GlowWidth * Mathf.Lerp(0.95f, 1.2f, pulse);
            glowLineRenderer.startWidth = glowW;
            glowLineRenderer.endWidth = glowW;
            SetLineColor(glowLineRenderer, ScaleAlpha(GlowColor, strength));
        }

        if (lineRenderer != null)
        {
            float coreW = CoreWidth * Mathf.Lerp(0.9f, 1.3f, pulse);
            lineRenderer.startWidth = coreW;
            lineRenderer.endWidth = coreW;
            SetLineColor(lineRenderer, ScaleAlpha(CoreColor, strength));
        }
    }

    private static Color ScaleAlpha(Color color, float alphaScale)
    {
        color.a *= Mathf.Clamp01(alphaScale);
        return color;
    }

    private static void SetLineColor(LineRenderer line, Color color)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(color, 0f),
                new GradientColorKey(color, 1f),
            },
            new[]
            {
                new GradientAlphaKey(color.a, 0f),
                new GradientAlphaKey(color.a, 1f),
            });
        line.colorGradient = gradient;
    }

    private void ApplyHopPointsToLines()
    {
        int count = liveHopPoints.Count;
        if (count == 0)
        {
            return;
        }

        ApplyPoints(outlineLineRenderer, count);
        ApplyPoints(glowLineRenderer, count);
        ApplyPoints(lineRenderer, count);
    }

    private void ApplyPoints(LineRenderer line, int count)
    {
        if (line == null)
        {
            return;
        }

        line.positionCount = count;
        for (int i = 0; i < count; i++)
        {
            line.SetPosition(i, liveHopPoints[i]);
        }
    }

    private void EnsureRenderers()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponentInChildren<LineRenderer>(true);
        }

        if (lineRenderer == null)
        {
            return;
        }

        glowLineRenderer = EnsureChildLine(glowLineRenderer, "LightningGlow");
        outlineLineRenderer = EnsureChildLine(outlineLineRenderer, "LightningOutline");
    }

    private LineRenderer EnsureChildLine(LineRenderer existing, string childName)
    {
        if (existing != null)
        {
            return existing;
        }

        Transform child = transform.Find(childName);
        if (child == null && lineRenderer.transform.parent != null)
        {
            child = lineRenderer.transform.parent.Find(childName);
        }

        if (child != null)
        {
            LineRenderer found = child.GetComponent<LineRenderer>();
            if (found != null)
            {
                return found;
            }
        }

        Transform parent = lineRenderer.transform.parent != null
            ? lineRenderer.transform.parent
            : transform;
        GameObject go = new GameObject(childName);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        LineRenderer line = go.AddComponent<LineRenderer>();
        line.sharedMaterial = lineRenderer.sharedMaterial;
        line.useWorldSpace = true;
        line.loop = false;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.allowOcclusionWhenDynamic = false;
        return line;
    }

    private void ConfigureRenderers()
    {
        // sorting: outline < glow < core
        ConfigureLine(outlineLineRenderer, OutlineWidth, OutlineColor, 38);
        ConfigureLine(glowLineRenderer, GlowWidth, GlowColor, 39);
        ConfigureLine(lineRenderer, CoreWidth, CoreColor, 40);
    }

    private static void ConfigureLine(LineRenderer line, float width, Color color, int sortingOrder)
    {
        if (line == null)
        {
            return;
        }

        line.widthMultiplier = 1f;
        line.widthCurve = AnimationCurve.Constant(0f, 1f, 1f);
        line.startWidth = width;
        line.endWidth = width;
        line.numCapVertices = 2;
        line.numCornerVertices = 2;
        line.sortingOrder = sortingOrder;
        line.textureMode = LineTextureMode.Stretch;
        line.positionCount = 0;
        line.enabled = false;
        SetLineColor(line, color);
    }

    private void SetLinesEnabled(bool enabled)
    {
        SetEnabled(outlineLineRenderer, enabled);
        SetEnabled(glowLineRenderer, enabled);
        SetEnabled(lineRenderer, enabled);
    }

    private static void SetEnabled(LineRenderer line, bool enabled)
    {
        if (line != null)
        {
            line.enabled = enabled;
        }
    }

    private void ClearLines()
    {
        ClearLine(outlineLineRenderer);
        ClearLine(glowLineRenderer);
        ClearLine(lineRenderer);
    }

    private static void ClearLine(LineRenderer line)
    {
        if (line == null)
        {
            return;
        }

        line.positionCount = 0;
        line.enabled = false;
    }
}
