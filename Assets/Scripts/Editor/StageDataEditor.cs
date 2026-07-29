#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

/// <summary>
/// StageData: 웨이브 선택 + Main/Sub 레인을 세로로 나란히 편집.
/// </summary>
[CustomEditor(typeof(StageData))]
public sealed class StageDataEditor : Editor
{
    private SerializedProperty stageId;
    private SerializedProperty displayName;
    private SerializedProperty clearBonusGold;
    private SerializedProperty waves;

    private int selectedWaveIndex;
    private ReorderableList mainList;
    private ReorderableList subList;

    private void OnEnable()
    {
        stageId = serializedObject.FindProperty("stageId");
        displayName = serializedObject.FindProperty("displayName");
        clearBonusGold = serializedObject.FindProperty("clearBonusGold");
        waves = serializedObject.FindProperty("waves");

        selectedWaveIndex = Mathf.Clamp(selectedWaveIndex, 0, Mathf.Max(0, waves.arraySize - 1));
        RebuildLaneLists();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Stage", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(stageId);
        EditorGUILayout.PropertyField(displayName);
        EditorGUILayout.PropertyField(clearBonusGold);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Boss (Final Wave)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("spawnBossOnFinalWave"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("bossEnemyType"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("bossEnemyTier"));

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
                RebuildLaneLists();
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
                RebuildLaneLists();
                GUI.FocusControl(null);
            }
        }

        if (GUILayout.Button("+ Wave", GUILayout.Width(64f)))
        {
            int insertAt = count == 0 ? 0 : selectedWaveIndex + 1;
            waves.InsertArrayElementAtIndex(insertAt);
            SerializedProperty wave = waves.GetArrayElementAtIndex(insertAt);
            wave.FindPropertyRelative("spawnDelay").floatValue = 1f;
            InitDefaultLane(wave.FindPropertyRelative("mainEnemies"), count: 10);
            wave.FindPropertyRelative("subEnemies").ClearArray();
            selectedWaveIndex = insertAt;
            RebuildLaneLists();
            GUI.FocusControl(null);
        }

