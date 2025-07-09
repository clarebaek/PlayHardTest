using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Bubble : MonoBehaviour
{
    [SerializeField]
    GameObject _normalTypeGO;
    [SerializeField]
    GameObject _fairyTypeGO;
    [SerializeField]
    GameObject _bombTypeGO;

    private bool _isLaunched;
    public eBubbleType bubbleType { get; private set; }
    public eBubbleColor bubbleColor { get; private set; }

    private void _SetData()
    {

    }

    public void SetType(eBubbleType type, eBubbleColor color,bool isLaunched = false)
    {
        bubbleType = type;
        bubbleColor = color;
        _isLaunched = isLaunched;
        _SetView();
    }

    private void _SetView()
    {
        _normalTypeGO.SetActive(bubbleType == eBubbleType.NORMAL);
        _fairyTypeGO.SetActive(bubbleType == eBubbleType.FAIRY);
        _bombTypeGO.SetActive(bubbleType == eBubbleType.BOMB || bubbleType == eBubbleType.CAT_BOMB);

        GameObject targetGO = bubbleType switch
        {
            eBubbleType.NORMAL => _normalTypeGO,
            eBubbleType.FAIRY => _fairyTypeGO,
            eBubbleType.CAT_BOMB => _bombTypeGO,
            eBubbleType.BOMB => _bombTypeGO,
            _ => _normalTypeGO,
        };

        if(targetGO.TryGetComponent<SpriteRenderer>(out var sprite))
        {
            sprite.color = bubbleColor switch
            {
                eBubbleColor.RED => Color.red,
                eBubbleColor.YELLOW => Color.yellow,
                eBubbleColor.BLUE => Color.blue,
                eBubbleColor.SPECIAL => Color.black,
                _ => Color.white,
            };
        }
    }

    /// <summary>
    /// �ٸ� �ݶ��̴��� ���������� �浹���� �� ȣ��˴ϴ�.
    /// �� �Լ��� ����Ϸ��� �ش� �ݶ��̴��� Is Trigger�� �����Ǿ� �־�� �մϴ�.
    /// </summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_isLaunched == false)
            return;

        // �浹�� ������Ʈ�� ���̾ �����ɴϴ�.
        int otherLayerNumber = collision.gameObject.layer;
        string otherLayerName = LayerMask.LayerToName(otherLayerNumber);

        // Debug.Log($"������ {collision.gameObject.name} (Layer: {otherLayerName})�� �浹!");

        // 1. "Bubble" ���̾��� �ٸ� ���� �ε����� �� ���߱�
        if (otherLayerName == "Bubble")
        {
            // Debug.Log("�ٸ� ���� �浹�Ͽ� ����ϴ�.");
            //_StopBubbleMovement();
            // TODO: ������ �׸��忡 ���̴� ���� (���߿� ����)
            // ���� ���, GridManager.AttachBubble(this.gameObject, collision.contacts[0].point);
        }
        // 2. "Wall" ���̾��� ���� �ε����� �� (�ݻ� �� ���� �Ǵ� ��� �̵�)
        else if (otherLayerName == "Wall")
        {
            // ���� �ε����� �Ϲ������� ������ ƨ��Ƿ� �ٷ� ������ �ʰ�,
            // �ٸ� ���� ���� ������ ��� �̵��ϴ� ���� �Ϲ����Դϴ�.
            // ���⼭�� Ư���� ���ߴ� ������ ���� �ʽ��ϴ�.
            // ������, ���� ���ڸ��� ���߰� �ϰ� �ʹٸ� StopBubbleMovement()�� ȣ���� �� �ֽ��ϴ�.
            // Debug.Log("���� �浹�߽��ϴ�.");
        }
    }
    
    /// <summary>
    /// ������ �������� ���߰� ���� ������ ��Ȱ��ȭ�մϴ�.
    /// </summary>
    private void _StopBubbleMovement()
    {
        //#TODO::����ó��
    }

    private void OnDrawGizmos()
    {
        // ������Ʈ�� ���õ��� �ʾƵ� �׻� Gizmo�� ���̵���
        // �ؽ�Ʈ ���� ����
        Handles.color = Color.red;
        // �ؽ�Ʈ�� ������Ʈ�� ��ġ�� �������� ���Ͽ� �׸��ϴ�.
        var gridPos = StageManager.Instance.GridManager.GetGridPosition(this.transform.position);
        Handles.Label(transform.position, $"{gridPos}");
    }
}
