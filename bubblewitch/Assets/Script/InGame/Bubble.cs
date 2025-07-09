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
    /// 다른 콜라이더와 물리적으로 충돌했을 때 호출됩니다.
    /// 이 함수를 사용하려면 해당 콜라이더의 Is Trigger가 해제되어 있어야 합니다.
    /// </summary>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (_isLaunched == false)
            return;

        // 충돌한 오브젝트의 레이어를 가져옵니다.
        int otherLayerNumber = collision.gameObject.layer;
        string otherLayerName = LayerMask.LayerToName(otherLayerNumber);

        // Debug.Log($"버블이 {collision.gameObject.name} (Layer: {otherLayerName})에 충돌!");

        // 1. "Bubble" 레이어의 다른 버블에 부딪혔을 때 멈추기
        if (otherLayerName == "Bubble")
        {
            // Debug.Log("다른 버블에 충돌하여 멈춥니다.");
            //_StopBubbleMovement();
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
        //#TODO::멈춤처리
    }

    private void OnDrawGizmos()
    {
        // 오브젝트가 선택되지 않아도 항상 Gizmo가 보이도록
        // 텍스트 색상 설정
        Handles.color = Color.red;
        // 텍스트를 오브젝트의 위치에 오프셋을 더하여 그립니다.
        var gridPos = StageManager.Instance.GridManager.GetGridPosition(this.transform.position);
        Handles.Label(transform.position, $"{gridPos}");
    }
}
