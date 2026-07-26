using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 타워 등급 링 오버레이. 스킨(행성)과 등급 표시를 분리한다.
/// 아트 없이 코드로 도넛 스프라이트를 생성해 캐시 — 링 전용 이미지가 생기면 교체.
/// </summary>
public sealed class TowerGradeRingView : MonoBehaviour
{
    private const string ChildName = "GradeRing";
    private const int TextureSize = 256;

    /// <summary>행성 bounds 대비 비율. 1보다 작으면 셀 밖으로 덜 튀어나감</summary>
    private const float DiameterScale = 0.92f;

    /// <summary>월드 지름 상한 (대략 1칸). EnergyCore처럼 bounds가 큰 스킨 대비</summary>
    private const float MaxWorldDiameter = 0.92f;

    private static readonly Dictionary<int, Sprite> ringSpriteCache =
        new Dictionary<int, Sprite>();

    private SpriteRenderer ringRenderer;

    /// <summary>등급별 색·두께(반지름 대비 비율)</summary>
    private static void GetGradeStyle(TowerGrade grade, out Color color, out float thickness)
    {
        switch (grade)
        {
            case TowerGrade.Grade2:
                color = new Color(0.30f, 0.85f, 1f);
                thickness = 0.08f;
                break;
            case TowerGrade.Grade3:
                color = new Color(1f, 0.80f, 0.25f);
                thickness = 0.10f;
                break;
            case TowerGrade.Grade4:
                color = new Color(1f, 0.70f, 0.10f);
                thickness = 0.14f;
                break;
            case TowerGrade.Grade5:
                color = new Color(1f, 0.95f, 0.65f);
                thickness = 0.18f;
                break;
            case TowerGrade.Grade1:
            default:
                color = new Color(0.75f, 0.75f, 0.78f);
                thickness = 0.055f;
                break;
        }
    }

    /// <summary>호스트(타워)에 링 child를 만들거나 갱신한다. Bind 시 호출.</summary>
    public static void Attach(GameObject host, TowerGrade grade)
    {
        if (host == null)
        {
            return;
        }

        SpriteRenderer hostRenderer = host.GetComponent<SpriteRenderer>();
        if (hostRenderer == null || hostRenderer.sprite == null)
        {
            return;
        }

        Transform child = host.transform.Find(ChildName);
        TowerGradeRingView view;
        if (child == null)
        {
            GameObject ringObject = new GameObject(ChildName);
            ringObject.transform.SetParent(host.transform, false);
            view = ringObject.AddComponent<TowerGradeRingView>();
            view.ringRenderer = ringObject.AddComponent<SpriteRenderer>();
        }
        else
        {
            view = child.GetComponent<TowerGradeRingView>();
            if (view == null)
            {
                view = child.gameObject.AddComponent<TowerGradeRingView>();
            }

            if (view.ringRenderer == null)
            {
                view.ringRenderer = child.GetComponent<SpriteRenderer>();
            }
        }

        view.ApplyGrade(grade, hostRenderer);
    }

    private void ApplyGrade(TowerGrade grade, SpriteRenderer hostRenderer)
    {
        GetGradeStyle(grade, out Color color, out float thickness);

        ringRenderer.sprite = GetRingSprite(thickness);
        ringRenderer.color = color;
        ringRenderer.sortingLayerID = hostRenderer.sortingLayerID;
        ringRenderer.sortingOrder = hostRenderer.sortingOrder + 1;

        // 월드 bounds 기준으로 맞추고, 칸 밖으로 안 나가게 상한
        float worldSpan = Mathf.Max(hostRenderer.bounds.size.x, hostRenderer.bounds.size.y);
        float desiredWorld = Mathf.Min(worldSpan * DiameterScale, MaxWorldDiameter);
        float parentScale = Mathf.Max(
            Mathf.Abs(hostRenderer.transform.lossyScale.x),
            Mathf.Abs(hostRenderer.transform.lossyScale.y),
            0.0001f);
        float localDiameter = desiredWorld / parentScale;

        transform.localPosition = Vector3.zero;
        transform.localScale = new Vector3(localDiameter, localDiameter, 1f);
    }

    /// <summary>두께 비율로 도넛 스프라이트 생성·캐시 (PPU=TextureSize → 지름 1유닛)</summary>
    private static Sprite GetRingSprite(float thickness)
    {
        int key = Mathf.RoundToInt(thickness * 1000f);
        if (ringSpriteCache.TryGetValue(key, out Sprite cached) && cached != null)
        {
            return cached;
        }

        int size = TextureSize;
        float outer = size * 0.5f - 2f;
        float inner = outer * (1f - thickness * 2f);
        float center = size * 0.5f;

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = $"GradeRing_{key}",
            hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp
        };

        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - center;
                float dy = y + 0.5f - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // 1.5px 소프트 에지
                float alphaOuter = Mathf.Clamp01((outer - dist) / 1.5f);
                float alphaInner = Mathf.Clamp01((dist - inner) / 1.5f);
                byte alpha = (byte)(Mathf.Min(alphaOuter, alphaInner) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size);
        sprite.name = texture.name;
        sprite.hideFlags = HideFlags.HideAndDontSave;

        ringSpriteCache[key] = sprite;
        return sprite;
    }
}
