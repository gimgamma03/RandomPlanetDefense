using System.Collections;
using UnityEngine;

/// <summary>
/// 타겟 방향으로 차징 후 관통 유성 발사. 유성 스프라이트는 Meteo 4종 중 랜덤.
/// </summary>
public sealed class ChargePierceBehavior : AttackBehaviorBase
{
    private const float MeteoScale = 0.55f;
    private const float MeteoSpinSpeed = 360f;
    private const float PostFireDelay = 0.2f;

    protected override IEnumerator AttackLoop()
    {
        while (true)
        {
            if (!Tower.IsPossibleToAttackTarget())
            {
                yield break;
            }

            float chargeDuration = Mathf.Max(0.35f, Tower.rate);
            float elapsed = 0f;
            TowerChargeGaugeView.Show(Tower.gameObject);

            while (elapsed < chargeDuration)
            {
                if (!Tower.IsPossibleToAttackTarget())
                {
                    TowerChargeGaugeView.Hide(Tower.gameObject);
                    yield break;
                }

                elapsed += Time.deltaTime;
                TowerChargeGaugeView.SetFill(Tower.gameObject, elapsed / chargeDuration);
                yield return null;
            }

            TowerChargeGaugeView.Hide(Tower.gameObject);

            if (Tower.IsPossibleToAttackTarget())
            {
                FireMeteor();
            }

            yield return new WaitForSeconds(PostFireDelay);
        }
    }

    protected override void OnAttackStopped()
    {
        TowerChargeGaugeView.Hide(Tower.gameObject);
    }

    private void FireMeteor()
    {
        Transform spawn = Tower.SpawnPoint;
        Transform target = Tower.AttackTarget;
        if (spawn == null || target == null || Tower.ProjectilePrefab == null)
        {
            return;
        }

        Vector3 direction = target.position - spawn.position;
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        GameObject clone = Tower.SpawnPooled(Tower.ProjectilePrefab, spawn.position, Quaternion.identity);
        if (clone == null)
        {
            return;
        }

        ApplyMeteoVisual(clone);

        Projectile projectile = clone.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.SetupPierce(direction.normalized, Tower.damage, MeteoSpinSpeed);
        }
    }

    private static void ApplyMeteoVisual(GameObject clone)
    {
        SpriteRenderer renderer = clone.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            return;
        }

        TowerVisualLibrary library = TowerVisualLibrary.Load();
        Sprite meteo = library != null ? library.PickRandomMeteo() : null;
        if (meteo != null)
        {
            renderer.sprite = meteo;
            renderer.color = Color.white;
        }

        clone.transform.localScale = Vector3.one * MeteoScale;

        ProjectileVfx vfx = clone.GetComponent<ProjectileVfx>();
        if (vfx != null)
        {
            vfx.SetTrailEnabled(false);
            vfx.RecaptureScale();
        }
    }
}
