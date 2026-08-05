using System;
using UnityEngine;

/// <summary>
/// ProjectileType → 계열 Base 프리팹.
/// </summary>
[CreateAssetMenu(menuName = "RPD/Projectile Base Library", fileName = "ProjectileBaseLibrary")]
public class ProjectileBaseLibrary : ScriptableObject
{
    public const string ResourcesName = "ProjectileBaseLibrary";

    [Serializable]
    public struct Entry
    {
        public ProjectileType projectileType;
        public GameObject basePrefab;
    }

    [SerializeField]
    private Entry[] entries;

    public Entry[] Entries => entries;

    public static ProjectileBaseLibrary Load()
    {
        return Resources.Load<ProjectileBaseLibrary>(ResourcesName);
    }

    public bool TryGetEntry(ProjectileType projectileType, out Entry entry)
    {
        entry = default;
        if (entries == null || projectileType == ProjectileType.Auto || projectileType == ProjectileType.None)
        {
            return false;
        }

        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].projectileType == projectileType)
            {
                entry = entries[i];
                return entry.basePrefab != null;
            }
        }

        return false;
    }

    public GameObject GetBasePrefab(ProjectileType projectileType)
    {
        return TryGetEntry(projectileType, out Entry entry) ? entry.basePrefab : null;
    }
}
