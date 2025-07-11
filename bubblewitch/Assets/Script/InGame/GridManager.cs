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
    private static readonly Vector2Int[] oddRowNeighbors = new Vector2Int[]
    {
        new Vector2Int(0, 1), new Vector2Int(-1, 1), new Vector2Int(-1, 0),
        new Vector2Int(-1, -1), new Vector2Int(0, -1), new Vector2Int(1, 0)
    };

    private static readonly Vector2Int[] evenRowNeighbors = new Vector2Int[]
    {
        new Vector2Int(1, 1), new Vector2Int(0, 1), new Vector2Int(-1, 0),
        new Vector2Int(0, -1), new Vector2Int(1, -1), new Vector2Int(1, 0)
    };

    // �׸��� ����
    public int gridRows = 10;
    public int gridCols = 8;
    public float bubbleRadius = 0.5f; // ���� ������ ������

    public float MIN_AXIS_X;
    public float MAX_AXIS_X;
    public float MIN_AXIS_Y;
    public float MAX_AXIS_Y;

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
#if UNITY_EDITOR
            Debug.LogWarning($"GridManager: ({col},{row})�� ��ȿ���� ���� �׸��� ��ġ�Դϴ�.");
#endif
            return;
        }
        if (grid[col, row] != null)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"GridManager: ({col},{row}) ��ġ�� �̹� ������ �ֽ��ϴ�.");
#endif

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

        // ������ ���� ��ġ�� �׸��忡 �°� ����
        bubble.transform.position = GetWorldPosition(col, row);

        //�߻�� �����̶��
        //�׸��忡 �������鼭 ���߰˻�����
        if(isLaunched == true)
        {
            if (bubble.TryGetComponent<Bubble>(out var bubbleScript))
            {
                switch(bubbleScript.bubbleType)
                {
                    case eBubbleType.CAT_BOMB:
                        _CatBombTypeProcess();
                        break;
                    case eBubbleType.BOMB:
                        _BombTypeProcess();
                        break;
                    case eBubbleType.FAIRY:
                    case eBubbleType.NORMAL:
                    default:
                        _NormalTypeProcess();
                        break;
                };
            }
        }

        return;

        void _NormalTypeProcess()
        {
            var popList = _FindMatchingBubbles(new Vector2Int(col, row));
            _ProcessPopList(popList , false);
        }

        void _CatBombTypeProcess()
        {
            var popList = FindBombRange(new Vector2Int(col, row), 2);
            _ProcessPopList(popList, true);
        }

        void _BombTypeProcess()
        {
            var popList = FindBombRange(new Vector2Int(col, row), 1);
            _ProcessPopList(popList, true);
        }

        void _ProcessPopList(List<GameObject> popList, bool unconditionally)
        {
            popList = popList.Distinct().ToList();
            if (popList.Count >= 3 || unconditionally == true)
            {
                PopBubbles(popList);
            }
            else
            {
                StageManager.Instance.CompleteGridProcess();
            }
        }
    }
    /// <summary>
    /// Ư�� �׸��� ��ġ���� �����Ͽ� ���� ������ ����� ������� ��� ã���ϴ�. (BFS/DFS)
    /// </summary>
    /// <param name="startGridPos">Ž���� ������ ������ �׸��� ��ǥ</param>
    /// <returns>����� ���� ���� ���� GameObject ����Ʈ</returns>
    private List<GameObject> _FindMatchingBubbles(Vector2Int startGridPos)
    {
        GameObject startBubble = GetBubbleAtGrid(startGridPos.x, startGridPos.y);
        if (startBubble == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"FindMatchingBubbles: ���� ��ġ ({startGridPos.x},{startGridPos.y})�� ������ �����ϴ�.");
#endif
            return new List<GameObject>();
        }

        var startBubbleController = startBubble.GetComponent<Bubble>();
        if (startBubbleController == null || startBubbleController.bubbleType == eBubbleType.NONE)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"FindMatchingBubbles: ���� ���� Bubble�� ���ų� ������ �����ϴ�.");
