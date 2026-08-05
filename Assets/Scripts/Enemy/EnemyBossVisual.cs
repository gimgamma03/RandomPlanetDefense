using UnityEngine;

/// <summary>
/// 보스: 본체 실루엣 네온 + (옵션) 보조 왕관 궤도.
/// </summary>
public sealed class EnemyBossVisual : MonoBehaviour
{
    private const string RootName = "BossVisual";
    private const string OutlineCyanName = "OutlineCyan";
    private const string OutlineMagentaName = "OutlineMagenta";
    private const string OrbitPivotName = "CrownOrbitPivot";

    private const int OrbitCrownCount = 1;
    private const float OrbitSpeedDegrees = 48f;
    private const float OrbitRadiusVsBody = 0.72f;
    private const float OrbitCrownHeightVsBody = 0.38f;

    /// <summary>일반 적(1) / 타워(2) / 탄(3)보다 위. UI·오버레이(49+)보다는 아래.</summary>
    public const int BossBodySortingOrder = 25;
    public const int DefaultBodySortingOrder = 1;

    private static readonly Color Cyan = new Color(0.35f, 0.85f, 1f, 0.85f);
    private static readonly Color Magenta = new Color(0.95f, 0.25f, 1f, 0.9f);

    private Transform visualRoot;
    private SpriteRenderer boundBody;

    public void Apply(SpriteRenderer body, Sprite orbitCrownSprite)
    {
        Clear();
        if (body == null || body.sprite == null)
        {
            return;
        }

        boundBody = body;
        body.sortingOrder = BossBodySortingOrder;

        GameObject root = new GameObject(RootName);
        visualRoot = root.transform;
        visualRoot.SetParent(transform, false);
        visualRoot.localPosition = Vector3.zero;
        visualRoot.localRotation = Quaternion.identity;
        visualRoot.localScale = Vector3.one;

        CreateOutline(OutlineCyanName, body, Cyan, 1.18f, body.sortingOrder - 2);
        CreateOutline(OutlineMagentaName, body, Magenta, 1.08f, body.sortingOrder - 1);

        if (orbitCrownSprite != null)
        {
            CreateOrbitCrowns(body, orbitCrownSprite);
        }
    }

    public void Clear()
    {
        if (boundBody != null)
        {
            boundBody.sortingOrder = DefaultBodySortingOrder;
            boundBody = null;
        }

        if (visualRoot != null)
        {
            Destroy(visualRoot.gameObject);
            visualRoot = null;
            return;
        }

        Transform existing = transform.Find(RootName);
        if (existing != null)
        {
            Destroy(existing.gameObject);
        }
    }

    private void CreateOutline(
        string name,
        SpriteRenderer body,
        Color color,
        float scale,
        int sortingOrder)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(visualRoot, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one * scale;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = body.sprite;
        sr.color = color;
        sr.sortingLayerID = body.sortingLayerID;
        sr.sortingOrder = sortingOrder;
    }

    private void CreateOrbitCrowns(SpriteRenderer body, Sprite crownSprite)
    {
        GameObject pivotObject = new GameObject(OrbitPivotName);
        Transform pivot = pivotObject.transform;
        pivot.SetParent(visualRoot, false);
        pivot.localPosition = Vector3.zero;
        pivot.localRotation = Quaternion.identity;
        pivot.localScale = Vector3.one;

        OrbitSatellitePivot spin = pivotObject.AddComponent<OrbitSatellitePivot>();
        spin.DegreesPerSecond = OrbitSpeedDegrees;

        float bodyWorldH = Mathf.Max(0.01f, body.bounds.size.y);
        float parentScale = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.y));
        float radiusLocal = (bodyWorldH * OrbitRadiusVsBody) / parentScale;

        float crownNativeH = Mathf.Max(0.01f, crownSprite.bounds.size.y);
        float crownLocalScale =
            (bodyWorldH * OrbitCrownHeightVsBody) / (crownNativeH * parentScale);

        for (int i = 0; i < OrbitCrownCount; i++)
        {
            float angle = (Mathf.PI * 2f / OrbitCrownCount) * i;
            Vector3 localPos = new Vector3(
                Mathf.Cos(angle) * radiusLocal,
                Mathf.Sin(angle) * radiusLocal,
                0f);

            GameObject crown = new GameObject($"OrbitCrown_{i + 1}");
            crown.transform.SetParent(pivot, false);
            crown.transform.localPosition = localPos;
            crown.transform.localScale = Vector3.one * crownLocalScale;

            SpriteRenderer sr = crown.AddComponent<SpriteRenderer>();
            sr.sprite = crownSprite;
            sr.color = Color.white;
            sr.sortingLayerID = body.sortingLayerID;
            sr.sortingOrder = body.sortingOrder + 10;

            crown.AddComponent<BossOrbitCrownUpright>();
        }
    }
}
