using UnityEditor;
using UnityEngine;

public class BubbleMakerPath : MonoBehaviour
{
    public int PathNum { get; set; }

    // OnDrawGizmos는 씬 뷰에 항상 기즈모를 그립니다. (오브젝트가 선택되지 않아도)
    void OnDrawGizmos()
    {
        // 기즈모는 런타임 빌드에 포함되지 않습니다.
        // #if UNITY_EDITOR // 이 조건문은 OnDrawGizmos에서는 필수는 아니지만, 안전을 위해 남겨둘 수 있습니다.
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(transform.position, 0.3f);
#if UNITY_EDITOR
        Handles.Label(this.transform.position + Vector3.up * 0.5f, $"Bubble Path {PathNum}");
#endif
        // #endif
    }
}
