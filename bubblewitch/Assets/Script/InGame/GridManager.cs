using UnityEngine;
using System.Collections.Generic;
using Utility.Singleton;
using System.Linq;
using System.Collections;

public class GridManager : MonoSingleton<GridManager>
{
    /// <summary>
     /// ������ �׸��忡�� ������ ������ ����� ������ ��ǥ (Odd-r Offset)
     /// ���� �� (row)�� ¦������ Ȧ�������� ���� �ٸ��ϴ�.
     /// </summary>
    private static readonly Vector2Int[] evenRowNeighbors = new Vector2Int[]
    {
    new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 1),
    new Vector2Int(-1, 0), new Vector2Int(-1, -1), new Vector2Int(0, -1)
    };

    private static readonly Vector2Int[] oddRowNeighbors = new Vector2Int[]
    {
    new Vector2Int(1, 0), new Vector2Int(1, 1), new Vector2Int(0, 1),
    new Vector2Int(-1, 0), new Vector2Int(0, -1), new Vector2Int(1, -1)
    };

    public int offsetX = 0;
    public int offsetY = 5;

    // �׸��� ����
    public int gridRows = 10;
    public int gridCols = 8;
    public float bubbleRadius = 0.5f; // ���� ������ ������

    // �׸��忡 ������ ������ 2���� �迭 (GameObject�� ���� �ν��Ͻ�)
    // null�� �� ĭ�� �ǹ�
    private GameObject[,] grid;

    // ���� ����: Ư�� �׸��� ��ġ�� ������ �����ϱ� ���� Dictionary
    private Dictionary<Vector2Int, GameObject> activeBubbles;

    void Awake()
    {
        grid = new GameObject[gridCols, gridRows];
        activeBubbles = new Dictionary<Vector2Int, GameObject>();
    }

    /// <summary>
    /// ������ Ư�� �׸��� ��ġ�� �߰��ϰ� ���� ��ǥ�� �����մϴ�.
    /// </summary>
    public void PlaceBubble(GameObject bubble, int col, int row, bool isLaunched = false)
    {
        if (col < 0 || col >= gridCols || row < 0 || row >= gridRows)
        {
            Debug.LogWarning($"GridManager: ({col},{row})�� ��ȿ���� ���� �׸��� ��ġ�Դϴ�.");
            return;
        }
        if (grid[col, row] != null)
        {
            Debug.LogWarning($"GridManager: ({col},{row}) ��ġ�� �̹� ������ �ֽ��ϴ�.");
            return;
        }

        grid[col, row] = bubble;
        activeBubbles[new Vector2Int(col, row)] = bubble;

        // ������ ���� ��ġ�� �׸��忡 �°� ���� (������ Rigidbody2D�� Kinematic���� ����)
        bubble.transform.position = GetWorldPosition(col, row);

        // ������ �׸��忡 �پ����Ƿ� Rigidbody2D�� Kinematic���� �����ϰ� �ӵ� �ʱ�ȭ
        if (bubble.TryGetComponent<Rigidbody2D>(out var rigidbody))
        {
            rigidbody.linearVelocity = Vector2.zero;
            rigidbody.angularVelocity = 0f;
            rigidbody.bodyType = RigidbodyType2D.Kinematic;
            rigidbody.simulated = true; // �ùķ��̼��� ��� Ȱ��ȭ
        }

        if(isLaunched == true)
        {
            var popList = FindMatchingBubbles(new Vector2Int(col, row));
            if(popList.Count >= 3)
            {
                PopBubbles(popList);
            }
        }
    }
    /// <summary>
    /// Ư�� �׸��� ��ġ���� �����Ͽ� ���� ������ ����� ������� ��� ã���ϴ�. (BFS/DFS)
    /// </summary>
    /// <param name="startGridPos">Ž���� ������ ������ �׸��� ��ǥ</param>
    /// <returns>����� ���� ���� ���� GameObject ����Ʈ</returns>
    public List<GameObject> FindMatchingBubbles(Vector2Int startGridPos)
    {
        GameObject startBubble = GetBubbleAtGrid(startGridPos.x, startGridPos.y);
        if (startBubble == null)
        {
            // Debug.LogWarning($"FindMatchingBubbles: ���� ��ġ ({startGridPos.x},{startGridPos.y})�� ������ �����ϴ�.");
            return new List<GameObject>(); // �� ����Ʈ ��ȯ
        }

        var startBubbleController = startBubble.GetComponent<Bubble>();
        if (startBubbleController == null || startBubbleController.bubbleType == eBubbleType.NONE)
        {
            // Debug.LogWarning($"FindMatchingBubbles: ���� ���� BubbleController�� ���ų� ������ �����ϴ�.");
            return new List<GameObject>();
        }

        var targetColor = startBubbleController.bubbleType;
        List<GameObject> matchingBubbles = new List<GameObject>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>(); // �̹� �湮�� �׸��� ��ġ ���

        queue.Enqueue(startGridPos);
        visited.Add(startGridPos);
        matchingBubbles.Add(startBubble);

        while (queue.Count > 0)
        {
            Vector2Int currentGridPos = queue.Dequeue();

            // ���� ������ ������ 6���� ���� Ž��
            Vector2Int[] offsets = (currentGridPos.y % 2 == 0) ? evenRowNeighbors : oddRowNeighbors;

            foreach (Vector2Int offset in offsets)
            {
                int neighborCol = currentGridPos.x + offset.x;
                int neighborRow = currentGridPos.y + offset.y;
                Vector2Int neighborGridPos = new Vector2Int(neighborCol, neighborRow);

                // ��ȿ�� �׸��� ���� ���� �ְ�, ���� �湮���� �ʾҴ��� Ȯ��
                if (neighborCol >= 0 && neighborCol < gridCols &&
                    neighborRow >= 0 && neighborRow < gridRows &&
                    !visited.Contains(neighborGridPos))
                {
                    GameObject neighborBubble = GetBubbleAtGrid(neighborCol, neighborRow);
                    if (neighborBubble != null)
                    {
                        var neighborController = neighborBubble.GetComponent<Bubble>();
                        // �̿� ������ �����ϰ�, BubbleController�� ������, ������ ������
                        if (neighborController != null && neighborController.bubbleType == targetColor)
                        {
                            visited.Add(neighborGridPos);
                            queue.Enqueue(neighborGridPos);
                            matchingBubbles.Add(neighborBubble);
                        }
                    }
                }
            }
        }

        return matchingBubbles;
    }

    /// <summary>
    /// �߰ߵ� ������� ��Ʈ���� �׸��忡�� �����մϴ�.
    /// </summary>
    /// <param name="bubblesToPop">��Ʈ�� ���� GameObject ����Ʈ</param>
    public void PopBubbles(List<GameObject> bubblesToPop)
    {
        if (bubblesToPop == null || bubblesToPop.Count == 0) return;

        // �ߺ� ���� (Ȥ�� ���� ������ ���� �� ����Ʈ�� �� ��� ���)
        bubblesToPop = bubblesToPop.Distinct().ToList();

        foreach (GameObject bubble in bubblesToPop)
        {
            // ���� ������Ʈ�� ��ġ�� �׸��� ��ǥ�� ��ȯ
            Vector2Int gridPos = GetGridPosition(bubble.transform.position);

            // �׸��忡�� ���� ����
            RemoveBubble(gridPos.x, gridPos.y);

            // TODO: ���� ������ �ð� ȿ��/���� ��� (��: particle system, audio source)
            // Debug.Log($"���� ����: {bubble.name} at {gridPos}");

            // ������ ������Ʈ Ǯ�� ��ȯ
            BubbleManager.Instance.ReleaseBubble(bubble);
        }

        // TODO: ������ ���� ��, ���߿� �� �ִ� (�������� �ʴ�) ���� ã�� ���� ȣ��
        FindFloatingBubbles();
    }

    /// <summary>
    /// ���� ����� ���� �������� �Ұ� ���߿� �� �ִ� ������� ã�� ����߸��ϴ�.
    /// </summary>
    public void FindFloatingBubbles()
    {
        // �ֻ�� �� (gridRows - 1)���� �����Ͽ� ��� ������ ��ȸ�մϴ�.
        // ����� ���� �׷��� ã��, �� �׷��� ��� ��(���� ���� ��)�� ����ִ��� Ȯ���մϴ�.
        // ������� �ʴٸ� ����߸��ϴ�.

        HashSet<GameObject> visited = new HashSet<GameObject>();
        List<GameObject> floatingBubbles = new List<GameObject>();

        // �׸��� ��ü�� ��ȸ�ϸ鼭 ��� Ȱ�� ������ Ȯ��
        for (int r = gridRows - 1; r >= 0; r--) // ������ �Ʒ���
        {
            for (int c = 0; c < gridCols; c++)
            {
                GameObject currentBubble = GetBubbleAtGrid(c, r);
                if (currentBubble != null && !visited.Contains(currentBubble))
                {
                    // �� ������ ���� ����� �׷��� ã���ϴ� (���� ����)
                    List<GameObject> connectedGroup = FindConnectedGroup(new Vector2Int(c, r), visited);

                    // �� �׷��� ���߿� �� �ִ��� Ȯ��
                    bool isFloating = true;
                    foreach (GameObject bubbleInGroup in connectedGroup)
                    {
                        Vector2Int groupBubblePos = GetGridPosition(bubbleInGroup.transform.position);
                        if (groupBubblePos.y >= gridRows - 1) // ���� ���� �ٿ� ����ִ� ������ �׷� ���� �ִٸ�
                        {
                            isFloating = false; // �� �׷��� ���߿� �� ���� ����
                            break;
                        }
                    }

                    if (isFloating)
                    {
                        floatingBubbles.AddRange(connectedGroup);
                    }
                }
            }
        }

        // ã�� ��� �� �ִ� ������� ����߸��ϴ�.
        foreach (GameObject floatingBubble in floatingBubbles)
        {
            Vector2Int gridPos = GetGridPosition(floatingBubble.transform.position);
            RemoveBubble(gridPos.x, gridPos.y); // �׸��忡�� ����

            Rigidbody2D rb = floatingBubble.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic; // ���� �ùķ��̼� �ٽ� Ȱ��ȭ
                rb.simulated = true;
                rb.gravityScale = 1.0f; // �߷� �����Ͽ� ����߸�
                // TODO: �������� �ִϸ��̼�/���� �� �߰�
                // N�� �� Ǯ�� ��ȯ�ϴ� �ڷ�ƾ ����
                StartCoroutine(DelayedReturnToPool(floatingBubble, 2f));
            }
            else
            {
                BubbleManager.Instance.ReleaseBubble(floatingBubble);
            }
        }
    }

    /// <summary>
    /// Ư�� �׸��� ��ġ���� �����Ͽ� ��� ����� ���� �׷��� ã���ϴ�. (���� ����)
    /// FindFloatingBubbles�� ���� ���� �Լ�.
    /// </summary>
    private List<GameObject> FindConnectedGroup(Vector2Int startGridPos, HashSet<GameObject> visitedBubblesTracker)
    {
        List<GameObject> connectedGroup = new List<GameObject>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        GameObject startBubble = GetBubbleAtGrid(startGridPos.x, startGridPos.y);
        if (startBubble == null || visitedBubblesTracker.Contains(startBubble)) return connectedGroup;

        queue.Enqueue(startGridPos);
        visitedBubblesTracker.Add(startBubble); // ��ü Ž������ �湮 ó��
        connectedGroup.Add(startBubble);

        while (queue.Count > 0)
        {
            Vector2Int currentGridPos = queue.Dequeue();

            Vector2Int[] offsets = (currentGridPos.y % 2 == 0) ? evenRowNeighbors : oddRowNeighbors;

            foreach (Vector2Int offset in offsets)
            {
                int neighborCol = currentGridPos.x + offset.x;
                int neighborRow = currentGridPos.y + offset.y;
                Vector2Int neighborGridPos = new Vector2Int(neighborCol, neighborRow);

                if (neighborCol >= 0 && neighborCol < gridCols &&
                    neighborRow >= 0 && neighborRow < gridRows)
                {
                    GameObject neighborBubble = GetBubbleAtGrid(neighborCol, neighborRow);
                    if (neighborBubble != null && !visitedBubblesTracker.Contains(neighborBubble))
                    {
                        visitedBubblesTracker.Add(neighborBubble);
                        queue.Enqueue(neighborGridPos);
                        connectedGroup.Add(neighborBubble);
                    }
                }
            }
        }
        return connectedGroup;
    }

    private IEnumerator DelayedReturnToPool(GameObject bubble, float delay)
    {
        yield return new WaitForSeconds(delay);
        BubbleManager.Instance.ReleaseBubble(bubble);
    }


    /// <summary>
    /// Ư�� �׸��� ��ġ���� ������ �����մϴ�.
    /// </summary>
    public void RemoveBubble(int col, int row)
    {
        if (col < 0 || col >= gridCols || row < 0 || row >= gridRows) return;

        if (grid[col, row] != null)
        {
            GameObject removedBubble = grid[col, row];
            grid[col, row] = null;
            activeBubbles.Remove(new Vector2Int(col, row));
            // TODO: ���ŵ� ������ Ǯ�� ��ȯ�ϰų� �ı��ϴ� ����
            BubbleManager.Instance.ReleaseBubble(removedBubble);
        }
    }

    /// <summary>
    /// Ư�� �׸��� ��ġ�� ������ �ִ��� Ȯ���մϴ�.
    /// </summary>
    public GameObject GetBubbleAtGrid(int col, int row)
    {
        if (col < 0 || col >= gridCols || row < 0 || row >= gridRows) return null;
        return grid[col, row];
    }

    /// <summary>
    /// �׸��� ��ǥ�� �ش��ϴ� ���� ��ġ�� ��ȯ�մϴ�.
    /// </summary>
    public Vector2 GetWorldPosition(int col, int row)
    {
        float x = col * bubbleRadius * 2;
        float y = row * bubbleRadius * Mathf.Sqrt(3); // ��ġ�� �κ� ���

        // Ȧ�� ���� X������ �� ĭ �̵�
        if (row % 2 == 1)
        {
            x += bubbleRadius;
        }
        return new Vector2(x, y);
    }

    /// <summary>
    /// ���� ��ġ���� ���� ����� �׸��� �� ��ǥ�� ã���ϴ�.
    /// �� �κ��� ������ �׸��� ��ǥ�� ��ȯ���� ���� ��ٷο� �κ��Դϴ�.
    /// </summary>
    public Vector2Int GetGridPosition(Vector2 worldPosition)
    {
        // RedBlobGames Hex Grid ���� Ȱ�� (Offset Coordinates)
        // https://www.redblobgames.com/grids/hexagons/

        // �뷫���� �׸��� x, y ��� (�ݿø� ��)
        float roughCol = worldPosition.x / (bubbleRadius * 2);
        float roughRow = worldPosition.y / (bubbleRadius * Mathf.Sqrt(3));

        // ¦�� �Ǵ� Ȧ�� �࿡ ���� ����
        // �� �κ��� ��Ȯ�� ������ ��ǥ ��ȯ ������ ���� �մϴ�.
        // ���� ���, axial/cube coordinate�� ��ȯ �� �ݿø�, �ٽ� offset���� ��ȯ�ϴ� ����� �� �߰��մϴ�.
        // ���⼭�� ���丸 �����ϰ� ���� ������ �� ������ �� �ֽ��ϴ�.

        // �ӽ� ��ȯ�� (���� ���� �ʿ�)
        int col = Mathf.RoundToInt(roughCol);
        int row = Mathf.RoundToInt(roughRow);

        // Ȧ�� �� ���� (�ٽ� ������ ����)
        if (row % 2 == 1)
        {
            col = Mathf.RoundToInt((worldPosition.x - bubbleRadius) / (bubbleRadius * 2));
        }

        return new Vector2Int(col, row);
    }

    // �ʱ� �׸��� ���� ���� (�׽�Ʈ��)
    void Start()
    {
        // ��ܿ� �̸� ���� ä���
        // (GridManager.Instance.PlaceBubble ȣ�� �� ObjectPoolManager �ʿ�)
        // ObjectPoolManager�� �̱����̶�� ������ ���� ���
        if (BubbleManager.Instance != null)
        {
            for (int r = 0; r < 4; r++) // ���÷� 4�ٸ� ä��
            {
                for (int c = 0; c < gridCols; c++)
                {
                    int convertC = c + offsetX;
                    int convertR = r + offsetY;

                    // Ȧ�� ���� �� ������ �÷��� ����δ� ��찡 ���� (�׸��� ��� ���߱�)
                    if (convertR % 2 == 1 && convertC == gridCols - 1) continue;

                    GameObject newBubble = BubbleManager.Instance.GetBubble();
                    if (newBubble != null)
                    {
                        // ������ Ÿ���� �����Ѵ�.
                        if (newBubble.TryGetComponent<Bubble>(out var bubbleScript))
                        {
                            bubbleScript.SetType((eBubbleType)Random.Range((int)eBubbleType.NORMAL_START, (int)eBubbleType.NORMAL_END));
                        }
                        PlaceBubble(newBubble, convertC, convertR);
                    }
                }
            }
        }
    }
}