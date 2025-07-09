using UnityEngine;
using System.Collections.Generic;
using System.Threading; // CancellationToken 사용
using System.Threading.Tasks;

public class PathFollower : MonoBehaviour
{
    private BubblePath _path; // 이동할 경로 정보를 가진 BubblePath 스크립트 참조
    private float _moveSpeed; // 이동 속도
    private int _currentPointIndex; // 현재 목표 지점 인덱스

    // 비동기 작업 취소를 위한 토큰 소스
    private CancellationTokenSource _cancellationTokenSource;

    // 초기화 함수
    public void Initialize(BubblePath path, float speed, int startPointIndex = 0)
    {
        _path = path;
        _moveSpeed = speed;
        _currentPointIndex = startPointIndex;

        // 기존 비동기 작업이 있다면 취소하고 새로운 소스 생성
        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }
        _cancellationTokenSource = new CancellationTokenSource();

        // 경로 이동 시작
        FollowPath(_cancellationTokenSource.Token); // Forget()으로 비동기 호출 (반환값 무시)
    }

    void OnDisable()
    {
        // 오브젝트 비활성화 시 모든 비동기 작업 취소
        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }
    }

    /// <summary>
    /// 경로를 따라 이동하는 비동기 함수.
    /// </summary>
    private async Task FollowPath(CancellationToken token)
    {
        if (_path == null || _path.pathPoints == null || _path.pathPoints.Count < 2)
        {
            Debug.LogWarning("PathFollower: 경로가 유효하지 않습니다. 이동을 시작할 수 없습니다.");
            return;
        }

        // 경로의 마지막 지점 인덱스 (닫힌 경로 여부에 따라 다름)
        int lastPointIndex = _path.pathPoints.Count - 1;

        // 경로의 모든 지점을 순회하며 이동
        while (true) // 닫힌 경로이거나, 특정 조건까지 반복
        {
            // 현재 이동하려는 지점이 유효한지 확인
            if (_currentPointIndex >= _path.pathPoints.Count)
            {
                // 닫힌 경로가 아니면 종료
                if (!_path.isClosedPath)
                {
                    Debug.Log("PathFollower: 경로의 끝에 도달했습니다.");
                    FinalizeBubblePosition(); // 최종 위치 처리
                    break; // 루프 종료
                }
                // 닫힌 경로이면 처음으로 돌아감
                _currentPointIndex = 0;
            }

            Vector3 startWorldPosition = transform.position; // 현재 버블의 월드 위치
            Vector3 targetPosition = _path.pathPoints[_currentPointIndex];

            // Debug.Log($"Moving from {startWorldPosition} to {targetWorldPosition} (point {_currentPointIndex})");

            float distance = Vector3.Distance(startWorldPosition, targetPosition);
            if (distance < 0.001f) // 이미 목표 지점에 거의 도달했으면 다음 지점으로
            {
                _currentPointIndex++;
                continue;
            }

            float duration = distance / _moveSpeed; // 이 구간을 이동하는 데 걸리는 시간
            float elapsed = 0f;

            while (elapsed < duration)
            {
                // 취소 요청이 있었는지 확인
                if (token.IsCancellationRequested)
                {
                    Debug.Log("PathFollower: 이동 중 취소 요청 감지.");
                    FinalizeBubblePosition(); // 최종 위치 처리 (중단 시)
                    return; // 함수 종료
                }

                // 이동 보간 (Lerp 함수로 일정 속도 시뮬레이션)
                transform.position = Vector3.Lerp(startWorldPosition, targetPosition, elapsed / duration);

                elapsed += Time.deltaTime;
                await Task.Yield(); // 다음 프레임까지 대기
            }

            // 정확히 목표 지점에 위치시키기 (오차 보정)
            transform.position = targetPosition;
            _currentPointIndex++; // 다음 목표 지점으로 인덱스 증가

            // 닫힌 경로가 아니면서 마지막 지점에 도달했는지 확인
            if (!_path.isClosedPath && _currentPointIndex > lastPointIndex)
            {
                Debug.Log("PathFollower: 경로의 끝에 도달했습니다.");
                FinalizeBubblePosition(); // 최종 위치 처리
                break; // 루프 종료
            }
        }
    }

    /// <summary>
    /// 경로 이동이 완료되거나 중단되었을 때 버블의 최종 상태를 처리합니다.
    /// (예: 그리드에 부착, 풀에 반환, 컴포넌트 제거 등)
    /// </summary>
    private void FinalizeBubblePosition()
    {
        var gridPos = StageManager.Instance.GridManager.GetGridPosition(this.transform.position);
        StageManager.Instance.GridManager.PlaceBubble(this.gameObject, gridPos.x, gridPos.y, isLaunched: true);

        Debug.Log("Bubble Finalized: " + gameObject.name);
    }
}