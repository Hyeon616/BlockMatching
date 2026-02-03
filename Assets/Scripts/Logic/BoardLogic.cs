using System.Collections.Generic;

/// <summary>
/// 보드 그리드의 순수 로직 (Unity 의존성 없음)
/// </summary>
public class BoardLogic
{
    public const int EMPTY = 0;
    public const int TETRIS_BLOCK = 1;
    public const int GARBAGE_LINE = 2;

    private readonly int width;
    private readonly int height;
    private int[,] grid;
    private HashSet<(int x, int y)> garbageCells;

    public int Width => width;
    public int Height => height;

    public BoardLogic(int width, int height)
    {
        this.width = width;
        this.height = height;
        grid = new int[width, height];
        garbageCells = new HashSet<(int x, int y)>();
    }

    /// <summary>
    /// 그리드 초기화
    /// </summary>
    public void Clear()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = EMPTY;
            }
        }
        garbageCells.Clear();
    }

    /// <summary>
    /// 특정 위치의 셀 값 가져오기
    /// </summary>
    public int GetCell(int x, int y)
    {
        if (!IsInsideBoard(x, y))
            return TETRIS_BLOCK; // 범위 밖은 막힌 것으로 간주

        return grid[x, y];
    }

    /// <summary>
    /// 특정 위치의 셀 값 설정
    /// </summary>
    public void SetCell(int x, int y, int value)
    {
        if (!IsInsideBoard(x, y))
            return;

        grid[x, y] = value;

        if (value == GARBAGE_LINE)
            garbageCells.Add((x, y));
        else
            garbageCells.Remove((x, y));
    }

    /// <summary>
    /// 특정 위치가 보드 범위 내인지 확인
    /// </summary>
    public bool IsInsideBoard(int x, int y)
    {
        return x >= 0 && x < width && y >= 0;
    }

    /// <summary>
    /// 특정 위치가 비어있는지 확인
    /// </summary>
    public bool IsEmpty(int x, int y)
    {
        if (y >= height) return true;
        if (!IsInsideBoard(x, y)) return false;
        return grid[x, y] == EMPTY;
    }

    /// <summary>
    /// 특정 행이 가득 찼는지 확인
    /// </summary>
    public bool IsLineFull(int y)
    {
        if (y < 0 || y >= height)
            return false;

        for (int x = 0; x < width; x++)
        {
            if (grid[x, y] == EMPTY)
                return false;
        }
        return true;
    }

    /// <summary>
    /// 완성된 모든 줄 찾기
    /// </summary>
    public List<int> GetFullLines()
    {
        List<int> fullLines = new List<int>();
        for (int y = 0; y < height; y++)
        {
            if (IsLineFull(y))
                fullLines.Add(y);
        }
        return fullLines;
    }

    /// <summary>
    /// 특정 줄 제거
    /// </summary>
    public void ClearLine(int y)
    {
        if (y < 0 || y >= height)
            return;

        for (int x = 0; x < width; x++)
        {
            garbageCells.Remove((x, y));
            grid[x, y] = EMPTY;
        }
    }

    /// <summary>
    /// 중력 적용: 각 셀이 얼마나 떨어져야 하는지 계산
    /// </summary>
    public List<CellMove> CalculateGravity()
    {
        List<CellMove> moves = new List<CellMove>();

        for (int x = 0; x < width; x++)
        {
            int writeY = 0;
            for (int readY = 0; readY < height; readY++)
            {
                if (grid[x, readY] == EMPTY)
                    continue;

                // 가비지 라인은 고정
                if (garbageCells.Contains((x, readY)))
                {
                    writeY = readY + 1;
                    continue;
                }

                // 테트리스 블록: 아래로 이동
                if (writeY < readY)
                {
                    int distance = readY - writeY;
                    moves.Add(new CellMove(x, readY, x, writeY, distance));
                    grid[x, writeY] = grid[x, readY];
                    grid[x, readY] = EMPTY;
                }
                writeY++;
            }
        }

        return moves;
    }

    /// <summary>
    /// 빈 행 제거 후 위의 행들을 아래로 압축
    /// </summary>
    public List<CellMove> CompressEmptyRows()
    {
        List<CellMove> moves = new List<CellMove>();

        int writeY = 0;
        for (int readY = 0; readY < height; readY++)
        {
            // 이 행에 셀이 있는지 확인
            bool hasCell = false;
            for (int x = 0; x < width; x++)
            {
                if (grid[x, readY] != EMPTY)
                {
                    hasCell = true;
                    break;
                }
            }

            if (hasCell)
            {
                // writeY와 readY가 다르면 행을 이동
                if (writeY != readY)
                {
                    int distance = readY - writeY;
                    for (int x = 0; x < width; x++)
                    {
                        if (grid[x, readY] != EMPTY)
                        {
                            moves.Add(new CellMove(x, readY, x, writeY, distance));
                            garbageCells.Remove((x, readY));
                            if (grid[x, readY] == GARBAGE_LINE)
                                garbageCells.Add((x, writeY));
                        }
                        grid[x, writeY] = grid[x, readY];
                        grid[x, readY] = EMPTY;
                    }
                }
                writeY++;
            }
        }

        return moves;
    }

    /// <summary>
    /// 가비지 라인 추가 준비: 그리드를 위로 시프트
    /// </summary>
    public List<CellMove> ShiftGridUp()
    {
        List<CellMove> moves = new List<CellMove>();

        // 최상단 행에 셀이 있으면 실패
        for (int x = 0; x < width; x++)
        {
            if (grid[x, height - 1] != EMPTY)
                return null;
        }

        // 그리드를 위로 1칸 시프트
        for (int y = height - 1; y > 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                if (grid[x, y - 1] != EMPTY)
                {
                    moves.Add(new CellMove(x, y - 1, x, y, 1));
                    garbageCells.Remove((x, y - 1));
                    if (grid[x, y - 1] == GARBAGE_LINE)
                        garbageCells.Add((x, y));
                }
                grid[x, y] = grid[x, y - 1];
            }
        }

        // row 0은 비움
        for (int x = 0; x < width; x++)
        {
            grid[x, 0] = EMPTY;
        }

        return moves;
    }

    /// <summary>
    /// 가비지 라인의 빈 칸 위치 계산 (Fisher-Yates)
    /// </summary>
    public HashSet<int> GetGarbageEmptyPositions(int emptyCount, System.Random random)
    {
        emptyCount = System.Math.Min(emptyCount, width - 1);
        int[] indices = new int[width];
        for (int i = 0; i < width; i++) indices[i] = i;
        for (int i = width - 1; i > 0; i--)
        {
            int j = random.Next(0, i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }
        HashSet<int> emptyPositions = new HashSet<int>();
        for (int i = 0; i < emptyCount; i++)
            emptyPositions.Add(indices[i]);
        return emptyPositions;
    }

    /// <summary>
    /// row 0에 가비지 라인 설정
    /// </summary>
    public void SetGarbageLineAtBottom(HashSet<int> emptyPositions)
    {
        for (int x = 0; x < width; x++)
        {
            if (emptyPositions.Contains(x))
            {
                grid[x, 0] = EMPTY;
            }
            else
            {
                grid[x, 0] = GARBAGE_LINE;
                garbageCells.Add((x, 0));
            }
        }
    }

    /// <summary>
    /// 블록 착지 위치까지의 거리 계산
    /// </summary>
    public int CalculateDropDistance(List<(int x, int y)> blockCells)
    {
        if (blockCells.Count == 0)
            return 0;

        for (int dropDistance = 1; dropDistance <= height; dropDistance++)
        {
            foreach (var cell in blockCells)
            {
                int x = cell.x;
                int y = cell.y - dropDistance;

                if (!IsInsideBoard(x, y) || !IsEmpty(x, y))
                {
                    return dropDistance - 1;
                }
            }
        }

        return height;
    }
}

/// <summary>
/// 셀 이동 정보
/// </summary>
public struct CellMove
{
    public int fromX;
    public int fromY;
    public int toX;
    public int toY;
    public int distance;

    public CellMove(int fromX, int fromY, int toX, int toY, int distance)
    {
        this.fromX = fromX;
        this.fromY = fromY;
        this.toX = toX;
        this.toY = toY;
        this.distance = distance;
    }
}
