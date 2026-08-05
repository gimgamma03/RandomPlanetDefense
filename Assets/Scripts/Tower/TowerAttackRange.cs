using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TowerAttackRange : MonoBehaviour
{
    public void OnAttackRange(Vector3 Position, float Range)
    {
        gameObject.SetActive(true);

        float Diameter = Range * 2.0f;
        
        transform.localScale = Vector3.one * Diameter;
        transform.position = Position;
    }

    public void OffAttackRange()
    {
        gameObject.SetActive(false);
    }
    // Start is called before the first frame update
    void Start()
    {
        gameObject.SetActive(false);
    }
}
