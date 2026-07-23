using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Slow : MonoBehaviour
{
    private float slow;

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
