using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 궤도 위성 가시성용 테두리 링. TowerGradeRingView와 같은 도넛 스프라이트 방식.
/// </summary>
public sealed class SatelliteOutlineView : MonoBehaviour
{
    private const string ChildName = "SatelliteOutline";
    private const int TextureSize = 128;
    private const float DiameterScale = 1.08f;
    private const float Thickness = 0.12f;

    private static readonly Color OutlineColor = new Color(0.75f, 0.95f, 1f, 0.95f);
    private static Sprite cachedRing;

    /// <summary>위성 본체 SpriteRenderer 기준으로 테두리 child 생성·갱신.</summary>
    public static void Attach(GameObject host)
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
        SatelliteOutlineView view;
        if (child == null)
        {
            GameObject ringObject = new GameObject(ChildName);
            ringObject.transform.SetParent(host.transform, false);
            view = ringObject.AddComponent<SatelliteOutlineView>();
            view.Apply(hostRenderer);
        }
        else
        {
            view = child.GetComponent<SatelliteOutlineView>();
            if (view == null)
            {
                view = child.gameObject.AddComponent<SatelliteOutlineView>();
            }

            view.Apply(hostRenderer);
        }
    }

    private void Apply(SpriteRenderer hostRenderer)
    {
        SpriteRenderer ringRenderer = GetComponent<SpriteRenderer>();
        if (ringRenderer == null)
        {
            ringRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        ringRenderer.sprite = GetRingSprite();
        ringRenderer.color = OutlineColor;
        ringRenderer.sortingLayerID = hostRenderer.sortingLayerID;
        ringRenderer.sortingOrder = hostRenderer.sortingOrder - 1;

        float worldSpan = Mathf.Max(hostRenderer.bounds.size.x, hostRenderer.bounds.size.y);
        float desiredWorld = worldSpan * DiameterScale;
        float parentScale = Mathf.Max(
            Mathf.Abs(hostRenderer.transform.lossyScale.x),
            Mathf.Abs(hostRenderer.transform.lossyScale.y),
            0.0001f);
        float localDiameter = desiredWorld / parentScale;

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = new Vector3(localDiameter, localDiameter, 1f);
    }

    private static Sprite GetRingSprite()
    {
        if (cachedRing != null)
        {
            return cachedRing;
        }

        int size = TextureSize;
        float outer = size * 0.5f - 1.5f;
        float inner = outer * (1f - Thickness * 2f);
        float center = size * 0.5f;

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "SatelliteOutline",
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

                float alphaOuter = Mathf.Clamp01((outer - dist) / 1.2f);
                float alphaInner = Mathf.Clamp01((dist - inner) / 1.2f);
                byte alpha = (byte)(Mathf.Min(alphaOuter, alphaInner) * 255f);
                pixels[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);

        cachedRing = Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size);
        cachedRing.name = texture.name;
        cachedRing.hideFlags = HideFlags.HideAndDontSave;
        return cachedRing;
    }
}
