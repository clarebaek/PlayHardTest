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
    private List<Bubble> _bubble = new List<Bubble>();
    private ObjectPool<GameObject> _bubblePool; // 유니티 공식 ObjectPool
    private ObjectPool<GameObject> _dropBubblePool;

    private void Awake()
    {
        _InitObjectPool();
    }

    private void _InitObjectPool()
    {
        _bubblePool = new ObjectPool<GameObject>(
            CreatePooledBubble,   // 풀이 비어있을 때 새 버블을 생성하는 메서드
            OnGetFromPool,        // 풀에서 버블을 가져올 때 호출되는 메서드
            OnReleaseToPool,      // 버블을 풀로 반환할 때 호출되는 메서드
            OnDestroyPooledBubble, // 풀의 maxSize를 초과하거나 풀이 파괴될 때 호출되는 메서드 (선택 사항)
            collectionCheck: false, // 컬렉션 중복 체크 (성능을 위해 false 권장)
            defaultCapacity: 100, // 초기 용량
            maxSize: 300      // 최대 풀 크기
        );

        _dropBubblePool = new ObjectPool<GameObject>(
            CreatePooledBubble,   // 풀이 비어있을 때 새 버블을 생성하는 메서드
            OnGetFromPool,        // 풀에서 버블을 가져올 때 호출되는 메서드
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
    private GameObject CreatePooledBubble()
    {
        var resource = Resources.Load<GameObject>("Prefab/Ingame/Bubble");
        GameObject bubble = Instantiate(resource);
        // 생성된 버블의 Rigidbody2D는 풀로 돌아가거나 활성화될 때마다 초기화되므로 여기서 특별히 설정할 필요는 없습니다.
        // Hierarchy 정리를 위해 부모 설정
        bubble.transform.SetParent(this.transform);
        return bubble;
    }

    /// <summary>
    /// 풀에서 버블을 가져올 때 호출됩니다. (ObjectPool 생성자의 actionOnGet)
    /// </summary>
    private void OnGetFromPool(GameObject bubble)
    {
        bubble.SetActive(true); // 버블 활성화
        bubble.transform.SetParent(this.transform); // Hierarchy 정리를 위해 다시 부모 설정
    }

    /// <summary>
    /// 버블을 풀로 반환할 때 호출됩니다. (ObjectPool 생성자의 actionOnRelease)
    /// </summary>
    private void OnReleaseToPool(GameObject bubble)
    {
        bubble.SetActive(false); // 버블 비활성화
        // 버블의 상태를 초기화하여 다음 사용을 준비합니다.
        bubble.transform.SetParent(this.transform); // Hierarchy 정리를 위해 다시 부모 설정
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
    /// 풀에서 버블을 가져오는 공개 메서드
    /// </summary>
    public GameObject GetDropBubble()
    {
        return _dropBubblePool.Get();
    }

    /// <summary>
    /// 버블을 풀로 반환하는 공개 메서드
    /// </summary>
    public void ReleaseDropBubble(GameObject bubble)
    {
        _dropBubblePool.Release(bubble);
    }
}
