using UnityEngine;
using UnityEditor;

// CustomEditor 어트리뷰트를 사용하여 어떤 MonoBehaviour에 대한 에디터인지 지정합니다.
[CustomEditor(typeof(BubbleMakerPath))]
public class BubblePathEditor : Editor // Editor 클래스를 상속합니다.
{
    // 대상 스크립트의 참조를 가져옵니다.
    BubbleMakerPath spawner;

    void OnEnable()
    {
        // 에디터가 활성화될 때 대상 스크립트를 가져옵니다.
        spawner = (BubbleMakerPath)target;
    }

    // 씬 뷰에서 오브젝트가 선택되었을 때 호출됩니다.
    void OnSceneGUI()
    {
        if (spawner == null || StageManager.Instance.GridManager == null)
        {
            return;
        }

        // 트랜스폼의 변경이 있었는지 감지
        EditorGUI.BeginChangeCheck(); // 변경 감지 시작

        // 현재 오브젝트의 위치를 핸들로 표시 (선택적으로 이동 가능)
        Vector3 newPosition = Handles.PositionHandle(spawner.transform.position, spawner.transform.rotation);

        if (EditorGUI.EndChangeCheck()) // 변경이 감지되었다면
        {
            Undo.RecordObject(spawner.transform, "Move Bubble Spawner Path"); // Undo/Redo 기능 지원
            spawner.transform.position = newPosition; // 위치 업데이트

            // 여기에 기존 BubbleSpawner의 ApplyPositionConstraints 로직을 호출하거나 직접 구현합니다.
            // ApplyPositionConstraints는 private이므로, 아래에 다시 구현하거나 public으로 변경해야 합니다.
            ApplyEditorPositionConstraints(spawner.transform);
        }
    }

    /// <summary>
    /// 스포너의 위치를 그리드 경계 내로 제한합니다. (에디터 전용)
    /// BubbleSpawner.cs의 ApplyPositionConstraints와 유사하지만, Editor 스크립트에서 호출됩니다.
    /// </summary>
    private void ApplyEditorPositionConstraints(Transform spawnerTransform)
    {
        if (StageManager.Instance.GridManager == null) return;

        Vector2 currentPos = spawnerTransform.position;

        float halfBubbleRadius = StageManager.Instance.GridManager.bubbleRadius;
        float gridWidth = StageManager.Instance.GridManager.gridCols * halfBubbleRadius * 2;
        float gridHeight = StageManager.Instance.GridManager.gridRows * halfBubbleRadius * Mathf.Sqrt(3);

        // 그리드 시작 위치 (GridManager의 GetWorldPosition(0,0)을 참고하여 정확한 원점 설정)
        // 여기서는 임시로 (0,0)을 그리드 시작점으로 가정.
        float minX = 0f;
        float maxX = gridWidth - halfBubbleRadius * 2;
        float minY = 0f;
        float maxY = gridHeight;

        var gridPos = StageManager.Instance.GridManager.GetGridPosition(currentPos);
        var changedPos = StageManager.Instance.GridManager.GetWorldPosition(gridPos.x, gridPos.y);

        // X축 제한
        // 스포너의 중앙이 그리드 내에 있도록 (또는 버블이 그리드 내에서 발사되도록)
        changedPos.x = Mathf.Clamp(changedPos.x, minX - halfBubbleRadius, maxX + halfBubbleRadius);

        // Y축 제한 (스포너는 보통 그리드 아래에 위치하므로, 이 범위는 조절 필요)
        changedPos.y = Mathf.Clamp(changedPos.y, minY - halfBubbleRadius, maxY);

        if (currentPos != changedPos)
        {
            spawnerTransform.position = changedPos;
        }
    }
}