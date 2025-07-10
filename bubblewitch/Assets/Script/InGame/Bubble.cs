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
        // ������Ʈ�� ���õ��� �ʾƵ� �׻� Gizmo�� ���̵���
        // �ؽ�Ʈ ���� ����
        Handles.color = Color.red;
        // �ؽ�Ʈ�� ������Ʈ�� ��ġ�� �������� ���Ͽ� �׸��ϴ�.
        var gridPos = StageManager.Instance.GridManager.GetGridPosition(this.transform.position);
        Handles.Label(transform.position, $"{gridPos}");
    }
#endif
}
