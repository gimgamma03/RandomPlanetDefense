#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

/// <summary>
/// StageData: 웨이브를 하나씩 골라 편집 + spawnWeight → % 미리보기.
/// 기본 중첩 배열 인스펙터보다 덜 답답하게.
/// </summary>
[CustomEditor(typeof(StageData))]
public sealed class StageDataEditor : Editor
{
    private SerializedProperty stageId;
    private SerializedProperty displayName;
    private SerializedProperty clearBonusGold;
    private SerializedProperty waves;

    private int selectedWaveIndex;
    private ReorderableList enemyList;

    private void OnEnable()
    {
        stageId = serializedObject.FindProperty("stageId");
        displayName = serializedObject.FindProperty("displayName");
        clearBonusGold = serializedObject.FindProperty("clearBonusGold");
        waves = serializedObject.FindProperty("waves");

        selectedWaveIndex = Mathf.Clamp(selectedWaveIndex, 0, Mathf.Max(0, waves.arraySize - 1));
        RebuildEnemyList();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Stage", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(stageId);
        EditorGUILayout.PropertyField(displayName);
        EditorGUILayout.PropertyField(clearBonusGold);

        EditorGUILayout.Space(12f);
        DrawWaveToolbar();
        EditorGUILayout.Space(6f);

        if (waves.arraySize == 0)
        {
            EditorGUILayout.HelpBox("웨이브가 없습니다. [+ Wave]로 추가하세요.", MessageType.Info);
        }
        else
        {
            DrawSelectedWave();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawWaveToolbar()
    {
        int count = waves.arraySize;
        selectedWaveIndex = count == 0 ? 0 : Mathf.Clamp(selectedWaveIndex, 0, count - 1);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Waves", EditorStyles.boldLabel, GUILayout.Width(50f));

        using (new EditorGUI.DisabledScope(count == 0 || selectedWaveIndex <= 0))
        {
            if (GUILayout.Button("◀", GUILayout.Width(28f)))
            {
                selectedWaveIndex--;
                RebuildEnemyList();
                GUI.FocusControl(null);
            }
        }

        string label = count == 0
            ? "— / —"
            : $"Wave {selectedWaveIndex + 1} / {count}";
        EditorGUILayout.LabelField(label, EditorStyles.centeredGreyMiniLabel, GUILayout.MinWidth(90f));

        using (new EditorGUI.DisabledScope(count == 0 || selectedWaveIndex >= count - 1))
        {
            if (GUILayout.Button("▶", GUILayout.Width(28f)))
            {
                selectedWaveIndex++;
                RebuildEnemyList();
                GUI.FocusControl(null);
            }
        }

        if (GUILayout.Button("+ Wave", GUILayout.Width(64f)))
        {
            int insertAt = count == 0 ? 0 : selectedWaveIndex + 1;
            waves.InsertArrayElementAtIndex(insertAt);
            SerializedProperty wave = waves.GetArrayElementAtIndex(insertAt);
            wave.FindPropertyRelative("spawnDelay").floatValue = 1f;
            wave.FindPropertyRelative("maxEnemyCount").intValue = 10;
            SerializedProperty enemies = wave.FindPropertyRelative("enemies");
            enemies.ClearArray();
            enemies.arraySize = 1;
            enemies.GetArrayElementAtIndex(0).FindPropertyRelative("enemyType").enumValueIndex = 0;
            enemies.GetArrayElementAtIndex(0).FindPropertyRelative("spawnWeight").floatValue = 1f;
            selectedWaveIndex = insertAt;
            RebuildEnemyList();
            GUI.FocusControl(null);
        }

        using (new EditorGUI.DisabledScope(count == 0))
        {
            if (GUILayout.Button("Dup", GUILayout.Width(40f)))
            {
                waves.InsertArrayElementAtIndex(selectedWaveIndex);
                // InsertArrayElementAtIndex duplicates the element at index into index+1 in Unity
                selectedWaveIndex++;
                RebuildEnemyList();
                GUI.FocusControl(null);
            }

            if (GUILayout.Button("Del", GUILayout.Width(40f)))
            {
                waves.DeleteArrayElementAtIndex(selectedWaveIndex);
                selectedWaveIndex = Mathf.Clamp(selectedWaveIndex, 0, Mathf.Max(0, waves.arraySize - 1));
                RebuildEnemyList();
                GUI.FocusControl(null);
            }
        }

        EditorGUILayout.EndHorizontal();

        if (count > 1)
        {
            EditorGUI.BeginChangeCheck();
            int jumped = EditorGUILayout.IntSlider("Jump to wave", selectedWaveIndex + 1, 1, count);
            if (EditorGUI.EndChangeCheck())
            {
                selectedWaveIndex = jumped - 1;
                RebuildEnemyList();
                GUI.FocusControl(null);
            }
        }
    }

    private void DrawSelectedWave()
    {
        SerializedProperty wave = waves.GetArrayElementAtIndex(selectedWaveIndex);
        SerializedProperty spawnDelay = wave.FindPropertyRelative("spawnDelay");
        SerializedProperty maxEnemyCount = wave.FindPropertyRelative("maxEnemyCount");

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField($"Wave {selectedWaveIndex + 1}", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(spawnDelay, new GUIContent("Spawn Delay (sec)"));
        EditorGUILayout.PropertyField(maxEnemyCount, new GUIContent("Enemy Count"));
        EditorGUILayout.Space(4f);

        if (enemyList == null || enemyList.serializedProperty == null ||
            enemyList.serializedProperty.propertyPath != wave.FindPropertyRelative("enemies").propertyPath)
        {
            RebuildEnemyList();
        }

        enemyList.DoLayoutList();

        DrawWeightSummary(wave.FindPropertyRelative("enemies"));
        EditorGUILayout.EndVertical();
    }

    private void RebuildEnemyList()
    {
        if (waves == null || waves.arraySize == 0)
        {
            enemyList = null;
            return;
        }

        selectedWaveIndex = Mathf.Clamp(selectedWaveIndex, 0, waves.arraySize - 1);
        SerializedProperty enemies = waves.GetArrayElementAtIndex(selectedWaveIndex)
            .FindPropertyRelative("enemies");

        enemyList = new ReorderableList(serializedObject, enemies, true, true, true, true)
        {
            drawHeaderCallback = rect =>
            {
                float w = rect.width;
                EditorGUI.LabelField(new Rect(rect.x, rect.y, w * 0.45f, rect.height), "Enemy Type");
                EditorGUI.LabelField(new Rect(rect.x + w * 0.45f, rect.y, w * 0.28f, rect.height), "Weight");
                EditorGUI.LabelField(new Rect(rect.x + w * 0.73f, rect.y, w * 0.27f, rect.height), "%");
            },
            drawElementCallback = (rect, index, active, focused) =>
            {
                SerializedProperty element = enemies.GetArrayElementAtIndex(index);
                SerializedProperty typeProp = element.FindPropertyRelative("enemyType");
                SerializedProperty weightProp = element.FindPropertyRelative("spawnWeight");

                float total = SumWeights(enemies);
                float weight = Mathf.Max(0f, weightProp.floatValue);
                float pct = total > 0f ? weight / total * 100f : 0f;

                rect.y += 2f;
                rect.height = EditorGUIUtility.singleLineHeight;
                float w = rect.width;

                EditorGUI.PropertyField(
                    new Rect(rect.x, rect.y, w * 0.45f - 4f, rect.height),
                    typeProp,
                    GUIContent.none);

                EditorGUI.PropertyField(
                    new Rect(rect.x + w * 0.45f, rect.y, w * 0.28f - 4f, rect.height),
                    weightProp,
                    GUIContent.none);

                EditorGUI.LabelField(
                    new Rect(rect.x + w * 0.73f, rect.y, w * 0.27f, rect.height),
                    $"{pct:0.#}%",
                    EditorStyles.miniLabel);
            },
            elementHeight = EditorGUIUtility.singleLineHeight + 6f
        };
    }

    private static void DrawWeightSummary(SerializedProperty enemies)
    {
        if (enemies == null || enemies.arraySize == 0)
        {
            EditorGUILayout.HelpBox("적 슬롯이 없으면 스폰할 수 없습니다.", MessageType.Warning);
            return;
        }

        float total = SumWeights(enemies);
        if (total <= 0f)
        {
            EditorGUILayout.HelpBox("spawnWeight 합이 0입니다. 하나 이상 > 0 이어야 합니다.", MessageType.Warning);
            return;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append($"합 {total:0.##} → ");
        for (int i = 0; i < enemies.arraySize; i++)
        {
            SerializedProperty e = enemies.GetArrayElementAtIndex(i);
            EnemyType type = (EnemyType)e.FindPropertyRelative("enemyType").enumValueIndex;
            float w = Mathf.Max(0f, e.FindPropertyRelative("spawnWeight").floatValue);
            float pct = w / total * 100f;
            if (i > 0)
            {
                sb.Append(" · ");
            }

            sb.Append($"{type} {pct:0.#}%");
        }

        EditorGUILayout.HelpBox(sb.ToString(), MessageType.None);
    }

    private static float SumWeights(SerializedProperty enemies)
    {
        float total = 0f;
        for (int i = 0; i < enemies.arraySize; i++)
        {
            total += Mathf.Max(0f, enemies.GetArrayElementAtIndex(i)
                .FindPropertyRelative("spawnWeight").floatValue);
        }

        return total;
    }
}
#endif
