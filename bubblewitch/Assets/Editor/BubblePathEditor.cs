using UnityEngine;
using UnityEditor;

// CustomEditor ��Ʈ����Ʈ�� ����Ͽ� � MonoBehaviour�� ���� ���������� �����մϴ�.
[CustomEditor(typeof(BubbleMakerPath))]
public class BubblePathEditor : Editor // Editor Ŭ������ ����մϴ�.
{
    // ��� ��ũ��Ʈ�� ������ �����ɴϴ�.
    BubbleMakerPath spawner;

    void OnEnable()
    {
        // �����Ͱ� Ȱ��ȭ�� �� ��� ��ũ��Ʈ�� �����ɴϴ�.
        spawner = (BubbleMakerPath)target;
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
            Undo.RecordObject(spawner.transform, "Move Bubble Spawner Path"); // Undo/Redo ��� ����
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
        float gridWidth = StageManager.Instance.GridManager.gridCols * halfBubbleRadius * 2;
        float gridHeight = StageManager.Instance.GridManager.gridRows * halfBubbleRadius * Mathf.Sqrt(3);

        // �׸��� ���� ��ġ (GridManager�� GetWorldPosition(0,0)�� �����Ͽ� ��Ȯ�� ���� ����)
        // ���⼭�� �ӽ÷� (0,0)�� �׸��� ���������� ����.
        float minX = 0f;
        float maxX = gridWidth - halfBubbleRadius * 2;
        float minY = 0f;
        float maxY = gridHeight;

        var gridPos = StageManager.Instance.GridManager.GetGridPosition(currentPos);
        var changedPos = StageManager.Instance.GridManager.GetWorldPosition(gridPos.x, gridPos.y);

        // X�� ����
        // �������� �߾��� �׸��� ���� �ֵ��� (�Ǵ� ������ �׸��� ������ �߻�ǵ���)
        changedPos.x = Mathf.Clamp(changedPos.x, minX - halfBubbleRadius, maxX + halfBubbleRadius);

        // Y�� ���� (�����ʴ� ���� �׸��� �Ʒ��� ��ġ�ϹǷ�, �� ������ ���� �ʿ�)
        changedPos.y = Mathf.Clamp(changedPos.y, minY - halfBubbleRadius, maxY);

        if (currentPos != changedPos)
        {
            spawnerTransform.position = changedPos;
        }
    }
}