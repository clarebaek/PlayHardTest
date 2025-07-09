using UnityEngine;
using System.Collections.Generic;
using System.Threading; // CancellationToken ���
using System.Threading.Tasks;

public class PathFollower : MonoBehaviour
{
    private BubblePath _path; // �̵��� ��� ������ ���� BubblePath ��ũ��Ʈ ����
    private float _moveSpeed; // �̵� �ӵ�
    private int _currentPointIndex; // ���� ��ǥ ���� �ε���

    // �񵿱� �۾� ��Ҹ� ���� ��ū �ҽ�
    private CancellationTokenSource _cancellationTokenSource;

    // �ʱ�ȭ �Լ�
    public void Initialize(BubblePath path, float speed, int startPointIndex = 0)
    {
        _path = path;
        _moveSpeed = speed;
        _currentPointIndex = startPointIndex;

        // ���� �񵿱� �۾��� �ִٸ� ����ϰ� ���ο� �ҽ� ����
        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }
        _cancellationTokenSource = new CancellationTokenSource();

        // ��� �̵� ����
        FollowPath(_cancellationTokenSource.Token); // Forget()���� �񵿱� ȣ�� (��ȯ�� ����)
    }

    void OnDisable()
    {
        // ������Ʈ ��Ȱ��ȭ �� ��� �񵿱� �۾� ���
        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }
    }

    /// <summary>
    /// ��θ� ���� �̵��ϴ� �񵿱� �Լ�.
    /// </summary>
    private async Task FollowPath(CancellationToken token)
    {
        if (_path == null || _path.pathPoints == null || _path.pathPoints.Count < 2)
        {
            Debug.LogWarning("PathFollower: ��ΰ� ��ȿ���� �ʽ��ϴ�. �̵��� ������ �� �����ϴ�.");
            return;
        }

        // ����� ������ ���� �ε��� (���� ��� ���ο� ���� �ٸ�)
        int lastPointIndex = _path.pathPoints.Count - 1;

        // ����� ��� ������ ��ȸ�ϸ� �̵�
        while (true) // ���� ����̰ų�, Ư�� ���Ǳ��� �ݺ�
        {
            // ���� �̵��Ϸ��� ������ ��ȿ���� Ȯ��
            if (_currentPointIndex >= _path.pathPoints.Count)
            {
                // ���� ��ΰ� �ƴϸ� ����
                if (!_path.isClosedPath)
                {
                    Debug.Log("PathFollower: ����� ���� �����߽��ϴ�.");
                    FinalizeBubblePosition(); // ���� ��ġ ó��
                    break; // ���� ����
                }
                // ���� ����̸� ó������ ���ư�
                _currentPointIndex = 0;
            }

            Vector3 startWorldPosition = transform.position; // ���� ������ ���� ��ġ
            Vector3 targetPosition = _path.pathPoints[_currentPointIndex];

            // Debug.Log($"Moving from {startWorldPosition} to {targetWorldPosition} (point {_currentPointIndex})");

            float distance = Vector3.Distance(startWorldPosition, targetPosition);
            if (distance < 0.001f) // �̹� ��ǥ ������ ���� ���������� ���� ��������
            {
                _currentPointIndex++;
                continue;
            }

            float duration = distance / _moveSpeed; // �� ������ �̵��ϴ� �� �ɸ��� �ð�
            float elapsed = 0f;

            while (elapsed < duration)
            {
                // ��� ��û�� �־����� Ȯ��
                if (token.IsCancellationRequested)
                {
                    Debug.Log("PathFollower: �̵� �� ��� ��û ����.");
                    FinalizeBubblePosition(); // ���� ��ġ ó�� (�ߴ� ��)
                    return; // �Լ� ����
                }

                // �̵� ���� (Lerp �Լ��� ���� �ӵ� �ùķ��̼�)
                transform.position = Vector3.Lerp(startWorldPosition, targetPosition, elapsed / duration);

                elapsed += Time.deltaTime;
                await Task.Yield(); // ���� �����ӱ��� ���
            }

            // ��Ȯ�� ��ǥ ������ ��ġ��Ű�� (���� ����)
            transform.position = targetPosition;
            _currentPointIndex++; // ���� ��ǥ �������� �ε��� ����

            // ���� ��ΰ� �ƴϸ鼭 ������ ������ �����ߴ��� Ȯ��
            if (!_path.isClosedPath && _currentPointIndex > lastPointIndex)
            {
                Debug.Log("PathFollower: ����� ���� �����߽��ϴ�.");
                FinalizeBubblePosition(); // ���� ��ġ ó��
                break; // ���� ����
            }
        }
    }

    /// <summary>
    /// ��� �̵��� �Ϸ�ǰų� �ߴܵǾ��� �� ������ ���� ���¸� ó���մϴ�.
    /// (��: �׸��忡 ����, Ǯ�� ��ȯ, ������Ʈ ���� ��)
    /// </summary>
    private void FinalizeBubblePosition()
    {
        var gridPos = StageManager.Instance.GridManager.GetGridPosition(this.transform.position);
        StageManager.Instance.GridManager.PlaceBubble(this.gameObject, gridPos.x, gridPos.y, isLaunched: true);

        Debug.Log("Bubble Finalized: " + gameObject.name);
    }
}