using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class TowerData : ScriptableObject
{
    public GameObject towerPrefab;
    public GameObject followTowerPrefab;    // 임시 타워 프리팹
    public Weapon weapon;             // 레벨별 타워(무기) 정보
    public WeaponUpGradeValue weaponUpGradeValue;
    public TowerGrade grade;
    public Sprite sprite;   // 보여지는 타워 이미지 (UI)


    [System.Serializable]
    public struct Weapon
    {
        public float damage;    // 공격력
        public float rate;  // 공격 속도
        public float range; // 공격 범위
        public int sell;    // 타워 판매 시 획득 골드
        public bool doubleShot;

        [Header("About SlowTower (0.0 ~ 1.0)")]
        public float slowValue;
    }

    [System.Serializable]
    public struct WeaponUpGradeValue
    {
        public float damage;    // 공격력
        public float rate;  // 공격 속도
        public float range; // 공격 범위

        [Header("About SlowTower")]
        public float slowValue;
    }


}
