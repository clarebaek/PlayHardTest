using UnityEditor;
using UnityEngine;

public class BubbleMakerPath : MonoBehaviour
{
    public int PathNum { get; set; }

    // OnDrawGizmos�� �� �信 �׻� ����� �׸��ϴ�. (������Ʈ�� ���õ��� �ʾƵ�)
    void OnDrawGizmos()
    {
        // ������ ��Ÿ�� ���忡 ���Ե��� �ʽ��ϴ�.
        // #if UNITY_EDITOR // �� ���ǹ��� OnDrawGizmos������ �ʼ��� �ƴ�����, ������ ���� ���ܵ� �� �ֽ��ϴ�.
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(transform.position, 0.3f);
#if UNITY_EDITOR
        Handles.Label(this.transform.position + Vector3.up * 0.5f, $"Bubble Path {PathNum}");
#endif
        // #endif
    }
}
