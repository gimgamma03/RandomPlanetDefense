using UnityEngine;

/// <summary>
/// 차징 중 타워/보스 위 HP바 스타일 게이지.
/// 호스트 회전에 묶이지 않도록 LateUpdate에서 월드 고정.
/// 호스트 localScale이 작아도(보스 0.13 등) 월드 크기는 유지.
/// </summary>
public sealed class TowerChargeGaugeView : MonoBehaviour
{
    private const string ChildName = "ChargeGauge";
    private const float DefaultBarWidth = 0.55f;
    private const float DefaultBarHeight = 0.08f;
    private const float DefaultYOffset = 0.55f;

    private Transform host;
    private Transform fillTransform;
    private SpriteRenderer fillRenderer;
    private SpriteRenderer backgroundRenderer;
    private bool built;
    private float yOffset = DefaultYOffset;
    private float barWidth = DefaultBarWidth;
    private float barHeight = DefaultBarHeight;

    public static void Show(GameObject hostObject)
    {
        Show(hostObject, DefaultYOffset, DefaultBarWidth, DefaultBarHeight);
    }

    public static void Show(GameObject hostObject, float worldYOffset)
    {
        Show(hostObject, worldYOffset, DefaultBarWidth, DefaultBarHeight);
    }

    public static void Show(
        GameObject hostObject,
        float worldYOffset,
        float width,
        float height)
    {
        TowerChargeGaugeView view = GetOrCreate(hostObject);
        if (view == null)
        {
            return;
        }

        view.yOffset = worldYOffset;
        view.barWidth = Mathf.Max(0.05f, width);
        view.barHeight = Mathf.Max(0.02f, height);
        view.gameObject.SetActive(true);
        view.ApplyBarSize();
        view.SetFill01(0f);
        view.SnapToHost();
    }

    /// <summary>스프라이트 상단 + padding 기준으로 게이지를 띄운다 (보스용).</summary>
    public static void ShowAboveSprite(
        GameObject hostObject,
        float padding = 0.25f,
        float width = 1.1f,
        float height = 0.14f)
    {
        float yOffset = DefaultYOffset;
        SpriteRenderer sr = hostObject != null
            ? hostObject.GetComponent<SpriteRenderer>()
            : null;
        if (sr != null && sr.enabled && sr.sprite != null)
        {
            yOffset = (sr.bounds.max.y - hostObject.transform.position.y) + padding;
            yOffset = Mathf.Max(0.4f, yOffset);
        }

        Show(hostObject, yOffset, width, height);
    }

    public static void Hide(GameObject hostObject)
    {
        if (hostObject == null)
        {
            return;
        }

        Transform child = hostObject.transform.Find(ChildName);
        if (child != null)
        {
            child.gameObject.SetActive(false);
        }
    }

    public static void SetFill(GameObject hostObject, float fill01)
    {
        TowerChargeGaugeView view = GetOrCreate(hostObject);
        if (view == null || !view.gameObject.activeSelf)
        {
            return;
        }

        view.SetFill01(fill01);
    }

    private static TowerChargeGaugeView GetOrCreate(GameObject hostObject)
    {
        if (hostObject == null)
        {
            return null;
        }

        Transform child = hostObject.transform.Find(ChildName);
        if (child == null)
        {
            GameObject gaugeObject = new GameObject(ChildName);
            gaugeObject.transform.SetParent(hostObject.transform, false);

            TowerChargeGaugeView view = gaugeObject.AddComponent<TowerChargeGaugeView>();
            view.host = hostObject.transform;
            view.BuildVisual(hostObject.GetComponent<SpriteRenderer>());
            view.SnapToHost();
            return view;
        }

        TowerChargeGaugeView existing = child.GetComponent<TowerChargeGaugeView>();
        if (existing == null)
        {
            existing = child.gameObject.AddComponent<TowerChargeGaugeView>();
            existing.host = hostObject.transform;
            existing.BuildVisual(hostObject.GetComponent<SpriteRenderer>());
        }
        else if (existing.host == null)
        {
            existing.host = hostObject.transform;
        }

        if (!existing.built)
        {
            existing.BuildVisual(hostObject.GetComponent<SpriteRenderer>());
        }

        return existing;
    }

