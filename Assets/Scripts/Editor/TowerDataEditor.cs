using UnityEditor;
using UnityEngine;

/// <summary>
/// weaponType을 바꾸면 그 타입 전용 입력만 즉시 보여준다.
/// </summary>
[CustomEditor(typeof(TowerData))]
[CanEditMultipleObjects]
public sealed class TowerDataEditor : Editor
{
    private SerializedProperty towerId;
    private SerializedProperty displayName;
    private SerializedProperty weapon;
    private SerializedProperty weaponUpGradeValue;
    private SerializedProperty grade;
    private SerializedProperty weaponType;
    private SerializedProperty projectileType;
    private SerializedProperty sprite;
    private SerializedProperty spriteColor;
    private SerializedProperty spawnWeight;
    private SerializedProperty multiShotCount;
    private SerializedProperty multiShotSpreadAngle;
    private SerializedProperty groundBombCount;
    private SerializedProperty groundBombLineLength;
    private SerializedProperty groundBombSpawnInterval;
    private SerializedProperty laserWidth;

    private void OnEnable()
    {
        towerId = serializedObject.FindProperty("towerId");
        displayName = serializedObject.FindProperty("displayName");
        weapon = serializedObject.FindProperty("weapon");
        weaponUpGradeValue = serializedObject.FindProperty("weaponUpGradeValue");
        grade = serializedObject.FindProperty("grade");
        weaponType = serializedObject.FindProperty("weaponType");
        projectileType = serializedObject.FindProperty("projectileType");
        sprite = serializedObject.FindProperty("sprite");
        spriteColor = serializedObject.FindProperty("spriteColor");
        spawnWeight = serializedObject.FindProperty("spawnWeight");
        multiShotCount = serializedObject.FindProperty("multiShotCount");
        multiShotSpreadAngle = serializedObject.FindProperty("multiShotSpreadAngle");
        groundBombCount = serializedObject.FindProperty("groundBombCount");
        groundBombLineLength = serializedObject.FindProperty("groundBombLineLength");
        groundBombSpawnInterval = serializedObject.FindProperty("groundBombSpawnInterval");
        laserWidth = serializedObject.FindProperty("laserWidth");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(towerId);
        EditorGUILayout.PropertyField(displayName);
        EditorGUILayout.PropertyField(grade);

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(weaponType);
        bool typeChanged = EditorGUI.EndChangeCheck();

        WeaponType type = (WeaponType)weaponType.intValue;
        bool isSlow = type == WeaponType.Slow;

        EditorGUILayout.PropertyField(projectileType, new GUIContent("Projectile Type"));
        ProjectileType proj = (ProjectileType)projectileType.intValue;
        ProjectileType effective = proj == ProjectileType.Auto
            ? ProjectileTypeDefaults.FromWeapon(type)
            : proj;
        EditorGUILayout.HelpBox(
            proj == ProjectileType.Auto
                ? $"Auto → 실제 사용: {effective} (WeaponType 기본)"
                : $"사용: {effective}",
            MessageType.None);

        DrawWeaponBlock(isSlow);

        EditorGUILayout.Space(14f);
        DrawUpgradeBlock(isSlow);
        EditorGUILayout.Space(14f);

        EditorGUILayout.PropertyField(sprite);
        EditorGUILayout.PropertyField(spriteColor);
        EditorGUILayout.PropertyField(spawnWeight);

        DrawTypeSpecificFields(type);

        if (typeChanged)
        {
            Repaint();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawWeaponBlock(bool isSlow)
    {
        EditorGUILayout.LabelField("Weapon", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(weapon.FindPropertyRelative("damage"));
        EditorGUILayout.PropertyField(weapon.FindPropertyRelative("rate"));
        EditorGUILayout.PropertyField(weapon.FindPropertyRelative("range"));
        EditorGUILayout.PropertyField(weapon.FindPropertyRelative("sell"));
        EditorGUILayout.PropertyField(weapon.FindPropertyRelative("doubleShot"));
        if (isSlow)
        {
            EditorGUILayout.PropertyField(
                weapon.FindPropertyRelative("slowValue"),
                new GUIContent("Slow Value", "0.0 ~ 1.0"));
        }

        EditorGUI.indentLevel--;
    }

    private void DrawUpgradeBlock(bool isSlow)
    {
        EditorGUILayout.LabelField("Weapon Upgrade Value", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(weaponUpGradeValue.FindPropertyRelative("damage"));
        EditorGUILayout.PropertyField(weaponUpGradeValue.FindPropertyRelative("rate"));
        EditorGUILayout.PropertyField(weaponUpGradeValue.FindPropertyRelative("range"));
        if (isSlow)
        {
            EditorGUILayout.PropertyField(
                weaponUpGradeValue.FindPropertyRelative("slowValue"),
                new GUIContent("Slow Value"));
        }

        EditorGUI.indentLevel--;
    }

    private void DrawTypeSpecificFields(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.MultiWayShooting:
                EditorGUILayout.Space(8f);
                EditorGUILayout.LabelField("MultiWayShooting", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(multiShotCount, new GUIContent("Shot Count"));
                EditorGUILayout.PropertyField(multiShotSpreadAngle, new GUIContent("Spread Angle"));
                break;

            case WeaponType.Laser:
            case WeaponType.MultiLaser:
                EditorGUILayout.Space(8f);
                EditorGUILayout.LabelField("Laser", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(
                    laserWidth,
                    new GUIContent("Laser Width", "0이면 프리팹 LineRenderer 굵기 그대로"));
                if (laserWidth.floatValue <= 0f)
                {
                    EditorGUILayout.HelpBox("0 → 프리팹 값 사용", MessageType.None);
                }

                break;

            case WeaponType.GroundBombLine:
                EditorGUILayout.Space(8f);
                EditorGUILayout.LabelField("Ground Bomb Line", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(groundBombCount, new GUIContent("Bomb Count"));
                EditorGUILayout.PropertyField(
                    groundBombLineLength,
                    new GUIContent("Line Length", "타워에서 마지막 폭탄까지 거리"));
                EditorGUILayout.PropertyField(
                    groundBombSpawnInterval,
                    new GUIContent("Spawn Interval", "0이면 동시 설치, 값이 있으면 순차 설치"));
                break;
        }
    }
}
