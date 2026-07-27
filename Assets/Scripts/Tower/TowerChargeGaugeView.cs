using UnityEngine;

/// <summary>
/// 차징 중 타워 위 HP바 스타일 게이지.
/// 타워 회전에 묶이지 않도록 LateUpdate에서 월드 고정.
/// 가득 찼을 때 바가 타워 좌우 정중앙에 오도록 한다.
/// </summary>
public sealed class TowerChargeGaugeView : MonoBehaviour
{
    private const string ChildName = "ChargeGauge";
    private const float BarWidth = 0.55f;
    private const float BarHeight = 0.08f;
    private const float YOffset = 0.55f;

    private Transform host;
    private Transform fillTransform;
    private SpriteRenderer fillRenderer;
    private bool built;

    public static void Show(GameObject hostObject)
    {
        TowerChargeGaugeView view = GetOrCreate(hostObject);
        if (view == null)
        {
            return;
        }

        view.gameObject.SetActive(true);
        view.SetFill01(0f);
        view.SnapToHost();
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

        transform.position = host.position + Vector3.up * YOffset;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;
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
        SpriteRenderer background = backgroundObject.AddComponent<SpriteRenderer>();
        background.sprite = barSprite;
        background.color = new Color(0.08f, 0.08f, 0.1f, 0.9f);
        background.drawMode = SpriteDrawMode.Sliced;
        background.size = new Vector2(BarWidth, BarHeight);
        ApplySorting(background, hostRenderer, 0);

        GameObject fillObject = new GameObject("Fill");
        fillObject.transform.SetParent(transform, false);
        fillRenderer = fillObject.AddComponent<SpriteRenderer>();
        fillRenderer.sprite = barSprite;
        fillRenderer.color = new Color(1f, 0.72f, 0.18f, 0.95f);
        fillRenderer.drawMode = SpriteDrawMode.Sliced;
        fillRenderer.size = new Vector2(0f, BarHeight);
        ApplySorting(fillRenderer, hostRenderer, 1);

        fillTransform = fillObject.transform;
        fillTransform.localPosition = Vector3.zero;
        fillTransform.localScale = Vector3.one;
    }

    private static void ApplySorting(SpriteRenderer renderer, SpriteRenderer hostRenderer, int orderOffset)
    {
        if (hostRenderer != null)
        {
            renderer.sortingLayerID = hostRenderer.sortingLayerID;
            renderer.sortingOrder = hostRenderer.sortingOrder + 5 + orderOffset;
        }
        else
        {
            renderer.sortingOrder = 20 + orderOffset;
        }
    }

    private void SetFill01(float fill01)
    {
        if (fillTransform == null || fillRenderer == null)
        {
            return;
        }

        float clamped = Mathf.Clamp01(fill01);
        float width = BarWidth * clamped;

        // 왼쪽에서 차고, 가득 차면 배경과 같은 중앙 정렬
        fillRenderer.size = new Vector2(width, BarHeight);
        fillTransform.localPosition = new Vector3((-BarWidth + width) * 0.5f, 0f, 0f);
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

        // 피벗 중앙 — 가득 찬 바가 타워 좌우 대칭
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
