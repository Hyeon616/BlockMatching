using UnityEngine;

/// <summary>
/// 블록 데이터를 기반으로 블록을 생성하는 클래스
/// </summary>
public class BlockMaker : MonoBehaviour
{
    [Header("블록 설정")]
    [SerializeField] private BlockData[] blockDataList;

    [Header("참조")]
    [SerializeField] private BoardView board;
    [SerializeField] private SoundManager soundManager;

    /// <summary>
    /// 랜덤 BlockData 반환
    /// </summary>
    public BlockData GetRandomBlockData()
    {
        if (blockDataList == null || blockDataList.Length == 0)
            return null;

        int randomIndex = Random.Range(0, blockDataList.Length);
        return blockDataList[randomIndex];
    }

    /// <summary>
    /// BlockData로 플레이 가능한 블록 생성 (Board 풀에서 셀을 가져옴)
    /// </summary>
    public GameObject CreateBlock(BlockData data)
    {
        if (data == null || board == null)
            return null;

        GameObject blockParent = board.GetBlockParentFromPool();
        blockParent.name = data.BlockName;

        Vector2Int[] filledCells = data.GetFilledCells();

        // 블록의 최소 좌표 계산
        int minX = int.MaxValue, minY = int.MaxValue;
        foreach (var cell in filledCells)
        {
            if (cell.x < minX) minX = cell.x;
            if (cell.y < minY) minY = cell.y;
        }

        // 보드 상단 중앙에서 생성 (정수 좌표)
        int spawnX = (board.Width / 2) - 1;
        int spawnY = board.Height - 2;
        blockParent.transform.position = new Vector3(spawnX, spawnY, 0);

        // 풀에서 셀을 가져와 배치
        foreach (Vector2Int cell in filledCells)
        {
            GameObject cellObj = board.GetCellFromPool();
            cellObj.transform.SetParent(blockParent.transform);
            cellObj.transform.localPosition = new Vector3(cell.x - minX, cell.y - minY, 0);
        }

        // BlockController 초기화 (풀에서 가져온 오브젝트에 이미 컴포넌트 존재)
        BlockController controller = blockParent.GetComponent<BlockController>();
        bool valid = controller.Initialize(board, soundManager);

        if (!valid)
        {
            // 게임오버: 셀을 풀에 반환하고 블록 부모도 반환
            board.ReturnBlockCells(blockParent.transform);
            board.ReturnBlockParentToPool(blockParent);
            return null;
        }

        return blockParent;
    }

    /// <summary>
    /// 랜덤 블록 생성
    /// </summary>
    private GameObject CreateRandomBlock()
    {
        BlockData data = GetRandomBlockData();
        return CreateBlock(data);
    }

}
