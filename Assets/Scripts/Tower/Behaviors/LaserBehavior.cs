using System.Collections;
using UnityEngine;

public sealed class LaserBehavior : AttackBehaviorBase
{
    private const int MultiBeamCount = 3;

    private readonly Transform[] multiTargets = new Transform[MultiBeamCount];
    private bool multiTarget;

    public override void Initialize(TowerWeapon tower)
    {
        base.Initialize(tower);
        multiTarget = tower.weaponType == WeaponType.MultiLaser
            || tower.towerGrade >= TowerBehaviorFactory.PassiveUnlockGrade;

        if (multiTarget)
        {
            tower.EnsureMultiLaserLines();
        }
    }

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

            if (multiTarget)
            {
                SpawnMultiLaser();
            }
            else
            {
                SpawnLaser(Tower.AttackTarget, Tower.LineRenderer);
            }

            yield return null;
        }
    }

    protected override void OnAttackStopped()
    {
        EnableLaser(false);
    }

    private void EnableLaser(bool enabled)
    {
        int beamCount = multiTarget ? MultiBeamCount : 1;
        for (int i = 0; i < beamCount; i++)
        {
            LineRenderer line = Tower.GetLaserLine(i);
            if (line != null)
            {
                line.gameObject.SetActive(enabled);
            }
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

        int beamCount = multiTarget ? MultiBeamCount : 1;
        for (int i = 0; i < beamCount; i++)
        {
            SetWidth(Tower.GetLaserLine(i), width);
        }
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

    private void SpawnMultiLaser()
    {
        int count = Tower.CollectClosestAttackTargets(multiTargets);
        if (count <= 0)
        {
            return;
        }

        for (int i = 0; i < MultiBeamCount; i++)
        {
            LineRenderer line = Tower.GetLaserLine(i);
            Transform target = i < count ? multiTargets[i] : null;

            if (target == null || line == null)
            {
                if (line != null)
                {
                    line.gameObject.SetActive(false);
                }

                continue;
            }

            line.gameObject.SetActive(true);
            SpawnLaser(target, line);
        }
    }

    private void SpawnLaser(Transform target, LineRenderer line)
    {
        Transform spawn = Tower.SpawnPoint;
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
