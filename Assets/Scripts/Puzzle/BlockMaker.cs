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

    [Header("생성 위치")]
    [SerializeField] private Transform spawnPoint;

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
        blockParent.transform.position = spawnPoint != null ? spawnPoint.position : Vector3.zero;

        Vector2Int[] filledCells = data.GetFilledCells();

        foreach (Vector2Int cell in filledCells)
        {
            GameObject cellObj = Instantiate(creamStonePrefab, blockParent.transform);
            cellObj.transform.localPosition = new Vector3(cell.x, cell.y, 0);
            cellObj.transform.localScale = cellScale;
        }

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