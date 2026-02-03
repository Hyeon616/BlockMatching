using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보드 애니메이션 담당 (중력, 가비지 라인 상승, 게임오버)
/// </summary>
public class BoardAnimator : MonoBehaviour
{
    private Coroutine riseCoroutine;

    /// <summary>
    /// 셀 낙하 애니메이션
    /// </summary>
    public IEnumerator AnimateFall(List<Transform> cells, List<int> distances)
    {
        float duration = 0.1f;
        float elapsed = 0f;

        // 시작 위치 기록
        Vector3[] startPositions = new Vector3[cells.Count];
        for (int i = 0; i < cells.Count; i++)
            startPositions[i] = cells[i].position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i] != null && cells[i].gameObject.activeSelf)
                {
                    Vector3 pos = startPositions[i];
                    pos.y -= distances[i] * t;
                    cells[i].position = pos;
                }
            }

            yield return null;
        }

        // 최종 위치 이동
        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i] != null && cells[i].gameObject.activeSelf)
            {
                Vector3 pos = startPositions[i];
                pos.y -= distances[i];
                cells[i].position = pos;
            }
        }
    }

    /// <summary>
    /// 가비지 라인 상승 애니메이션
    /// </summary>
    public void StartRiseAnimation(List<Transform> cells)
    {
        if (riseCoroutine != null)
            StopCoroutine(riseCoroutine);
        riseCoroutine = StartCoroutine(AnimateRise(cells));
    }

    /// <summary>
    /// 가비지 라인 상승 애니메이션 정지
    /// </summary>
    public void StopRiseAnimation()
    {
        if (riseCoroutine != null)
        {
            StopCoroutine(riseCoroutine);
            riseCoroutine = null;
        }
    }

    private IEnumerator AnimateRise(List<Transform> cells)
    {
        float duration = 0.15f;
        float elapsed = 0f;

        // 현재 위치를 시작점으로, +1이 목표
        Vector3[] startPositions = new Vector3[cells.Count];
        for (int i = 0; i < cells.Count; i++)
            startPositions[i] = cells[i].position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            for (int i = 0; i < cells.Count; i++)
            {
                if (cells[i] != null && cells[i].gameObject.activeSelf)
                {
                    Vector3 pos = startPositions[i];
                    pos.y += t;
                    cells[i].position = pos;
                }
            }

            yield return null;
        }

        // 최종 위치
        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i] != null && cells[i].gameObject.activeSelf)
            {
                Vector3 pos = startPositions[i];
                pos.y += 1f;
                cells[i].position = pos;
            }
        }
    }

    /// <summary>
    /// 게임오버 연출: 모든 블록이 위로 튀어올랐다가 아래로 떨어짐
    /// </summary>
    public IEnumerator GameOverEffect(List<Transform> cells, Action<GameObject> returnCellCallback)
    {
        float gravity = -75f;
        float bottomY = -5f;
        float maxDuration = 5f;
        float elapsed = 0f;
        int count = cells.Count;

        // 각 셀의 속도와 회전속도를 미리 계산
        Vector2[] velocities = new Vector2[count];
        float[] angularSpeeds = new float[count];

        for (int i = 0; i < count; i++)
        {
            cells[i].SetParent(null);

            float vx = UnityEngine.Random.Range(-8f, 8f);
            float vy = UnityEngine.Random.Range(10f, 25f);
            velocities[i] = new Vector2(vx, vy);
            angularSpeeds[i] = UnityEngine.Random.Range(-360f, 360f);
        }

        bool allDone = false;
        while (!allDone && elapsed < maxDuration)
        {
            allDone = true;
            float dt = Time.deltaTime;
            elapsed += dt;

            for (int i = 0; i < count; i++)
            {
                Transform cell = cells[i];
                if (cell == null || !cell.gameObject.activeSelf) continue;

                // 중력 적용
                velocities[i].y += gravity * dt;

                // 위치 업데이트
                Vector3 pos = cell.position;
                pos.x += velocities[i].x * dt;
                pos.y += velocities[i].y * dt;
                cell.position = pos;

                // 회전 업데이트
                cell.Rotate(0, 0, angularSpeeds[i] * dt);

                if (pos.y > bottomY)
                {
                    allDone = false;
                }
                else
                {
                    cell.rotation = Quaternion.identity;
                    returnCellCallback(cell.gameObject);
                }
            }

            yield return null;
        }

        // 타임아웃 시 남은 셀 정리
        for (int i = 0; i < count; i++)
        {
            if (cells[i] != null && cells[i].gameObject.activeSelf)
            {
                cells[i].rotation = Quaternion.identity;
                returnCellCallback(cells[i].gameObject);
            }
        }
    }
}
