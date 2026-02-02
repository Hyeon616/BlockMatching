using System;
using UnityEngine;

/// <summary>
/// 블록의 이동과 회전을 담당하는 컨트롤러
/// </summary>
public class BlockController : MonoBehaviour
{
    [Header("이동 설정")]
    [SerializeField] private float moveInterval = 0.1f;
    [SerializeField] private float softDropInterval = 0.05f;

    [Header("자동 낙하 설정")]
    [SerializeField] private float fallInterval = 1f;

    private Board board;
    private SoundManager soundManager;
    private float lastMoveTime;
    private float lastFallTime;
    private bool isActive;

    /// <summary>
    /// 블록이 착지했을 때 호출되는 이벤트
    /// </summary>
    public event Action OnLanded;



    /// <summary>
    /// 블록 초기화 (참조 주입). 스폰 위치가 유효하면 true, 게임오버면 false 반환
    /// </summary>
    public bool Initialize(Board board, SoundManager soundManager)
    {
        this.board = board;
        this.soundManager = soundManager;
        isActive = true;
        lastMoveTime = 0f;
        lastFallTime = Time.time;
        OnLanded = null;

        if (!IsValidPosition())
        {
            isActive = false;
            return false;
        }

        board.ShowLandingPreview(transform);
        return true;
    }

    void Update()
    {
        if (!isActive || board == null) return;

        HandleInput();
        HandleAutoFall();
    }

    void HandleInput()
    {
        // 좌우 이동
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
        {
            if (Time.time - lastMoveTime > moveInterval)
            {
                Move(Vector3.left);
                lastMoveTime = Time.time;
            }
        }
        else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
        {
            if (Time.time - lastMoveTime > moveInterval)
            {
                Move(Vector3.right);
                lastMoveTime = Time.time;
            }
        }

        // 소프트 드롭
        if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
        {
            if (Time.time - lastMoveTime > softDropInterval)
            {
                if (!Move(Vector3.down))
                {
                    Land();
                }
                lastMoveTime = Time.time;
            }
        }

        // 회전
        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            Rotate();
        }

        // 하드 드롭
        if (Input.GetKeyDown(KeyCode.Space))
        {
            HardDrop();
        }
    }

    void HandleAutoFall()
    {
        if (Time.time - lastFallTime > fallInterval)
        {
            if (!Move(Vector3.down))
            {
                Land();
            }
            lastFallTime = Time.time;
        }
    }

    bool Move(Vector3 direction)
    {
        transform.position += direction;

        if (!IsValidPosition())
        {
            transform.position -= direction;
            return false;
        }

        board.ShowLandingPreview(transform);
        return true;
    }

    void Rotate()
    {
        // 원본 위치 저장
        int childCount = transform.childCount;
        Vector3[] originalPositions = new Vector3[childCount];
        int i = 0;
        foreach (Transform child in transform)
        {
            originalPositions[i++] = child.localPosition;
        }

        // 회전 적용
        ApplyRotation();

        // 유효하면 성공
        if (IsValidPosition())
        {
            board.ShowLandingPreview(transform);
            return;
        }

        // Wall Kick 시도: 좌, 우, 상 이동
        Vector3[] wallKicks = { Vector3.left, Vector3.right, Vector3.up,
                                Vector3.left * 2, Vector3.right * 2 };

        foreach (Vector3 kick in wallKicks)
        {
            transform.position += kick;
            if (IsValidPosition())
            {
                board.ShowLandingPreview(transform);
                return;
            }
            transform.position -= kick;
        }

        // 모든 시도 실패 시 원본 복원
        i = 0;
        foreach (Transform child in transform)
        {
            child.localPosition = originalPositions[i++];
        }
    }

    void ApplyRotation()
    {
        if (transform.childCount == 0) return;

        // 모든 셀의 정수 위치 수집
        int[] posX = new int[transform.childCount];
        int[] posY = new int[transform.childCount];

        for (int i = 0; i < transform.childCount; i++)
        {
            posX[i] = Mathf.RoundToInt(transform.GetChild(i).localPosition.x);
            posY[i] = Mathf.RoundToInt(transform.GetChild(i).localPosition.y);
        }

        // 바운딩 박스 계산
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;
        for (int i = 0; i < transform.childCount; i++)
        {
            if (posX[i] < minX) minX = posX[i];
            if (posX[i] > maxX) maxX = posX[i];
            if (posY[i] < minY) minY = posY[i];
            if (posY[i] > maxY) maxY = posY[i];
        }

        int width = maxX - minX;

        // 각 셀 회전 (minX, minY 기준으로 시계 방향 90도)
        for (int i = 0; i < transform.childCount; i++)
        {
            int localX = posX[i] - minX;
            int localY = posY[i] - minY;

            // 시계 방향 90도: (x, y) -> (y, width - x)
            int newLocalX = localY;
            int newLocalY = width - localX;

            transform.GetChild(i).localPosition = new Vector3(minX + newLocalX, minY + newLocalY, 0);
        }
    }

    void HardDrop()
    {
        int maxDrop = board.Height + 4;
        int count = 0;
        while (Move(Vector3.down) && count < maxDrop) { count++; }
        Land();
    }

    void Land()
    {
        if (!isActive) return;

        isActive = false;

        board.HideLandingPreview();

        soundManager.Play(SoundType.Land);

        // 블록을 보드에 등록
        board.PlaceBlock(transform);

        // 자식 셀이 모두 Board로 이전된 후 빈 부모를 풀에 반환
        board.ReturnBlockParentToPool(gameObject);

        // 중력 + 연쇄 클리어 (완료 후 다음 블록 스폰)
        board.ApplyGravityAndClear(() =>
        {
            OnLanded?.Invoke();
        });
    }

    bool IsValidPosition()
    {
        foreach (Transform child in transform)
        {
            Vector3 pos = child.position;
            int x = Mathf.RoundToInt(pos.x);
            int y = Mathf.RoundToInt(pos.y);

            // Board의 유효성 검사 메서드 사용
            if (!board.IsInsideBoard(x, y) || !board.IsEmpty(x, y))
            {
                return false;
            }
        }
        return true;
    }
}
