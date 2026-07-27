using UnityEngine;

/// <summary>
/// 발사체 비행 비주얼: 약한 펄스, Trail, 히트 스파크.
/// Base 프리팹에 붙이고, 탄 스크립트가 BeginFlight / NotifyHit / NotifyMiss를 호출한다.
/// </summary>
[DisallowMultipleComponent]
public class ProjectileVfx : MonoBehaviour
{
    [Header("Pulse")]
    [SerializeField]
    private bool enablePulse = true;

    [SerializeField]
    private float pulseSpeed = 18f;

    [SerializeField]
    [Range(0f, 0.2f)]
    private float pulseAmplitudeX = 0.05f;

    [SerializeField]
    [Range(0f, 0.2f)]
    private float pulseAmplitudeY = 0.05f;

    [Header("Trail")]
    [SerializeField]
    private bool enableTrail = true;

    [SerializeField]
    private TrailRenderer trail;

    [SerializeField]
    private float trailTime = 0.16f;

    [SerializeField]
    private float trailStartWidth = 0.14f;

    [SerializeField]
    private float trailEndWidth = 0.02f;

    [SerializeField]
    private Color trailColor = new Color(0.55f, 1f, 0.65f, 0.85f);

    [Header("Impact")]
    [SerializeField]
    private bool enableImpact = true;

    [SerializeField]
    private GameObject impactPrefab;

    [SerializeField]
    private float impactLife = 0.7f;

    [SerializeField]
    private float impactScale = 0.4f;

    private Vector3 baseScale;
    private SpriteRenderer spriteRenderer;
    private bool inFlight;
    private bool baseScaleCaptured;

    private void Awake()
    {
        CaptureBaseScale();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (trail == null)
        {
            trail = GetComponent<TrailRenderer>();
        }

        EnsureTrailConfigured();
        StopVisuals(clearTrail: true);
    }

    private void OnDisable()
    {
        StopVisuals(clearTrail: true);
        if (baseScaleCaptured)
        {
            transform.localScale = baseScale;
        }
    }

    private void Update()
    {
        if (!inFlight || !enablePulse || !baseScaleCaptured)
        {
            return;
        }

        float t = Time.time * pulseSpeed;
        float sx = 1f + Mathf.Sin(t) * pulseAmplitudeX;
        float sy = 1f - Mathf.Sin(t * 1.37f + 0.6f) * pulseAmplitudeY;
        transform.localScale = new Vector3(baseScale.x * sx, baseScale.y * sy, baseScale.z);
    }

    public void SetTrailEnabled(bool enabled)
    {
        enableTrail = enabled;
        if (trail == null)
        {
            return;
        }

        if (!enabled)
        {
            trail.emitting = false;
            trail.Clear();
            trail.enabled = false;
            return;
        }

        trail.enabled = true;
    }

    /// <summary>스프라이트/스케일을 런타임에 바꾼 뒤 BeginFlight 전에 호출.</summary>
    public void RecaptureScale()
    {
        baseScale = transform.localScale;
        if (baseScale.sqrMagnitude < 0.0001f)
        {
            baseScale = Vector3.one;
        }

        baseScaleCaptured = true;
    }

    public void BeginFlight()
    {
        if (!baseScaleCaptured)
        {
            CaptureBaseScale();
        }
        else
        {
            // 풀 재사용·런타임 스프라이트 교체 후 현재 스케일 유지
            baseScale = transform.localScale.sqrMagnitude > 0.0001f
                ? transform.localScale
                : baseScale;
        }

        inFlight = true;
        transform.localScale = baseScale;

        if (enableTrail && trail != null)
        {
            ApplyTrailColor();
            trail.Clear();
            trail.emitting = true;
            trail.enabled = true;
        }
        else if (trail != null)
        {
            trail.emitting = false;
            trail.enabled = false;
        }
    }

    public void NotifyHit(Vector3 worldPosition)
    {
        SpawnImpact(worldPosition);
        StopVisuals(clearTrail: true);
    }

