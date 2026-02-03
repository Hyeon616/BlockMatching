using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 테트리스 블록 셀과 가비지 라인 셀 풀 관리
/// </summary>
public class CellPoolManager : MonoBehaviour
{
    private const int CELL_POOL_INITIAL_SIZE = 64;
    private const int LINE_POOL_INITIAL_SIZE = 32;

    private BoardConfig config;

    private Stack<GameObject> cellPool;
    private Transform cellPoolContainer;

    private Stack<GameObject> lineCellPool;
    private Transform lineCellPoolContainer;
    private HashSet<GameObject> lineCellSet = new HashSet<GameObject>();

    /// <summary>
    /// 셀 풀 초기화
    /// </summary>
    public void Initialize(BoardConfig config)
    {
        this.config = config;
        CreateCellPool();
        CreateLineCellPool();
    }

    private void CreateCellPool()
    {
        cellPool = new Stack<GameObject>(CELL_POOL_INITIAL_SIZE);

        GameObject containerObj = new GameObject("CellPool");
        containerObj.transform.SetParent(transform);
        cellPoolContainer = containerObj.transform;

        if (config.tetrisBlockPrefab == null) return;

        for (int i = 0; i < CELL_POOL_INITIAL_SIZE; i++)
        {
            GameObject cell = Instantiate(config.tetrisBlockPrefab, cellPoolContainer);
            cell.transform.localScale = config.TetrisBlockScale;
            cell.SetActive(false);
            cellPool.Push(cell);
        }
    }

    private void CreateLineCellPool()
    {
        lineCellPool = new Stack<GameObject>(LINE_POOL_INITIAL_SIZE);

        GameObject containerObj = new GameObject("LineCellPool");
        containerObj.transform.SetParent(transform);
        lineCellPoolContainer = containerObj.transform;

        if (config.lineBlockPrefab == null) return;

        for (int i = 0; i < LINE_POOL_INITIAL_SIZE; i++)
        {
            GameObject cell = Instantiate(config.lineBlockPrefab, lineCellPoolContainer);
            cell.transform.localScale = config.LineBlockScale;
            cell.SetActive(false);
            lineCellPool.Push(cell);
        }
    }

    /// <summary>
    /// 풀에서 테트리스 셀 가져오기
    /// </summary>
    public GameObject GetTetrisCell()
    {
        GameObject cell;
        if (cellPool.Count > 0)
        {
            cell = cellPool.Pop();
        }
        else
        {
            cell = Instantiate(config.tetrisBlockPrefab, cellPoolContainer);
            cell.transform.localScale = config.TetrisBlockScale;
        }
        cell.SetActive(true);
        cell.transform.rotation = Quaternion.identity;
        return cell;
    }

    /// <summary>
    /// 풀에서 가비지 라인 셀 가져오기
    /// </summary>
    public GameObject GetLineCell()
    {
        GameObject cell;
        if (lineCellPool.Count > 0)
        {
            cell = lineCellPool.Pop();
        }
        else
        {
            cell = Instantiate(config.lineBlockPrefab, lineCellPoolContainer);
            cell.transform.localScale = config.LineBlockScale;
        }
        cell.SetActive(true);
        cell.transform.rotation = Quaternion.identity;
        lineCellSet.Add(cell);
        return cell;
    }

    /// <summary>
    /// 셀을 적절한 풀에 반환
    /// </summary>
    public void ReturnCell(GameObject cell)
    {
        cell.SetActive(false);

        if (lineCellSet.Remove(cell))
        {
            cell.transform.SetParent(lineCellPoolContainer);
            lineCellPool.Push(cell);
        }
        else
        {
            cell.transform.SetParent(cellPoolContainer);
            cellPool.Push(cell);
        }
    }

    /// <summary>
    /// 블록 부모의 모든 자식 셀을 풀에 반환
    /// </summary>
    public void ReturnBlockCells(Transform blockTransform)
    {
        int childCount = blockTransform.childCount;
        Transform[] children = new Transform[childCount];
        for (int i = 0; i < childCount; i++)
        {
            children[i] = blockTransform.GetChild(i);
        }
        foreach (Transform child in children)
        {
            ReturnCell(child.gameObject);
        }
    }

    /// <summary>
    /// 모든 라인 셀 추적 정보 초기화
    /// </summary>
    public void ClearLineCellTracking()
    {
        lineCellSet.Clear();
    }
}