#endif
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
    /// FindMatchBubble�� �������� �ֺ� nĭ��ŭ Ž���ϵ��� ������ �Լ�
    /// </summary>
    /// <param name="startGridPos"></param>
    /// <param name="range"></param>
    /// <returns></returns>
    public List<GameObject> FindBombRange(Vector2Int startGridPos, int range)
    {
        GameObject startBubble = GetBubbleAtGrid(startGridPos.x, startGridPos.y);
        if (startBubble == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"FindBombRange: ���� ��ġ ({startGridPos.x},{startGridPos.y})�� ������ �����ϴ�.");
#endif
            return new List<GameObject>(); // �� ����Ʈ ��ȯ
        }

        var startBubbleController = startBubble.GetComponent<Bubble>();
        if (startBubbleController == null || startBubbleController.bubbleType == eBubbleType.NONE)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"FindBombRange: ���� ���� Bubble�� ���ų� ������ �����ϴ�.");
#endif
            return new List<GameObject>();
        }

        List<GameObject> matchingBubbles = new List<GameObject>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>(); // �̹� �湮�� �׸��� ��ġ ���

        queue.Enqueue(startGridPos);
        visited.Add(startGridPos);
        matchingBubbles.Add(startBubble);

        while (range>0)
        {
            range--;
            Queue<Vector2Int> newQueue = new Queue<Vector2Int>();
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
                        visited.Add(neighborGridPos);
                        newQueue.Enqueue(neighborGridPos);

                        GameObject neighborBubble = GetBubbleAtGrid(neighborCol, neighborRow);
                        if (neighborBubble != null)
                        {
                            var neighborController = neighborBubble.GetComponent<Bubble>();
                            if (neighborController != null)
                            {
                                matchingBubbles.Add(neighborBubble);
                            }
                        }
                    }
                }
            }
            queue = newQueue;
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
                    //�������� ������
                    StageManager.Instance.DamageBoss(-5);
                }
            }

#if UNITY_EDITOR
            Debug.Log($"���� ����: {bubble.name} at {gridPos}");
