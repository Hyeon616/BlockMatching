using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 게임 보드와 다음 블록 미리보기를 관리하는 클래스
/// </summary>
public class Board : MonoBehaviour
{
    #region 보드 설정
    [Header("보드 설정")]
    [SerializeField] private int width = 16;
    [SerializeField] private int height = 27;

    public int Width => width;
    public int Height => height;
    #endregion

    #region 미리보기 설정
    [Header("미리보기 설정")]
    [SerializeField] private int previewSize = 4;
    [SerializeField] private Transform previewRoot;
    #endregion

    #region 참조
    [Header("참조")]
    [SerializeField] private SoundManager soundManager;
    #endregion

    #region 프리팹
    [FormerlySerializedAs("backgroundPrefab1")]
    [Header("프리팹")]
    [SerializeField] private GameObject backgroundPrefab_Light;
    [SerializeField] private GameObject backgroundPrefab_Dark;
    [SerializeField] private GameObject blockPrefab;
    [SerializeField, Range(1f, 2f)] private float scalePadding = 1.3f;
    #endregion

    // 보드 그리드 (셀 점유 상태)
    private Transform[,] grid;

    // 미리보기
    private Transform previewContainer;
    private const int PREVIEW_POOL_SIZE = 16;
    private List<GameObject> previewPool;

    // 블록 셀 풀
    private const int CELL_POOL_INITIAL_SIZE = 64;
    private Stack<GameObject> cellPool;
    private Transform cellPoolContainer;

    // 셀 스케일 캐시
    private Vector3 backgroundScale1;
    private Vector3 backgroundScale2;
    private Vector3 blockScale;

    #region Unity 생명주기
    private void Start()
    {
        grid = new Transform[width, height];
        backgroundScale1 = GetScaleForUnitSize(backgroundPrefab_Light);
        backgroundScale2 = GetScaleForUnitSize(backgroundPrefab_Dark);
        if (blockPrefab != null)
            blockScale = GetScaleForUnitSize(blockPrefab);

        CreateBackground();
        CreatePreviewArea();
        CreateCellPool();
        SetupCamera();
    }
    #endregion

    #region 배경 생성
    /// <summary>
    /// 보드 배경 그리드 생성
    /// </summary>
    private void CreateBackground()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                bool isEven = (x + y) % 2 == 0;
                GameObject prefab = isEven ? backgroundPrefab_Light : backgroundPrefab_Dark;
                Vector3 scale = isEven ? backgroundScale1 : backgroundScale2;

