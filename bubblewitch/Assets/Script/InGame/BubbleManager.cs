using System.Collections.Generic;
using UnityEngine;
using Utility.Singleton;
using UnityEngine.Pool;

public enum eBubbleType
{
    NORMAL_START = 0,
    NORMAL_RED,         // 일반 빨간색 버블
    NORMAL_YELLOW,      // 요정 노란색 버블
    NORMAL_BLUE,        // 일반 파란색 버블
    NORMAL_END,

    FAIRY_START,
    FAIRY_RED,          // 요정 빨간색 버블
    FAIRY_YELLOW,       // 요정 노란색 버블
    FAIRY_BLUE,         // 요정 파란색 버블
    FAIRY_END,

    BOMB,               // 맵에서 생성되는 폭탄버블 --> 1칸 제거
    CAT_BOMB,           // 고양이가 만들어준 폭탄버블 --> 2칸 제거
    GENERATOR,          // 버블이 생성되는 지점
}

public class BubbleManager : MonoSingleton<BubbleManager>
{
    private List<Bubble> _bubble = new List<Bubble>();
    private ObjectPool<GameObject> _bubblePool; // 유니티 공식 ObjectPool

    private void Awake()
    {
        _SetData();
        _InitObjectPool();
        _InitBubble();
    }

    /// <summary>
    /// 맵관련 데이터를 가져오도록 설정
    /// </summary>
    private void _SetData()
    {

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
        if (bubble.TryGetComponent<Rigidbody2D>(out var rigidbody))
        {
            rigidbody.bodyType = RigidbodyType2D.Kinematic;
        }
        bubble.transform.SetParent(this.transform); // Hierarchy 정리를 위해 다시 부모 설정
    }

    /// <summary>
    /// 버블을 풀로 반환할 때 호출됩니다. (ObjectPool 생성자의 actionOnRelease)
    /// </summary>
    private void OnReleaseToPool(GameObject bubble)
    {
        bubble.SetActive(false); // 버블 비활성화
        // 버블의 상태를 초기화하여 다음 사용을 준비합니다.
        if (bubble.TryGetComponent<Rigidbody2D>(out var rigidbody))
        {
            rigidbody.bodyType = RigidbodyType2D.Kinematic;
        }
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
    /// 첫 버블 생성
    /// </summary>
    private void _InitBubble()
    {
        //*------------------------------------------------------------------------
        //첫시작시 좌우 N개씩 생성
        float x = 0;
        float y = 8;
        for(int i = 0; i<16; i++)
        {
            var bubble = _bubblePool.Get();
            if(bubble.TryGetComponent<Bubble>(out var bubbleScript))
            {
                _bubble.Add(bubbleScript);
                bubble.transform.position = new Vector2(x, y);
                bubbleScript.SetType((eBubbleType)Random.Range((int)eBubbleType.NORMAL_START, (int)eBubbleType.NORMAL_END));
            }
            x = (x + 1) % 9;
            y = x == 0 ? y + 1 : y;
        }
        //*------------------------------------------------------------------------
    }

    /// <summary>
    /// 버블을 쏘고나서 버블이 붙었을때 맵 처리
    /// </summary>
    public void CrushBubble()
    {
        //*------------------------------------------------------------------------
        //1. 없앨 부분 설정
        //*------------------------------------------------------------------------
        //*------------------------------------------------------------------------
        //2. 아래로 떨어질 버블 설정
        //*------------------------------------------------------------------------
    }

    /// <summary>
    /// 버블이 터진 뒤 새로운 버블 생성
    /// </summary>
    public void GenerateBubble()
    {
        //*------------------------------------------------------------------------
        //y값이 홀수일때 9까지
        //y값이 짝수일때 10까지
        //n칸씩 push하는느낌으로
        //*------------------------------------------------------------------------
    }
}
