using UnityEngine;
using UnityEditor; // Handles, Editor ���ӽ����̽� ����� ���� �ʿ�

// EditorOnly ��ũ��Ʈ�� ������ �� ���Ե��� �ʵ��� ó�� (���� ����)
// [ExecuteInEditMode] // ������ ��忡���� Update�� OnGUI ���� ����ǵ��� �� (�����ؼ� ���)
public class BubbleMaker : MonoBehaviour
{

    [Tooltip("�������� X ��ġ�� �׸��� ��� ���� �����մϴ�.")]
    public bool snapToGridX = true; // X�� �׸��� ���� Ȱ��ȭ ����
    [Tooltip("�������� Y ��ġ�� �׸��� ��� ���� �����մϴ�.")]
    public bool snapToGridY = true; // Y�� �׸��� ���� Ȱ��ȭ ����

    private Vector3 lastPosition; // ���� ��ġ�� �����Ͽ� ���� ����

    void Awake()
    {
        // ��Ÿ�� �ÿ��� �� ��ũ��Ʈ�� ��Ȱ��ȭ�ǰų� �ı��� �� �ֽ��ϴ�.
        // �����ʴ� �ַ� �����Ϳ����� ��ġ�� �����ϰ� ��Ÿ�ӿ��� LaunchPoint�� ����ϹǷ�.
        // ������ Gizmo ��� ������ ����.
    }

    // OnDrawGizmos�� �� �信 �׻� ����� �׸��ϴ�. (������Ʈ�� ���õ��� �ʾƵ�)
    void OnDrawGizmos()
    {
        // ������ ��Ÿ�� ���忡 ���Ե��� �ʽ��ϴ�.
        // #if UNITY_EDITOR // �� ���ǹ��� OnDrawGizmos������ �ʼ��� �ƴ�����, ������ ���� ���ܵ� �� �ֽ��ϴ�.
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(transform.position, 0.3f);
#if UNITY_EDITOR
        Handles.Label(this.transform.position + Vector3.up * 0.5f, "Bubble Maker");
#endif
        // #endif
    }
}