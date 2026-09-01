using UnityEngine;

/// <summary>
/// 배경 스프라이트를 왼쪽으로 흘리고, 카메라 왼쪽으로 완전히 나가면 오른쪽 끝으로 보낸다.
/// 랩 거리는 화면 폭이 아니라 타일 실제 폭을 쓴다.
/// </summary>
public class ScrollImage : MonoBehaviour
{
    public float speed;
    public Transform[] backgrounds;

    float spriteWorldWidth;
    float wrapDistance;

    void Start()
    {
        CacheMetrics();
    }

    void Update()
    {
        if (backgrounds == null || backgrounds.Length == 0 || wrapDistance <= 0f)
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

            // 중심이 아니라 오른쪽 끝이 카메라 왼쪽보다 왼쪽일 때만 랩한다.
            if (tile.position.x + halfWidth < camLeft)
            {
                Vector3 nextPos = tile.position;
                nextPos.x += wrapDistance;
                tile.position = nextPos;
            }
        }
    }

    private void CacheMetrics()
    {
        spriteWorldWidth = MeasureSpriteWorldWidth();
        float spacing = MeasureTileSpacing();
        float tileWidth = Mathf.Max(spriteWorldWidth, spacing);
        if (tileWidth < 0.01f)
        {
            tileWidth = 32f;
        }

        spriteWorldWidth = Mathf.Max(spriteWorldWidth, tileWidth);
        wrapDistance = tileWidth * backgrounds.Length;
    }

    private float MeasureSpriteWorldWidth()
    {
        if (backgrounds == null)
        {
            return 0f;
        }

        for (int i = 0; i < backgrounds.Length; i++)
        {
            Transform tile = backgrounds[i];
            if (tile == null)
            {
                continue;
            }

            SpriteRenderer spriteRenderer = tile.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null || spriteRenderer.sprite == null)
            {
                continue;
            }

            float localWidth = spriteRenderer.sprite.rect.width / spriteRenderer.sprite.pixelsPerUnit;
            float worldWidth = localWidth * Mathf.Abs(tile.lossyScale.x);
            if (worldWidth > 0.01f)
            {
                return worldWidth;
            }
        }

        return 0f;
    }

    private float MeasureTileSpacing()
    {
        if (backgrounds == null || backgrounds.Length < 2)
        {
            return 0f;
        }

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        int count = 0;
        for (int i = 0; i < backgrounds.Length; i++)
        {
            if (backgrounds[i] == null)
            {
                continue;
            }

            float x = backgrounds[i].position.x;
            if (x < minX)
            {
                minX = x;
            }

            if (x > maxX)
            {
                maxX = x;
            }

            count++;
        }

        if (count < 2)
        {
            return 0f;
        }

        return (maxX - minX) / (count - 1);
    }
}
