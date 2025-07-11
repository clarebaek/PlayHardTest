using UnityEditor;
using UnityEngine;

public class BubbleMakerPath : MonoBehaviour
{
    public int PathNum { get; set; }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(transform.position, 0.3f);
        Handles.Label(this.transform.position + Vector3.up * 0.5f, $"Bubble Path {PathNum}");
    }
#endif
}
