using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

public class Bubble : MonoBehaviour
{
    [SerializeField]
    GameObject _normalTypeGO;
    [SerializeField]
    SpriteRenderer _normalTypeImage;
    [SerializeField]
    GameObject _fairyTypeGO;
    public SpriteAtlas targetAtlas;

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
        _fairyTypeGO.SetActive(bubbleType == eBubbleType.FAIRY);

        if(_normalTypeImage.TryGetComponent<SpriteRenderer>(out var sprite))
        {
            sprite.sprite = bubbleColor switch
            {
                eBubbleColor.RED => targetAtlas.GetSprite("celery_0"),
                eBubbleColor.YELLOW => targetAtlas.GetSprite("onion_0"),
                eBubbleColor.BLUE => targetAtlas.GetSprite("riceball_0"),
                eBubbleColor.SPECIAL => targetAtlas.GetSprite("bomb_0"),
                _ => targetAtlas.GetSprite("onion_0"),
            };
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // 오브젝트가 선택되지 않아도 항상 Gizmo가 보이도록
        // 텍스트 색상 설정
        Handles.color = Color.red;
        // 텍스트를 오브젝트의 위치에 오프셋을 더하여 그립니다.
        var gridPos = StageManager.Instance.GridManager.GetGridPosition(this.transform.position);
        Handles.Label(transform.position, $"{gridPos}");
    }
#endif
}
