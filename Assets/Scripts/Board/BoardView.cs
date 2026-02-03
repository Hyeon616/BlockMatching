using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보드 메인 컴포넌트 - 모든 모듈을 조합하고 로직과 뷰를 연결
/// </summary>
public class BoardView : MonoBehaviour
{
    [Header("보드 설정")]
    [SerializeField] private int width = 16;
    [SerializeField] private int height = 27;
    [SerializeField] private BoardConfig config;

    [Header("미리보기")]
    [SerializeField] private int previewSize = 4;
    [SerializeField] private Transform previewRoot;

    [Header("참조")]
    [SerializeField] private SoundManager soundManager;

    [Header("가비지 라인 설정")]
    [SerializeField] private float garbageInterval = 1f;

    public int Width => width;
    public int Height => height;

    // 로직
    private BoardLogic logic;
    private GameLogic gameLogic;

    // Transform 그리드 (로직과 매핑)
    private Transform[,] transformGrid;
    private Transform placedBlockContainer;
    private Transform currentBlockTransform;

    // 모듈들
    private BoardRenderer boardRenderer;
    private PreviewManager previewManager;
    private LandingPreviewManager landingPreviewManager;
    private CellPoolManager cellPoolManager;
    private BlockParentPool blockParentPool;
    private BoardAnimator boardAnimator;

    // 가비지 라인
    private Coroutine garbageCoroutine;
    private System.Action onGarbageGameOver;
    private bool isProcessingGravity;
    private Coroutine gravityCoroutine;
    private System.Random random = new System.Random();

    // 게임 로직 접근
    public int TotalLinesCleared => gameLogic.TotalLinesCleared;
    public int Level => gameLogic.Level;
    public int Score => gameLogic.Score;

    // UI 업데이트 이벤트
    public event System.Action<int> OnScoreChanged;
    public event System.Action<int> OnLevelChanged;
    public event System.Action<int> OnLinesClearedChanged;

    private void Start()
    {
        // 로직 초기화
        logic = new BoardLogic(width, height);
        gameLogic = new GameLogic();
        transformGrid = new Transform[width, height];

        // GameLogic 이벤트 구독
        gameLogic.OnScoreChanged += (score) => OnScoreChanged?.Invoke(score);
        gameLogic.OnLevelChanged += (level) => OnLevelChanged?.Invoke(level);
        gameLogic.OnLinesClearedChanged += (lines) => OnLinesClearedChanged?.Invoke(lines);

        // 컨테이너 생성
        CreatePlacedBlockContainer();

        // 모듈 초기화
        boardRenderer = gameObject.AddComponent<BoardRenderer>();
        boardRenderer.Initialize(config, width, height);

        previewManager = gameObject.AddComponent<PreviewManager>();
        previewManager.Initialize(config, previewSize, previewRoot);

        landingPreviewManager = gameObject.AddComponent<LandingPreviewManager>();
        landingPreviewManager.Initialize(config);

        cellPoolManager = gameObject.AddComponent<CellPoolManager>();
        cellPoolManager.Initialize(config);

        blockParentPool = gameObject.AddComponent<BlockParentPool>();
        blockParentPool.Initialize();

        boardAnimator = gameObject.AddComponent<BoardAnimator>();
    }

    private void CreatePlacedBlockContainer()
    {
        GameObject obj = new GameObject("Block");
        obj.transform.SetParent(transform);
        obj.transform.localPosition = Vector3.zero;
        placedBlockContainer = obj.transform;
    }

    #region 공개 API

