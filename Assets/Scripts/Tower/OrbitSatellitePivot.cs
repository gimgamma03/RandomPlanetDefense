using UnityEngine;

/// <summary>
/// 위성 궤도 피벗 회전.
/// </summary>
public sealed class OrbitSatellitePivot : MonoBehaviour
{
    [SerializeField]
    private float degreesPerSecond = 55f;

    public float DegreesPerSecond
    {
        get => degreesPerSecond;
        set => degreesPerSecond = value;
    }

    private void Update()
    {
        transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime);
    }
}
