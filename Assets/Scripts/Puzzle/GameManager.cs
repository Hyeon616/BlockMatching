using TMPro;
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
    [SerializeField] private TMP_Text clearLineText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text highScoreText;

    private const string HIGH_SCORE_KEY = "HighScore";
    private int highScore;
    private BlockController currentBlock;
    private BlockData nextBlockData;

    void Start()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(StartGame);
        }

        highScore = PlayerPrefs.GetInt(HIGH_SCORE_KEY, 0);
        UpdateHighScoreUI();
    }

    /// <summary>
    /// 게임 시작 (버튼에서 호출)
    /// </summary>
    public void StartGame()
    {
        soundManager.Play(SoundType.UIClick);

        if (startButton != null)
        {
            startButton.gameObject.SetActive(false);
        }

        board.ClearBoard();
        UpdateUI();

        // 첫 번째 다음 블록 준비
        PrepareNextBlock();

        // 현재 블록 생성
        SpawnBlock();

        // 가비지 라인 타이머 시작
        board.StartGarbageLines(GameOver);

        // BGM 재생
        soundManager.PlayBGM();
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

        UpdateUI();

        // 새 블록 생성
        SpawnBlock();
    }

    void UpdateUI()
    {
        if (clearLineText != null)
            clearLineText.text = board.TotalLinesCleared.ToString();
        if (levelText != null)
            levelText.text = board.Level.ToString();
        if (scoreText != null)
            scoreText.text = board.Score.ToString();
    }

    void UpdateHighScoreUI()
    {
        if (highScoreText != null)
            highScoreText.text = highScore.ToString();
    }

    void TrySaveHighScore()
    {
        int currentScore = board.Score;
        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt(HIGH_SCORE_KEY, highScore);
            PlayerPrefs.Save();
            UpdateHighScoreUI();
        }
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
        TrySaveHighScore();
        soundManager.StopBGM();
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
