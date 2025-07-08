using UnityEngine;
using System.Collections.Generic;
using Utility.Singleton;
using System.Linq;
using System.Collections;
using System.Threading.Tasks;
using System;
using System.Threading;

public class GridManager : MonoBehaviour
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

    [SerializeField]
    private List<BubbleMaker> _bubbleMakerList = new List<BubbleMaker>();
    private CancellationTokenSource _cancellationTokenSource;

    void Awake()
    {
        grid = new GameObject[gridCols, gridRows];
        activeBubbles = new Dictionary<Vector2Int, GameObject>();
    }
    void OnEnable()
    {
        // ������Ʈ�� Ȱ��ȭ�� ������ ���ο� CancellationTokenSource ����
        // ���� �۾��� ���� ���� ���� ��츦 ����Ͽ� ���ο� �ҽ��� ����ϴ�.
        _cancellationTokenSource = new CancellationTokenSource();
    }

    void OnDisable()
    {
        // ������Ʈ�� ��Ȱ��ȭ�ǰų� �ı��� ��
        // ���� ���� ���� ��� �񵿱� �۾��� ����մϴ�.
        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel(); // ��� ��û
            _cancellationTokenSource.Dispose(); // CancellationTokenSource ���ҽ� ����
            _cancellationTokenSource = null;
        }
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

            Vector2Int[] offsets = (row % 2 == 0) ? evenRowNeighbors : oddRowNeighbors;
            foreach (Vector2Int offset in offsets)
            {
                int neighborCol = col + offset.x;
                int neighborRow = row + offset.y;

                if (col < 0 || col >= gridCols || row < 0 || row >= gridRows)
                    continue;
                
                if (GetBubbleAtGrid(neighborCol, neighborRow) == null)
                {
                    PlaceBubble(bubble, neighborCol, neighborRow, isLaunched);
                    return;
                }
            }
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
            rigidbody.gravityScale = 0;
        }

        if(isLaunched == true)
        {
            var popList = FindMatchingBubbles(new Vector2Int(col, row));
            popList = popList.Distinct().ToList();
            if (popList.Count >= 3)
            {
                PopBubbles(popList);
            }
            else
            {
                StageManager.Instance.BubbleLauncher.SpawnNewBubble();
            }
        }

        return;
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

        var targetColor = startBubbleController.bubbleColor;
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
                        if (neighborController != null && neighborController.bubbleColor == targetColor)
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

        foreach (GameObject bubble in bubblesToPop)
        {
            // ���� ������Ʈ�� ��ġ�� �׸��� ��ǥ�� ��ȯ
            Vector2Int gridPos = GetGridPosition(bubble.transform.position);

            // �׸��忡�� ���� ����
            RemoveBubble(gridPos.x, gridPos.y);

            if (bubble.TryGetComponent<Bubble>(out var bubbleScript))
            {
                if(bubbleScript.bubbleType == eBubbleType.FAIRY)
                {
                    // TODO : �������� ������
                    StageManager.Instance.DamageBoss(-5);
                }
            }

            // TODO: ���� ������ �ð� ȿ��/���� ��� (��: particle system, audio source)
            // Debug.Log($"���� ����: {bubble.name} at {gridPos}");
        }

        // TODO: ������ ���� ��, ���߿� �� �ִ� (�������� �ʴ�) ���� ã�� ���� ȣ��
        FindFloatingBubbles();

        _BubbleGeneration();
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
            RemoveBubble(gridPos.x, gridPos.y, true, true); // �׸��忡�� ����
            StageManager.Instance.BubbleManager.ReleaseBubble(floatingBubble);

            GameObject dropBubble = StageManager.Instance.BubbleManager.GetDropBubble();
            dropBubble.transform.position = floatingBubble.transform.position;
            
            if(dropBubble.TryGetComponent<Bubble>(out var dropBubbleScript))
            {
                if (floatingBubble.TryGetComponent<Bubble>(out var floatingBubbleScript))
                {
                    dropBubbleScript.SetType(floatingBubbleScript.bubbleType, floatingBubbleScript.bubbleColor, false);
                }
            }

            Rigidbody2D rb = dropBubble.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic; // ���� �ùķ��̼� �ٽ� Ȱ��ȭ
                rb.simulated = true;
                rb.gravityScale = 1.0f; // �߷� �����Ͽ� ����߸�
                                        // TODO: �������� �ִϸ��̼�/���� �� �߰�
                                        // N�� �� Ǯ�� ��ȯ�ϴ� �ڷ�ƾ ����
                StartCoroutine(DelayedReturnToPool(dropBubble, 2f));
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
        StageManager.Instance.BubbleManager.ReleaseDropBubble(bubble);
    }


    /// <summary>
    /// Ư�� �׸��� ��ġ���� ������ �����մϴ�.
    /// </summary>
    public void RemoveBubble(int col, int row, bool isRelease = true, bool isFloating = false)
    {
        if (col < 0 || col >= gridCols || row < 0 || row >= gridRows) return;

        if (grid[col, row] != null)
        {
            GameObject removedBubble = grid[col, row];
            grid[col, row] = null;
            activeBubbles.Remove(new Vector2Int(col, row));

            if (isRelease == true)
            {
                foreach (var bubbleMaker in _bubbleMakerList)
                {
                    bubbleMaker.ReleaseBubble(removedBubble);
                }

                if (isFloating == false)
                {
                    // TODO: ���ŵ� ������ Ǯ�� ��ȯ�ϰų� �ı��ϴ� ����
                    StageManager.Instance.BubbleManager.ReleaseBubble(removedBubble);
                }
            }
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
        _StartBubbleGeneration();
    }

    async void _StartBubbleGeneration()
    {
        await _InitBubble();
        StageManager.Instance.BubbleLauncher.SpawnNewBubble();
    }

    private async Task _InitBubble() // async UniTask�� �����Ͽ� await �����ϰ� ��
    {
        if (_bubbleMakerList == null || _bubbleMakerList.Count == 0)
        {
            Debug.LogWarning("BubbleSpawnManager: BubbleMaker ����Ʈ�� ��� �ֽ��ϴ�.");
            return;
        }

        if (_cancellationTokenSource != null)
        {
            // ��� ��ū ��������
            CancellationToken token = _cancellationTokenSource.Token;

            // ���� ���� �� ��� ��û�� �־����� Ȯ��
            if (token.IsCancellationRequested)
            {
                Debug.Log("MakeBubble: ��� ��û ����, ���� ���� �ߴ�.");
                return;
            }
        }

        foreach (var bubbleMaker in _bubbleMakerList)
        {
            for (int i = 0; i < bubbleMaker.BubblePathCount; i++) // �� maker�� 5���� ���� ����
            {
                bubbleMaker.MakeBubble();
                // �� ���� ���� �� ���� �ð� ���
                await Task.Delay(TimeSpan.FromSeconds(0.2f));
            }
            // �ϳ��� BubbleMaker�� ��� ������ ���� �� ���� BubbleMaker�� �Ѿ�� �� ���
            await Task.Delay(TimeSpan.FromSeconds(0.2f));
        }

        Debug.Log("��� ���� ����Ŀ�� �ʱ� ���� ������ �Ϸ�Ǿ����ϴ�.");
    }

    async void _BubbleGeneration()
    {
        await _RefillBubble();
        StageManager.Instance.BubbleLauncher.SpawnNewBubble();
    }

    private async Task _RefillBubble() // async UniTask�� �����Ͽ� await �����ϰ� ��
    {
        if (_bubbleMakerList == null || _bubbleMakerList.Count == 0)
        {
            Debug.LogWarning("BubbleSpawnManager: BubbleMaker ����Ʈ�� ��� �ֽ��ϴ�.");
            return;
        }

        if (_cancellationTokenSource != null)
        {
            // ��� ��ū ��������
            CancellationToken token = _cancellationTokenSource.Token;

            // ���� ���� �� ��� ��û�� �־����� Ȯ��
            if (token.IsCancellationRequested)
            {
                Debug.Log("MakeBubble: ��� ��û ����, ���� ���� �ߴ�.");
                return;
            }
        }

        foreach (var bubbleMaker in _bubbleMakerList)
        {
            await bubbleMaker.RefillBubble();
            // �ϳ��� BubbleMaker�� ��� ������ ���� �� ���� BubbleMaker�� �Ѿ�� �� ���
            await Task.Delay(TimeSpan.FromSeconds(0.2f));
        }

        Debug.Log("��� ���� ����Ŀ�� �ʱ� ���� ������ �Ϸ�Ǿ����ϴ�.");
    }

    /// <summary>
    /// ����Ƽ �������� �� �信 �׸��� ���ڸ� �׸��ϴ�.
    /// </summary>
    void OnDrawGizmos()
    {
        // ����� �׸� ���� ����
        Gizmos.color = Color.cyan; // �ϴû����� �׸��� ���� ǥ��

        // �׸����� �� ���� ��ȸ�ϸ� ������ �׵θ� �׸���
        for (int r = 0; r < gridRows; r++)
        {
            for (int c = 0; c < gridCols; c++)
            {
                // Ȧ�� ���� ������ �÷��� ������ ��ġ���� �ʴ� ��찡 �����Ƿ� ���� (�׸��� ��翡 ���� ����)
                if (r % 2 == 1 && c == gridCols - 1) continue;

                // �ش� �׸��� ���� ���� �߽� ��ġ�� ���
                Vector2 cellCenter = GetWorldPosition(c, r);

                // Gizmos.DrawWireSphere�� ����Ͽ� ������ ���� ��ġ�� ������ ǥ��
                // ������ �׸����̹Ƿ�, �� ���� �߽ɿ� ������ ���δٰ� �����ϰ� ���� �׸��ϴ�.
                Gizmos.DrawWireSphere(cellCenter, bubbleRadius); // ������ ������ ũ��� ���� �׸�

                // (���� ����) �׸��� ��ǥ �ؽ�Ʈ ǥ�� - Gizmos.DrawSphere�� ���� ���� ���, Handles.Label ���
                // Handles�� UnityEditor ���ӽ����̽��� ���ϹǷ�, ������ ��ũ��Ʈ������ ��� �����մϴ�.
                // �Ϲ� ��ũ��Ʈ������ ������ ������ �߻��մϴ�.
                // ���� ������ Ȯ�� ��ũ��Ʈ�� ���� �����Ѵٸ� �����մϴ�.
                // using UnityEditor;
                // Handles.Label(cellCenter + Vector2.up * bubbleRadius * 0.5f, $"({c},{r})");

                // (���� ����) ������ ���� �׵θ��� ���� �׸��� (�� ��Ȯ�� �ð�ȭ)
                // �������� 6�� ������ ���
                Vector3[] hexCorners = new Vector3[6];
                for (int i = 0; i < 6; i++)
                {
                    float angle_deg = 60 * i;
                    float angle_rad = Mathf.PI / 180 * angle_deg;
                    hexCorners[i] = cellCenter + new Vector2(bubbleRadius * Mathf.Cos(angle_rad), bubbleRadius * Mathf.Sin(angle_rad));
                }

                // 6�� ���� �׸���
                for (int i = 0; i < 6; i++)
                {
                    Gizmos.DrawLine(hexCorners[i], hexCorners[(i + 1) % 6]);
                }
            }
        }

        // ���� ���� �߻� ���� ǥ�� (���� ����)
        // BubbleLauncher���� launchPoint�� �����ϰ� �ִٸ� ���⼭�� �׸� �� �ֽ��ϴ�.
        // �Ǵ� BubbleLauncher.cs�� OnDrawGizmos���� �׷��� �˴ϴ�.
        // if (GetComponent<BubbleLauncher>() != null && GetComponent<BubbleLauncher>().launchPoint != null)
        // {
        //     Gizmos.color = Color.red;
        //     Gizmos.DrawSphere(GetComponent<BubbleLauncher>().launchPoint.position, 0.2f);
        // }
    }
}