using UnityEngine;

/// <summary>
/// 배경 스프라이트를 왼쪽으로 흘리고, 카메라 왼쪽으로 완전히 나가면 오른쪽 끝으로 보낸다.
/// 타일 크기·간격은 카메라 화면을 덮도록 맞춘다. (20:9에서 별 하늘 틈 방지)
/// </summary>
public class ScrollImage : MonoBehaviour
{
    public float speed;
    public Transform[] backgrounds;

    const float CoverPadding = 1.08f;
    const float TileOverlap = 0.02f;

    float spriteWorldWidth;
    float wrapDistance;
    int lastScreenWidth;
    int lastScreenHeight;

    void Start()
    {
        FitToCameraAndLayout();
    }

    void Update()
    {
        if (backgrounds == null || backgrounds.Length == 0)
        {
            return;
        }

        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            FitToCameraAndLayout();
        }

        if (wrapDistance <= 0f)
        {
            return;
        }

        Camera cam = Camera.main;
        float camLeft = cam != null
            ? cam.transform.position.x - cam.orthographicSize * cam.aspect
            : -spriteWorldWidth;

        Vector3 delta = new Vector3(-speed, 0f, 0f) * Time.deltaTime;
        float halfWidth = spriteWorldWidth * 0.5f;

        for (int i = 0; i < backgrounds.Length; i++)
        {
            Transform tile = backgrounds[i];
            if (tile == null)
            {
                continue;
            }

            tile.position += delta;

            if (tile.position.x + halfWidth < camLeft)
            {
                Vector3 nextPos = tile.position;
                nextPos.x += wrapDistance;
                tile.position = nextPos;
            }
        }
    }

    private void FitToCameraAndLayout()
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        Sprite sprite = FindSprite();
        if (sprite == null)
        {
            CacheMetricsFallback();
            return;
        }

        Camera cam = Camera.main;
        float camHeight = cam != null ? cam.orthographicSize * 2f : 20f;
        float camWidth = cam != null ? camHeight * cam.aspect : camHeight * 16f / 9f;

        float localWidth = sprite.rect.width / sprite.pixelsPerUnit;
        float localHeight = sprite.rect.height / sprite.pixelsPerUnit;
        if (localWidth < 0.01f || localHeight < 0.01f)
        {
            CacheMetricsFallback();
            return;
        }

        float scaleY = (camHeight / localHeight) * CoverPadding;
        // 타일 2장이 화면 가로를 덮어야 랩 직후에도 틈이 안 난다.
        float minTileWorldWidth = (camWidth * 0.55f) * CoverPadding;
        float scaleX = Mathf.Max(scaleY, minTileWorldWidth / localWidth);
        transform.localScale = new Vector3(scaleX, scaleY, 1f);

        float localStep = localWidth * (1f - TileOverlap);
        int index = 0;
        for (int i = 0; i < backgrounds.Length; i++)
        {
            Transform tile = backgrounds[i];
            if (tile == null)
            {
                continue;
            }

            tile.localPosition = new Vector3((index - 1) * localStep, 0f, 0f);
            index++;
        }

        spriteWorldWidth = localWidth * scaleX;
        wrapDistance = localStep * scaleX * CountTiles();
    }

    private Sprite FindSprite()
    {
        if (backgrounds == null)
        {
            return null;
        }

        for (int i = 0; i < backgrounds.Length; i++)
        {
            if (backgrounds[i] == null)
            {
                continue;
            }

            SpriteRenderer spriteRenderer = backgrounds[i].GetComponent<SpriteRenderer>();
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                return spriteRenderer.sprite;
            }
        }

        return null;
    }

    private int CountTiles()
    {
        int count = 0;
        for (int i = 0; i < backgrounds.Length; i++)
        {
            if (backgrounds[i] != null)
            {
                count++;
            }
        }

        return Mathf.Max(count, 1);
    }

    private void CacheMetricsFallback()
    {
        spriteWorldWidth = MeasureSpriteWorldWidth();
        if (spriteWorldWidth < 0.01f)
        {
            spriteWorldWidth = 32f;
        }

        wrapDistance = spriteWorldWidth * CountTiles();
    }

    private float MeasureSpriteWorldWidth()
    {
        Sprite sprite = FindSprite();
        if (sprite == null)
        {
            return 0f;
        }

        Transform tile = null;
        for (int i = 0; i < backgrounds.Length; i++)
        {
            if (backgrounds[i] != null)
            {
                tile = backgrounds[i];
                break;
            }
        }

        if (tile == null)
        {
            return 0f;
        }

        float localWidth = sprite.rect.width / sprite.pixelsPerUnit;
        return localWidth * Mathf.Abs(tile.lossyScale.x);
    }
}
