using UnityEngine;

public class Slow : MonoBehaviour
{
    private float slow;
    private SpriteRenderer rangeVisual;

    private void Awake()
    {
        CacheVisual();
        SetVisualVisible(false);
    }

    public void SetUp(float slow, float range)
    {
        this.slow = slow;
        SetRange(range);
    }

    public void SetRange(float range)
    {
        float diameter = range * 2.0f;
        transform.localScale = Vector3.one * diameter;
    }

    public void SetVisualVisible(bool visible)
    {
        CacheVisual();
        if (rangeVisual != null)
        {
            rangeVisual.enabled = visible;
        }
    }

    private void CacheVisual()
    {
        if (rangeVisual == null)
        {
            rangeVisual = GetComponent<SpriteRenderer>();
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!collision.CompareTag("Enemy"))
        {
            return;
        }

        Enemy enemy = collision.GetComponent<Enemy>();

        if (enemy != null && !(enemy.obstructed))
        {
            enemy.nextNodeMoveTime *= (1.0f + slow);
            enemy.obstructed = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Enemy"))
        {
            return;
        }

        Enemy enemy = collision.GetComponent<Enemy>();

        if (enemy != null)
        {
            enemy.ReSetSpeed();
            enemy.obstructed = false;
        }
    }
}
