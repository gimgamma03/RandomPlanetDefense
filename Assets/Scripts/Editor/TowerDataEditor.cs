#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// TowerData: 섹션 정리 + WeaponType별 관련 필드만.
/// 발사체/폭탄 규칙은 아직 확정 전이라, 요약 HelpBox로만 안내하고 과설계는 안 함.
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

        WeaponType type = (WeaponType)weaponType.intValue;
        ProjectileType proj = (ProjectileType)projectileType.intValue;
        ProjectileType effective = proj == ProjectileType.Auto
            ? ProjectileTypeDefaults.FromWeapon(type)
            : proj;

        DrawIdentity();
        EditorGUILayout.Space(8f);

        EditorGUI.BeginChangeCheck();
        DrawCombatHeader(type, proj, effective);
        bool typeChanged = EditorGUI.EndChangeCheck();

        EditorGUILayout.Space(8f);
        DrawWeaponStats(type);
        EditorGUILayout.Space(8f);
        DrawUpgradeStats(type);
        EditorGUILayout.Space(8f);
        DrawTypeSpecificFields(type);
        EditorGUILayout.Space(8f);
        DrawVisual();

        if (typeChanged)
        {
            Repaint();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawIdentity()
    {
        EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.PropertyField(towerId);
        EditorGUILayout.PropertyField(displayName);
        EditorGUILayout.PropertyField(grade);
        EditorGUILayout.PropertyField(spawnWeight, new GUIContent("Spawn Weight", "같은 등급 풀 상대 비중"));
        EditorGUILayout.EndVertical();
    }

    private void DrawCombatHeader(WeaponType type, ProjectileType proj, ProjectileType effective)
    {
        EditorGUILayout.LabelField("Combat", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.PropertyField(weaponType);

        // 발사체 쓰는 타입은 항상 노출. 오라/레이저 등은 Auto 유지가 기본이라 접힌 고급만.
        bool showProjectileField = typicallyFiresProjectile(type) || proj != ProjectileType.Auto;
        if (showProjectileField)
        {
            EditorGUILayout.PropertyField(projectileType, new GUIContent("Projectile Type"));
        }
        else
        {
            EditorGUILayout.LabelField("Projectile Type", "Auto (이 무기는 기본 발사체 없음)");
            EditorGUI.indentLevel++;
            EditorGUI.BeginChangeCheck();
            ProjectileType forced = (ProjectileType)EditorGUILayout.EnumPopup(
                new GUIContent("Override (고급)", "Behavior가 안 쓰면 무시될 수 있음"),
                proj);
            if (EditorGUI.EndChangeCheck())
            {
                projectileType.enumValueIndex = (int)forced;
            }

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.HelpBox(BuildDeliverySummary(type, proj, effective), MessageType.None);
        EditorGUILayout.EndVertical();
    }

    private static string BuildDeliverySummary(
        WeaponType type,
        ProjectileType proj,
        ProjectileType effective)
    {
        string delivery;
        switch (type)
        {
            case WeaponType.Slow:
                delivery = "전달: 오라(범위) — 발사체 스폰 없음";
                break;
            case WeaponType.Buff:
                delivery = "전달: 버프 — 발사체 스폰 없음";
                break;
            case WeaponType.Laser:
            case WeaponType.MultiLaser:
                delivery = "전달: 즉시 라인(Laser) — 발사체 스폰 없음";
                break;
            case WeaponType.ChainLightning:
                delivery = "전달: 체인 라이트닝 — 발사체 스폰 없음";
                break;
            case WeaponType.GroundBombLine:
                delivery = "전달: 지면 폭탄 일렬 설치";
                break;
            case WeaponType.Bomb:
                delivery = "전달: 날아가는 폭탄 발사체";
                break;
            case WeaponType.MultiWayShooting:
                delivery = "전달: 부채꼴 직진 발사체";
                break;
            case WeaponType.Cannon:
                delivery = "전달: 유도 발사체";
                break;
            default:
                delivery = "전달: (미분류 — Behavior 확인)";
                break;
        }

        string projLine = proj == ProjectileType.Auto
            ? $"Projectile: Auto → {effective}"
            : $"Projectile: {proj} (수동)";

        string note = string.Empty;
        if (type == WeaponType.GroundBombLine && effective != ProjectileType.GroundBomb && proj != ProjectileType.Auto)
        {
            note = "\n주의: GroundBombLine인데 Projectile이 GroundBomb이 아님 — Behavior/Library 확인";
        }
        else if (!typicallyFiresProjectile(type) && ProjectileTypeDefaults.UsesProjectile(effective))
        {
            note = "\n주의: 이 WeaponType Behavior는 보통 발사체를 안 씀. Override는 실험용";
        }

        return $"{delivery}\n{projLine}{note}";
    }

    private static bool typicallyFiresProjectile(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.Cannon:
            case WeaponType.Bomb:
            case WeaponType.MultiWayShooting:
            case WeaponType.GroundBombLine:
                return true;
            default:
                return false;
        }
    }

    private void DrawWeaponStats(WeaponType type)
    {
        bool isSlow = type == WeaponType.Slow;
        bool showDoubleShot = typicallyFiresProjectile(type) && type != WeaponType.GroundBombLine;

        EditorGUILayout.LabelField("Weapon Stats", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.PropertyField(weapon.FindPropertyRelative("damage"));
        EditorGUILayout.PropertyField(weapon.FindPropertyRelative("rate"));
        EditorGUILayout.PropertyField(weapon.FindPropertyRelative("range"));
        EditorGUILayout.PropertyField(weapon.FindPropertyRelative("sell"));

        if (showDoubleShot)
        {
            EditorGUILayout.PropertyField(weapon.FindPropertyRelative("doubleShot"));
        }

        if (isSlow)
        {
            EditorGUILayout.PropertyField(
                weapon.FindPropertyRelative("slowValue"),
                new GUIContent("Slow Value", "0.0 ~ 1.0"));
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawUpgradeStats(WeaponType type)
    {
        bool isSlow = type == WeaponType.Slow;

        EditorGUILayout.LabelField("Upgrade (레벨업 증가분)", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.PropertyField(weaponUpGradeValue.FindPropertyRelative("damage"));
        EditorGUILayout.PropertyField(weaponUpGradeValue.FindPropertyRelative("rate"));
        EditorGUILayout.PropertyField(weaponUpGradeValue.FindPropertyRelative("range"));
        if (isSlow)
        {
            EditorGUILayout.PropertyField(
                weaponUpGradeValue.FindPropertyRelative("slowValue"),
                new GUIContent("Slow Value"));
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawTypeSpecificFields(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.MultiWayShooting:
                EditorGUILayout.LabelField("MultiWayShooting", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.PropertyField(multiShotCount, new GUIContent("Shot Count"));
                EditorGUILayout.PropertyField(multiShotSpreadAngle, new GUIContent("Spread Angle"));
                EditorGUILayout.EndVertical();
                break;

            case WeaponType.Laser:
            case WeaponType.MultiLaser:
                EditorGUILayout.LabelField("Laser", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.PropertyField(
                    laserWidth,
                    new GUIContent("Laser Width", "0이면 프리팹 LineRenderer 굵기 그대로"));
                if (laserWidth.floatValue <= 0f)
                {
                    EditorGUILayout.HelpBox("0 → 프리팹 값 사용", MessageType.None);
                }

                EditorGUILayout.EndVertical();
                break;

            case WeaponType.GroundBombLine:
                EditorGUILayout.LabelField("Ground Bomb Line", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.PropertyField(groundBombCount, new GUIContent("Bomb Count"));
                EditorGUILayout.PropertyField(
                    groundBombLineLength,
                    new GUIContent("Line Length", "타워에서 마지막 폭탄까지 거리"));
                EditorGUILayout.PropertyField(
                    groundBombSpawnInterval,
                    new GUIContent("Spawn Interval", "0이면 동시 설치, 값이 있으면 순차 설치"));
                EditorGUILayout.EndVertical();
                break;

            case WeaponType.Slow:
            case WeaponType.Buff:
            case WeaponType.ChainLightning:
            case WeaponType.Cannon:
            case WeaponType.Bomb:
                // 전용 수치 없음 — Combat 요약만으로 충분
                break;
        }
    }

    private void DrawVisual()
    {
        EditorGUILayout.LabelField("Visual", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.PropertyField(sprite);
        EditorGUILayout.PropertyField(spriteColor);
        DrawSpritePreview();
        EditorGUILayout.EndVertical();
    }

    private void DrawSpritePreview()
    {
        Sprite s = sprite.objectReferenceValue as Sprite;
        if (s == null)
        {
            EditorGUILayout.HelpBox("스프라이트 없음", MessageType.Info);
            return;
        }

        Color tint = spriteColor.colorValue;
        Rect rect = GUILayoutUtility.GetRect(64f, 64f, GUILayout.ExpandWidth(false));
        rect.width = 64f;
        rect.height = 64f;
        EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 1f));

        Texture2D tex = AssetPreview.GetAssetPreview(s) ?? s.texture;
        if (tex != null)
        {
            Color prev = GUI.color;
            GUI.color = tint.a > 0f ? tint : Color.white;
            GUI.DrawTexture(rect, tex, ScaleMode.ScaleToFit, true);
            GUI.color = prev;
        }
    }
}
#endif
