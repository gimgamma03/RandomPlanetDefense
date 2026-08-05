#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// EnemyData: 역할별 관련 필드 + 스프라이트 미리보기.
/// </summary>
[CustomEditor(typeof(EnemyData))]
[CanEditMultipleObjects]
public sealed class EnemyDataEditor : Editor
{
    private SerializedProperty enemyId;
    private SerializedProperty enemyType;
    private SerializedProperty displayName;
    private SerializedProperty enemyRole;
    private SerializedProperty enemyTier;
    private SerializedProperty gold;
    private SerializedProperty scorePoint;
    private SerializedProperty maxHp;
    private SerializedProperty moveSpeed;
    private SerializedProperty rotateSpeed;
    private SerializedProperty visualScale;
    private SerializedProperty shieldHp;
    private SerializedProperty splitCount;
    private SerializedProperty splitChildType;
    private SerializedProperty sprite;
    private SerializedProperty spriteColor;
    private SerializedProperty isBoss;
    private SerializedProperty bossCrownSprite;
    private SerializedProperty enableSummonSkill;
    private SerializedProperty summonMinionType;
    private SerializedProperty summonCountMin;
    private SerializedProperty summonCountMax;
    private SerializedProperty summonChargeDuration;
    private SerializedProperty summonCastHoldDuration;
    private SerializedProperty summonChargeGaugeYOffset;

