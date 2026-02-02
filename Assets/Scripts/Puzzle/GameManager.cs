using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 흐름을 관리하는 매니저
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private BlockMaker blockMaker;
    [SerializeField] private Board board;
    [SerializeField] private SoundManager soundManager;

    [Header("UI")]
    [SerializeField] private Button startButton;

    private BlockController currentBlock;
    private BlockData nextBlockData;

    void Start()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(StartGame);
        }
    }

    /// <summary>
    /// 게임 시작 (버튼에서 호출)
    /// </summary>
    public void StartGame()
    {
        if (startButton != null)
        {
            startButton.gameObject.SetActive(false);
        }

        // 첫 번째 다음 블록 준비
        PrepareNextBlock();

        // 현재 블록 생성
        SpawnBlock();

        // 가비지 라인 타이머 시작
        board.StartGarbageLines(GameOver);
    }

    /// <summary>
    /// 다음 블록 데이터 준비 및 미리보기 표시
    /// </summary>
    void PrepareNextBlock()
    {
        nextBlockData = blockMaker.GetRandomBlockData();

        if (board != null)
        {
            board.ShowPreviewBlock(nextBlockData);
        }
    }

    /// <summary>
    /// 현재 블록 생성 (다음 블록 데이터 사용)
    /// </summary>
    void SpawnBlock()
    {
        if (nextBlockData == null)
        {
            nextBlockData = blockMaker.GetRandomBlockData();
        }

        GameObject blockObj = blockMaker.CreateBlock(nextBlockData);
        if (blockObj == null)
        {
            GameOver();
            return;
        }

        currentBlock = blockObj.GetComponent<BlockController>();
        if (currentBlock != null)
        {
            currentBlock.OnLanded += OnBlockLanded;
            board.SetCurrentBlock(blockObj.transform);
        }

        // 다음 블록 준비
        PrepareNextBlock();
    }

    /// <summary>
    /// 블록 착지 시 호출
    /// </summary>
    void OnBlockLanded()
    {
        if (currentBlock != null)
        {
            currentBlock.OnLanded -= OnBlockLanded;
            currentBlock = null;
        }

        // 새 블록 생성
        SpawnBlock();
    }

    /// <summary>
    /// 게임오버 처리
    /// </summary>
    void GameOver()
    {
        // 현재 낙하 중인 블록의 셀을 보드 그리드에 등록 
        if (currentBlock != null)
        {
            board.PlaceBlock(currentBlock.transform);
            board.ReturnBlockParentToPool(currentBlock.gameObject);
        }
        currentBlock = null;
        nextBlockData = null;

        board.StopGarbageLines();
        board.SetCurrentBlock(null);
        soundManager.Play(SoundType.GameOver);

        board.PlayGameOverEffect(() =>
        {
            if (startButton != null)
            {
                startButton.gameObject.SetActive(true);
            }
        });
    }
}
