using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Utility.Singleton;

public class BubbleLauncher : MonoBehaviour
{
    [Header("발사 예정 버블 표기")]
    [SerializeField]
    private Transform _launchPoint;
    [SerializeField]
    private Transform _nextLaunchPoint;
    [SerializeField]
    private float _launchForce = 15f;

    [Header("조준선 관련 표기")]
    [SerializeField]
    private LineRenderer _lineRenderer;
    [SerializeField]
    private int _maxReflectionCount;

    private GameObject _currentBubble;
    private GameObject _nextBubble;
    private bool _canLaunch;

    // Line Renderer의 시작점과 끝점
    private List<Vector2> _linePoints = new List<Vector2>();

    [Header("타겟 지점 표기")]
    [SerializeField]
    private GameObject _targetGo;

    void Start()
    {
        if (_lineRenderer == null)
        {
#if UNITY_EDITOR
            Debug.LogError("Line Renderer가 BubbleLauncher에 연결되지 않았습니다.");
#endif
            return;
        }
        _lineRenderer.positionCount = 0; // 초기에는 선을 그리지 않음
        _lineRenderer.enabled = false; // 시작 시 비활성화
    }

    void Update()
    {
        if (!_canLaunch || _currentBubble == null)
        {
            // 조준선 비활성화
            if (_lineRenderer.enabled)
            {
                _lineRenderer.enabled = false;
                _lineRenderer.positionCount = 0;
            }
            return;
        }

        Vector2 inputPosition = Vector2.zero;
        bool inputActive = false; // 입력이 활성화되었는지 (터치 또는 마우스)
        bool inputEnded = false;  // 입력이 끝났는지 (손을 떼거나 마우스 버튼을 놓을 때)


        // --- 모바일 터치 입력 처리 ---
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            inputPosition = touch.position;
            inputActive = (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved);
            inputEnded = (touch.phase == TouchPhase.Ended);
        }
        // --- PC 마우스 입력 처리 ---
        else if (Input.GetMouseButton(0) || Input.GetMouseButtonDown(0) || Input.GetMouseButtonUp(0))
        {
            inputPosition = Input.mousePosition;
            inputActive = true; // 마우스 왼쪽 버튼이 눌려있거나 방금 눌렸을 때
            inputEnded = Input.GetMouseButtonUp(0); // 마우스 왼쪽 버튼을 놓았을 때
        }
        // --- 그 외의 경우 (입력이 없을 때) ---
        else
        {
            DisableAimLine();
            return;
        }

        // 입력이 활성화된 상태이면 조준선 그리기
        if (inputActive)
        {
            DrawAimLine(inputPosition);
        }

