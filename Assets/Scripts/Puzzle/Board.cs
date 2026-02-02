using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 게임 보드와 다음 블록 미리보기를 관리
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
    [Header("프리팹")]
    [SerializeField] private GameObject backgroundPrefab_Light;
    [SerializeField] private GameObject backgroundPrefab_Dark;
    [SerializeField] private GameObject blockPrefab;
    [SerializeField] private GameObject previewLandPrefab;
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

    // 착지 미리보기 풀
    private const int LANDING_PREVIEW_POOL_SIZE = 16;
    private List<GameObject> landingPreviewPool;
    private Transform landingPreviewContainer;

    // 블록 부모 오브젝트 풀
    private const int BLOCK_PARENT_POOL_SIZE = 2;
    private Stack<GameObject> blockParentPool;
    private Transform blockParentPoolContainer;

    // 셀 스케일 캐시
    private Vector3 backgroundScale1;
    private Vector3 backgroundScale2;
    private Vector3 blockScale;
    private Vector3 previewLandScale;

    // 가비지 라인
    private int totalLinesCleared;
    private int emptyCountPerLine = 1;
    [Header("가비지 라인 설정")]
    [SerializeField] private float garbageInterval = 1f;
    private Transform currentBlockTransform;
    private Coroutine garbageCoroutine;
    private System.Action onGarbageGameOver;

    #region Unity 생명주기
    private void Start()
    {
        grid = new Transform[width, height];
        backgroundScale1 = GetScaleForUnitSize(backgroundPrefab_Light);
        backgroundScale2 = GetScaleForUnitSize(backgroundPrefab_Dark);
        if (blockPrefab != null)
            blockScale = GetScaleForUnitSize(blockPrefab);
        if (previewLandPrefab != null)
            previewLandScale = GetScaleForUnitSize(previewLandPrefab);

        CreateBackground();
        CreatePreviewArea();
        CreateCellPool();
        CreateLandingPreviewPool();
        CreateBlockParentPool();
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

    #region 보드 초기화
    /// <summary>
    /// 보드의 모든 셀을 풀에 반환하고 그리드 초기화
    /// </summary>
    public void ClearBoard()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] != null)
                {
                    ReturnCellToPool(grid[x, y].gameObject);
                    grid[x, y] = null;
                }
            }
        }

        HideLandingPreview();
        ClearPreviewBlock();

        totalLinesCleared = 0;
        emptyCountPerLine = 1;
    }

    /// <summary>
    /// 게임오버 연출: 모든 블록이 위로 튀어올랐다가 아래로 떨어짐
    /// </summary>
    public void PlayGameOverEffect(Action onComplete)
    {
        HideLandingPreview();
        ClearPreviewBlock();

        List<Transform> cells = new List<Transform>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] != null)
                {
                    cells.Add(grid[x, y]);
                    grid[x, y] = null;
                }
            }
        }

        if (cells.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        StartCoroutine(GameOverEffectCoroutine(cells, onComplete));
    }

    private IEnumerator GameOverEffectCoroutine(List<Transform> cells, Action onComplete)
    {
        float gravity = -75f;
        float bottomY = -5f;
        int count = cells.Count;

        // 각 셀의 속도와 회전속도를 미리 계산
        Vector2[] velocities = new Vector2[count];
        float[] angularSpeeds = new float[count];

        for (int i = 0; i < count; i++)
        {
            cells[i].SetParent(null);

            float vx = UnityEngine.Random.Range(-8f, 8f);
            float vy = UnityEngine.Random.Range(10f, 25f);
            velocities[i] = new Vector2(vx, vy);
            angularSpeeds[i] = UnityEngine.Random.Range(-360f, 360f);
        }

        bool allDone = false;
        while (!allDone)
        {
            allDone = true;
            float dt = Time.deltaTime;

            for (int i = 0; i < count; i++)
            {
                Transform cell = cells[i];
                if (cell == null || !cell.gameObject.activeSelf) continue;

                // 중력 적용
                velocities[i].y += gravity * dt;

                // 위치 업데이트
                Vector3 pos = cell.position;
                pos.x += velocities[i].x * dt;
                pos.y += velocities[i].y * dt;
                cell.position = pos;

                // 회전 업데이트
                cell.Rotate(0, 0, angularSpeeds[i] * dt);

                if (pos.y > bottomY)
                {
                    allDone = false;
                }
                else
                {
                    cell.rotation = Quaternion.identity;
                    ReturnCellToPool(cell.gameObject);
                }
            }

            yield return null;
        }

        onComplete?.Invoke();
    }
    #endregion

    #region 가비지 라인
    /// <summary>
    /// 가비지 라인 타이머 시작
    /// </summary>
    public void StartGarbageLines(Action onGameOver)
    {
        onGarbageGameOver = onGameOver;
        StopGarbageLines();
        garbageCoroutine = StartCoroutine(GarbageLineRoutine());
    }

    /// <summary>
    /// 가비지 라인 타이머 정지
    /// </summary>
    public void StopGarbageLines()
    {
        if (garbageCoroutine != null)
        {
            StopCoroutine(garbageCoroutine);
            garbageCoroutine = null;
        }
    }

    /// <summary>
    /// 현재 조작 중인 블록 참조 갱신
    /// </summary>
    public void SetCurrentBlock(Transform block)
    {
        currentBlockTransform = block;
    }

    private IEnumerator GarbageLineRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(garbageInterval);

            if (!PushGarbageLine(currentBlockTransform))
            {
                garbageCoroutine = null;
                onGarbageGameOver?.Invoke();
                yield break;
            }

            if (currentBlockTransform != null)
                ShowLandingPreview(currentBlockTransform);
        }
    }

    /// <summary>
    /// 밑에서 가비지 라인 1줄 올리기. 오버플로우 시 false 반환 (게임오버)
    /// </summary>
    public bool PushGarbageLine(Transform currentBlock)
    {
        // 최상단 행에 셀이 있으면 게임오버
        for (int x = 0; x < width; x++)
        {
            if (grid[x, height - 1] != null)
                return false;
        }

        // 애니메이션 대상 수집
        List<Transform> cellsToAnimate = new List<Transform>();

        // 그리드를 위로 1칸 시프트
        for (int y = height - 1; y > 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                grid[x, y] = grid[x, y - 1];
                if (grid[x, y] != null)
                    cellsToAnimate.Add(grid[x, y]);
            }
        }

        // row 0에 가비지 셀 생성 (랜덤 빈 칸)
        HashSet<int> emptyPositions = new HashSet<int>();
        int emptyCount = Mathf.Min(emptyCountPerLine, width - 1);
        while (emptyPositions.Count < emptyCount)
        {
            emptyPositions.Add(UnityEngine.Random.Range(0, width));
        }

        for (int x = 0; x < width; x++)
        {
            if (emptyPositions.Contains(x))
            {
                grid[x, 0] = null;
                continue;
            }
            GameObject cell = GetCellFromPool();
            cell.transform.position = new Vector3(x, -1, 0);
            cell.transform.SetParent(transform);
            grid[x, 0] = cell.transform;
            cellsToAnimate.Add(cell.transform);
        }

        // 현재 블록 위치를 즉시 위로 이동
        if (currentBlock != null)
            currentBlock.position += Vector3.up;

        // 부드러운 상승 애니메이션
        StartCoroutine(AnimateRise(cellsToAnimate));

        return true;
    }

    private IEnumerator AnimateRise(List<Transform> cells)
    {
        float duration = 0.15f;
        float elapsed = 0f;

        // 현재 위치를 시작점으로, +1이 목표
        Vector3[] startPositions = new Vector3[cells.Count];
        for (int i = 0; i < cells.Count; i++)
            startPositions[i] = cells[i].position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i] != null && cells[i].gameObject.activeSelf)
                {
                    Vector3 pos = startPositions[i];
                    pos.y += t;
                    cells[i].position = pos;
                }
            }

            yield return null;
        }

        // 최종 위치 스냅 (정확한 정수 좌표)
        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i] != null && cells[i].gameObject.activeSelf)
            {
                Vector3 pos = startPositions[i];
                pos.y += 1f;
                cells[i].position = pos;
            }
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
        // reparent 시 컬렉션 변경을 피하기 위해 먼저 수집
        int childCount = blockTransform.childCount;
        Transform[] children = new Transform[childCount];
        for (int i = 0; i < childCount; i++)
        {
            children[i] = blockTransform.GetChild(i);
        }

        foreach (Transform child in children)
        {
            int x = Mathf.RoundToInt(child.position.x);
            int y = Mathf.RoundToInt(child.position.y);

            if (y < height && x >= 0 && x < width && y >= 0)
            {
                child.SetParent(transform);
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
            totalLinesCleared += linesCleared;
            emptyCountPerLine = 1 + totalLinesCleared / 10;
            if (emptyCountPerLine > width - 1)
                emptyCountPerLine = width - 1;
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
    /// 블록 부모의 모든 자식 셀을 풀에 반환
    /// </summary>
    public void ReturnBlockCells(Transform blockTransform)
    {
        int childCount = blockTransform.childCount;
        Transform[] children = new Transform[childCount];
        for (int i = 0; i < childCount; i++)
        {
            children[i] = blockTransform.GetChild(i);
        }
        foreach (Transform child in children)
        {
            ReturnCellToPool(child.gameObject);
        }
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

    #region 블록 부모 오브젝트 풀
    /// <summary>
    /// 블록 부모(BlockController 포함) 오브젝트 풀 초기화
    /// </summary>
    private void CreateBlockParentPool()
    {
        blockParentPool = new Stack<GameObject>(BLOCK_PARENT_POOL_SIZE);

        GameObject containerObj = new GameObject("BlockParentPool");
        containerObj.transform.SetParent(transform);
        blockParentPoolContainer = containerObj.transform;

        for (int i = 0; i < BLOCK_PARENT_POOL_SIZE; i++)
        {
            GameObject blockParent = new GameObject("Block");
            blockParent.transform.SetParent(blockParentPoolContainer);
            blockParent.AddComponent<BlockController>();
            blockParent.SetActive(false);
            blockParentPool.Push(blockParent);
        }
    }

    /// <summary>
    /// 풀에서 블록 부모 가져오기 (부족하면 새로 생성)
    /// </summary>
    public GameObject GetBlockParentFromPool()
    {
        GameObject blockParent;
        if (blockParentPool.Count > 0)
        {
            blockParent = blockParentPool.Pop();
        }
        else
        {
            blockParent = new GameObject("Block");
            blockParent.AddComponent<BlockController>();
        }
        blockParent.transform.SetParent(null);
        blockParent.SetActive(true);
        return blockParent;
    }

    /// <summary>
    /// 블록 부모를 풀에 반환
    /// </summary>
    public void ReturnBlockParentToPool(GameObject blockParent)
    {
        blockParent.SetActive(false);
        blockParent.transform.SetParent(blockParentPoolContainer);
        blockParentPool.Push(blockParent);
    }
    #endregion

    #region 착지 미리보기
    /// <summary>
    /// 착지 미리보기 셀 풀 생성
    /// </summary>
    private void CreateLandingPreviewPool()
    {
        if (previewLandPrefab == null) return;

        GameObject containerObj = new GameObject("LandingPreviewPool");
        containerObj.transform.SetParent(transform);
        landingPreviewContainer = containerObj.transform;

        landingPreviewPool = new List<GameObject>(LANDING_PREVIEW_POOL_SIZE);
        for (int i = 0; i < LANDING_PREVIEW_POOL_SIZE; i++)
        {
            GameObject cell = Instantiate(previewLandPrefab, landingPreviewContainer);
            cell.transform.localScale = previewLandScale;
            cell.SetActive(false);
            landingPreviewPool.Add(cell);
        }
    }

    /// <summary>
    /// 현재 블록의 착지 위치에 미리보기 표시
    /// </summary>
    public void ShowLandingPreview(Transform blockTransform)
    {
        HideLandingPreview();

        if (blockTransform == null || landingPreviewPool == null) return;

        // 현재 블록의 각 셀 위치를 수집
        int childCount = blockTransform.childCount;
        Vector3[] cellPositions = new Vector3[childCount];
        for (int i = 0; i < childCount; i++)
        {
            cellPositions[i] = blockTransform.GetChild(i).position;
        }

        // 아래로 내릴 수 있는 최대 거리 계산
        int dropDistance = CalculateDropDistance(blockTransform);

        // 미리보기 셀 배치
        for (int i = 0; i < childCount && i < landingPreviewPool.Count; i++)
        {
            GameObject previewCell = landingPreviewPool[i];
            previewCell.transform.position = new Vector3(
                cellPositions[i].x,
                cellPositions[i].y - dropDistance,
                0f
            );
            previewCell.SetActive(true);
        }
    }

    /// <summary>
    /// 착지 미리보기 숨김
    /// </summary>
    public void HideLandingPreview()
    {
        if (landingPreviewPool == null) return;

        foreach (var cell in landingPreviewPool)
        {
            cell.SetActive(false);
        }
    }

    /// <summary>
    /// 블록이 떨어질 수 있는 최대 거리 계산
    /// </summary>
    private int CalculateDropDistance(Transform blockTransform)
    {
        int dropDistance = 0;

        while (true)
        {
            dropDistance++;
            bool valid = true;

            foreach (Transform child in blockTransform)
            {
                int x = Mathf.RoundToInt(child.position.x);
                int y = Mathf.RoundToInt(child.position.y) - dropDistance;

                if (!IsInsideBoard(x, y) || !IsEmpty(x, y))
                {
                    valid = false;
                    break;
                }
            }

            if (!valid)
            {
                return dropDistance - 1;
            }
        }
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
