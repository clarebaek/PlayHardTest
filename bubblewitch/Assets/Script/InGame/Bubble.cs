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
}
