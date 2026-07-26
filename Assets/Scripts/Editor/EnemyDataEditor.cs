#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// EnemyData: 섹션 정리 + 스프라이트 미리보기.
/// </summary>
[CustomEditor(typeof(EnemyData))]
[CanEditMultipleObjects]
public sealed class EnemyDataEditor : Editor
{
    private SerializedProperty enemyId;
    private SerializedProperty enemyType;
    private SerializedProperty displayName;
    private SerializedProperty gold;
    private SerializedProperty scorePoint;
    private SerializedProperty maxHp;
    private SerializedProperty moveSpeed;
    private SerializedProperty rotateSpeed;
    private SerializedProperty sprite;
    private SerializedProperty spriteColor;

    private void OnEnable()
    {
        enemyId = serializedObject.FindProperty("enemyId");
        enemyType = serializedObject.FindProperty("enemyType");
        displayName = serializedObject.FindProperty("displayName");
        gold = serializedObject.FindProperty("gold");
        scorePoint = serializedObject.FindProperty("scorePoint");
        maxHp = serializedObject.FindProperty("maxHp");
        moveSpeed = serializedObject.FindProperty("moveSpeed");
        rotateSpeed = serializedObject.FindProperty("rotateSpeed");
        sprite = serializedObject.FindProperty("sprite");
        spriteColor = serializedObject.FindProperty("spriteColor");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Identity", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.PropertyField(enemyId, new GUIContent("Enemy Id", "비우면 asset 이름"));
        EditorGUILayout.PropertyField(enemyType, new GUIContent("Enemy Type", "Stage 웨이브 드롭다운 키"));
        EditorGUILayout.PropertyField(displayName, new GUIContent("Display Name", "비우면 Id"));

        EnemyData data = target as EnemyData;
        if (data != null)
        {
            EditorGUILayout.HelpBox(
                $"Catalog 키: {data.enemyType}  ·  Id: {data.Id}  ·  표시: {data.DisplayName}",
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
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Visual", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.PropertyField(sprite);
        EditorGUILayout.PropertyField(spriteColor);
        DrawSpritePreview();
        EditorGUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
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
