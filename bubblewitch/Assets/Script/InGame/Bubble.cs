using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bubble : MonoBehaviour
{
    public eBubbleType bubbleType { get; private set; }

    private void _SetData()
    {

    }

    public void SetType(eBubbleType type)
    {
        bubbleType = type;
        _SetView();
    }

    private void _SetView()
    {
        if(TryGetComponent<SpriteRenderer>(out var sprite))
        {
            sprite.color = bubbleType switch
            {
                eBubbleType.NORMAL_RED => Color.red,
                eBubbleType.NORMAL_YELLOW => Color.yellow,
                eBubbleType.NORMAL_BLUE => Color.blue,
                eBubbleType.FAIRY_RED => Color.red,
                eBubbleType.FAIRY_YELLOW => Color.yellow,
                eBubbleType.FAIRY_BLUE => Color.blue,
                eBubbleType.CAT_BOMB => Color.black,
                eBubbleType.BOMB => Color.black,
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
        // �浹�� ������Ʈ�� ���̾ �����ɴϴ�.
        int otherLayerNumber = collision.gameObject.layer;
        string otherLayerName = LayerMask.LayerToName(otherLayerNumber);

        // Debug.Log($"������ {collision.gameObject.name} (Layer: {otherLayerName})�� �浹!");

        // 1. "Bubble" ���̾��� �ٸ� ���� �ε����� �� ���߱�
        if (otherLayerName == "Bubble")
        {
            // Debug.Log("�ٸ� ���� �浹�Ͽ� ����ϴ�.");
            _StopBubbleMovement();
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
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            var gridPos = GridManager.Instance.GetGridPosition(this.transform.position);
            GridManager.Instance.PlaceBubble(this.gameObject, gridPos.x, gridPos.y);
        }
        // �ݶ��̴��� ��Ȱ��ȭ�ϸ� �ٸ� ������ �� ������ ����� �� �����Ƿ�,
        // ������ ��Ȱ��ȭ���� �ʰ� �׸��忡 ���� �� �ٸ� ó�� (��: Sorting Order ����)�� �մϴ�.
        // Collider2D col = GetComponent<Collider2D>();
        // if (col != null) col.enabled = false; // �����ϰ� ���!
    }
}
