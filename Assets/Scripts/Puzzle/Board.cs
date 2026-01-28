using UnityEngine;

/// <summary>
/// 게임 보드를 관리하는 클래스
/// </summary>
public class Board : MonoBehaviour
{
    [Header("Board Settings")]
    [SerializeField] private int width = 16;
    [SerializeField] private int height = 27;

    public int Width => width;
    public int Height => height;

    [Header("Prefab")]
    [SerializeField] private GameObject brownStonePrefab;
    [SerializeField, Range(1f, 2f)] private float scalePadding = 1.3f;

    // 각 셀의 점유 상태 (null이면 비어있음)
    private Transform[,] grid;

    void Start()
    {
        grid = new Transform[width, height];
        CreateBackground();
        SetupCamera();
    }

    void CreateBackground()
    {
        Vector3 scale = GetScaleForUnitSize(brownStonePrefab);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 position = new Vector3(x, y, 0);
                GameObject tile = Instantiate(brownStonePrefab, position, Quaternion.identity, transform);
                tile.transform.localScale = scale;
                tile.name = $"BG_{x}_{y}";
            }
        }
    }

    Vector3 GetScaleForUnitSize(GameObject prefab)
    {
        SpriteRenderer sr = prefab.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null)
            return Vector3.one;

        Vector2 spriteSize = sr.sprite.bounds.size;
        return new Vector3(scalePadding / spriteSize.x, scalePadding / spriteSize.y, 1f);
    }

    void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        float centerX = (width - 1) / 2f;
        float centerY = (height - 1) / 2f;
        cam.transform.position = new Vector3(centerX, centerY, -10f);

        float screenRatio = (float)Screen.width / Screen.height;
        float boardRatio = (float)width / height;

        if (screenRatio >= boardRatio)
        {
            cam.orthographicSize = height / 2f;
        }
        else
        {
            cam.orthographicSize = width / 2f / screenRatio;
        }
    }

    /// <summary>
    /// 해당 위치가 보드 범위 내인지 확인
    /// </summary>
    public bool IsInsideBoard(int x, int y)
    {
        return x >= 0 && x < width && y >= 0;
    }

    /// <summary>
    /// 해당 위치가 비어있는지 확인
    /// </summary>
    public bool IsEmpty(int x, int y)
    {
        if (y >= height) return true; // 보드 위는 비어있음
        if (!IsInsideBoard(x, y)) return false;
        return grid[x, y] == null;
    }

    /// <summary>
    /// 해당 위치가 유효한지 확인 (범위 내이고 비어있음)
    /// </summary>
    public bool IsValidCell(int x, int y)
    {
        return IsInsideBoard(x, y) && IsEmpty(x, y);
    }

    /// <summary>
    /// 블록의 모든 셀을 보드에 등록
    /// </summary>
    public void PlaceBlock(Transform blockTransform)
    {
        foreach (Transform child in blockTransform)
        {
            int x = Mathf.RoundToInt(child.position.x);
            int y = Mathf.RoundToInt(child.position.y);

            if (y < height && x >= 0 && x < width && y >= 0)
            {
                grid[x, y] = child;
            }
        }
    }

    /// <summary>
    /// 완성된 줄 확인 및 제거
    /// </summary>
    public int ClearFullLines()
    {
        int linesCleared = 0;

        for (int y = height - 1; y >= 0; y--)
        {
            if (IsLineFull(y))
            {
                ClearLine(y);
                MoveAllLinesDown(y);
                linesCleared++;
                y++; // 같은 줄 다시 확인
            }
        }

        return linesCleared;
    }

    bool IsLineFull(int y)
    {
        for (int x = 0; x < width; x++)
        {
            if (grid[x, y] == null)
                return false;
        }
        return true;
    }

    void ClearLine(int y)
    {
        for (int x = 0; x < width; x++)
        {
            if (grid[x, y] != null)
            {
                Destroy(grid[x, y].gameObject);
                grid[x, y] = null;
            }
        }
    }

    void MoveAllLinesDown(int clearedY)
    {
        for (int y = clearedY + 1; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (grid[x, y] != null)
                {
                    grid[x, y - 1] = grid[x, y];
                    grid[x, y] = null;
                    grid[x, y - 1].position += Vector3.down;
                }
            }
        }
    }
}