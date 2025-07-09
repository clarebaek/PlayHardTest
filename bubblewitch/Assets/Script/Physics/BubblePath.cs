using UnityEngine;
using System.Collections.Generic;

// [ExecuteInEditMode] // ������ �ٷ� Ȯ���ϰ� ���� �� ��� (����: ���� ����)
public class BubblePath : MonoBehaviour
{
    // ����� �� ���� (���� ��ǥ)
    public List<Vector2> pathPoints = new List<Vector2>();

    // ��ΰ� ���� ������� (������ �������� ó�� �������� �ٽ� ����Ǵ���)
    public bool isClosedPath = false;

    // ����� �ð�ȭ�� ���� �ʺ� (���� ����)
    public float pathWidth = 1f;

    /// <summary>
    /// ���� ��� ������ ���� ��ǥ�� ��ȯ�Ͽ� ��ȯ�մϴ�.
    /// </summary>
    public Vector2 GetWorldPoint(int index)
    {
        if (index >= 0 && index < pathPoints.Count)
        {
            return new Vector2(transform.position.x, transform.position.y) + pathPoints[index];
        }
        return transform.position; // ��ȿ���� ���� �ε����� ������Ʈ ��ġ ��ȯ
    }

    // �� �信�� ��θ� �ð������� �����ֱ� ���� Gizmo
    void OnDrawGizmos()
    {
        if (pathPoints == null || pathPoints.Count < 2) return;

        Gizmos.color = Color.yellow; // ��� ���� ����

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
                nextPoint = GetWorldPoint(0); // ���� ����� ��� ���������� ����
            }
            else
            {
                break; // ������ �����̸� �� �׸� �ʿ� ����
            }

            Gizmos.DrawLine(currentPoint, nextPoint);
            Gizmos.DrawSphere(currentPoint, 0.1f); // �� ������ ���� ��ü ǥ��
        }

        // ��� �ʺ� �ð������� ǥ�� (���� ����)
        Gizmos.color = new Color(1, 0.5f, 0, 0.2f); // ��Ȳ�� ������
        for (int i = 0; i < pathPoints.Count; i++)
        {
            Vector3 currentPoint = GetWorldPoint(i);
            Gizmos.DrawWireSphere(currentPoint, pathWidth / 2); // ��ü�� �ƴ� ���� ���ϸ� Handles.DrawWireDisc ��� (Editor ��ũ��Ʈ����)
        }
    }
}