        // 입력이 끝났으면 버블 발사
        if (inputEnded)
        {
            DisableAimLine(); // 조준선 끄기
            if (LaunchBubble(inputPosition) == true)
            {
                StageManager.Instance.ShootBubble();
            }
        }
    }
    
    /// <summary>
    /// 조준선을 비활성화하고 위치를 초기화하는 헬퍼 함수
    /// </summary>
    void DisableAimLine()
    {
        _SetTargetGoActive(false, 0, 0);
        if (_lineRenderer != null && _lineRenderer.enabled)
        {
            _lineRenderer.enabled = false;
            _lineRenderer.positionCount = 0;
        }
    }

    /// <summary>
    /// 터치 위치를 기반으로 조준선과 반사 경로를 그립니다.
    /// </summary>
    /// <param name="touchPosition">화면 터치 픽셀 좌표</param>
    void DrawAimLine(Vector2 touchPosition)
    {
        if (_lineRenderer == null) return;

        _lineRenderer.enabled = true; // 조준선 활성화
        _linePoints.Clear();// 시작점 + 반사점들 + 마지막 예측점

        Vector3 startPosition = _launchPoint.position;
        Vector3 worldTouchPosition = Camera.main.ScreenToWorldPoint(new Vector3(touchPosition.x, touchPosition.y, Camera.main.nearClipPlane));
        worldTouchPosition.z = 0; // 2D 게임이므로 Z축 고정

        Vector2 direction = (worldTouchPosition - startPosition).normalized;

        // 버블이 발사 지점보다 위로 조준될 때만 선을 그림
        if (direction.y < 0.1f) // 0.1f는 임계값, 필요에 따라 조절
        {
            _lineRenderer.enabled = false; // 너무 아래로 조준하면 선을 그리지 않음
            _lineRenderer.positionCount = 0;
            return;
        }

        _linePoints.Add(startPosition);
        int currentReflectionCount = 0;
        Vector2 currentOrigin = startPosition;
        Vector2 currentDirection = direction;

        for (int i = 0; i <= _maxReflectionCount; i++)
        {
            // 0.5만큼 전진
            Vector2 targetPos = currentOrigin + currentDirection * 0.5f;

            var gridAxis = StageManager.Instance.GridManager.GetGridPosition(targetPos);
            var gridPos = StageManager.Instance.GridManager.GetWorldPosition(gridAxis.x, gridAxis.y);

            // 버블 충돌여부 체크
            bool bHitBubble = StageManager.Instance.GridManager.GetBubbleAtGrid(gridAxis.x, gridAxis.y) != null;
            // 근처에 버블이 있는지 체크
            bool bNearBubble = StageManager.Instance.GridManager.GetNearBubbleByPosition(targetPos);
            // 좌우 벽 체크
            bool bHitHorizontalWall = StageManager.Instance.GridManager.GetHorizontalWallAtPosition(targetPos);
            // 상하 벽 체크
            bool bHitVerticalWall = StageManager.Instance.GridManager.GetVerticalWallAtPosition(targetPos);

            if(bHitBubble == true)
            {
                // 버블충돌
                currentReflectionCount++;
                var lastPos = _linePoints.ElementAt(_linePoints.Count - 1);
                gridAxis = StageManager.Instance.GridManager.GetGridPosition(lastPos);
                gridPos = StageManager.Instance.GridManager.GetWorldPosition(gridAxis.x, gridAxis.y);
                _linePoints[_linePoints.Count - 1] = gridPos;
                _SetTargetGoActive(true, gridPos.x, gridPos.y);
                break; // 더 이상 반사되지 않으므로 루프 종료
            }

            if(bNearBubble== true)
            {
                // 어차피 주변에 버블로 막혀서 경로가 진행되면 안된다.
                currentReflectionCount++;
                _linePoints.Add(gridPos);
                _SetTargetGoActive(true, gridPos.x, gridPos.y);
                break; // 더 이상 반사되지 않으므로 루프 종료
            }

            if (bHitVerticalWall == true)
            {
                // 상하단벽에 부딪힐경우에는 소멸임
                currentReflectionCount++;
                _linePoints.Add(gridPos);
                _SetTargetGoActive(false, 0,0);
                break; // 더 이상 반사되지 않으므로 루프 종료
            }

            if (bHitHorizontalWall == false && bHitVerticalWall  == false && bHitBubble == false)
            {
                // 나머지의 경우 계속 전진처리
                _linePoints.Add(targetPos);
                currentReflectionCount++;
                currentOrigin = targetPos;
                continue;
            }

            if (bHitHorizontalWall == true)
            {
                // 벽에 부딪혔을 때 반사 방향 계산
                currentDirection = Vector2.Reflect(currentDirection, currentDirection.x >= 0 ? Vector2.left : Vector2.right);
                _SetTargetGoActive(false, 0, 0);
            }
        }

        // Line Renderer의 점 개수 설정 및 위치 업데이트
        _lineRenderer.positionCount = _linePoints.Count;
        for (int i = 0; i < _lineRenderer.positionCount; i++)
        {
            var pos = _linePoints.ElementAtOrDefault(i);
            if(pos != Vector2.zero)
            {
                _lineRenderer.SetPosition(i, pos);
            }
            else
            {
                return;
            }
        }
    }

    /// <summary>
    /// 도착 예정지역 표기의 Active 및 Position을 조정합니다.
    /// </summary>
    /// <param name="set">활성화여부</param>
    /// <param name="x">x좌표</param>
    /// <param name="y">y좌표</param>
    void _SetTargetGoActive(bool set, float x, float y)
    {
        _targetGo.SetActive(set);
        _targetGo.transform.position = new Vector2(x,y);
    }

    /// <summary>
    /// 다음 버블을 장전합니다.
    /// </summary>
    public void SpawnNewBubble()
    {
        if (_currentBubble != null)
            return;

        _currentBubble = _nextBubble != null ? _nextBubble : StageManager.Instance.BubbleManager.GetBubble();

        if (_currentBubble != null && _launchPoint != null)
        {
            _currentBubble.transform.position = _launchPoint.position;
            _currentBubble.transform.rotation = Quaternion.identity;

            _canLaunch = true;

            // 다음 버블도 없을떄는 현재 버블도 새로 생성됐음으로 초기화처리
            if (_nextBubble == null)
            {
                if (_currentBubble.TryGetComponent<Bubble>(out var bubbleScript))
                {
                    bubbleScript.InitBubble(StageManager.Instance.BubbleManager.RandomBubbleType(eBubbleType.NORMAL, eBubbleType.BOMB),
                            StageManager.Instance.BubbleManager.RandomBubbleColor(eBubbleColor.RED, eBubbleColor.BLUE));
                }
            }
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogError("ObjectPoolManager 또는 발사 지점에 문제가 있습니다.");
#endif
        }

        _nextBubble = StageManager.Instance.BubbleManager.GetBubble();

        if (_nextBubble != null && _nextLaunchPoint != null)
        {
            _nextBubble.transform.position = _nextLaunchPoint.position;
            _nextBubble.transform.rotation = Quaternion.identity;

            // 버블의 타입을 설정한다.
            if (_nextBubble.TryGetComponent<Bubble>(out var bubbleScript))
            {
                bubbleScript.InitBubble(StageManager.Instance.BubbleManager.RandomBubbleType(eBubbleType.NORMAL, eBubbleType.BOMB),
                        StageManager.Instance.BubbleManager.RandomBubbleColor(eBubbleColor.RED, eBubbleColor.BLUE));
            }
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogError("ObjectPoolManager 또는 발사 지점에 문제가 있습니다.");
#endif
        }
    }

    /// <summary>
    /// 현재 장전된 버블 삭제
    /// </summary>
    public void RemoveCurrentBubble()
    {
        if(_currentBubble != null)
        {
            StageManager.Instance.BubbleManager.ReleaseBubble(_currentBubble);
            _currentBubble = null;
        }
    }

    /// <summary>
    /// 현재 장전된 버블 발사
    /// </summary>
    /// <param name="touchPosition"></param>
    /// <returns></returns>
    bool LaunchBubble(Vector2 touchPosition)
    {
        if (_currentBubble == null) return false;

        Vector3 worldTouchPosition = Camera.main.ScreenToWorldPoint(new Vector3(touchPosition.x, touchPosition.y, Camera.main.nearClipPlane));
        worldTouchPosition.z = 0;

        Vector2 launchDirection = (worldTouchPosition - _launchPoint.position).normalized;

        if(launchDirection.y < 0)
        {
            // 반대로 쏘는것임으로 쏘지않는다.
            return false;
        }

        _canLaunch = false;

        //버블경로설정
        PathFollower pathFollower = _currentBubble.AddComponent<PathFollower>();
        BubblePath path = _currentBubble.AddComponent<BubblePath>();
        path.pathPoints = _linePoints;
        pathFollower.Initialize(path, _launchForce, 0);

        _currentBubble = null;

        return true;
    }

    /// <summary>
    /// 다음 버블과 장전된 버블을 교체한다.
    /// </summary>
    public void SwitchCurrentAndNext()
    {
        GameObject tempGO = _currentBubble;
        _currentBubble = _nextBubble;
        _nextBubble = tempGO;

        if (_currentBubble != null && _launchPoint != null)
        {
            _currentBubble.transform.position = _launchPoint.position;
            _currentBubble.transform.rotation = Quaternion.identity;
        }
        if (_nextBubble != null && _nextLaunchPoint != null)
        {
            _nextBubble.transform.position = _nextLaunchPoint.position;
            _nextBubble.transform.rotation = Quaternion.identity;
        }
    }

    /// <summary>
    /// 장전된 버블을 특수버블로 교체한다.
    /// </summary>
    public void ChangeCurrentBubbleToBomb()
    {
        if(_currentBubble.TryGetComponent<Bubble>(out var currentBubbleScript))
        {
            currentBubbleScript.InitBubble(eBubbleType.CAT_BOMB, eBubbleColor.SPECIAL);
        }
    }
}