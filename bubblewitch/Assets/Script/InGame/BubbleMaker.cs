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

    public void MakeBubble()
    {
        GameObject newBubble = StageManager.Instance.BubbleManager.GetBubble();
        if (newBubble != null)
        {
            // 버블의 타입을 설정한다.
            if (newBubble.TryGetComponent<Bubble>(out var bubbleScript))
            {
                bubbleScript.SetType((eBubbleType)UnityEngine.Random.Range((int)eBubbleType.NORMAL, (int)eBubbleType.FAIRY + 1)
                    , (eBubbleColor)UnityEngine.Random.Range((int)eBubbleColor.RED, (int)eBubbleColor.BLUE + 1));
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

    public async Task RefillBubble()
    {
        if (bubblePathQueue.Count >= MINIMUM_BUBBLE)
            return;

        for (int i = bubblePathQueue.Count; i < MINIMUM_BUBBLE; i++) // 각 maker당 5개의 버블 생성
        {
            MakeBubble();
            // 각 버블 생성 후 일정 시간 대기
            await Task.Delay(TimeSpan.FromSeconds(.1f));
        }
    }

    public void ReleaseBubble(GameObject bubble)
    {
        if (bubblePathQueue.Contains(bubble) == true)
        {
            while (bubblePathQueue.Count > 0)
            {
                GameObject dequeuedBubble = bubblePathQueue.Dequeue();
                Debug.Log($"'{dequeuedBubble.name}' Dequeue됨.");

                // 현재 Dequeue한 버블이 우리가 찾던 버블인지 확인
                if (dequeuedBubble == bubble)
                {
                    Debug.Log($"'{bubble.name}' 발견 및 Dequeue 완료.");
                    return; // 원하는 버블을 찾았으므로 함수 종료
                }
            }
        }
    }

#if UNITY_EDITOR
    // OnDrawGizmos는 씬 뷰에 항상 기즈모를 그립니다. (오브젝트가 선택되지 않아도)
    void OnDrawGizmos()
    {
        // 기즈모는 런타임 빌드에 포함되지 않습니다.
        // #if UNITY_EDITOR // 이 조건문은 OnDrawGizmos에서는 필수는 아니지만, 안전을 위해 남겨둘 수 있습니다.
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(transform.position, 0.3f);
        Handles.Label(this.transform.position + Vector3.up * 0.5f, "Bubble Maker");
        // #endif
    }
#endif
}