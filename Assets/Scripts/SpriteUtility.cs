using UnityEngine;

/// <summary>
/// 스프라이트 관련 유틸리티
/// </summary>
public static class SpriteUtility
{
    /// <summary>
    /// 프리팹을 1x1 유닛 크기에 맞추기 위한 스케일 계산
    /// </summary>
    public static Vector3 GetScaleForUnitSize(GameObject prefab, float padding = 1.3f)
    {
        SpriteRenderer sr = prefab.GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null)
            return Vector3.one;

        Vector2 spriteSize = sr.sprite.bounds.size;
        return new Vector3(padding / spriteSize.x, padding / spriteSize.y, 1f);
    }
}
