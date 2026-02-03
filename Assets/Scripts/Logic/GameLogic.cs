/// <summary>
/// 게임 상태와 점수 관리 (Unity 의존성 없음)
/// </summary>
public class GameLogic
{
    public enum GameState
    {
        Idle,
        Playing,
        GameOver
    }

    private GameState state;
    private int score;
    private int totalLinesCleared;
    private int level;

    public GameState State => state;
    public int Score => score;
    public int TotalLinesCleared => totalLinesCleared;
    public int Level => level;

    // 이벤트
    public event System.Action<int> OnScoreChanged;
    public event System.Action<int> OnLevelChanged;
    public event System.Action<int> OnLinesClearedChanged;

    public GameLogic()
    {
        state = GameState.Idle;
        score = 0;
        totalLinesCleared = 0;
        level = 1;
    }

    /// <summary>
    /// 게임 시작
    /// </summary>
    public void StartGame()
    {
        state = GameState.Playing;
        score = 0;
        totalLinesCleared = 0;
        level = 1;

        // 초기값 이벤트 발생
        OnScoreChanged?.Invoke(score);
        OnLevelChanged?.Invoke(level);
        OnLinesClearedChanged?.Invoke(totalLinesCleared);
    }

    /// <summary>
    /// 게임 오버
    /// </summary>
    public void GameOver()
    {
        state = GameState.GameOver;
    }

    /// <summary>
    /// 게임 재시작
    /// </summary>
    public void Reset()
    {
        state = GameState.Idle;
        score = 0;
        totalLinesCleared = 0;
        level = 1;

        // 초기값 이벤트 발생
        OnScoreChanged?.Invoke(score);
        OnLevelChanged?.Invoke(level);
        OnLinesClearedChanged?.Invoke(totalLinesCleared);
    }

    /// <summary>
    /// 라인 클리어 점수 추가
    /// </summary>
    /// <param name="linesCleared">한 번에 클리어한 라인 수 (연쇄 포함)</param>
    /// <returns>레벨이 올랐으면 true</returns>
    public bool AddScore(int linesCleared)
    {
        if (linesCleared <= 0)
            return false;

        int previousLevel = level;

        // 점수 계산: 1줄=100, 2줄=400, 3줄=800, 4줄=1600...
        score += linesCleared == 1 ? 100 : 100 * (1 << linesCleared);
        OnScoreChanged?.Invoke(score);

        // 총 클리어 라인 수 증가
        totalLinesCleared += linesCleared;
        OnLinesClearedChanged?.Invoke(totalLinesCleared);

        // 레벨 계산: 10줄마다 레벨 1 증가
        level = 1 + totalLinesCleared / 10;

        bool levelUp = level > previousLevel;
        if (levelUp)
        {
            OnLevelChanged?.Invoke(level);
        }

        return levelUp;
    }

    /// <summary>
    /// 현재 레벨에 따른 가비지 라인 빈 칸 수
    /// </summary>
    public int GetGarbageEmptyCount(int maxWidth)
    {
        int emptyCount = level;
        if (emptyCount > maxWidth - 1)
            emptyCount = maxWidth - 1;
        return emptyCount;
    }
}
