using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 다음 블록 미리보기 관리
/// </summary>
public class PreviewManager : MonoBehaviour
{
    private const int PREVIEW_POOL_SIZE = 16;

    private BoardConfig config;
    private int previewSize;
    private Transform previewRoot;
    private Transform previewContainer;
    private List<GameObject> previewPool;

    /// <summary>
    /// 미리보기 영역 초기화
    /// </summary>
    public void Initialize(BoardConfig config, int previewSize, Transform previewRoot)
    {
        this.config = config;
        this.previewSize = previewSize;
        this.previewRoot = previewRoot;
        CreatePreviewArea();
    }

    private void CreatePreviewArea()
    {
        if (config.backgroundPrefab_Light == null || config.backgroundPrefab_Dark == null ||
            config.tetrisBlockPrefab == null || previewRoot == null) return;

        // 미리보기 배경 생성
        for (int x = 0; x < previewSize; x++)
        {
            for (int y = 0; y < previewSize; y++)
            {
                bool isEven = (x + y) % 2 == 0;
                GameObject prefab = isEven ? config.backgroundPrefab_Light : config.backgroundPrefab_Dark;
                Vector3 scale = isEven ? config.BgScaleLight : config.BgScaleDark;

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
            GameObject cell = Instantiate(config.tetrisBlockPrefab, previewContainer);
            cell.transform.localScale = config.TetrisBlockScale;
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

        int blockWidth = maxX - minX + 1;
        int blockHeight = maxY - minY + 1;

        float offsetX = (previewSize - blockWidth) / 2f;
        float offsetY = (previewSize - blockHeight) / 2f;

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
    public void ClearPreviewBlock()
    {
        if (previewPool == null) return;

        foreach (var cell in previewPool)
        {
            cell.SetActive(false);
        }
    }
}
