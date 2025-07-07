using UnityEngine;
using System.Collections;

public class BubbleLauncher : MonoBehaviour
{
    public Transform launchPoint;
    public float launchForce = 15f;

    // --- ���ؼ� ���� �߰� ���� ---
    public LineRenderer lineRenderer; // �ν����Ϳ��� ������ Line Renderer ������Ʈ
    public int maxReflectionCount = 2; // �ִ� �ݻ� Ƚ��
    public LayerMask collisionLayer; // ����ĳ��Ʈ�� ������ ��/���� ���̾� (��: Wall, Bubble)

    private GameObject currentBubble;
    private bool canLaunch = true;

    // Line Renderer�� �������� ���� ����
    private Vector3[] linePoints;

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

        SpawnNewBubble();
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
        linePoints = new Vector3[maxReflectionCount + 2]; // ������ + �ݻ����� + ������ ������

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

        linePoints[0] = startPosition;
        int currentReflectionCount = 0;
        Vector2 currentOrigin = startPosition;
        Vector2 currentDirection = direction;

        for (int i = 0; i <= maxReflectionCount; i++)
        {
            // Raycast�� ���� �浹 ���� Ȯ��
            // Physics2D.Raycast(������, ����, �ִ� �Ÿ�, �浹 ���̾�)
            RaycastHit2D hit = Physics2D.Raycast(currentOrigin, currentDirection, 100f, collisionLayer); // 100f�� �ִ� ���� ����

            if (hit.collider != null)
            {
                // �浹 ������ Line Renderer�� �߰�
                linePoints[i + 1] = hit.point;
                currentReflectionCount++;

                // ���� �ε����� �� �ݻ� ���� ���
                currentDirection = Vector2.Reflect(currentDirection, hit.normal);
                currentOrigin = hit.point + currentDirection * 0.01f; // �浹 �������� ��¦ ������ ������ ���� ���� ���� (��ħ ����)

                // ����(����)�� ���� ����� ���� ��Ȯ�� �ݻ� ������ ����ϱ� ���� hit.point���� ���� ��������ŭ ������ ���� ����� �� ����
                // ������ �ð����� ���ؼ������� hit.point�����ε� �����.
            }
            else
            {
                // �浹���� �ʾҴٸ� �������� �� ���� ����
                linePoints[i + 1] = currentOrigin + currentDirection * 100f; // �ִ� ���̱��� �׸�
                currentReflectionCount++;
                break; // �� �̻� �ݻ���� �����Ƿ� ���� ����
            }
        }

        // Line Renderer�� �� ���� ���� �� ��ġ ������Ʈ
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

            // OnGetFromPool���� ��κ� �ʱ�ȭ�ǹǷ� ���⼭�� Ư���� �ڵ尡 �ʿ����� ����.
            // Rigidbody2D, Collider2D ���´� ObjectPoolManager�� OnGetFromPool���� ����.

            canLaunch = true;
        }
        else
        {
            Debug.LogError("ObjectPoolManager �Ǵ� �߻� ������ ������ �ֽ��ϴ�.");
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