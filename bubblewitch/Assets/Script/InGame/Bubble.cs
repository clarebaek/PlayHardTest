using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

public class Bubble : MonoBehaviour
{
    [Header("������ ���")]
    [SerializeField]
    GameObject _normalTypeGO;
    [SerializeField]
    SpriteRenderer _normalTypeImage;
    [Header("�������� �̹���")]
    [SerializeField]
    GameObject _fairyTypeGO;
    [Header("��ź���� �̹���")]
    [SerializeField]
    GameObject _bombTypeGO;

    [Header("��Ʋ��")]
    [SerializeField]
    private SpriteAtlas _targetAtlas;

    public eBubbleType bubbleType { get; private set; }
    public eBubbleColor bubbleColor { get; private set; }

    /// <summary>
    /// ������ �ʱ�ȭ�մϴ�.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="color"></param>
    public void InitBubble(eBubbleType type, eBubbleColor color)
    {
        bubbleType = type;
        bubbleColor = color;
        _SetView();
    }

    /// <summary>
    /// ������ ����� �����մϴ�.
    /// </summary>
    private void _SetView()
    {
        _normalTypeGO.SetActive(bubbleType != eBubbleType.BOMB);
        _fairyTypeGO.SetActive(bubbleType == eBubbleType.FAIRY);
        _bombTypeGO.SetActive(bubbleType == eBubbleType.BOMB);

        if (bubbleType != eBubbleType.BOMB)
        {
            if (_normalTypeImage.TryGetComponent<SpriteRenderer>(out var sprite))
            {
                sprite.sprite = bubbleColor switch
                {
                    eBubbleColor.RED => _targetAtlas.GetSprite("celery_0"),
                    eBubbleColor.YELLOW => _targetAtlas.GetSprite("onion_0"),
                    eBubbleColor.BLUE => _targetAtlas.GetSprite("riceball_0"),
                    eBubbleColor.SPECIAL => _targetAtlas.GetSprite("bomb_0"),
                    _ => _targetAtlas.GetSprite("onion_0"),
                };
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Handles.color = Color.red;
        var gridPos = StageManager.Instance.GridManager.GetGridPosition(this.transform.position);
        Handles.Label(transform.position, $"{gridPos}");
    }
#endif
}
