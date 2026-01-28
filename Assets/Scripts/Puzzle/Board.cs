using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    [Header("Board Settings")]
    [SerializeField] private int width = 16;
    [SerializeField] private int height = 27;

    [Header("Prefab")]
    [SerializeField] private GameObject brownStonePrefab;
    [SerializeField, Range(1f, 2f)] private float scalePadding = 1.3f;

    private GameObject[,] backgroundTiles;

    void Start()
    {
        CreateBackground();
        SetupCamera();
    }

    void CreateBackground()
    {
        backgroundTiles = new GameObject[width, height];

        Vector3 scale = GetScaleForUnitSize(brownStonePrefab);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 position = new Vector3(x, y, 0);
                GameObject tile = Instantiate(brownStonePrefab, position, Quaternion.identity, transform);
                tile.transform.localScale = scale;
                tile.name = $"BG_{x}_{y}";
                backgroundTiles[x, y] = tile;
            }
        }
    }

    private Vector3 GetScaleForUnitSize(GameObject prefab)
    {
        SpriteRenderer sr = prefab.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null)
            return Vector3.one;

        Vector2 spriteSize = sr.sprite.bounds.size;
        return new Vector3(scalePadding / spriteSize.x, scalePadding / spriteSize.y, 1f);
    }

    /// <summary>
    /// 보드 전체가 보이도록 카메라 설정
    /// </summary>
    void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        // 보드 중심 위치 계산
        float centerX = (width - 1) / 2f;
        float centerY = (height - 1) / 2f;
        cam.transform.position = new Vector3(centerX, centerY, -10f);

        // 화면 비율에 맞게 orthographicSize 계산
        float screenRatio = (float)Screen.width / Screen.height;
        float boardRatio = (float)width / height;

        if (screenRatio >= boardRatio)
        {
            // 세로 기준
            cam.orthographicSize = height / 2f;
        }
        else
        {
            // 가로 기준
            cam.orthographicSize = width / 2f / screenRatio;
        }
    }
}