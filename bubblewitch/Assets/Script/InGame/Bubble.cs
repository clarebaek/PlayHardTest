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
    /// 다른 콜라이더와 물리적으로 충돌했을 때 호출됩니다.
    /// 이 함수를 사용하려면 해당 콜라이더의 Is Trigger가 해제되어 있어야 합니다.
    /// </summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 충돌한 오브젝트의 레이어를 가져옵니다.
        int otherLayerNumber = collision.gameObject.layer;
        string otherLayerName = LayerMask.LayerToName(otherLayerNumber);

        // Debug.Log($"버블이 {collision.gameObject.name} (Layer: {otherLayerName})에 충돌!");

        // 1. "Bubble" 레이어의 다른 버블에 부딪혔을 때 멈추기
        if (otherLayerName == "Bubble")
        {
            // Debug.Log("다른 버블에 충돌하여 멈춥니다.");
            _StopBubbleMovement();
            // TODO: 버블을 그리드에 붙이는 로직 (나중에 구현)
            // 예를 들어, GridManager.AttachBubble(this.gameObject, collision.contacts[0].point);
        }
        // 2. "Wall" 레이어의 벽에 부딪혔을 때 (반사 후 멈춤 또는 계속 이동)
        else if (otherLayerName == "Wall")
        {
            // 벽에 부딪히면 일반적으로 버블이 튕기므로 바로 멈추지 않고,
            // 다른 버블에 닿을 때까지 계속 이동하는 것이 일반적입니다.
            // 여기서는 특별히 멈추는 로직을 넣지 않습니다.
            // 하지만, 벽에 닿자마자 멈추게 하고 싶다면 StopBubbleMovement()를 호출할 수 있습니다.
            // Debug.Log("벽에 충돌했습니다.");
        }
    }
    
    /// <summary>
    /// 버블의 움직임을 멈추고 물리 영향을 비활성화합니다.
    /// </summary>
    private void _StopBubbleMovement()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            var gridPos = GridManager.Instance.GetGridPosition(this.transform.position);
            GridManager.Instance.PlaceBubble(this.gameObject, gridPos.x, gridPos.y);
        }
        // 콜라이더를 비활성화하면 다른 버블이 이 버블을 통과할 수 있으므로,
        // 보통은 비활성화하지 않고 그리드에 붙인 후 다른 처리 (예: Sorting Order 변경)를 합니다.
        // Collider2D col = GetComponent<Collider2D>();
        // if (col != null) col.enabled = false; // 신중하게 사용!
    }
}
