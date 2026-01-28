using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 흐름을 관리하는 매니저
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private BlockMaker blockMaker;

    [Header("UI")]
    [SerializeField] private Button startButton;

    private BlockController currentBlock;

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

        SpawnBlock();
    }

    /// <summary>
    /// 새 블록 생성
    /// </summary>
    void SpawnBlock()
    {
        GameObject blockObj = blockMaker.CreateRandomBlock();
        if (blockObj != null)
        {
            currentBlock = blockObj.GetComponent<BlockController>();
            if (currentBlock != null)
            {
                currentBlock.OnLanded += OnBlockLanded;
            }
        }
    }

    /// <summary>
    /// 블록 착지 시 호출
    /// </summary>
    void OnBlockLanded()
    {
        if (currentBlock != null)
        {
            currentBlock.OnLanded -= OnBlockLanded;
        }

        // 새 블록 생성
        SpawnBlock();
    }
}