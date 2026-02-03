using UnityEngine;

/// <summary>
/// 보드 배경과 카메라 설정 담당
/// </summary>
public class BoardRenderer : MonoBehaviour
{
    private BoardConfig config;

    /// <summary>
    /// 배경 생성 및 카메라 설정
    /// </summary>
    public void Initialize(BoardConfig config, int width, int height)
    {
        this.config = config;
        CreateBackground(width, height);
        SetupCamera(width, height);
    }

    private void CreateBackground(int width, int height)
    {
        GameObject bgObj = new GameObject("BG");
        bgObj.transform.SetParent(transform);
        bgObj.transform.localPosition = Vector3.zero;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                bool isEven = (x + y) % 2 == 0;
                GameObject prefab = isEven ? config.backgroundPrefab_Light : config.backgroundPrefab_Dark;
                Vector3 scale = isEven ? config.BgScaleLight : config.BgScaleDark;

                Vector3 position = new Vector3(x, y, 0);
                GameObject tile = Instantiate(prefab, position, Quaternion.identity, bgObj.transform);
                tile.transform.localScale = scale;
                tile.name = $"BG_{x}_{y}";
            }
        }
    }

    private void SetupCamera(int width, int height)
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        float centerX = (width - 1) / 2f;
        float centerY = (height - 1) / 2f;
        cam.transform.position = new Vector3(centerX, centerY, -10f);

        float screenRatio = (float)Screen.width / Screen.height;
        float boardRatio = (float)width / height;

        if (screenRatio >= boardRatio)
            cam.orthographicSize = height / 2f;
        else
            cam.orthographicSize = width / 2f / screenRatio;
    }
}
