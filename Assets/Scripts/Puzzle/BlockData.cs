using UnityEngine;

/// <summary>
/// 블록의 모양과 속성을 정의하는 ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "NewBlock", menuName = "Puzzle/Block Data")]
public class BlockData : ScriptableObject
{
    [Header("블록 정보")]
    public string blockName;

    [Header("블록 모양 (4x4 그리드)")]
    [Tooltip("4x4 그리드에서 채워질 셀을 선택하세요.")]
    public bool[] shape = new bool[16];

    public int GridSize => 4;

    /// <summary>
    /// 특정 좌표의 셀이 채워져 있는지 반환
    /// </summary>
    public bool GetCell(int x, int y)
    {
        if (x < 0 || x >= GridSize || y < 0 || y >= GridSize)
            return false;
        return shape[y * GridSize + x];
    }

    /// <summary>
    /// 특정 좌표의 셀 값을 설정
    /// </summary>
    public void SetCell(int x, int y, bool value)
    {
        if (x >= 0 && x < GridSize && y >= 0 && y < GridSize)
            shape[y * GridSize + x] = value;
    }

    /// <summary>
    /// 채워진 모든 셀의 좌표 배열 반환
    /// </summary>
    public Vector2Int[] GetFilledCells()
    {
        var cells = new System.Collections.Generic.List<Vector2Int>();
        for (int y = 0; y < GridSize; y++)
        {
            for (int x = 0; x < GridSize; x++)
            {
                if (GetCell(x, y))
                    cells.Add(new Vector2Int(x, y));
            }
        }
        return cells.ToArray();
    }
}