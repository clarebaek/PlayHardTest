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

    // --- ���ؼ� ���� �߰� ���� ---
    public LineRenderer lineRenderer; // �ν����Ϳ��� ������ Line Renderer ������Ʈ
    public int maxReflectionCount;
    public LayerMask collisionLayer; // ����ĳ��Ʈ�� ������ ��/���� ���̾� (��: Wall, Bubble)

    private GameObject currentBubble;
    private GameObject nextBubble;
    private bool canLaunch = true;

    // Line Renderer�� �������� ���� ����
    private List<Vector2> _linePoints = new List<Vector2>();

    void Start()
    {
        // Line Renderer �ʱ� ����
        if (lineRenderer == null)
        {
            Debug.LogError("Line Renderer�� BubbleLauncher�� ������� �ʾҽ��ϴ�.");
            return;
        }
        lineRenderer.positionCount = 0; // �ʱ⿡�� ���� �׸��� ����
        lineRenderer.enabled = false; // ���� �� ��Ȱ��ȭ
    }

    void Update()
    {
        if (!canLaunch || currentBubble == null)
        {
            // ���ؼ� ��Ȱ��ȭ
            if (lineRenderer.enabled)
            {
                lineRenderer.enabled = false;
                lineRenderer.positionCount = 0;
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
        if (lineRenderer != null && lineRenderer.enabled)
        {
            lineRenderer.enabled = false;
            lineRenderer.positionCount = 0;
        }
    }

    /// <summary>
    /// ��ġ ��ġ�� ������� ���ؼ��� �ݻ� ��θ� �׸��ϴ�.
    /// </summary>
    /// <param name="touchPosition">ȭ�� ��ġ �ȼ� ��ǥ</param>
    void DrawAimLine(Vector2 touchPosition)
    {
        if (lineRenderer == null) return;

        lineRenderer.enabled = true; // ���ؼ� Ȱ��ȭ
        _linePoints.Clear();// ������ + �ݻ����� + ������ ������

        Vector3 startPosition = launchPoint.position;
        Vector3 worldTouchPosition = Camera.main.ScreenToWorldPoint(new Vector3(touchPosition.x, touchPosition.y, Camera.main.nearClipPlane));
        worldTouchPosition.z = 0; // 2D �����̹Ƿ� Z�� ����

        Vector2 direction = (worldTouchPosition - startPosition).normalized;

        // ������ �߻� �������� ���� ���ص� ���� ���� �׸�
        if (direction.y < 0.1f) // 0.1f�� �Ӱ谪, �ʿ信 ���� ����
        {
            lineRenderer.enabled = false; // �ʹ� �Ʒ��� �����ϸ� ���� �׸��� ����
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
                // �����浹
                currentReflectionCount++;
                var lastPos = _linePoints.ElementAt(_linePoints.Count - 1);
                gridAxis = StageManager.Instance.GridManager.GetGridPosition(lastPos);
                gridPos = StageManager.Instance.GridManager.GetWorldPosition(gridAxis.x, gridAxis.y);
                _linePoints[_linePoints.Count - 1] = gridPos;
                break; // �� �̻� �ݻ���� �����Ƿ� ���� ����
            }

            if(bNearBubble== true)
            {
                // ������ �ֺ��� ����� ������ ��ΰ� ����Ǹ� �ȵȴ�.
                currentReflectionCount++;
                _linePoints.Add(gridPos);
                break; // �� �̻� �ݻ���� �����Ƿ� ���� ����
            }

            if(bHitWall == false && bHitBubble == false)
            {
                _linePoints.Add(targetPos);
                currentReflectionCount++;
                currentOrigin = targetPos;
            }

            if (bHitWall == true)
            {
                // ���浹
                //_linePoints.Add(targetPos);
                //currentReflectionCount++;

                // ���� �ε����� �� �ݻ� ���� ���
                currentDirection = Vector2.Reflect(currentDirection, currentDirection.x >= 0 ? Vector2.left : Vector2.right);
            }
        }

        // Line Renderer�� �� ���� ���� �� ��ġ ������Ʈ
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

            // ������ Ÿ���� �����Ѵ�.
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
            Debug.LogError("ObjectPoolManager �Ǵ� �߻� ������ ������ �ֽ��ϴ�.");
        }

        nextBubble = StageManager.Instance.BubbleManager.GetBubble();

        if (nextBubble != null && nextLaunchPoint != null)
        {
            nextBubble.transform.position = nextLaunchPoint.position;
            nextBubble.transform.rotation = Quaternion.identity;

            nextBubble.layer = LayerMask.NameToLayer("Default");

            canLaunch = true;

            // ������ Ÿ���� �����Ѵ�.
            if (nextBubble.TryGetComponent<Bubble>(out var bubbleScript))
            {
                bubbleScript.SetType((eBubbleType)Random.Range((int)eBubbleType.NORMAL, (int)eBubbleType.FAIRY + 1)
                    , (eBubbleColor)UnityEngine.Random.Range((int)eBubbleColor.RED, (int)eBubbleColor.BLUE + 1)
                    , isLaunched: true);
            }
        }
        else
        {
            Debug.LogError("ObjectPoolManager �Ǵ� �߻� ������ ������ �ֽ��ϴ�.");
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
            // �ݴ�� ��°������� �����ʴ´�.
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