                Vector3 position = new Vector3(x, y, 0);
                GameObject tile = Instantiate(prefab, position, Quaternion.identity, transform);
                tile.transform.localScale = scale;
                tile.name = $"BG_{x}_{y}";
            }
        }
    }
    #endregion

    #region Preview
    /// <summary>
    /// 미리보기 영역 생성
    /// </summary>
    private void CreatePreviewArea()
    {
        if (backgroundPrefab_Light == null || backgroundPrefab_Dark == null || blockPrefab == null || previewRoot == null) return;

        // 미리보기 배경 생성 (격자무늬)
        for (int x = 0; x < previewSize; x++)
        {
            for (int y = 0; y < previewSize; y++)
            {
                bool isEven = (x + y) % 2 == 0;
                GameObject prefab = isEven ? backgroundPrefab_Light : backgroundPrefab_Dark;
                Vector3 scale = isEven ? backgroundScale1 : backgroundScale2;

                GameObject tile = Instantiate(prefab, previewRoot);
                tile.transform.localPosition = new Vector3(x, y, 0);
                tile.transform.localScale = scale;
                tile.name = $"PreviewBG_{x}_{y}";
            }
        }

        // 블록 셀 컨테이너
        GameObject blockContainer = new GameObject("BlockContainer");
        blockContainer.transform.SetParent(previewRoot);
        blockContainer.transform.localPosition = Vector3.zero;
        previewContainer = blockContainer.transform;

        // 미리보기 블록 셀 풀 생성
        previewPool = new List<GameObject>(PREVIEW_POOL_SIZE);
        for (int i = 0; i < PREVIEW_POOL_SIZE; i++)
        {
            GameObject cell = Instantiate(blockPrefab, previewContainer);
            cell.transform.localScale = blockScale;
            cell.name = "PreviewBlock";
            cell.SetActive(false);
            previewPool.Add(cell);
        }
    }

    /// <summary>
    /// 미리보기 영역에 다음 블록 표시
    /// </summary>
    public void ShowPreviewBlock(BlockData data)
    {
        ClearPreviewBlock();

        if (data == null || previewContainer == null) return;

        Vector2Int[] filledCells = data.GetFilledCells();
        if (filledCells.Length == 0) return;

        // 블록의 바운딩 박스 계산
        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;
        foreach (var cell in filledCells)
        {
            if (cell.x < minX) minX = cell.x;
            if (cell.x > maxX) maxX = cell.x;
            if (cell.y < minY) minY = cell.y;
            if (cell.y > maxY) maxY = cell.y;
        }

        // 블록 크기
        int blockWidth = maxX - minX + 1;
        int blockHeight = maxY - minY + 1;

        // 그리드 중앙에 배치하기 위한 오프셋
        float offsetX = (previewSize - blockWidth) / 2f;
        float offsetY = (previewSize - blockHeight) / 2f;

        // 풀에서 셀을 꺼내 배치
        for (int i = 0; i < filledCells.Length && i < previewPool.Count; i++)
        {
            Vector2Int cell = filledCells[i];
            GameObject cellObj = previewPool[i];
            cellObj.transform.localPosition = new Vector3(
                (cell.x - minX) + offsetX,
                (cell.y - minY) + offsetY,
                0f
            );
            cellObj.SetActive(true);
        }
    }

    /// <summary>
    /// 미리보기 블록 비활성화
    /// </summary>
    private void ClearPreviewBlock()
    {
        if (previewPool == null) return;

        foreach (var cell in previewPool)
        {
            cell.SetActive(false);
        }
    }
    #endregion

    #region 카메라 설정
    /// <summary>
    /// 보드 전체가 보이도록 카메라 설정
    /// </summary>
    private void SetupCamera()
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
    #endregion

    #region 보드 유효성 검사
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
        if (y >= height) return true;
        if (!IsInsideBoard(x, y)) return false;
        return grid[x, y] == null;
    }

    /// <summary>
    /// 해당 위치가 유효한지 확인 (범위 내이고 비어있음)
    /// </summary>
    private bool IsValidCell(int x, int y)
    {
        return IsInsideBoard(x, y) && IsEmpty(x, y);
    }
    #endregion

    #region 블록 배치 및 줄 제거
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
                y++;
            }
        }

        if (linesCleared > 0)
        {
            soundManager.Play(SoundType.ClearLine);
        }

        return linesCleared;
    }

    /// <summary>
    /// 해당 줄이 가득 찼는지 확인
    /// </summary>
    private bool IsLineFull(int y)
    {
        for (int x = 0; x < width; x++)
        {
            if (grid[x, y] == null)
                return false;
        }
        return true;
    }

    /// <summary>
    /// 해당 줄의 모든 셀 비활성화 후 풀에 반환
    /// </summary>
    private void ClearLine(int y)
    {
        for (int x = 0; x < width; x++)
        {
            if (grid[x, y] != null)
            {
                ReturnCellToPool(grid[x, y].gameObject);
                grid[x, y] = null;
            }
        }
    }

    /// <summary>
    /// 제거된 줄 위의 모든 셀을 아래로 이동
    /// </summary>
    private void MoveAllLinesDown(int clearedY)
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
    #endregion

    #region 셀 오브젝트 풀
    /// <summary>
    /// 블록 셀 풀 초기화
    /// </summary>
    private void CreateCellPool()
    {
        cellPool = new Stack<GameObject>(CELL_POOL_INITIAL_SIZE);

        GameObject containerObj = new GameObject("CellPool");
        containerObj.transform.SetParent(transform);
        cellPoolContainer = containerObj.transform;

        if (blockPrefab == null) return;

        for (int i = 0; i < CELL_POOL_INITIAL_SIZE; i++)
        {
            GameObject cell = Instantiate(blockPrefab, cellPoolContainer);
            cell.transform.localScale = blockScale;
            cell.SetActive(false);
            cellPool.Push(cell);
        }
    }

    /// <summary>
    /// 풀에서 셀 가져오기 (부족하면 새로 생성)
    /// </summary>
    public GameObject GetCellFromPool()
    {
        GameObject cell;
        if (cellPool.Count > 0)
        {
            cell = cellPool.Pop();
        }
        else
        {
            cell = Instantiate(blockPrefab, cellPoolContainer);
            cell.transform.localScale = blockScale;
        }
        cell.SetActive(true);
        return cell;
    }

    /// <summary>
    /// 셀을 풀에 반환
    /// </summary>
    private void ReturnCellToPool(GameObject cell)
    {
        cell.SetActive(false);
        cell.transform.SetParent(cellPoolContainer);
        cellPool.Push(cell);
    }
    #endregion

    #region 유틸리티
    /// <summary>
    /// 프리팹을 1x1 유닛 크기에 맞추기 위한 스케일 계산
    /// </summary>
    private Vector3 GetScaleForUnitSize(GameObject prefab)
    {
        SpriteRenderer sr = prefab.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null)
            return Vector3.one;

        Vector2 spriteSize = sr.sprite.bounds.size;
        return new Vector3(scalePadding / spriteSize.x, scalePadding / spriteSize.y, 1f);
    }
    #endregion
}
