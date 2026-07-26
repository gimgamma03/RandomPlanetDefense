using System.Collections;
using UnityEngine;

public sealed class LaserBehavior : AttackBehaviorBase
{
    protected override IEnumerator AttackLoop()
    {
        EnableLaser(true);

        while (true)
        {
            if (!Tower.IsPossibleToAttackTarget())
            {
                EnableLaser(false);
                yield break;
            }

            SpawnLaser();
            yield return null;
        }
    }

    protected override void OnAttackStopped()
    {
        EnableLaser(false);
    }

    private void EnableLaser(bool enabled)
    {
        LineRenderer line = Tower.LineRenderer;
        if (line != null)
        {
            line.gameObject.SetActive(enabled);
        }

        if (enabled)
        {
            ApplyWidth();
        }
    }

    /// <summary>TowerData의 굵기를 적용. 0이면 프리팹 값을 건드리지 않는다.</summary>
    private void ApplyWidth()
    {
        float width = Tower.LaserWidth;
        if (width <= 0f)
        {
            return;
        }

        SetWidth(Tower.LineRenderer, width);
        SetWidth(Tower.LineRenderer2, width);
        SetWidth(Tower.LineRenderer3, width);
    }

    private static void SetWidth(LineRenderer line, float width)
    {
        if (line == null)
        {
            return;
        }

        line.widthMultiplier = 1f;
        line.startWidth = width;
        line.endWidth = width;

        // 각진 사각형 → 둥근 캡슐형 빔. 카메라를 향하게 정렬.
        line.numCapVertices = 8;
        line.numCornerVertices = 4;
        line.alignment = LineAlignment.View;
        line.textureMode = LineTextureMode.Stretch;
    }

    private void SpawnLaser()
    {
        Transform target = Tower.AttackTarget;
        Transform spawn = Tower.SpawnPoint;
        LineRenderer line = Tower.LineRenderer;
        if (target == null || spawn == null || line == null)
        {
            return;
        }

        Vector3 direction = target.position - spawn.position;
        RaycastHit2D[] hits = Physics2D.RaycastAll(spawn.position, direction, Tower.range);

        for (int i = 0; i < hits.Length; ++i)
        {
            if (hits[i].transform != target)
            {
                continue;
            }

            // 양 끝 Z가 다르면 굵은 LineRenderer가 3D 판처럼 비틀려 보인다.
            // 같은 평면에 두고 Sorting Order로 앞뒤를 정한다.
            float beamZ = spawn.position.z;
            Vector3 start = new Vector3(spawn.position.x, spawn.position.y, beamZ);
            Vector3 end = new Vector3(hits[i].point.x, hits[i].point.y, beamZ);

            line.positionCount = 2;
            line.useWorldSpace = true;
            line.SetPosition(0, start);
            line.SetPosition(1, end);

            EnemyHp hp = target.GetComponent<EnemyHp>();
            if (hp != null)
            {
                hp.TakeDamage(Tower.damage * Time.deltaTime);
            }

            break;
        }
    }
}