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
