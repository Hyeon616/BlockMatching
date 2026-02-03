using System.Collections.Generic;

/// <summary>
/// 블록의 형태와 회전 계산 (Unity 의존성 없음)
/// </summary>
public class BlockShape
{
    private List<(int x, int y)> cells;
    private int centerX;
    private int centerY;

    public IReadOnlyList<(int x, int y)> Cells => cells;
    public int CenterX => centerX;
    public int CenterY => centerY;

    public BlockShape(List<(int x, int y)> cells, int centerX, int centerY)
    {
        this.cells = new List<(int x, int y)>(cells);
        this.centerX = centerX;
        this.centerY = centerY;
    }

    /// <summary>
    /// 셀 개수
    /// </summary>
    public int CellCount => cells.Count;

    /// <summary>
    /// 특정 인덱스의 셀 좌표 (중심 기준)
    /// </summary>
    public (int x, int y) GetCell(int index)
    {
        return cells[index];
    }

    /// <summary>
    /// 절대 좌표로 변환 (블록 위치 + 상대 좌표)
    /// </summary>
    public List<(int x, int y)> GetAbsolutePositions(int blockX, int blockY)
    {
        List<(int x, int y)> positions = new List<(int x, int y)>();
        foreach (var cell in cells)
        {
            positions.Add((blockX + cell.x, blockY + cell.y));
        }
        return positions;
    }

    /// <summary>
    /// 시계 방향 90도 회전
    /// </summary>
    public BlockShape Rotate()
    {
        if (cells.Count == 0)
            return new BlockShape(cells, centerX, centerY);

        // 바운딩 박스 계산
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;
        foreach (var cell in cells)
        {
            if (cell.x < minX) minX = cell.x;
            if (cell.x > maxX) maxX = cell.x;
            if (cell.y < minY) minY = cell.y;
            if (cell.y > maxY) maxY = cell.y;
        }

        int width = maxX - minX;

        // 회전 (minX, minY 기준)
        List<(int x, int y)> rotatedCells = new List<(int x, int y)>();
        foreach (var cell in cells)
        {
            int localX = cell.x - minX;
            int localY = cell.y - minY;

            // 시계 방향 90도: (x, y) -> (y, width - x)
            int newLocalX = localY;
            int newLocalY = width - localX;

            rotatedCells.Add((minX + newLocalX, minY + newLocalY));
        }

        return new BlockShape(rotatedCells, centerX, centerY);
    }

    /// <summary>
    /// Wall Kick 시도 (회전 후 유효하지 않을 때 이동 시도)
    /// </summary>
    public static List<(int dx, int dy)> GetWallKickOffsets()
    {
        return new List<(int dx, int dy)>
        {
            (-1, 0), // 왼쪽
            (1, 0),  // 오른쪽
            (0, 1),  // 위
            (-2, 0), // 왼쪽 2칸
            (2, 0)   // 오른쪽 2칸
        };
    }

    /// <summary>
    /// 블록이 특정 위치에서 유효한지 확인
    /// </summary>
    public bool IsValidPosition(int blockX, int blockY, BoardLogic board)
    {
        foreach (var cell in cells)
        {
            int x = blockX + cell.x;
            int y = blockY + cell.y;

            if (!board.IsInsideBoard(x, y) || !board.IsEmpty(x, y))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 블록을 보드에 배치 (좌표 목록 반환)
    /// </summary>
    public List<(int x, int y)> PlaceOnBoard(int blockX, int blockY, int boardHeight)
    {
        List<(int x, int y)> validPositions = new List<(int x, int y)>();
        foreach (var cell in cells)
        {
            int x = blockX + cell.x;
            int y = blockY + cell.y;

            // 보드 범위 내에 있는 셀만 추가
            if (y < boardHeight && x >= 0 && y >= 0)
            {
                validPositions.Add((x, y));
            }
        }
        return validPositions;
    }

    /// <summary>
    /// BlockData에서 BlockShape 생성
    /// </summary>
    public static BlockShape FromBlockData(BlockData data)
    {
        if (data == null)
            return null;

        var filledCells = data.GetFilledCells();
        if (filledCells.Length == 0)
            return null;

        // 최소 좌표 계산
        int minX = int.MaxValue, minY = int.MaxValue;
        foreach (var cell in filledCells)
        {
            if (cell.x < minX) minX = cell.x;
            if (cell.y < minY) minY = cell.y;
        }

        // 상대 좌표로 변환
        List<(int x, int y)> cells = new List<(int x, int y)>();
        foreach (var cell in filledCells)
        {
            cells.Add((cell.x - minX, cell.y - minY));
        }

        return new BlockShape(cells, 0, 0);
    }
}
