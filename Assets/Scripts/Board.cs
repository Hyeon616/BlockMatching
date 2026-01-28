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
    [SerializeField, Range(1f, 2f)] private float scalePadding = 1.1f;

    private GameObject[,] backgroundTiles;

    void Start()
    {
        CreateBackground();
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
}