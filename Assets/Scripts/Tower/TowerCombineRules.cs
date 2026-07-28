using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 합성 재료 수집 규칙.
/// 클릭한 타워를 앵커로, 같은 타입·같은 등급을 필요한 수만큼 모은다.
/// </summary>
public static class TowerCombineRules
{
    public static bool TryCollectMaterials(
        GameObject clickedTower,
        IReadOnlyList<GameObject> allTowers,
        out List<GameObject> materials,
        out TowerGrade nextGrade,
        out WeaponType weaponType)
    {
        materials = null;
        nextGrade = TowerGrade.Grade1;
        weaponType = default;

        if (clickedTower == null || allTowers == null)
        {
            return false;
        }

        TowerWeapon anchor = clickedTower.GetComponent<TowerWeapon>();
        if (anchor == null)
        {
            return false;
        }

        TowerGrade materialGrade = anchor.towerGrade;
        if ((int)materialGrade >= Constants.MaxTowerGrade)
        {
            Debug.Log($"[TowerCombine] Already max grade {Constants.MaxTowerGrade}.");
            return false;
        }

        materials = new List<GameObject>(Constants.towerCombineCount) { clickedTower };
        weaponType = anchor.weaponType;

        for (int i = 0; i < allTowers.Count; i++)
        {
            GameObject candidate = allTowers[i];
            if (candidate == null || candidate == clickedTower)
            {
                continue;
            }

            TowerWeapon other = candidate.GetComponent<TowerWeapon>();
            if (other == null)
            {
                continue;
            }

            if (other.weaponType != weaponType || other.towerGrade != materialGrade)
            {
                continue;
            }

            materials.Add(candidate);
            if (materials.Count >= Constants.towerCombineCount)
            {
                break;
            }
        }

        if (materials.Count < Constants.towerCombineCount)
        {
            materials = null;
            return false;
        }

        nextGrade = materialGrade + 1;
        return true;
    }
}