    public void NotifyMiss()
    {
        StopVisuals(clearTrail: true);
    }

    private void CaptureBaseScale()
    {
        if (baseScaleCaptured)
        {
            return;
        }

        baseScale = transform.localScale;
        if (baseScale.sqrMagnitude < 0.0001f)
        {
            baseScale = Vector3.one;
        }

        baseScaleCaptured = true;
    }

    private void StopVisuals(bool clearTrail)
    {
        inFlight = false;
        if (baseScaleCaptured)
        {
            transform.localScale = baseScale;
        }

        if (trail == null)
        {
            return;
        }

        trail.emitting = false;
        if (clearTrail)
        {
            trail.Clear();
        }
    }

    private void EnsureTrailConfigured()
    {
        if (!enableTrail)
        {
            return;
        }

        if (trail == null)
        {
            trail = gameObject.AddComponent<TrailRenderer>();
        }

        trail.time = trailTime;
        trail.startWidth = trailStartWidth;
        trail.endWidth = trailEndWidth;
        trail.minVertexDistance = 0.02f;
        trail.autodestruct = false;
        trail.emitting = false;
        trail.numCornerVertices = 2;
        trail.numCapVertices = 2;
        trail.textureMode = LineTextureMode.Stretch;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;

        if (spriteRenderer != null)
        {
            trail.sortingLayerID = spriteRenderer.sortingLayerID;
            trail.sortingOrder = spriteRenderer.sortingOrder - 1;
        }

        if (trail.sharedMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                trail.sharedMaterial = new Material(shader);
            }
        }

        ApplyTrailColor();
    }

    private void ApplyTrailColor()
    {
        if (trail == null)
        {
            return;
        }

        Color c = trailColor;
        if (spriteRenderer != null)
        {
            Color sc = spriteRenderer.color;
            c = new Color(
                Mathf.Lerp(sc.r, trailColor.r, 0.35f),
                Mathf.Lerp(sc.g, trailColor.g, 0.35f),
                Mathf.Lerp(sc.b, trailColor.b, 0.35f),
                trailColor.a);
        }

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(c, 0f),
                new GradientColorKey(c, 1f),
            },
            new[]
            {
                new GradientAlphaKey(c.a, 0f),
                new GradientAlphaKey(0f, 1f),
            });
        trail.colorGradient = gradient;
    }

    private void SpawnImpact(Vector3 worldPosition)
    {
        if (!enableImpact)
        {
            return;
        }

        if (impactPrefab != null)
        {
            GameObject fx = Instantiate(impactPrefab, worldPosition, Quaternion.identity);
            fx.transform.localScale = Vector3.one * impactScale;

            ParticleSystem[] systems = fx.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                systems[i].Play(true);
            }

            Destroy(fx, impactLife);
            return;
        }

        SpawnFallbackImpact(worldPosition);
    }

    private void SpawnFallbackImpact(Vector3 worldPosition)
    {
        GameObject go = new GameObject("ProjectileImpact");
        go.transform.position = worldPosition;

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        // AddComponent 직후 playOnAwake로 이미 재생 중 → duration 변경 전에 완전 정지
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        main.duration = 0.25f;
        main.loop = false;
        main.startLifetime = 0.2f;
        main.startSpeed = 1.8f;
        main.startSize = 0.08f;
        main.startColor = trailColor;
        main.gravityModifier = 0f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 24;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 12) });

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.05f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(
            new[]
            {
                new GradientColorKey(trailColor, 0f),
                new GradientColorKey(trailColor, 1f),
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0f, 1f),
            });
        colorOverLifetime.color = fade;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            renderer.sharedMaterial = new Material(shader);
        }

        if (spriteRenderer != null)
        {
            renderer.sortingLayerID = spriteRenderer.sortingLayerID;
            renderer.sortingOrder = spriteRenderer.sortingOrder + 1;
        }

        ps.Play(true);
        Destroy(go, impactLife);
    }
}
