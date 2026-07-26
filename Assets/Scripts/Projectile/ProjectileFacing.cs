using UnityEngine;

/// <summary>
/// 발사체 스프라이트 헤드가 +X(오른쪽)일 때 이동 방향으로 Z축 회전.
/// </summary>
public static class ProjectileFacing
{
    public static void FaceDirection(Transform transform, Vector2 direction)
    {
        if (transform == null || direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    public static void FacePoint(Transform transform, Vector3 worldPoint)
    {
        if (transform == null)
        {
            return;
        }

        FaceDirection(transform, (Vector2)(worldPoint - transform.position));
    }
}
