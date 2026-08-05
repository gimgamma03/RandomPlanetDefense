using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateObject : MonoBehaviour
{
    [SerializeField]
    private float RotateSpeed = 0.1f;

    public void SetSpeed(float speed)
    {
        RotateSpeed = speed;
    }

    void Update()
    {
        transform.Rotate(Vector3.forward * -RotateSpeed);
    }
}
