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
            LaunchBubble(inputPosition);
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
            // Raycast�� ���� �浹 ���� Ȯ��
            // Physics2D.Raycast(������, ����, �ִ� �Ÿ�, �浹 ���̾�)
            bool bHitWall = _CheckRay(currentOrigin, currentDirection, "Wall", out var hitWall);
            bool bHitBubble = _CheckRay(currentOrigin, currentDirection, "Bubble", out var hitBubble);

            if(bHitBubble == true)
            {
                // �����浹

                var gridAxis = StageManager.Instance.GridManager.GetGridPosition(hitBubble.point);
                var gridPos = StageManager.Instance.GridManager.GetWorldPosition(gridAxis.x, gridAxis.y);
                _linePoints.Add(gridPos);// hitBubble.point;
                currentReflectionCount++;
                break; // �� �̻� �ݻ���� �����Ƿ� ���� ����
            }

            if(bHitWall == false && bHitBubble == false)
            {
                // �浹���� �ʾҴٸ� �������� �� ���� ����
                _linePoints.Add(currentOrigin + currentDirection * 100f); // �ִ� ���̱��� �׸�
                currentReflectionCount++;
                break; // �� �̻� �ݻ���� �����Ƿ� ���� ����
            }

            if (bHitWall == true)
            {
                // ���浹

                // �浹 ������ Line Renderer�� �߰�
                _linePoints.Add(hitWall.point);
                currentReflectionCount++;

                // ���� �ε����� �� �ݻ� ���� ���
                currentDirection = Vector2.Reflect(currentDirection, hitWall.normal);
                currentOrigin = hitWall.point + currentDirection * 0.01f; // �浹 �������� ��¦ ������ ������ ���� ���� ���� (��ħ ����)

                // ����(����)�� ���� ����� ���� ��Ȯ�� �ݻ� ������ ����ϱ� ���� hit.point���� ���� ��������ŭ ������ ���� ����� �� ����
                // ������ �ð����� ���ؼ������� hit.point�����ε� �����.
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

    private bool _CheckRay(Vector2 origin, Vector2 direction, string layerName, out RaycastHit2D hit)
    {
        hit = Physics2D.Raycast(origin, direction, 100f, LayerMask.GetMask(layerName));
        return hit.collider != null;
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

    void LaunchBubble(Vector2 touchPosition)
    {
        if (currentBubble == null) return;

        Vector3 worldTouchPosition = Camera.main.ScreenToWorldPoint(new Vector3(touchPosition.x, touchPosition.y, Camera.main.nearClipPlane));
        worldTouchPosition.z = 0;

        Vector2 launchDirection = (worldTouchPosition - launchPoint.position).normalized;

        if(launchDirection.y < 0)
        {
            // �ݴ�� ��°������� �����ʴ´�.
            return;
        }

        canLaunch = false;

        currentBubble.layer = LayerMask.NameToLayer("Bubble");
        if (currentBubble.TryGetComponent<Rigidbody2D>(out var rigidbody))
        {
            rigidbody.bodyType = RigidbodyType2D.Dynamic;
            rigidbody.gravityScale = 0;
            rigidbody.AddForce(launchDirection * launchForce, ForceMode2D.Impulse);
        }
        currentBubble = null;
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
}