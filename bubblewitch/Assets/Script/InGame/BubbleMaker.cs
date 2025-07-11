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
    /// 버블을 지정된 경로를 따라서 생성합니다.
    /// </summary>
    public void MakeBubble()
    {
        GameObject newBubble = StageManager.Instance.BubbleManager.GetBubble();
        if (newBubble != null)
        {
            // 버블의 타입을 설정한다.
            if (newBubble.TryGetComponent<Bubble>(out var bubbleScript))
            {
                bubbleScript.InitBubble(StageManager.Instance.BubbleManager.RandomBubbleType(eBubbleType.NORMAL, eBubbleType.BOMB),
                        StageManager.Instance.BubbleManager.RandomBubbleColor(eBubbleColor.RED, eBubbleColor.BLUE));
            }

            //*----------------------------------------------------------
            //큐에 기존에 있는것들을 하나씩 밀어넣는다.
            for (int i = bubblePathQueue.Count - 1; i >= 0; i--)
            {
                var bubble = bubblePathQueue.Dequeue();
                BubbleLogic(bubble, i+1);
            }
            //*----------------------------------------------------------

            //*----------------------------------------------------------
            //새로운애 큐에 추가
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
    /// MINIMUM_BUBBLE보다 현재 버블이 적을경우 리필한다.
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
    /// 스테이지 관리용
    /// </summary>
    /// <param name="bubble"></param>
    public void ReleaseBubble(GameObject bubble)
    {
        if (bubblePathQueue.Contains(bubble) == true)
        {
            // #TODO
            // 큐에서 좌표로 직접 접근 개선여지 있어보임.
            while (bubblePathQueue.Count > 0)
            {
                GameObject dequeuedBubble = bubblePathQueue.Dequeue();
#if UNITY_EDITOR
                Debug.Log($"'{dequeuedBubble.name}' Dequeue됨.");
#endif

                // 현재 Dequeue한 버블이 우리가 찾던 버블인지 확인
                if (dequeuedBubble == bubble)
                {
#if UNITY_EDITOR
                    Debug.Log($"'{bubble.name}' 발견 및 Dequeue 완료.");
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