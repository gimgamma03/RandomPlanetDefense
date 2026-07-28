using UnityEngine;

/// <summary>
/// 쉴드 활성 시 적 주위 원형 버블 (시안 네온 링).
/// </summary>
public sealed class EnemyShieldVisual : MonoBehaviour
{
    private const string RootName = "ShieldBubble";
    private const float Padding = 1.42f;

    private static readonly Color GlowTint = new Color(0.2f, 0.75f, 1f, 0.55f);
    private static readonly Color RingTint = new Color(0.85f, 0.98f, 1f, 0.95f);

    private Transform rootTransform;
    private SpriteRenderer glowRenderer;
    private SpriteRenderer ringRenderer;
    private static Sprite softFillSprite;
    private static Sprite hardRingSprite;

    public void Refresh(SpriteRenderer body, bool active)
    {
        if (!active)
        {
            if (rootTransform != null)
            {
                rootTransform.gameObject.SetActive(false);
            }

            return;
        }

        EnsureBuilt(body);
        FitToBody(body);
        rootTransform.gameObject.SetActive(true);
    }

    public void Clear()
    {
        if (rootTransform != null)
        {
            rootTransform.gameObject.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        if (rootTransform == null || !rootTransform.gameObject.activeSelf || ringRenderer == null)
        {
            return;
        }

        float pulse = 0.82f + 0.18f * (0.5f + 0.5f * Mathf.Sin(Time.time * 7f));

        Color ringColor = RingTint;
        ringColor.a = RingTint.a * pulse;
        ringRenderer.color = ringColor;

        if (glowRenderer != null)
        {
            Color glowColor = GlowTint;
            glowColor.a = GlowTint.a * (0.75f + 0.25f * pulse);
            glowRenderer.color = glowColor;
        }
    }

    private void EnsureBuilt(SpriteRenderer body)
    {
        if (rootTransform != null && glowRenderer != null && ringRenderer != null)
        {
            return;
        }

        Transform existing = transform.Find(RootName);
        if (existing != null)
        {
            Destroy(existing.gameObject);
        }

        GameObject rootGo = new GameObject(RootName);
        rootTransform = rootGo.transform;
        rootTransform.SetParent(transform, false);
        rootTransform.localPosition = Vector3.zero;
        rootTransform.localRotation = Quaternion.identity;
        rootTransform.localScale = Vector3.one;

        glowRenderer = CreateLayer("Glow", GetSoftFill(), GlowTint, body, 2);
        ringRenderer = CreateLayer("Ring", GetHardRing(), RingTint, body, 4);
    }

    private SpriteRenderer CreateLayer(
        string name,
        Sprite sprite,
        Color color,
        SpriteRenderer body,
        int orderOffset)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(rootTransform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.drawMode = SpriteDrawMode.Simple;

        if (body != null)
        {
            sr.sortingLayerID = body.sortingLayerID;
            sr.sortingOrder = body.sortingOrder + orderOffset;
        }
        else
        {
            sr.sortingOrder = orderOffset;
        }

        return sr;
    }

    private void FitToBody(SpriteRenderer body)
    {
        if (rootTransform == null || body == null || body.sprite == null)
        {
            return;
        }

        Bounds localBounds = body.sprite.bounds;
        float diameter = Mathf.Max(localBounds.size.x, localBounds.size.y) * Padding;
        diameter = Mathf.Clamp(diameter, 0.4f, 2.4f);

        rootTransform.localPosition = localBounds.center;
        rootTransform.localScale = Vector3.one * diameter;

        if (glowRenderer != null)
        {
            glowRenderer.transform.localScale = Vector3.one * 1.06f;
            glowRenderer.color = GlowTint;
        }

        if (ringRenderer != null)
        {
            ringRenderer.transform.localScale = Vector3.one;
            ringRenderer.color = RingTint;
        }
    }

    private static Sprite GetSoftFill()
    {
        if (softFillSprite != null)
        {
            return softFillSprite;
        }

        softFillSprite = BuildCircleSprite(96, 0.22f, 0.78f, 0.28f, 0.55f);
        return softFillSprite;
    }

    private static Sprite GetHardRing()
    {
        if (hardRingSprite != null)
        {
            return hardRingSprite;
        }

        hardRingSprite = BuildCircleSprite(
            128, 0.02f, 0.86f, 0.10f, 1.0f,
            0.92f, 0.14f, 0.7f, 1.35f);
        return hardRingSprite;
    }

    private static Sprite BuildCircleSprite(
        int size,
        float fillStrength,
        float ringCenter,
        float ringWidth,
        float ringStrength,
        float outerGlowCenter = -1f,
        float outerGlowWidth = 0f,
        float outerGlowStrength = 0f,
        float coreBoost = 1f)
    {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        float center = (size - 1) * 0.5f;
        float radius = center - 1.5f;
        Color32[] pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float n = dist / radius;

                if (n > 1.05f)
                {
                    pixels[y * size + x] = new Color32(255, 255, 255, 0);
                    continue;
                }

                float fill = Mathf.Clamp01(1f - n) * fillStrength;

                float ringValue = 0f;
                if (ringWidth > 0f)
                {
                    ringValue = Mathf.Clamp01(1f - Mathf.Abs(n - ringCenter) / ringWidth);
                    ringValue = ringValue * ringValue * ringStrength;
                }

                float outer = 0f;
                if (outerGlowWidth > 0f && outerGlowCenter > 0f)
                {
                    outer = Mathf.Clamp01(1f - Mathf.Abs(n - outerGlowCenter) / outerGlowWidth);
                    outer = outer * outer * outerGlowStrength;
                }

                float alpha = Mathf.Clamp01((fill + ringValue + outer) * coreBoost);
                byte a = (byte)Mathf.RoundToInt(alpha * 255f);
                byte c = (byte)Mathf.RoundToInt(Mathf.Lerp(210f, 255f, ringValue));
                pixels[y * size + x] = new Color32(c, c, 255, a);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }
}
