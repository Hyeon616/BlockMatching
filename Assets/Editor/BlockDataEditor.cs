using UnityEditor;
using UnityEngine;

/// <summary>
/// BlockData의 Inspector를 커스텀하여 4x4 그리드로 블록 모양을 편집할 수 있게 함
/// </summary>
[CustomEditor(typeof(BlockData))]
public class BlockDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        BlockData blockData = (BlockData)target;

        EditorGUILayout.LabelField("블록 정보", EditorStyles.boldLabel);
        blockData.blockName = EditorGUILayout.TextField("블록 이름", blockData.blockName);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("블록 모양 (4x4 그리드)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("체크박스를 선택하여 블록 모양을 정의하세요.", MessageType.Info);

        EditorGUILayout.Space(5);

        // 위에서 아래로 그리드 표시 (y=3이 맨 위)
        for (int y = 3; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            for (int x = 0; x < 4; x++)
            {
                bool current = blockData.GetCell(x, y);
                bool newValue = EditorGUILayout.Toggle(current, GUILayout.Width(20), GUILayout.Height(20));
                if (newValue != current)
                {
                    Undo.RecordObject(blockData, "블록 모양 수정");
                    blockData.SetCell(x, y, newValue);
                    EditorUtility.SetDirty(blockData);
                }
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(10);

        if (GUILayout.Button("모두 지우기"))
        {
            Undo.RecordObject(blockData, "블록 모양 초기화");
            for (int i = 0; i < 16; i++)
                blockData.shape[i] = false;
            EditorUtility.SetDirty(blockData);
        }
    }
}