    /// <summary>
    /// 보드 초기화
    /// </summary>
    public void ClearBoard()
    {
        logic.Clear();
        gameLogic.Reset();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (transformGrid[x, y] != null)
                {
                    cellPoolManager.ReturnCell(transformGrid[x, y].gameObject);
                    transformGrid[x, y] = null;
                }
            }
        }

        landingPreviewManager.HideLandingPreview();
        previewManager.ClearPreviewBlock();
        cellPoolManager.ClearLineCellTracking();
    }

    /// <summary>
    /// 다음 블록 미리보기 표시
    /// </summary>
    public void ShowPreviewBlock(BlockData data)
    {
        previewManager.ShowPreviewBlock(data);
    }

    /// <summary>
    /// 현재 조작 중인 블록 설정
    /// </summary>
    public void SetCurrentBlock(Transform block)
    {
        currentBlockTransform = block;
    }

    /// <summary>
    /// 블록 부모 가져오기
    /// </summary>
    public GameObject GetBlockParentFromPool()
    {
        return blockParentPool.GetBlockParent();
    }

    /// <summary>
    /// 블록 부모 반환
    /// </summary>
    public void ReturnBlockParentToPool(GameObject blockParent)
    {
        blockParentPool.ReturnBlockParent(blockParent);
    }

    /// <summary>
    /// 테트리스 셀 가져오기
    /// </summary>
    public GameObject GetCellFromPool()
    {
        return cellPoolManager.GetTetrisCell();
    }

    /// <summary>
    /// 블록 셀들 반환
    /// </summary>
    public void ReturnBlockCells(Transform blockTransform)
    {
        cellPoolManager.ReturnBlockCells(blockTransform);
    }

    /// <summary>
    /// 특정 위치가 보드 범위 내인지
    /// </summary>
    public bool IsInsideBoard(int x, int y)
    {
        return logic.IsInsideBoard(x, y);
    }

    /// <summary>
    /// 특정 위치가 비어있는지
    /// </summary>
    public bool IsEmpty(int x, int y)
    {
        return logic.IsEmpty(x, y);
    }

    /// <summary>
    /// 착지 미리보기 표시
    /// </summary>
    public void ShowLandingPreview(Transform blockTransform)
    {
        List<(int x, int y)> blockCells = GetBlockCells(blockTransform);
        int dropDistance = logic.CalculateDropDistance(blockCells);
        landingPreviewManager.ShowLandingPreview(blockTransform, dropDistance);
    }

    /// <summary>
    /// 착지 미리보기 숨김
    /// </summary>
    public void HideLandingPreview()
    {
        landingPreviewManager.HideLandingPreview();
    }

    private List<(int x, int y)> GetBlockCells(Transform blockTransform)
    {
        List<(int x, int y)> cells = new List<(int x, int y)>();
        foreach (Transform child in blockTransform)
        {
            int x = Mathf.RoundToInt(child.position.x);
            int y = Mathf.RoundToInt(child.position.y);
            cells.Add((x, y));
        }
        return cells;
    }

    private bool IsBlockPositionValid(Transform blockTransform)
    {
        foreach (Transform child in blockTransform)
        {
            int x = Mathf.RoundToInt(child.position.x);
            int y = Mathf.RoundToInt(child.position.y);
            if (!logic.IsInsideBoard(x, y) || !logic.IsEmpty(x, y))
                return false;
        }
        return true;
    }

    #endregion

    #region 블록 배치 및 중력

    /// <summary>
    /// 블록을 보드에 배치
    /// </summary>
    public void PlaceBlock(Transform blockTransform)
    {
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
                child.SetParent(placedBlockContainer);
                transformGrid[x, y] = child;
                logic.SetCell(x, y, BoardLogic.TETRIS_BLOCK);
            }
            else
            {
                cellPoolManager.ReturnCell(child.gameObject);
            }
        }
    }

    /// <summary>
    /// 중력 및 라인 클리어 (연쇄)
    /// </summary>
    public void ApplyGravityAndClear(Action onComplete)
    {
        if (gravityCoroutine != null)
        {
            StopCoroutine(gravityCoroutine);
            gravityCoroutine = null;
            isProcessingGravity = false;
        }

        gravityCoroutine = StartCoroutine(GravityAndClearCoroutine(onComplete));
    }

    private IEnumerator GravityAndClearCoroutine(Action onComplete)
    {
        StopAnimationsAndSnapCells();
        isProcessingGravity = true;

        const int maxChainIterations = 50;
        int chainCount = 0;
        int totalLinesClearedThisDrop = 0;

        while (chainCount < maxChainIterations)
        {
            // 1. 중력 적용
            List<CellMove> gravityMoves = logic.CalculateGravity();
            if (gravityMoves.Count > 0)
            {
                List<Transform> fallingCells = new List<Transform>();
                List<int> fallDistances = new List<int>();
                foreach (var move in gravityMoves)
                {
                    Transform cell = transformGrid[move.fromX, move.fromY];
                    transformGrid[move.toX, move.toY] = cell;
                    transformGrid[move.fromX, move.fromY] = null;
                    fallingCells.Add(cell);
                    fallDistances.Add(move.distance);
                }
                yield return StartCoroutine(boardAnimator.AnimateFall(fallingCells, fallDistances));
            }

            // 2. 완성 줄 제거
            List<int> fullLines = logic.GetFullLines();
            if (fullLines.Count == 0)
                break;

            soundManager.Play(SoundType.ClearLine);

            foreach (int y in fullLines)
            {
                for (int x = 0; x < width; x++)
                {
                    if (transformGrid[x, y] != null)
                    {
                        cellPoolManager.ReturnCell(transformGrid[x, y].gameObject);
                        transformGrid[x, y] = null;
                    }
                }
                logic.ClearLine(y);
            }

            totalLinesClearedThisDrop += fullLines.Count;

            chainCount++;
            yield return null;
        }

        // 연쇄 완료 후 총 클리어 수로 점수 계산
        if (totalLinesClearedThisDrop > 0)
        {
            bool levelUp = gameLogic.AddScore(totalLinesClearedThisDrop);
            if (levelUp)
                soundManager.Play(SoundType.LevelUp);
        }

        isProcessingGravity = false;
        gravityCoroutine = null;

        onComplete?.Invoke();
    }

    private void StopAnimationsAndSnapCells()
    {
        boardAnimator.StopRiseAnimation();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (transformGrid[x, y] != null)
                    transformGrid[x, y].position = new Vector3(x, y, 0);
            }
        }
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
        gameLogic.StartGame();
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

    private IEnumerator GarbageLineRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(garbageInterval);

            while (isProcessingGravity)
                yield return null;

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

    private bool PushGarbageLine(Transform currentBlock)
    {
        // 1. 빈 행 제거
        List<CellMove> compressMoves = logic.CompressEmptyRows();
        foreach (var move in compressMoves)
        {
            Transform cell = transformGrid[move.fromX, move.fromY];
            transformGrid[move.toX, move.toY] = cell;
            transformGrid[move.fromX, move.fromY] = null;
            if (cell != null)
                cell.position = new Vector3(move.toX, move.toY, 0);
        }

        // 2. 그리드 시프트
        List<CellMove> shiftMoves = logic.ShiftGridUp();
        if (shiftMoves == null)
            return false;

        List<Transform> cellsToAnimate = new List<Transform>();
        foreach (var move in shiftMoves)
        {
            Transform cell = transformGrid[move.fromX, move.fromY];
            transformGrid[move.toX, move.toY] = cell;
            transformGrid[move.fromX, move.fromY] = null;
            if (cell != null)
                cellsToAnimate.Add(cell);
        }

        // 3. row 0에 가비지 라인 생성
        int emptyCount = gameLogic.GetGarbageEmptyCount(width);
        HashSet<int> emptyPositions = logic.GetGarbageEmptyPositions(emptyCount, random);
        logic.SetGarbageLineAtBottom(emptyPositions);

        for (int x = 0; x < width; x++)
        {
            if (emptyPositions.Contains(x))
            {
                transformGrid[x, 0] = null;
            }
            else
            {
                GameObject cell = cellPoolManager.GetLineCell();
                cell.transform.position = new Vector3(x, -1, 0);
                cell.transform.SetParent(placedBlockContainer);
                transformGrid[x, 0] = cell.transform;
                cellsToAnimate.Add(cell.transform);
            }
        }

        // 4. 현재 블록: 올린 뒤 내릴 수 있으면 즉시 내림 (시각적 낙하 유지)
        if (currentBlock != null)
        {
            currentBlock.position += Vector3.up;
            currentBlock.position += Vector3.down;
            if (!IsBlockPositionValid(currentBlock))
                currentBlock.position += Vector3.up;
        }

        // 5. 애니메이션
        boardAnimator.StartRiseAnimation(cellsToAnimate);

        return true;
    }

    #endregion

    #region 게임오버

    /// <summary>
    /// 게임오버 연출
    /// </summary>
    public void PlayGameOverEffect(Action onComplete)
    {
        StopAllCoroutines();
        garbageCoroutine = null;
        gravityCoroutine = null;
        isProcessingGravity = false;

        // 모든 셀을 정수 좌표로 이동
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (transformGrid[x, y] != null)
                    transformGrid[x, y].position = new Vector3(x, y, 0);

        landingPreviewManager.HideLandingPreview();
        previewManager.ClearPreviewBlock();

        List<Transform> cells = new List<Transform>();
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (transformGrid[x, y] != null)
                {
                    cells.Add(transformGrid[x, y]);
                    transformGrid[x, y] = null;
                }
            }
        }

        logic.Clear();
        gameLogic.GameOver();

        if (cells.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        StartCoroutine(GameOverCoroutine(cells, onComplete));
    }

    private IEnumerator GameOverCoroutine(List<Transform> cells, Action onComplete)
    {
        yield return StartCoroutine(boardAnimator.GameOverEffect(cells, (cell) =>
        {
            cellPoolManager.ReturnCell(cell);
        }));

        onComplete?.Invoke();
    }

    #endregion
}
