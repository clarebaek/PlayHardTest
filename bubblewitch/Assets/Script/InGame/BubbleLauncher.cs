using UnityEngine;
using System.Collections;

public class BubbleLauncher : MonoBehaviour
{
    public Transform launchPoint;
    public float launchForce = 15f;

    // --- 조준선 관련 추가 변수 ---
    public LineRenderer lineRenderer; // 인스펙터에서 연결할 Line Renderer 컴포넌트
    public int maxReflectionCount = 2; // 최대 반사 횟수
    public LayerMask collisionLayer; // 레이캐스트가 감지할 벽/버블 레이어 (예: Wall, Bubble)

    private GameObject currentBubble;
    private bool canLaunch = true;

    // Line Renderer의 시작점과 끝점 개수
    private Vector3[] linePoints;

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

        SpawnNewBubble();
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
            LaunchBubble(inputPosition);
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
        linePoints = new Vector3[maxReflectionCount + 2]; // 시작점 + 반사점들 + 마지막 예측점

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

        linePoints[0] = startPosition;
        int currentReflectionCount = 0;
        Vector2 currentOrigin = startPosition;
        Vector2 currentDirection = direction;

        for (int i = 0; i <= maxReflectionCount; i++)
        {
            // Raycast를 쏴서 충돌 여부 확인
            // Physics2D.Raycast(시작점, 방향, 최대 거리, 충돌 레이어)
            RaycastHit2D hit = Physics2D.Raycast(currentOrigin, currentDirection, 100f, collisionLayer); // 100f는 최대 레이 길이

            if (hit.collider != null)
            {
                // 충돌 지점을 Line Renderer에 추가
                linePoints[i + 1] = hit.point;
                currentReflectionCount++;

                // 벽에 부딪혔을 때 반사 방향 계산
                currentDirection = Vector2.Reflect(currentDirection, hit.normal);
                currentOrigin = hit.point + currentDirection * 0.01f; // 충돌 지점에서 살짝 떨어진 곳에서 다음 레이 시작 (겹침 방지)

                // 버블(원형)이 벽에 닿았을 때의 정확한 반사 지점을 고려하기 위해 hit.point에서 버블 반지름만큼 떨어진 곳을 계산할 수 있음
                // 하지만 시각적인 조준선에서는 hit.point만으로도 충분함.
            }
            else
            {
                // 충돌하지 않았다면 직선으로 쭉 뻗어 나감
                linePoints[i + 1] = currentOrigin + currentDirection * 100f; // 최대 길이까지 그림
                currentReflectionCount++;
                break; // 더 이상 반사되지 않으므로 루프 종료
            }
        }

        // Line Renderer의 점 개수 설정 및 위치 업데이트
        lineRenderer.positionCount = currentReflectionCount + 1;
        for (int i = 0; i < lineRenderer.positionCount; i++)
        {
            lineRenderer.SetPosition(i, linePoints[i]);
        }
    }


    void SpawnNewBubble()
    {
        currentBubble = BubbleManager.Instance.GetBubble();

        if (currentBubble != null && launchPoint != null)
        {
            currentBubble.transform.position = launchPoint.position;
            currentBubble.transform.rotation = Quaternion.identity;

            // OnGetFromPool에서 대부분 초기화되므로 여기서는 특별한 코드가 필요하지 않음.
            // Rigidbody2D, Collider2D 상태는 ObjectPoolManager의 OnGetFromPool에서 관리.

            canLaunch = true;
        }
        else
        {
            Debug.LogError("ObjectPoolManager 또는 발사 지점에 문제가 있습니다.");
        }
    }

    void LaunchBubble(Vector2 touchPosition)
    {
        if (currentBubble == null) return;

        canLaunch = false;

        Vector3 worldTouchPosition = Camera.main.ScreenToWorldPoint(new Vector3(touchPosition.x, touchPosition.y, Camera.main.nearClipPlane));
        worldTouchPosition.z = 0;

        Vector2 launchDirection = (worldTouchPosition - launchPoint.position).normalized;

        Rigidbody2D rb = currentBubble.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.AddForce(launchDirection * launchForce, ForceMode2D.Impulse);
            Debug.Log($"<color=red><b>LaunchBubble Called! Current Bubble: {currentBubble?.name ?? "NULL"}, CanLaunch: {canLaunch} force : {launchDirection * launchForce }</b></color>");

        }

        StartCoroutine(SpawnNextBubbleAfterDelay(0.5f));
    }

    IEnumerator SpawnNextBubbleAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnNewBubble();
    }
}