using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 블록 착지 미리보기 관리
/// </summary>
public class LandingPreviewManager : MonoBehaviour
{
    private const int LANDING_PREVIEW_POOL_SIZE = 16;

    private BoardConfig config;
    private List<GameObject> landingPreviewPool;
    private Transform landingPreviewContainer;

    /// <summary>
    /// 착지 미리보기 풀 초기화
    /// </summary>
    public void Initialize(BoardConfig config)
    {
        this.config = config;
        CreateLandingPreviewPool();
    }

    private void CreateLandingPreviewPool()
    {
        if (config.previewLandPrefab == null) return;

        GameObject containerObj = new GameObject("LandingPreviewPool");
        containerObj.transform.SetParent(transform);
        landingPreviewContainer = containerObj.transform;

        landingPreviewPool = new List<GameObject>(LANDING_PREVIEW_POOL_SIZE);
        for (int i = 0; i < LANDING_PREVIEW_POOL_SIZE; i++)
        {
            GameObject cell = Instantiate(config.previewLandPrefab, landingPreviewContainer);
            cell.transform.localScale = config.LandPreviewScale;
            cell.SetActive(false);
            landingPreviewPool.Add(cell);
        }
    }

    /// <summary>
    /// 현재 블록의 착지 위치에 미리보기 표시
    /// </summary>
    public void ShowLandingPreview(Transform blockTransform, int dropDistance)
    {
        HideLandingPreview();

        if (blockTransform == null || landingPreviewPool == null) return;

        int childCount = blockTransform.childCount;
        Vector3[] cellPositions = new Vector3[childCount];
        for (int i = 0; i < childCount; i++)
        {
            cellPositions[i] = blockTransform.GetChild(i).position;
        }

        for (int i = 0; i < childCount && i < landingPreviewPool.Count; i++)
        {
            GameObject previewCell = landingPreviewPool[i];
            previewCell.transform.position = new Vector3(
                cellPositions[i].x,
                cellPositions[i].y - dropDistance,
                0f
            );
            previewCell.SetActive(true);
        }
    }

    /// <summary>
    /// 착지 미리보기 숨김
    /// </summary>
    public void HideLandingPreview()
    {
        if (landingPreviewPool == null) return;

        foreach (var cell in landingPreviewPool)
        {
            cell.SetActive(false);
        }
    }
}
