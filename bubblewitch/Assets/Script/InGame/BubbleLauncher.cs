using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Utility.Singleton;

public class BubbleLauncher : MonoBehaviour
{
    public Transform launchPoint;
    public Transform nextLaunchPoint;
    public float launchForce = 15f;

    // --- 조준선 관련 추가 변수 ---
    public LineRenderer lineRenderer; // 인스펙터에서 연결할 Line Renderer 컴포넌트
    public int maxReflectionCount;
    public LayerMask collisionLayer; // 레이캐스트가 감지할 벽/버블 레이어 (예: Wall, Bubble)

    private GameObject currentBubble;
    private GameObject nextBubble;
    private bool canLaunch = true;

    // Line Renderer의 시작점과 끝점 개수
    private List<Vector2> _linePoints = new List<Vector2>();

    void Start()
    {
        // Line Renderer 초기 설정
        if (lineRenderer == null)
        {
            Debug.LogError("Line Renderer가 BubbleLauncher에 연결되지 않았습니다.");
            return;
        }
        lineRenderer.positionCount = 0; // 초기에는 선을 그리지 않음
        lineRenderer.enabled = false; // 시작 시 비활성화
    }

    void Update()
    {
        if (!canLaunch || currentBubble == null)
        {
            // 조준선 비활성화
            if (lineRenderer.enabled)
            {
                lineRenderer.enabled = false;
                lineRenderer.positionCount = 0;
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
        if (lineRenderer != null && lineRenderer.enabled)
        {
            lineRenderer.enabled = false;
            lineRenderer.positionCount = 0;
        }
    }

    /// <summary>
    /// 터치 위치를 기반으로 조준선과 반사 경로를 그립니다.
    /// </summary>
    /// <param name="touchPosition">화면 터치 픽셀 좌표</param>
    void DrawAimLine(Vector2 touchPosition)
    {
        if (lineRenderer == null) return;

        lineRenderer.enabled = true; // 조준선 활성화
        _linePoints.Clear();// 시작점 + 반사점들 + 마지막 예측점

        Vector3 startPosition = launchPoint.position;
        Vector3 worldTouchPosition = Camera.main.ScreenToWorldPoint(new Vector3(touchPosition.x, touchPosition.y, Camera.main.nearClipPlane));
        worldTouchPosition.z = 0; // 2D 게임이므로 Z축 고정

        Vector2 direction = (worldTouchPosition - startPosition).normalized;

        // 버블이 발사 지점보다 위로 조준될 때만 선을 그림
        if (direction.y < 0.1f) // 0.1f는 임계값, 필요에 따라 조절
        {
            lineRenderer.enabled = false; // 너무 아래로 조준하면 선을 그리지 않음
            lineRenderer.positionCount = 0;
            return;
        }

        _linePoints.Add(startPosition);
        int currentReflectionCount = 0;
        Vector2 currentOrigin = startPosition;
        Vector2 currentDirection = direction;

        for (int i = 0; i <= maxReflectionCount; i++)
        {
            Vector2 targetPos = currentOrigin + currentDirection * 0.5f;

            var gridAxis = StageManager.Instance.GridManager.GetGridPosition(targetPos);
            var gridPos = StageManager.Instance.GridManager.GetWorldPosition(gridAxis.x, gridAxis.y);
            bool bHitBubble = StageManager.Instance.GridManager.GetBubbleAtGrid(gridAxis.x, gridAxis.y) != null;
            bool bNearBubble = StageManager.Instance.GridManager.GetNearBubbleByPosition(targetPos);
            bool bHitWall = StageManager.Instance.GridManager.GetWallAtGrid(gridAxis.x, gridAxis.y);

            if(bHitBubble == true)
            {
                // 버블충돌
                currentReflectionCount++;
                var lastPos = _linePoints.ElementAt(_linePoints.Count - 1);
                gridAxis = StageManager.Instance.GridManager.GetGridPosition(lastPos);
                gridPos = StageManager.Instance.GridManager.GetWorldPosition(gridAxis.x, gridAxis.y);
                _linePoints[_linePoints.Count - 1] = gridPos;
                break; // 더 이상 반사되지 않으므로 루프 종료
            }

            if(bNearBubble== true)
            {
                // 어차피 주변에 버블로 막혀서 경로가 진행되면 안된다.
                currentReflectionCount++;
                _linePoints.Add(gridPos);
                break; // 더 이상 반사되지 않으므로 루프 종료
            }

            if(bHitWall == false && bHitBubble == false)
            {
                _linePoints.Add(targetPos);
                currentReflectionCount++;
                currentOrigin = targetPos;
            }

            if (bHitWall == true)
            {
                // 벽충돌
                //_linePoints.Add(targetPos);
                //currentReflectionCount++;

                // 벽에 부딪혔을 때 반사 방향 계산
                currentDirection = Vector2.Reflect(currentDirection, currentDirection.x >= 0 ? Vector2.left : Vector2.right);
            }
        }

        // Line Renderer의 점 개수 설정 및 위치 업데이트
        lineRenderer.positionCount = _linePoints.Count;
        for (int i = 0; i < lineRenderer.positionCount; i++)
        {
            var pos = _linePoints.ElementAtOrDefault(i);
            if(pos != Vector2.zero)
            {
                lineRenderer.SetPosition(i, pos);
            }
            else
            {
                return;
            }
        }
    }

    public void SpawnNewBubble()
    {
        if (currentBubble != null)
            return;

        currentBubble = nextBubble != null ? nextBubble : StageManager.Instance.BubbleManager.GetBubble();

        if (currentBubble != null && launchPoint != null)
        {
            currentBubble.transform.position = launchPoint.position;
            currentBubble.transform.rotation = Quaternion.identity;

            currentBubble.layer = LayerMask.NameToLayer("Default");

            canLaunch = true;

            // 버블의 타입을 설정한다.
            if (nextBubble == null)
            {
                if (currentBubble.TryGetComponent<Bubble>(out var bubbleScript))
                {
                    bubbleScript.SetType((eBubbleType)Random.Range((int)eBubbleType.NORMAL, (int)eBubbleType.FAIRY + 1)
                        , (eBubbleColor)UnityEngine.Random.Range((int)eBubbleColor.RED, (int)eBubbleColor.BLUE + 1)
                        , isLaunched: true);
                }
            }
        }
        else
        {
            Debug.LogError("ObjectPoolManager 또는 발사 지점에 문제가 있습니다.");
        }

        nextBubble = StageManager.Instance.BubbleManager.GetBubble();

        if (nextBubble != null && nextLaunchPoint != null)
        {
            nextBubble.transform.position = nextLaunchPoint.position;
            nextBubble.transform.rotation = Quaternion.identity;

            nextBubble.layer = LayerMask.NameToLayer("Default");

            canLaunch = true;

            // 버블의 타입을 설정한다.
            if (nextBubble.TryGetComponent<Bubble>(out var bubbleScript))
            {
                bubbleScript.SetType((eBubbleType)Random.Range((int)eBubbleType.NORMAL, (int)eBubbleType.FAIRY + 1)
                    , (eBubbleColor)UnityEngine.Random.Range((int)eBubbleColor.RED, (int)eBubbleColor.BLUE + 1)
                    , isLaunched: true);
            }
        }
        else
        {
            Debug.LogError("ObjectPoolManager 또는 발사 지점에 문제가 있습니다.");
        }
    }

    public void RemoveCurrentBubble()
    {
        if(currentBubble != null)
        {
            StageManager.Instance.BubbleManager.ReleaseBubble(currentBubble);
            currentBubble = null;
        }
    }

    bool LaunchBubble(Vector2 touchPosition)
    {
        if (currentBubble == null) return false;

        Vector3 worldTouchPosition = Camera.main.ScreenToWorldPoint(new Vector3(touchPosition.x, touchPosition.y, Camera.main.nearClipPlane));
        worldTouchPosition.z = 0;

        Vector2 launchDirection = (worldTouchPosition - launchPoint.position).normalized;

        if(launchDirection.y < 0)
        {
            // 반대로 쏘는것임으로 쏘지않는다.
            return false;
        }

        canLaunch = false;

        currentBubble.layer = LayerMask.NameToLayer("Bubble");
        //#TODO::LAUNCH
        PathFollower pathFollower = currentBubble.AddComponent<PathFollower>();
        BubblePath path = currentBubble.AddComponent<BubblePath>();
        path.pathPoints = _linePoints;
        pathFollower.Initialize(path, launchForce, 0);

        currentBubble = null;

        return true;
    }

    public void ChangeNextBubble()
    {
        GameObject tempGO = currentBubble;
        currentBubble = nextBubble;
        nextBubble = tempGO;

        if (currentBubble != null && launchPoint != null)
        {
            currentBubble.transform.position = launchPoint.position;
            currentBubble.transform.rotation = Quaternion.identity;
        }
        if (nextBubble != null && nextLaunchPoint != null)
        {
            nextBubble.transform.position = nextLaunchPoint.position;
            nextBubble.transform.rotation = Quaternion.identity;
        }
    }

    public void ChangeCurrentBubble()
    {
        if(currentBubble.TryGetComponent<Bubble>(out var currentBubbleScript))
        {
            currentBubbleScript.SetType(eBubbleType.CAT_BOMB, eBubbleColor.SPECIAL, true);
        }
    }
}