        using (new EditorGUI.DisabledScope(count == 0))
        {
            if (GUILayout.Button("Dup", GUILayout.Width(40f)))
            {
                waves.InsertArrayElementAtIndex(selectedWaveIndex);
                selectedWaveIndex++;
                RebuildLaneLists();
                GUI.FocusControl(null);
            }

            if (GUILayout.Button("Del", GUILayout.Width(40f)))
            {
                waves.DeleteArrayElementAtIndex(selectedWaveIndex);
                selectedWaveIndex = Mathf.Clamp(selectedWaveIndex, 0, Mathf.Max(0, waves.arraySize - 1));
                RebuildLaneLists();
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
                RebuildLaneLists();
                GUI.FocusControl(null);
            }
        }
    }

    private static void InitDefaultLane(SerializedProperty lane, int count)
    {
        lane.ClearArray();
        lane.arraySize = 1;
        SerializedProperty slot = lane.GetArrayElementAtIndex(0);
        slot.FindPropertyRelative("enemyType").enumValueIndex = 0;
        slot.FindPropertyRelative("enemyTier").intValue = (int)EnemyTier.Tier1;
        slot.FindPropertyRelative("count").intValue = count;
        slot.FindPropertyRelative("earlyBias").floatValue = 1f;
    }

    private void DrawSelectedWave()
    {
        SerializedProperty wave = waves.GetArrayElementAtIndex(selectedWaveIndex);
        SerializedProperty spawnDelay = wave.FindPropertyRelative("spawnDelay");
        SerializedProperty mainProp = wave.FindPropertyRelative("mainEnemies");
        SerializedProperty subProp = wave.FindPropertyRelative("subEnemies");

        EnsureLaneLists(mainProp, subProp);

        int mainSum = SumCounts(mainProp);
        int subSum = SumCounts(subProp);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField($"Wave {selectedWaveIndex + 1}", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(spawnDelay, new GUIContent("Spawn Delay (sec)"));
        EditorGUILayout.HelpBox("Sub는 spawnDelay×0.5 후 엇박 스폰. 비우면 메인만.", MessageType.None);

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField($"Main 소환 ({mainSum})", EditorStyles.boldLabel);
        mainList.DoLayoutList();
        DrawLaneComposition(mainProp);

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField($"Sub 소환 ({subSum})", EditorStyles.boldLabel);
        if (subProp.arraySize == 0 || subSum == 0)
        {
            EditorGUILayout.HelpBox("비어 있으면 메인만 스폰합니다.", MessageType.None);
        }

        subList.DoLayoutList();
        DrawLaneComposition(subProp);

        EditorGUILayout.Space(8f);
        DrawWaveTotal(mainProp, subProp, mainSum, subSum);
        EditorGUILayout.EndVertical();
    }

    private void EnsureLaneLists(SerializedProperty mainProp, SerializedProperty subProp)
    {
        if (mainList == null || mainList.serializedProperty == null ||
            mainList.serializedProperty.propertyPath != mainProp.propertyPath ||
            subList == null || subList.serializedProperty == null ||
            subList.serializedProperty.propertyPath != subProp.propertyPath)
        {
            RebuildLaneLists();
        }
    }

    private void RebuildLaneLists()
    {
        mainList = null;
        subList = null;

        if (waves == null || waves.arraySize == 0)
        {
            return;
        }

        selectedWaveIndex = Mathf.Clamp(selectedWaveIndex, 0, waves.arraySize - 1);
        SerializedProperty wave = waves.GetArrayElementAtIndex(selectedWaveIndex);
        mainList = CreateLaneList(wave.FindPropertyRelative("mainEnemies"));
        subList = CreateLaneList(wave.FindPropertyRelative("subEnemies"));
    }

    private ReorderableList CreateLaneList(SerializedProperty lane)
    {
        return new ReorderableList(serializedObject, lane, true, true, true, true)
        {
            drawHeaderCallback = rect =>
            {
                float w = rect.width;
                EditorGUI.LabelField(new Rect(rect.x, rect.y, w * 0.28f, rect.height), "Enemy Type");
                EditorGUI.LabelField(new Rect(rect.x + w * 0.28f, rect.y, w * 0.14f, rect.height), "Tier");
                EditorGUI.LabelField(new Rect(rect.x + w * 0.42f, rect.y, w * 0.14f, rect.height), "Count");
                EditorGUI.LabelField(
                    new Rect(rect.x + w * 0.56f, rect.y, w * 0.22f, rect.height),
                    "빨리 나올");
                EditorGUI.LabelField(new Rect(rect.x + w * 0.78f, rect.y, w * 0.22f, rect.height), "%");
            },
            drawElementCallback = (rect, index, active, focused) =>
            {
                SerializedProperty element = lane.GetArrayElementAtIndex(index);
                SerializedProperty typeProp = element.FindPropertyRelative("enemyType");
                SerializedProperty tierProp = element.FindPropertyRelative("enemyTier");
                SerializedProperty countProp = element.FindPropertyRelative("count");
                SerializedProperty biasProp = element.FindPropertyRelative("earlyBias");

                if (tierProp.intValue < (int)EnemyTier.Tier1)
                {
                    tierProp.intValue = (int)EnemyTier.Tier1;
                }

                float total = SumBiases(lane);
                float bias = Mathf.Max(0f, biasProp.floatValue);
                float pct = total > 0f ? bias / total * 100f : 0f;

                rect.y += 2f;
                rect.height = EditorGUIUtility.singleLineHeight;
                float w = rect.width;

                EditorGUI.PropertyField(
                    new Rect(rect.x, rect.y, w * 0.28f - 4f, rect.height),
                    typeProp,
                    GUIContent.none);

                EditorGUI.PropertyField(
                    new Rect(rect.x + w * 0.28f, rect.y, w * 0.14f - 4f, rect.height),
                    tierProp,
                    GUIContent.none);

                EditorGUI.PropertyField(
                    new Rect(rect.x + w * 0.42f, rect.y, w * 0.14f - 4f, rect.height),
                    countProp,
                    GUIContent.none);

                EditorGUI.PropertyField(
                    new Rect(rect.x + w * 0.56f, rect.y, w * 0.22f - 4f, rect.height),
                    biasProp,
                    GUIContent.none);

                EditorGUI.LabelField(
                    new Rect(rect.x + w * 0.78f, rect.y, w * 0.22f, rect.height),
                    $"{pct:0.#}%",
                    EditorStyles.miniLabel);
            },
            elementHeight = EditorGUIUtility.singleLineHeight + 6f
        };
    }

    private static void DrawLaneComposition(SerializedProperty lane)
    {
        string line = FormatComposition(lane);
        if (!string.IsNullOrEmpty(line))
        {
            EditorGUILayout.LabelField(line, EditorStyles.miniLabel);
        }
    }

    private static void DrawWaveTotal(
        SerializedProperty mainProp,
        SerializedProperty subProp,
        int mainSum,
        int subSum)
    {
        int total = mainSum + subSum;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"총합 {total}마리  (Main {mainSum} + Sub {subSum})");

        string mainLine = FormatComposition(mainProp);
        string subLine = FormatComposition(subProp);
        if (!string.IsNullOrEmpty(mainLine))
        {
            sb.AppendLine($"Main: {mainLine}");
        }

        if (!string.IsNullOrEmpty(subLine))
        {
            sb.Append($"Sub: {subLine}");
        }
        else
        {
            sb.Append("Sub: (없음)");
        }

        EditorGUILayout.HelpBox(sb.ToString(), MessageType.Info);
    }

    private static string FormatComposition(SerializedProperty lane)
    {
        if (lane == null || lane.arraySize == 0)
        {
            return string.Empty;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        bool first = true;
        for (int i = 0; i < lane.arraySize; i++)
        {
            SerializedProperty e = lane.GetArrayElementAtIndex(i);
            int c = Mathf.Max(0, e.FindPropertyRelative("count").intValue);
            if (c <= 0)
            {
                continue;
            }

            EnemyType type = (EnemyType)e.FindPropertyRelative("enemyType").enumValueIndex;
            int tierValue = e.FindPropertyRelative("enemyTier").intValue;
            if (tierValue < (int)EnemyTier.Tier1)
            {
                tierValue = (int)EnemyTier.Tier1;
            }

            if (!first)
            {
                sb.Append(" · ");
            }

            first = false;
            sb.Append($"{type} T{tierValue}×{c}");
        }

        return first ? string.Empty : sb.ToString();
    }

    private static int SumCounts(SerializedProperty enemies)
    {
        int total = 0;
        if (enemies == null)
        {
            return 0;
        }

        for (int i = 0; i < enemies.arraySize; i++)
        {
            total += Mathf.Max(0, enemies.GetArrayElementAtIndex(i)
                .FindPropertyRelative("count").intValue);
        }

        return total;
    }

    private static float SumBiases(SerializedProperty enemies)
    {
        float total = 0f;
        for (int i = 0; i < enemies.arraySize; i++)
        {
            total += Mathf.Max(0f, enemies.GetArrayElementAtIndex(i)
                .FindPropertyRelative("earlyBias").floatValue);
        }

        return total;
    }
}
#endif
