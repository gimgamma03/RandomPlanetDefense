using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 꺾은선 경로를 점선으로 그리고 끝에 삼각 화살촉을 붙이는 UI 그래픽.
/// 좌표는 이 RectTransform 로컬 기준.
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class UIDashedArrow : MaskableGraphic
{
    [SerializeField]
    private List<Vector2> points = new List<Vector2>();

    [SerializeField]
    private float dashLength = 14f;
    [SerializeField]
    private float gapLength = 10f;
    [SerializeField]
    private float thickness = 4f;

    [SerializeField]
    private bool drawArrowHead = true;
    [SerializeField]
    private float arrowLength = 26f;
    [SerializeField]
    private float arrowWidth = 22f;

    public void SetPoints(IList<Vector2> path)
    {
        points.Clear();
        if (path != null)
        {
            for (int i = 0; i < path.Count; i++)
            {
                points.Add(path[i]);
            }
        }

        SetVerticesDirty();
    }

    public void SetStyle(float dash, float gap, float lineThickness, float headLength, float headWidth)
    {
        dashLength = Mathf.Max(1f, dash);
        gapLength = Mathf.Max(0f, gap);
        thickness = Mathf.Max(0.5f, lineThickness);
        arrowLength = Mathf.Max(0f, headLength);
        arrowWidth = Mathf.Max(0f, headWidth);
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (points.Count < 2)
        {
            return;
        }

        float half = thickness * 0.5f;
        float step = dashLength + gapLength;

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector2 from = points[i];
            Vector2 to = points[i + 1];
            Vector2 delta = to - from;
            float length = delta.magnitude;
            if (length < 0.01f)
            {
                continue;
            }

            Vector2 dir = delta / length;
            Vector2 normal = new Vector2(-dir.y, dir.x) * half;

            // 마지막 구간은 화살촉 자리를 비워 둔다
            float usable = length;
            if (drawArrowHead && i == points.Count - 2)
            {
                usable = Mathf.Max(0f, length - arrowLength);
            }

            for (float traveled = 0f; traveled < usable; traveled += step)
            {
                float segment = Mathf.Min(dashLength, usable - traveled);
                Vector2 a = from + dir * traveled;
                Vector2 b = from + dir * (traveled + segment);
                AddQuad(vh, a - normal, a + normal, b + normal, b - normal);
            }
        }

        if (drawArrowHead)
        {
            AddArrowHead(vh);
        }
    }

    private void AddArrowHead(VertexHelper vh)
    {
        Vector2 tip = points[points.Count - 1];
        Vector2 previous = points[points.Count - 2];
        Vector2 delta = tip - previous;
        if (delta.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Vector2 dir = delta.normalized;
        Vector2 side = new Vector2(-dir.y, dir.x) * (arrowWidth * 0.5f);
        Vector2 back = tip - dir * arrowLength;

        int index = vh.currentVertCount;
        AddVertex(vh, tip);
        AddVertex(vh, back + side);
        AddVertex(vh, back - side);
        vh.AddTriangle(index, index + 1, index + 2);
    }

    private void AddQuad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        int index = vh.currentVertCount;
        AddVertex(vh, a);
        AddVertex(vh, b);
        AddVertex(vh, c);
        AddVertex(vh, d);
        vh.AddTriangle(index, index + 1, index + 2);
        vh.AddTriangle(index + 2, index + 3, index);
    }

    private void AddVertex(VertexHelper vh, Vector2 position)
    {
        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;
        vertex.position = position;
        vh.AddVert(vertex);
    }
}
