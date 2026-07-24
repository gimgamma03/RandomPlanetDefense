#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

/// <summary>
/// 메뉴 한 번으로 Tower Bases를 Addressables에 등록.
/// </summary>
public static class TowerAddressablesSetup
{
    private const string BasesFolder = "Assets/Prefabs/Towers/Bases";
    private const string GroupName = "TowerBases";
    private const string Label = TowerBaseLibrary.AddressablesLabel;

    [MenuItem("RPD/Addressables/1. Create Settings (if needed)")]
    private static void CreateSettings()
    {
        var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
        Debug.Log(settings != null
            ? "[RPD] AddressableAssetSettings ready."
            : "[RPD] Failed to create AddressableAssetSettings.");
    }

    [MenuItem("RPD/Addressables/2. Register Tower Base Prefabs")]
    private static void RegisterTowerBases()
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
        if (settings == null)
        {
            Debug.LogError("[RPD] No Addressable settings. Run menu 1 first / open Addressables Groups once.");
            return;
        }

        AddressableAssetGroup group = settings.FindGroup(GroupName);
        if (group == null)
        {
            group = settings.CreateGroup(GroupName, false, false, true, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
        }

        if (!settings.GetLabels().Contains(Label))
        {
            settings.AddLabel(Label);
        }

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { BasesFolder });
        int count = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);
            if (fileName == "TowerBase")
            {
                // 원형은 소환에 안 씀 — 원하면 주소만 달아둠
            }

            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, false, false);
            string address = $"Towers/Bases/{fileName}";
            entry.SetAddress(address);
            entry.SetLabel(Label, true, true);
            count++;
            Debug.Log($"[RPD] Addressable: {address}");
        }

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, null, true);
        AssetDatabase.SaveAssets();
        Debug.Log($"[RPD] Registered {count} base prefab(s) in group '{GroupName}', label '{Label}'.");
        Debug.Log("[RPD] Next: Window > Asset Management > Addressables > Groups 확인 후 Play. TowerSpawner에서 Use Addressables 체크.");
    }
}
#endif