#endif
        }

        // ���߿� �� ���� Ž��
        _FindFloatingBubbles();

        // ���� ����
        _RunBubbleRefill();
    }

    /// <summary>
    /// ���� ����� ���� �������� �Ұ� ���߿� �� �ִ� ������� ã�� ����߸��ϴ�.
    /// </summary>
    private void _FindFloatingBubbles()
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
                    List<GameObject> connectedGroup = _FindConnectedGroup(new Vector2Int(c, r), visited);

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
                    dropBubbleScript.InitBubble(floatingBubbleScript.bubbleType, floatingBubbleScript.bubbleColor);
                }
            }

            List<Vector2> dropPath = new List<Vector2>();
            PathFollower pathFollower = dropBubble.AddComponent<PathFollower>();
            BubblePath path = dropBubble.AddComponent<BubblePath>();
            dropPath.Add(new Vector2(dropBubble.transform.position.x, dropBubble.transform.position.y));
            dropPath.Add(new Vector2(dropBubble.transform.position.x, -5));
            path.pathPoints = dropPath;
            pathFollower.Initialize(path, 9.8f, 0, true);
        }
    }

    /// <summary>
    /// Ư�� �׸��� ��ġ���� �����Ͽ� ��� ����� ���� �׷��� ã���ϴ�. (���� ����)
    /// FindFloatingBubbles�� ���� ���� �Լ�.
    /// </summary>
    private List<GameObject> _FindConnectedGroup(Vector2Int startGridPos, HashSet<GameObject> visitedBubblesTracker)
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
                // �ʻ��� ��λ󿡼� ���� ����
                foreach (var bubbleMaker in _bubbleMakerList)
                {
                    bubbleMaker.ReleaseBubble(removedBubble);
                }

                if (isFloating == false)
                {
                    StageManager.Instance.BubbleManager.ReleaseBubble(removedBubble);
                }
            }
        }
    }

    /// <summary>
    /// ���Ž���� �׸��� �ֺ��� ������ �ִ��� ã�� ����
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public bool GetNearBubbleByPosition(Vector2 pos)
    {
        // �׸����� ������ �������� 6�������� ��������
        //#TODO :: GridPosition + WorldPosition ������ ���̽Ἥ ����ȭ��Ű��
        var gridAxis = GetGridPosition(pos);
        var gridPos = GetWorldPosition(gridAxis.x, gridAxis.y);

        // ������ pos�� � ���ǿ� �ִ��� Ȯ���Ѵ�.
        Vector2[] hexCorners = new Vector2[6];
        for (int i = 0; i < 6; i++)
        {
            float angle_deg = 30 + 60 * i;
            float angle_rad = Mathf.PI / 180 * angle_deg;
            hexCorners[i] = gridPos + new Vector2(bubbleRadius * Mathf.Cos(angle_rad), bubbleRadius * Mathf.Sin(angle_rad));
        }

        int section = -1;
        for (int i = 0; i < 6; i++)
        {
            if(_IsPointInTriangleBarycentric(pos, gridPos, hexCorners[i], hexCorners[(i+1)%6]) == true)
            {
                section = i;
            }
            break;
        }

        // �� ���Ǻ��� ������ �׸��忡 ������ �ִ��� üũ�Ѵ�.
        int startIndex = section - 1 < 0 ? 5 : section - 1;
        int endIndex = (section + 1) % 6;
        int neighborIndex = startIndex;
        for(int index = 0; index < 3; index++)
        {
            // ¦���� Ȧ���Ŀ� ���� ������ �޶���
            var checkGrid = gridAxis.y % 2 == 0 ? gridAxis + evenRowNeighbors[neighborIndex] : gridAxis + oddRowNeighbors[neighborIndex];
            if (GetBubbleAtGrid(checkGrid.x, checkGrid.y) == true)
            {
                return true;
            }
            neighborIndex = (neighborIndex + 1) % 6;
        }

        return false;
    }

    /// <summary>
    /// ���� 2D �ﰢ�� ���ο� �ִ��� Ȯ���մϴ� (�����߽� ��ǥ ���).
    /// </summary>
    /// <param name="p">Ȯ���� ���� ��ġ</param>
    /// <param name="a">�ﰢ���� ù ��° ������</param>
    /// <param name="b">�ﰢ���� �� ��° ������</param>
    /// <param name="c">�ﰢ���� �� ��° ������</param>
    /// <returns>���� �ﰢ�� ���ο� ������ true, �ܺο� ������ false</returns>
    private bool _IsPointInTriangleBarycentric(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        // ������ A�� �������� ���͸� ����ϴ�.
        Vector2 v0 = c - a; // AC ����
        Vector2 v1 = b - a; // AB ����
        Vector2 v2 = p - a; // AP ����

        // �� ������ ������ ����մϴ�.
        // Dot(v, v) = v.magnitude^2
        float dot00 = Vector2.Dot(v0, v0); // AC . AC
        float dot01 = Vector2.Dot(v0, v1); // AC . AB
        float dot02 = Vector2.Dot(v0, v2); // AC . AP
        float dot11 = Vector2.Dot(v1, v1); // AB . AB
        float dot12 = Vector2.Dot(v1, v2); // AB . AP

        // �����߽� ��ǥ�� ����ϱ� ���� �и�
        float invDenom = 1 / (dot00 * dot11 - dot01 * dot01);

        // ��Ÿ (beta) �� ���� (gamma) �����߽� ��ǥ ���
        // ���� (alpha)�� 1 - beta - gamma
        float u = (dot11 * dot02 - dot01 * dot12) * invDenom; // u = gamma
        float v = (dot00 * dot12 - dot01 * dot02) * invDenom; // v = beta

        // ���� �ﰢ�� �ȿ� �������� ��� �����߽� ��ǥ�� 0�� 1 ���̿� �־�� �մϴ�.
        // u >= 0 && v >= 0 �� u, v�� ������� ���� �ǹ��մϴ�.
        // u + v < 1 �� ������ ��ǥ (alpha)�� ������� ���� �ǹ��մϴ�.
        // ��輱�� �ִ� ���� �����Ϸ��� >= �� <= �� ����մϴ�.
        return (u >= 0) && (v >= 0) && (u + v < 1);
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
    /// ���κ��� �ִ��� Ȯ���մϴ�.
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public bool GetHorizontalWallAtPosition(Vector2 pos)
    {
        if (pos.x < MIN_AXIS_X || pos.x > MAX_AXIS_X) return true;
        return false;
    }

    /// <summary>
    /// ���κ��� �ִ��� Ȯ���մϴ�.
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public bool GetVerticalWallAtPosition(Vector2 pos)
    {
        if (pos.y < MIN_AXIS_Y || pos.y > MAX_AXIS_Y) return true;
        return false;
    }

    /// <summary>
    /// �׸��� ��ǥ�� �ش��ϴ� ���� ��ġ�� ��ȯ�մϴ�.
    /// </summary>
    public Vector2 GetWorldPosition(int col, int row)
    {
        float y = row * bubbleRadius * 1.5f;
        float x = row % 2 == 0 ?
            col * bubbleRadius * Mathf.Sqrt(3) :
            col * bubbleRadius * Mathf.Sqrt(3) - bubbleRadius * 0.5f * Mathf.Sqrt(3); // ��ġ�� �κ� ���

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

        float roughRow = worldPosition.y / (bubbleRadius * 1.5f);
        int row = Mathf.RoundToInt(roughRow);

        float roughCol = row % 2 == 0 ?
            worldPosition.x / (bubbleRadius * Mathf.Sqrt(3)) :
            worldPosition.x / (bubbleRadius * Mathf.Sqrt(3)) + 0.5f;
        int col = Mathf.RoundToInt(roughCol);

        return new Vector2Int(col, row);
    }

    // �ʱ� �׸��� ���� ���� (�׽�Ʈ��)
    void Start()
    {
        _StartBubbleGeneration();
    }

    async void _StartBubbleGeneration()
    {
        StageManager.Instance.ProgressGridProcess();
        await _InitBubble();
        StageManager.Instance.CompleteGridProcess();
    }

    private async Task _InitBubble() // async UniTask�� �����Ͽ� await �����ϰ� ��
    {
        if (_bubbleMakerList == null || _bubbleMakerList.Count == 0)
        {
#if UNITY_EDITOR
            Debug.LogWarning("BubbleSpawnManager: BubbleMaker ����Ʈ�� ��� �ֽ��ϴ�.");
#endif
            return;
        }

        if (_cancellationTokenSource != null)
        {
            // ��� ��ū ��������
            CancellationToken token = _cancellationTokenSource.Token;

            // ���� ���� �� ��� ��û�� �־����� Ȯ��
            if (token.IsCancellationRequested)
            {
#if UNITY_EDITOR
                Debug.Log("MakeBubble: ��� ��û ����, ���� ���� �ߴ�.");
#endif
                return;
            }
        }

        foreach (var bubbleMaker in _bubbleMakerList)
        {
            await _InitBubbleByBubbleMaker(bubbleMaker);
        }

#if UNITY_EDITOR
        Debug.Log("��� ���� ����Ŀ�� �ʱ� ���� ������ �Ϸ�Ǿ����ϴ�.");
#endif
    }


    private async Task _InitBubbleByBubbleMaker(BubbleMaker bubbleMaker)
    {
        for (int i = 0; i < bubbleMaker.BubblePathCount; i++)
        {
            bubbleMaker.MakeBubble();
            // �� ���� ���� �� ���� �ð� ���
            await Task.Delay(TimeSpan.FromSeconds(0.1f));
        }
    }

    /// <summary>
    /// ���� ����
    /// </summary>
    async void _RunBubbleRefill()
    {
        StageManager.Instance.ProgressGridProcess();
        await _RefillBubble();
        StageManager.Instance.CompleteGridProcess();
    }

    /// <summary>
    /// ���� ���� ����
    /// </summary>
    /// <returns></returns>
    private async Task _RefillBubble()
    {
        if (_bubbleMakerList == null || _bubbleMakerList.Count == 0)
        {
#if UNITY_EDITOR
            Debug.LogWarning("BubbleSpawnManager: BubbleMaker ����Ʈ�� ��� �ֽ��ϴ�.");
#endif
            return;
        }

        if (_cancellationTokenSource != null)
        {
            // ��� ��ū ��������
            CancellationToken token = _cancellationTokenSource.Token;

            // ���� ���� �� ��� ��û�� �־����� Ȯ��
            if (token.IsCancellationRequested)
            {
#if UNITY_EDITOR
                Debug.Log("MakeBubble: ��� ��û ����, ���� ���� �ߴ�.");
#endif
                return;
            }
        }

        foreach (var bubbleMaker in _bubbleMakerList)
        {
            await bubbleMaker.RefillBubble();
        }
    }

#if UNITY_EDITOR
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
                    float angle_deg = 30 + 60 * i;
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
#endif
}