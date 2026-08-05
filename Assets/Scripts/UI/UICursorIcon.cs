using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스프라이트 없이 마우스 커서(포인터) 모양을 메시로 그리는 UI 그래픽.
/// 외곽선은 같은 폴리곤을 살짝 키워 먼저 그린다.
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class UICursorIcon : MaskableGraphic
{
    // 팁이 좌상단인 표준 포인터 실루엣 (x: 0~1, y: 0~-1)
    private static readonly Vector2[] Shape =
    {
        new Vector2(0f, 0f),
        new Vector2(0f, -1f),
        new Vector2(0.26f, -0.74f),
        new Vector2(0.42f, -1.06f),
        new Vector2(0.60f, -0.98f),
        new Vector2(0.44f, -0.66f),
        new Vector2(0.72f, -0.66f),
    };

    [SerializeField]
    private Color outlineColor = new Color(0.02f, 0.06f, 0.04f, 0.9f);

    [SerializeField]
    private float outlineWidth = 0.08f;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();
        float scale = Mathf.Min(rect.width, rect.height);
        Vector2 origin = new Vector2(rect.center.x - scale * 0.36f, rect.center.y + scale * 0.53f);

        if (outlineWidth > 0f && outlineColor.a > 0f)
        {
            AddShape(vh, origin, scale, outlineWidth, outlineColor);
        }

        AddShape(vh, origin, scale, 0f, color);
    }

    private void AddShape(VertexHelper vh, Vector2 origin, float scale, float expand, Color fill)
    {
        int baseIndex = vh.currentVertCount;

        Vector2 centroid = Vector2.zero;
        for (int i = 0; i < Shape.Length; i++)
        {
            centroid += Shape[i];
        }

        centroid /= Shape.Length;

        for (int i = 0; i < Shape.Length; i++)
        {
            Vector2 local = Shape[i];
            if (expand > 0f)
            {
                Vector2 outward = (local - centroid).normalized;
                local += outward * expand;
            }

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = fill;
            vertex.position = origin + new Vector2(local.x * scale, local.y * scale);
            vh.AddVert(vertex);
        }

        // 머리 부분
        vh.AddTriangle(baseIndex + 0, baseIndex + 1, baseIndex + 2);
        vh.AddTriangle(baseIndex + 0, baseIndex + 2, baseIndex + 6);
        // 꼬리 부분
        vh.AddTriangle(baseIndex + 2, baseIndex + 3, baseIndex + 4);
        vh.AddTriangle(baseIndex + 2, baseIndex + 4, baseIndex + 5);
    }
}
