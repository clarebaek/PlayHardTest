using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Utility.Singleton;

public class BubbleLauncher : MonoBehaviour
{
    [Header("�߻� ���� ���� ǥ��")]
    [SerializeField]
    private Transform _launchPoint;
    [SerializeField]
    private Transform _nextLaunchPoint;
    [SerializeField]
    private float _launchForce = 15f;

    [Header("���ؼ� ���� ǥ��")]
    [SerializeField]
    private LineRenderer _lineRenderer;
    [SerializeField]
    private int _maxReflectionCount;

    private GameObject _currentBubble;
    private GameObject _nextBubble;
    private bool _canLaunch;

    // Line Renderer�� �������� ����
    private List<Vector2> _linePoints = new List<Vector2>();

    [Header("Ÿ�� ���� ǥ��")]
    [SerializeField]
    private GameObject _targetGo;

    void Start()
    {
        if (_lineRenderer == null)
        {
#if UNITY_EDITOR
            Debug.LogError("Line Renderer�� BubbleLauncher�� ������� �ʾҽ��ϴ�.");
#endif
            return;
        }
        _lineRenderer.positionCount = 0; // �ʱ⿡�� ���� �׸��� ����
        _lineRenderer.enabled = false; // ���� �� ��Ȱ��ȭ
    }

    void Update()
    {
        if (!_canLaunch || _currentBubble == null)
        {
            // ���ؼ� ��Ȱ��ȭ
            if (_lineRenderer.enabled)
            {
                _lineRenderer.enabled = false;
                _lineRenderer.positionCount = 0;
            }
            return;
        }

        Vector2 inputPosition = Vector2.zero;
        bool inputActive = false; // �Է��� Ȱ��ȭ�Ǿ����� (��ġ �Ǵ� ���콺)
        bool inputEnded = false;  // �Է��� �������� (���� ���ų� ���콺 ��ư�� ���� ��)


        // --- ����� ��ġ �Է� ó�� ---
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            inputPosition = touch.position;
            inputActive = (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved);
            inputEnded = (touch.phase == TouchPhase.Ended);
        }
        // --- PC ���콺 �Է� ó�� ---
        else if (Input.GetMouseButton(0) || Input.GetMouseButtonDown(0) || Input.GetMouseButtonUp(0))
        {
            inputPosition = Input.mousePosition;
            inputActive = true; // ���콺 ���� ��ư�� �����ְų� ��� ������ ��
            inputEnded = Input.GetMouseButtonUp(0); // ���콺 ���� ��ư�� ������ ��
        }
        // --- �� ���� ��� (�Է��� ���� ��) ---
        else
        {
            DisableAimLine();
            return;
        }

        // �Է��� Ȱ��ȭ�� �����̸� ���ؼ� �׸���
        if (inputActive)
        {
            DrawAimLine(inputPosition);
        }