    private void LateUpdate()
    {
        if (!isActiveAndEnabled || host == null)
        {
            return;
        }

        SnapToHost();
    }

    private void SnapToHost()
    {
        if (host == null)
        {
            return;
        }

        transform.position = host.position + Vector3.up * yOffset;
        transform.rotation = Quaternion.identity;

        // 부모(보스 visualScale 0.13 등) 스케일을 상쇄해 월드 크기 유지
        Vector3 lossy = host.lossyScale;
        float sx = Mathf.Abs(lossy.x) > 0.0001f ? 1f / lossy.x : 1f;
        float sy = Mathf.Abs(lossy.y) > 0.0001f ? 1f / lossy.y : 1f;
        transform.localScale = new Vector3(sx, sy, 1f);
    }

    private void BuildVisual(SpriteRenderer hostRenderer)
    {
        if (built)
        {
            return;
        }

        built = true;
        Sprite barSprite = CreateBarSprite();

        GameObject backgroundObject = new GameObject("Background");
        backgroundObject.transform.SetParent(transform, false);
        backgroundObject.transform.localPosition = Vector3.zero;
        backgroundRenderer = backgroundObject.AddComponent<SpriteRenderer>();
        backgroundRenderer.sprite = barSprite;
        backgroundRenderer.color = new Color(0.05f, 0.05f, 0.08f, 0.95f);
        backgroundRenderer.drawMode = SpriteDrawMode.Sliced;
        backgroundRenderer.size = new Vector2(barWidth, barHeight);
        ApplySorting(backgroundRenderer, hostRenderer, 0);

        GameObject fillObject = new GameObject("Fill");
        fillObject.transform.SetParent(transform, false);
        fillRenderer = fillObject.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = barSprite;
        fillRenderer.color = new Color(1f, 0.85f, 0.15f, 1f);
        fillRenderer.drawMode = SpriteDrawMode.Sliced;
        fillRenderer.size = new Vector2(0f, barHeight);
        ApplySorting(fillRenderer, hostRenderer, 1);

        fillTransform = fillObject.transform;
        fillTransform.localPosition = Vector3.zero;
        fillTransform.localScale = Vector3.one;
    }

    private void ApplyBarSize()
    {
        if (backgroundRenderer != null)
        {
            backgroundRenderer.size = new Vector2(barWidth, barHeight);
        }
    }

    private static void ApplySorting(SpriteRenderer renderer, SpriteRenderer hostRenderer, int orderOffset)
    {
        if (hostRenderer != null)
        {
            renderer.sortingLayerID = hostRenderer.sortingLayerID;
            // 보스 아웃라인/왕관보다 확실히 위
            renderer.sortingOrder = hostRenderer.sortingOrder + 40 + orderOffset;
        }
        else
        {
            renderer.sortingOrder = 50 + orderOffset;
        }
    }

    private void SetFill01(float fill01)
    {
        if (fillTransform == null || fillRenderer == null)
        {
            return;
        }

        float clamped = Mathf.Clamp01(fill01);
        float width = barWidth * clamped;

        fillRenderer.size = new Vector2(width, barHeight);
        fillTransform.localPosition = new Vector3((-barWidth + width) * 0.5f, 0f, 0f);
    }

    private static Sprite CreateBarSprite()
    {
        Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave
        };

        Color32 white = new Color32(255, 255, 255, 255);
        Color32[] pixels = new Color32[16];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = white;
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, 4f, 4f),
            new Vector2(0.5f, 0.5f),
            4f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(1f, 1f, 1f, 1f));
    }
}
