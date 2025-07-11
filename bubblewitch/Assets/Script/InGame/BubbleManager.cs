using System.Collections.Generic;
using UnityEngine;
using Utility.Singleton;
using UnityEngine.Pool;

public enum eBubbleType
{
    NONE = -1,
    NORMAL = 0,
    FAIRY,
    BOMB,
    CAT_BOMB,
    GENERATOR,          
}

public enum eBubbleColor
{
    NONE = -1,
    RED,
    YELLOW,
    BLUE,
    SPECIAL,
    END,
}

public class BubbleManager : MonoBehaviour
{
    private ObjectPool<GameObject> _bubblePool;
    private ObjectPool<GameObject> _dropBubblePool;

    private void Awake()
    {
        _InitObjectPool();
    }

    private void _InitObjectPool()
    {
        _bubblePool = new ObjectPool<GameObject>(
            _CreatePooledBubble,   // 풀이 비어있을 때 새 버블을 생성하는 메서드
            _OnGetFromPool,        // 풀에서 버블을 가져올 때 호출되는 메서드
            OnReleaseToPool,      // 버블을 풀로 반환할 때 호출되는 메서드
            OnDestroyPooledBubble, // 풀의 maxSize를 초과하거나 풀이 파괴될 때 호출되는 메서드 (선택 사항)
            collectionCheck: false, // 컬렉션 중복 체크 (성능을 위해 false 권장)
            defaultCapacity: 100, // 초기 용량
            maxSize: 300      // 최대 풀 크기
        );

        _dropBubblePool = new ObjectPool<GameObject>(
            _CreatePooledBubble,   // 풀이 비어있을 때 새 버블을 생성하는 메서드
            _OnGetFromPool,        // 풀에서 버블을 가져올 때 호출되는 메서드
            OnReleaseToPool,      // 버블을 풀로 반환할 때 호출되는 메서드
            OnDestroyPooledBubble, // 풀의 maxSize를 초과하거나 풀이 파괴될 때 호출되는 메서드 (선택 사항)
            collectionCheck: false, // 컬렉션 중복 체크 (성능을 위해 false 권장)
            defaultCapacity: 100, // 초기 용량
            maxSize: 300      // 최대 풀 크기
        );
    }

    /// <summary>
    /// 풀이 비어있을 때 새로운 버블 오브젝트를 생성합니다. (ObjectPool 생성자의 createFunc)
    /// </summary>
    private GameObject _CreatePooledBubble()
    {
        var resource = Resources.Load<GameObject>("Prefab/Ingame/Bubble");
        GameObject bubble = Instantiate(resource);
        bubble.transform.SetParent(this.transform);
        return bubble;
    }

    /// <summary>
    /// 풀에서 버블을 가져올 때 호출됩니다. (ObjectPool 생성자의 actionOnGet)
    /// </summary>
    private void _OnGetFromPool(GameObject bubble)
    {
        bubble.SetActive(true); // 버블 활성화
    }

    /// <summary>
    /// 버블을 풀로 반환할 때 호출됩니다. (ObjectPool 생성자의 actionOnRelease)
    /// </summary>
    private void OnReleaseToPool(GameObject bubble)
    {
        bubble.SetActive(false); // 버블 비활성화
    }

    /// <summary>
    /// 풀에서 maxSize를 초과하여 제거되거나 풀이 파괴될 때 호출됩니다. (ObjectPool 생성자의 actionOnDestroy)
    /// </summary>
    private void OnDestroyPooledBubble(GameObject bubble)
    {
        Destroy(bubble);
    }

    /// <summary>
    /// 풀에서 버블을 가져오는 공개 메서드
    /// </summary>
    public GameObject GetBubble()
    {
        return _bubblePool.Get();
    }

    /// <summary>
    /// 버블을 풀로 반환하는 공개 메서드
    /// </summary>
    public void ReleaseBubble(GameObject bubble)
    {
        _bubblePool.Release(bubble);
    }

    /// <summary>
    /// 풀에서 떨어지는용 버블을 가져오는 공개 메서드
    /// </summary>
    public GameObject GetDropBubble()
    {
        return _dropBubblePool.Get();
    }

    /// <summary>
    /// 떨어지는용 버블을 풀로 반환하는 공개 메서드
    /// </summary>
    public void ReleaseDropBubble(GameObject bubble)
    {
        _dropBubblePool.Release(bubble);
    }


    /// <summary>
    /// 버블 타입 랜덤 지정
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public eBubbleType RandomBubbleType(eBubbleType start, eBubbleType end)
    {
        return (eBubbleType)Random.Range((int)start, (int)end + 1);
    }

    /// <summary>
    /// 버블 색상 랜덤 지정
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public eBubbleColor RandomBubbleColor(eBubbleColor start, eBubbleColor end)
    {
        return (eBubbleColor)Random.Range((int)start, (int)end + 1);
    }
}
