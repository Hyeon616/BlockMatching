using UnityEngine;

/// <summary>
/// 보드 관련 프리팹과 설정을 한 곳에서 관리
/// </summary>
[CreateAssetMenu(fileName = "BoardConfig", menuName = "Puzzle/Board Config")]
public class BoardConfig : ScriptableObject
{
    [Header("배경")]
    public GameObject backgroundPrefab_Light;
    public GameObject backgroundPrefab_Dark;

    [Header("블록")]
    public GameObject tetrisBlockPrefab;
    public GameObject lineBlockPrefab;
    public GameObject previewLandPrefab;

    [Header("스케일")]
    [Range(1f, 2f)] public float scalePadding = 1.3f;

    // 캐시된 스케일 값
    private Vector3? bgScale1;
    private Vector3? bgScale2;
    private Vector3? tetrisScale;
    private Vector3? lineScale;
    private Vector3? landScale;

    public Vector3 BgScaleLight => bgScale1 ??= SpriteUtility.GetScaleForUnitSize(backgroundPrefab_Light, scalePadding);
    public Vector3 BgScaleDark => bgScale2 ??= SpriteUtility.GetScaleForUnitSize(backgroundPrefab_Dark, scalePadding);
    public Vector3 TetrisBlockScale => tetrisScale ??= SpriteUtility.GetScaleForUnitSize(tetrisBlockPrefab, scalePadding);
    public Vector3 LineBlockScale => lineScale ??= SpriteUtility.GetScaleForUnitSize(lineBlockPrefab, scalePadding);
    public Vector3 LandPreviewScale => landScale ??= SpriteUtility.GetScaleForUnitSize(previewLandPrefab, scalePadding);

    private void OnEnable()
    {
        // ScriptableObject 재로드 시 캐시 초기화
        bgScale1 = null;
        bgScale2 = null;
        tetrisScale = null;
        lineScale = null;
        landScale = null;
    }
}
