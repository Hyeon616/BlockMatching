using UnityEngine;

/// <summary>
/// 블록 데이터를 기반으로 블록을 생성하는 클래스
/// </summary>
public class BlockMaker : MonoBehaviour
{
    [Header("블록 설정")]
    [SerializeField] private BlockData[] blockDataList;
    [SerializeField] private GameObject creamStonePrefab;
    [SerializeField, Range(1f, 2f)] private float scalePadding = 1.3f;

    [Header("참조")]
    [SerializeField] private Board board;

    private Vector3 cellScale;

    void Awake()
    {
        cellScale = GetScaleForUnitSize(creamStonePrefab);
    }

    /// <summary>
    /// 인덱스로 블록 생성
    /// </summary>
    public GameObject CreateBlock(int index)
    {
        if (index < 0 || index >= blockDataList.Length)
            return null;

        return CreateBlock(blockDataList[index]);
    }

    /// <summary>
    /// BlockData로 블록 생성
    /// </summary>
    public GameObject CreateBlock(BlockData data)
    {
        if (data == null || creamStonePrefab == null)
            return null;

        GameObject blockParent = new GameObject(data.blockName);

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
        int spawnY = board.Height - 4;
        blockParent.transform.position = new Vector3(spawnX, spawnY, 0);

        // 셀 생성 (정수 좌표 사용)
        foreach (Vector2Int cell in filledCells)
        {
            GameObject cellObj = Instantiate(creamStonePrefab, blockParent.transform);
            // 최소 좌표를 기준으로 0부터 시작하도록 오프셋
            cellObj.transform.localPosition = new Vector3(cell.x - minX, cell.y - minY, 0);
            cellObj.transform.localScale = cellScale;
        }

        // BlockController 추가 및 초기화
        BlockController controller = blockParent.AddComponent<BlockController>();
        controller.Initialize(board);

        return blockParent;
    }

    /// <summary>
    /// 랜덤 블록 생성
    /// </summary>
    public GameObject CreateRandomBlock()
    {
        if (blockDataList == null || blockDataList.Length == 0)
            return null;

        int randomIndex = Random.Range(0, blockDataList.Length);
        return CreateBlock(randomIndex);
    }

    Vector3 GetScaleForUnitSize(GameObject prefab)
    {
        SpriteRenderer sr = prefab.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null)
            return Vector3.one;

        Vector2 spriteSize = sr.sprite.bounds.size;
        return new Vector3(scalePadding / spriteSize.x, scalePadding / spriteSize.y, 1f);
    }
}
