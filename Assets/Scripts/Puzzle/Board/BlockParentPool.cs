using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 블록 부모 오브젝트 풀 관리
/// </summary>
public class BlockParentPool : MonoBehaviour
{
    private const int BLOCK_PARENT_POOL_SIZE = 2;
    private Stack<GameObject> blockParentPool;
    private Transform blockParentPoolContainer;

    /// <summary>
    /// 블록 부모 풀 초기화
    /// </summary>
    public void Initialize()
    {
        CreateBlockParentPool();
    }

    /// <summary>
    /// 블록 부모(BlockController 포함) 오브젝트 풀 초기화
    /// </summary>
    private void CreateBlockParentPool()
    {
        blockParentPool = new Stack<GameObject>(BLOCK_PARENT_POOL_SIZE);

        GameObject containerObj = new GameObject("BlockParentPool");
        containerObj.transform.SetParent(transform);
        blockParentPoolContainer = containerObj.transform;

        for (int i = 0; i < BLOCK_PARENT_POOL_SIZE; i++)
        {
            GameObject blockParent = new GameObject("Block");
            blockParent.transform.SetParent(blockParentPoolContainer);
            blockParent.AddComponent<BlockController>();
            blockParent.SetActive(false);
            blockParentPool.Push(blockParent);
        }
    }

    /// <summary>
    /// 풀에서 블록 부모 가져오기
    /// </summary>
    public GameObject GetBlockParent()
    {
        GameObject blockParent;
        if (blockParentPool.Count > 0)
        {
            blockParent = blockParentPool.Pop();
        }
        else
        {
            blockParent = new GameObject("Block");
            blockParent.AddComponent<BlockController>();
        }
        blockParent.transform.SetParent(null);
        blockParent.SetActive(true);
        return blockParent;
    }

    /// <summary>
    /// 블록 부모를 풀에 반환
    /// </summary>
    public void ReturnBlockParent(GameObject blockParent)
    {
        blockParent.SetActive(false);
        blockParent.transform.SetParent(blockParentPoolContainer);
        blockParentPool.Push(blockParent);
    }
}
