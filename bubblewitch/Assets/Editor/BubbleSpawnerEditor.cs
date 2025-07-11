using UnityEngine;
using UnityEditor;

// CustomEditor ��Ʈ����Ʈ�� ����Ͽ� � MonoBehaviour�� ���� ���������� �����մϴ�.
[CustomEditor(typeof(BubbleMaker))]
public class BubbleSpawnerEditor : Editor // Editor Ŭ������ ����մϴ�.
{
    // ��� ��ũ��Ʈ�� ������ �����ɴϴ�.
    BubbleMaker spawner;

    void OnEnable()
    {
        // �����Ͱ� Ȱ��ȭ�� �� ��� ��ũ��Ʈ�� �����ɴϴ�.
        spawner = (BubbleMaker)target;
    }

    // �� �信�� ������Ʈ�� ���õǾ��� �� ȣ��˴ϴ�.
    void OnSceneGUI()
    {
        if (spawner == null || StageManager.Instance.GridManager == null)
        {
            return;
        }

        // Ʈ�������� ������ �־����� ����
        EditorGUI.BeginChangeCheck(); // ���� ���� ����

        // ���� ������Ʈ�� ��ġ�� �ڵ�� ǥ�� (���������� �̵� ����)
        Vector3 newPosition = Handles.PositionHandle(spawner.transform.position, spawner.transform.rotation);

        if (EditorGUI.EndChangeCheck()) // ������ �����Ǿ��ٸ�
        {
            Undo.RecordObject(spawner.transform, "Move Bubble Spawner"); // Undo/Redo ��� ����
            spawner.transform.position = newPosition; // ��ġ ������Ʈ

            // ���⿡ ���� BubbleSpawner�� ApplyPositionConstraints ������ ȣ���ϰų� ���� �����մϴ�.
            // ApplyPositionConstraints�� private�̹Ƿ�, �Ʒ��� �ٽ� �����ϰų� public���� �����ؾ� �մϴ�.
            ApplyEditorPositionConstraints(spawner.transform);
        }
    }

    /// <summary>
    /// �������� ��ġ�� �׸��� ��� ���� �����մϴ�. (������ ����)
    /// BubbleSpawner.cs�� ApplyPositionConstraints�� ����������, Editor ��ũ��Ʈ���� ȣ��˴ϴ�.
    /// </summary>
    private void ApplyEditorPositionConstraints(Transform spawnerTransform)
    {
        if (StageManager.Instance.GridManager == null) return;

        Vector2 currentPos = spawnerTransform.position;

        float halfBubbleRadius = StageManager.Instance.GridManager.bubbleRadius;
        float gridWidth = StageManager.Instance.GridManager.gridCols * halfBubbleRadius * Mathf.Sqrt(3);
        float gridHeight = StageManager.Instance.GridManager.gridRows * halfBubbleRadius * 1.5f;

        // �׸��� ���� ��ġ
        float minX = StageManager.Instance.GridManager.offset_x;
        float maxX = StageManager.Instance.GridManager.offset_x + gridWidth - halfBubbleRadius * 2;
        float maxY = StageManager.Instance.GridManager.offset_y;
        float minY = StageManager.Instance.GridManager.offset_y - gridHeight;

        var gridPos = StageManager.Instance.GridManager.GetGridPosition(currentPos);
        var changedPos = StageManager.Instance.GridManager.GetWorldPosition(gridPos.x, gridPos.y);

        // X�� ����
        changedPos.x = Mathf.Clamp(changedPos.x, minX - halfBubbleRadius, maxX + halfBubbleRadius);

        // Y�� ����
        changedPos.y = Mathf.Clamp(changedPos.y, minY + halfBubbleRadius, maxY);

        if (currentPos != changedPos)
        {
            spawnerTransform.position = changedPos;
        }
    }
}