    private void OnEnable()
    {
        enemyId = serializedObject.FindProperty("enemyId");
        enemyType = serializedObject.FindProperty("enemyType");
        displayName = serializedObject.FindProperty("displayName");
        enemyRole = serializedObject.FindProperty("enemyRole");
        enemyTier = serializedObject.FindProperty("enemyTier");
        gold = serializedObject.FindProperty("gold");
        scorePoint = serializedObject.FindProperty("scorePoint");
        maxHp = serializedObject.FindProperty("maxHp");
        moveSpeed = serializedObject.FindProperty("moveSpeed");
        rotateSpeed = serializedObject.FindProperty("rotateSpeed");
        visualScale = serializedObject.FindProperty("visualScale");
        shieldHp = serializedObject.FindProperty("shieldHp");
        splitCount = serializedObject.FindProperty("splitCount");
        splitChildType = serializedObject.FindProperty("splitChildType");
        sprite = serializedObject.FindProperty("sprite");
        spriteColor = serializedObject.FindProperty("spriteColor");
        isBoss = serializedObject.FindProperty("isBoss");
        bossCrownSprite = serializedObject.FindProperty("bossCrownSprite");
        enableSummonSkill = serializedObject.FindProperty("enableSummonSkill");
        summonMinionType = serializedObject.FindProperty("summonMinionType");
        summonCountMin = serializedObject.FindProperty("summonCountMin");
        summonCountMax = serializedObject.FindProperty("summonCountMax");
        summonChargeDuration = serializedObject.FindProperty("summonChargeDuration");
        summonCastHoldDuration = serializedObject.FindProperty("summonCastHoldDuration");
        summonChargeGaugeYOffset = serializedObject.FindProperty("summonChargeGaugeYOffset");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EnemyRole role = (EnemyRole)enemyRole.intValue;

        EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.PropertyField(enemyId, new GUIContent("Enemy Id", "비우면 asset 이름"));
        EditorGUILayout.PropertyField(enemyType, new GUIContent("Enemy Type", "Stage 웨이브 드롭다운 키"));
        EditorGUILayout.PropertyField(displayName, new GUIContent("Display Name", "비우면 Id"));
        EditorGUILayout.PropertyField(enemyRole, new GUIContent("Role", "전투 역할"));
        EditorGUILayout.PropertyField(enemyTier, new GUIContent("Tier", "강도 1~3"));

        EnemyData data = target as EnemyData;
        if (data != null)
        {
            EditorGUILayout.HelpBox(
                $"Catalog: {data.enemyType} T{(int)data.enemyTier}  ·  Role: {data.enemyRole}  ·  Id: {data.Id}",
                MessageType.None);
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Stats", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.PropertyField(maxHp);
        EditorGUILayout.PropertyField(moveSpeed);
        EditorGUILayout.PropertyField(rotateSpeed);
        EditorGUILayout.PropertyField(gold);
        EditorGUILayout.PropertyField(scorePoint);
        EditorGUILayout.PropertyField(visualScale);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(8f);
        DrawRoleFields(role);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Visual", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.PropertyField(sprite);
        EditorGUILayout.PropertyField(spriteColor, new GUIContent("Sprite Color", "같은 스프라이트 변형용"));
        DrawSpritePreview();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Boss", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.PropertyField(isBoss, new GUIContent("Is Boss", "네온 실루엣 + 왕관"));
        if (isBoss.boolValue)
        {
            EditorGUILayout.HelpBox(
                "Is Boss면 본체 스프라이트 실루엣 네온.\nOrbit Crown: 킹 주위를 도는 보조 왕관.",
                MessageType.None);
            EditorGUILayout.PropertyField(
                bossCrownSprite,
                new GUIContent("Orbit Crown", "예: 노란 neon crown"));

            EditorGUILayout.Space(4f);
            EditorGUILayout.PropertyField(
                enableSummonSkill,
                new GUIContent("Summon Skill", "차징 → 정지 소환 → 재이동"));
            if (enableSummonSkill.boolValue)
            {
                EditorGUILayout.PropertyField(summonMinionType, new GUIContent("Minion Type"));
                EditorGUILayout.PropertyField(summonCountMin, new GUIContent("Count Min"));
                EditorGUILayout.PropertyField(summonCountMax, new GUIContent("Count Max"));
                EditorGUILayout.PropertyField(
                    summonChargeDuration,
                    new GUIContent("Charge Duration", "게이지 차는 시간(초)"));
                EditorGUILayout.PropertyField(
                    summonCastHoldDuration,
                    new GUIContent("Cast Hold", "소환 끝난 뒤 추가 정지(초)"));
                SerializedProperty summonInterval = serializedObject.FindProperty("summonInterval");
                if (summonInterval != null)
                {
                    EditorGUILayout.PropertyField(
                        summonInterval,
                        new GUIContent("Summon Interval", "한 마리씩 간격(초)"));
                }
                EditorGUILayout.PropertyField(
                    summonChargeGaugeYOffset,
                    new GUIContent("Gauge Y Offset"));
                EditorGUILayout.HelpBox(
                    "차징 중엔 이동 유지. 풀 차면 정지 후 0.5초마다 쫄 툭툭 소환.",
                    MessageType.None);
            }
        }

        EditorGUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawRoleFields(EnemyRole role)
    {
        switch (role)
        {
            case EnemyRole.Shielded:
                EditorGUILayout.LabelField("Shielded", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.PropertyField(
                    shieldHp,
                    new GUIContent("Shield Hp", "본체 HP보다 먼저 깎임"));
                EditorGUILayout.HelpBox("실드가 남아 있으면 본체 HP는 안 깎인다.", MessageType.None);
                EditorGUILayout.EndVertical();
                break;

            case EnemyRole.Splitter:
                EditorGUILayout.LabelField("Splitter", EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.PropertyField(splitCount, new GUIContent("Split Count"));
                EditorGUILayout.PropertyField(
                    splitChildType,
                    new GUIContent("Split Child Type", "보통 Swarm EnemyType"));
                EditorGUILayout.HelpBox("Kill 시에만 분열. Arrive·연쇄 분열(자식)은 안 함.", MessageType.None);
                EditorGUILayout.EndVertical();
                break;

            case EnemyRole.Runner:
                EditorGUILayout.HelpBox("Runner: 빠른 스탯 위주. 엘리트는 색·수치만 다른 SO.", MessageType.None);
                break;
            case EnemyRole.Tank:
                EditorGUILayout.HelpBox("Tank: 높은 HP·낮은 속도 권장.", MessageType.None);
                break;
            case EnemyRole.Swarm:
                EditorGUILayout.HelpBox("Swarm: 약한 다수. Splitter 잔해로도 자주 사용.", MessageType.None);
                break;
        }
    }

    private void DrawSpritePreview()
    {
        Sprite s = sprite.objectReferenceValue as Sprite;
        if (s == null || s.texture == null)
        {
            EditorGUILayout.HelpBox("스프라이트 없음 — Bind 시 Base 기본 비주얼", MessageType.Info);
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
