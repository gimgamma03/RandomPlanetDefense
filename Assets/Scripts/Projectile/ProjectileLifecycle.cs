using UnityEngine;

/// <summary>
/// 발사체 히트/미스 VFX + 풀 반환 공통 경로.
/// </summary>
public static class ProjectileLifecycle
{
    public static void Release(GameObject go, ProjectileVfx vfx, bool hit)
    {
        if (go == null)
        {
            return;
        }

        if (vfx != null)
        {
            if (hit)
            {
                vfx.NotifyHit(go.transform.position);
            }
            else
            {
                vfx.NotifyMiss();
            }
        }

        ReturnToPool(go);
    }

    public static void ReturnToPool(GameObject go)
    {
        if (go == null)
        {
            return;
        }

        PooledObject pooled = go.GetComponent<PooledObject>();
        if (pooled != null)
        {
            pooled.ReturnToPool();
            return;
        }

        if (ServiceLocator.TryGet(out IPoolService pool))
        {
            pool.Return(go);
            return;
        }

        Object.Destroy(go);
    }
}
