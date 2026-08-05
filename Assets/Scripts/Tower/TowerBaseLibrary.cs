using System;
using UnityEngine;

/// <summary>
/// 무기 타입 → 계열 베이스.
/// basePrefab = 직접 참조(기존/폴백), address = Addressables 주소(파일럿).
/// </summary>
[CreateAssetMenu(menuName = "RPD/Tower Base Library", fileName = "TowerBaseLibrary")]
public class TowerBaseLibrary : ScriptableObject
{
    public const string ResourcesName = "TowerBaseLibrary";
    public const string AddressablesLabel = "towers_base";

    [Serializable]
    public struct Entry
    {
        public WeaponType weaponType;
        [Tooltip("직접 참조. Addressables 끄거나 주소 비었을 때 사용")]
        public GameObject basePrefab;
        [Tooltip("예: Towers/Bases/CannonBase. 비우면 직접 참조만 사용")]
        public string address;
    }

    [SerializeField]
    private Entry[] entries;

    [SerializeField]
    private GameObject towerBasePrefab;

    public GameObject TowerBasePrefab => towerBasePrefab;
    public Entry[] Entries => entries;

    public static TowerBaseLibrary Load()
    {
        return Resources.Load<TowerBaseLibrary>(ResourcesName);
    }

    public bool TryGetEntry(WeaponType weaponType, out Entry entry)
    {
        entry = default;
        if (entries == null)
        {
            return false;
        }

        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].weaponType == weaponType)
            {
                entry = entries[i];
                return true;
            }
        }

        return false;
    }

    public GameObject GetBasePrefab(WeaponType weaponType)
    {
        return TryGetEntry(weaponType, out Entry entry) ? entry.basePrefab : null;
    }

    public string GetAddress(WeaponType weaponType)
    {
        return TryGetEntry(weaponType, out Entry entry) ? entry.address : null;
    }
}