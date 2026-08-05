using UnityEngine;

/// <summary>궤도 피벗 회전과 무관하게 월드 upright 유지.</summary>
public sealed class BossOrbitCrownUpright : MonoBehaviour
{
    private void LateUpdate()
    {
        transform.rotation = Quaternion.identity;
    }
}