        // �Է��� �������� ���� �߻�
        if (inputEnded)
        {
            DisableAimLine(); // ���ؼ� ����
            if (LaunchBubble(inputPosition) == true)
            {
                StageManager.Instance.ShootBubble();
            }
        }
    }
    
    /// <summary>
    /// ���ؼ��� ��Ȱ��ȭ�ϰ� ��ġ�� �ʱ�ȭ�ϴ� ���� �Լ�
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
    /// ��ġ ��ġ�� ������� ���ؼ��� �ݻ� ��θ� �׸��ϴ�.
    /// </summary>
    /// <param name="touchPosition">ȭ�� ��ġ �ȼ� ��ǥ</param>
    void DrawAimLine(Vector2 touchPosition)
    {
        if (_lineRenderer == null) return;

        _lineRenderer.enabled = true; // ���ؼ� Ȱ��ȭ
        _linePoints.Clear();// ������ + �ݻ����� + ������ ������

        Vector3 startPosition = _launchPoint.position;
        Vector3 worldTouchPosition = Camera.main.ScreenToWorldPoint(new Vector3(touchPosition.x, touchPosition.y, Camera.main.nearClipPlane));
        worldTouchPosition.z = 0; // 2D �����̹Ƿ� Z�� ����

        Vector2 direction = (worldTouchPosition - startPosition).normalized;

        // ������ �߻� �������� ���� ���ص� ���� ���� �׸�
        if (direction.y < 0.1f) // 0.1f�� �Ӱ谪, �ʿ信 ���� ����
        {
            _lineRenderer.enabled = false; // �ʹ� �Ʒ��� �����ϸ� ���� �׸��� ����
            _lineRenderer.positionCount = 0;
            return;
        }

        _linePoints.Add(startPosition);
        int currentReflectionCount = 0;
        Vector2 currentOrigin = startPosition;
        Vector2 currentDirection = direction;

        for (int i = 0; i <= _maxReflectionCount; i++)
        {
            // 0.5��ŭ ����
            Vector2 targetPos = currentOrigin + currentDirection * 0.5f;

            var gridAxis = StageManager.Instance.GridManager.GetGridPosition(targetPos);
            var gridPos = StageManager.Instance.GridManager.GetWorldPosition(gridAxis.x, gridAxis.y);

            // ���� �浹���� üũ
            bool bHitBubble = StageManager.Instance.GridManager.GetBubbleAtGrid(gridAxis.x, gridAxis.y) != null;
            // ��ó�� ������ �ִ��� üũ
            bool bNearBubble = StageManager.Instance.GridManager.GetNearBubbleByPosition(targetPos);
            // �¿� �� üũ
            bool bHitHorizontalWall = StageManager.Instance.GridManager.GetHorizontalWallAtPosition(targetPos);
            // ���� �� üũ
            bool bHitVerticalWall = StageManager.Instance.GridManager.GetVerticalWallAtPosition(targetPos);

            if(bHitBubble == true)
            {
                // �����浹
                currentReflectionCount++;
                var lastPos = _linePoints.ElementAt(_linePoints.Count - 1);
                gridAxis = StageManager.Instance.GridManager.GetGridPosition(lastPos);
                gridPos = StageManager.Instance.GridManager.GetWorldPosition(gridAxis.x, gridAxis.y);
                _linePoints[_linePoints.Count - 1] = gridPos;
                _SetTargetGoActive(true, gridPos.x, gridPos.y);
                break; // �� �̻� �ݻ���� �����Ƿ� ���� ����
            }

            if(bNearBubble== true)
            {
                // ������ �ֺ��� ����� ������ ��ΰ� ����Ǹ� �ȵȴ�.
                currentReflectionCount++;
                _linePoints.Add(gridPos);
                _SetTargetGoActive(true, gridPos.x, gridPos.y);
                break; // �� �̻� �ݻ���� �����Ƿ� ���� ����
            }

            if (bHitVerticalWall == true)
            {
                // ���ϴܺ��� �ε�����쿡�� �Ҹ���
                currentReflectionCount++;
                _linePoints.Add(gridPos);
                _SetTargetGoActive(false, 0,0);
                break; // �� �̻� �ݻ���� �����Ƿ� ���� ����
            }

            if (bHitHorizontalWall == false && bHitVerticalWall  == false && bHitBubble == false)
            {
                // �������� ��� ��� ����ó��
                _linePoints.Add(targetPos);
                currentReflectionCount++;
                currentOrigin = targetPos;
                continue;
            }

            if (bHitHorizontalWall == true)
            {
                // ���� �ε����� �� �ݻ� ���� ���
                currentDirection = Vector2.Reflect(currentDirection, currentDirection.x >= 0 ? Vector2.left : Vector2.right);
                _SetTargetGoActive(false, 0, 0);
            }
        }

        // Line Renderer�� �� ���� ���� �� ��ġ ������Ʈ
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
    /// ���� �������� ǥ���� Active �� Position�� �����մϴ�.
    /// </summary>
    /// <param name="set">Ȱ��ȭ����</param>
    /// <param name="x">x��ǥ</param>
    /// <param name="y">y��ǥ</param>
    void _SetTargetGoActive(bool set, float x, float y)
    {
        _targetGo.SetActive(set);
        _targetGo.transform.position = new Vector2(x,y);
    }

    /// <summary>
    /// ���� ������ �����մϴ�.
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

            // ���� ���� �������� ���� ���� ���� ������������ �ʱ�ȭó��
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
            Debug.LogError("ObjectPoolManager �Ǵ� �߻� ������ ������ �ֽ��ϴ�.");
#endif
        }

        _nextBubble = StageManager.Instance.BubbleManager.GetBubble();

        if (_nextBubble != null && _nextLaunchPoint != null)
        {
            _nextBubble.transform.position = _nextLaunchPoint.position;
            _nextBubble.transform.rotation = Quaternion.identity;

            // ������ Ÿ���� �����Ѵ�.
            if (_nextBubble.TryGetComponent<Bubble>(out var bubbleScript))
            {
                bubbleScript.InitBubble(StageManager.Instance.BubbleManager.RandomBubbleType(eBubbleType.NORMAL, eBubbleType.BOMB),
                        StageManager.Instance.BubbleManager.RandomBubbleColor(eBubbleColor.RED, eBubbleColor.BLUE));
            }
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogError("ObjectPoolManager �Ǵ� �߻� ������ ������ �ֽ��ϴ�.");
#endif
        }
    }

    /// <summary>
    /// ���� ������ ���� ����
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
    /// ���� ������ ���� �߻�
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
            // �ݴ�� ��°������� �����ʴ´�.
            return false;
        }

        _canLaunch = false;

        //�����μ���
        PathFollower pathFollower = _currentBubble.AddComponent<PathFollower>();
        BubblePath path = _currentBubble.AddComponent<BubblePath>();
        path.pathPoints = _linePoints;
        pathFollower.Initialize(path, _launchForce, 0);

        _currentBubble = null;

        return true;
    }

    /// <summary>
    /// ���� ����� ������ ������ ��ü�Ѵ�.
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
    /// ������ ������ Ư������� ��ü�Ѵ�.
    /// </summary>
    public void ChangeCurrentBubbleToBomb()
    {
        if(_currentBubble.TryGetComponent<Bubble>(out var currentBubbleScript))
        {
            currentBubbleScript.InitBubble(eBubbleType.CAT_BOMB, eBubbleColor.SPECIAL);
        }
    }
}