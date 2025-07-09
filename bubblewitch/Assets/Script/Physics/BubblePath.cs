using UnityEngine;
using System.Collections.Generic;

// [ExecuteInEditMode] // 씬에서 바로 확인하고 싶을 때 사용 (주의: 성능 저하)
public class BubblePath : MonoBehaviour
{
    // 경로의 각 지점 (로컬 좌표)
    public List<Vector2> pathPoints = new List<Vector2>();

    // 경로가 닫힌 경로인지 (마지막 지점에서 처음 지점으로 다시 연결되는지)
    public bool isClosedPath = false;

    // 경로의 시각화를 위한 너비 (선택 사항)
    public float pathWidth = 1f;

    /// <summary>
    /// 로컬 경로 지점을 월드 좌표로 변환하여 반환합니다.
    /// </summary>
    public Vector2 GetWorldPoint(int index)
    {
        if (index >= 0 && index < pathPoints.Count)
        {
            return new Vector2(transform.position.x, transform.position.y) + pathPoints[index];
        }
        return transform.position; // 유효하지 않은 인덱스면 오브젝트 위치 반환
    }

    // 씬 뷰에서 경로를 시각적으로 보여주기 위한 Gizmo
    void OnDrawGizmos()
    {
        if (pathPoints == null || pathPoints.Count < 2) return;

        Gizmos.color = Color.yellow; // 경로 라인 색상

        for (int i = 0; i < pathPoints.Count; i++)
        {
            Vector3 currentPoint = GetWorldPoint(i);
            Vector3 nextPoint;

            if (i < pathPoints.Count - 1)
            {
                nextPoint = GetWorldPoint(i + 1);
            }
            else if (isClosedPath)
            {
                nextPoint = GetWorldPoint(0); // 닫힌 경로일 경우 시작점으로 연결
            }
            else
            {
                break; // 마지막 지점이면 더 그릴 필요 없음
            }

            Gizmos.DrawLine(currentPoint, nextPoint);
            Gizmos.DrawSphere(currentPoint, 0.1f); // 각 지점에 작은 구체 표시
        }

        // 경로 너비를 시각적으로 표시 (선택 사항)
        Gizmos.color = new Color(1, 0.5f, 0, 0.2f); // 주황색 반투명
        for (int i = 0; i < pathPoints.Count; i++)
        {
            Vector3 currentPoint = GetWorldPoint(i);
            Gizmos.DrawWireSphere(currentPoint, pathWidth / 2); // 구체가 아닌 원을 원하면 Handles.DrawWireDisc 사용 (Editor 스크립트에서)
        }
    }
}