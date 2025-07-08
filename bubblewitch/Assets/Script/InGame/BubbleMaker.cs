using UnityEngine;
using UnityEditor; // Handles, Editor 네임스페이스 사용을 위해 필요

// EditorOnly 스크립트로 컴파일 시 포함되지 않도록 처리 (선택 사항)
// [ExecuteInEditMode] // 에디터 모드에서도 Update나 OnGUI 등이 실행되도록 함 (주의해서 사용)
public class BubbleMaker : MonoBehaviour
{

    [Tooltip("스포너의 X 위치를 그리드 경계 내로 제한합니다.")]
    public bool snapToGridX = true; // X축 그리드 스냅 활성화 여부
    [Tooltip("스포너의 Y 위치를 그리드 경계 내로 제한합니다.")]
    public bool snapToGridY = true; // Y축 그리드 스냅 활성화 여부

    private Vector3 lastPosition; // 이전 위치를 저장하여 변경 감지

    void Awake()
    {
        // 런타임 시에는 이 스크립트가 비활성화되거나 파괴될 수 있습니다.
        // 스포너는 주로 에디터에서만 위치를 설정하고 런타임에는 LaunchPoint만 사용하므로.
        // 하지만 Gizmo 기능 때문에 유지.
    }

    // OnDrawGizmos는 씬 뷰에 항상 기즈모를 그립니다. (오브젝트가 선택되지 않아도)
    void OnDrawGizmos()
    {
        // 기즈모는 런타임 빌드에 포함되지 않습니다.
        // #if UNITY_EDITOR // 이 조건문은 OnDrawGizmos에서는 필수는 아니지만, 안전을 위해 남겨둘 수 있습니다.
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(transform.position, 0.3f);
#if UNITY_EDITOR
        Handles.Label(this.transform.position + Vector3.up * 0.5f, "Bubble Maker");
#endif
        // #endif
    }
}