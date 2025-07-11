using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

public class BubbleMaker : MonoBehaviour
{
    const int MINIMUM_BUBBLE = 14;
    public int BubblePathCount { get => bubblePath.Count; }

    public List<BubbleMakerPath> bubblePath = new List<BubbleMakerPath>();
    private Queue<GameObject> bubblePathQueue = new Queue<GameObject>();

    /// <summary>
    /// ������ ������ ��θ� ���� �����մϴ�.
    /// </summary>
    public void MakeBubble()
    {
        GameObject newBubble = StageManager.Instance.BubbleManager.GetBubble();
        if (newBubble != null)
        {
            // ������ Ÿ���� �����Ѵ�.
            if (newBubble.TryGetComponent<Bubble>(out var bubbleScript))
            {
                bubbleScript.InitBubble(StageManager.Instance.BubbleManager.RandomBubbleType(eBubbleType.NORMAL, eBubbleType.BOMB),
                        StageManager.Instance.BubbleManager.RandomBubbleColor(eBubbleColor.RED, eBubbleColor.BLUE));
            }

            //*----------------------------------------------------------
            //ť�� ������ �ִ°͵��� �ϳ��� �о�ִ´�.
            for (int i = bubblePathQueue.Count - 1; i >= 0; i--)
            {
                var bubble = bubblePathQueue.Dequeue();
                BubbleLogic(bubble, i+1);
            }
            //*----------------------------------------------------------

            //*----------------------------------------------------------
            //���ο�� ť�� �߰�
            BubbleLogic(newBubble, 0);
            //*----------------------------------------------------------

            void BubbleLogic(GameObject bubble, int index)
            {
                var path = bubblePath.ElementAtOrDefault(index);
                if (path != null)
                {
                    var axis = StageManager.Instance.GridManager.GetGridPosition(path.transform.position);

                    StageManager.Instance.GridManager.RemoveBubble(axis.x, axis.y, false);
                    StageManager.Instance.GridManager.PlaceBubble(bubble, axis.x, axis.y);

                    bubblePathQueue.Enqueue(bubble);
                }
                else
                {
                    StageManager.Instance.BubbleManager.ReleaseBubble(newBubble);
                }
            }
        }
    }

    /// <summary>
    /// MINIMUM_BUBBLE���� ���� ������ ������� �����Ѵ�.
    /// </summary>
    /// <returns></returns>
    public async Task RefillBubble()
    {
        if (bubblePathQueue.Count >= MINIMUM_BUBBLE)
            return;

        for (int i = bubblePathQueue.Count; i < MINIMUM_BUBBLE; i++)
        {
            MakeBubble();
            await Task.Delay(TimeSpan.FromSeconds(.1f));
        }
    }

    /// <summary>
    /// �������� ������
    /// </summary>
    /// <param name="bubble"></param>
    public void ReleaseBubble(GameObject bubble)
    {
        if (bubblePathQueue.Contains(bubble) == true)
        {
            // #TODO
            // ť���� ��ǥ�� ���� ���� �������� �־��.
            while (bubblePathQueue.Count > 0)
            {
                GameObject dequeuedBubble = bubblePathQueue.Dequeue();
#if UNITY_EDITOR
                Debug.Log($"'{dequeuedBubble.name}' Dequeue��.");
#endif

                // ���� Dequeue�� ������ �츮�� ã�� �������� Ȯ��
                if (dequeuedBubble == bubble)
                {
#if UNITY_EDITOR
                    Debug.Log($"'{bubble.name}' �߰� �� Dequeue �Ϸ�.");
#endif
                    return;
                }
            }
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(transform.position, 0.3f);
        Handles.Label(this.transform.position + Vector3.up * 0.5f, "Bubble Maker");
    }
#endif
}