using UnityEngine;
using System.Collections.Generic;
using Utility.Singleton;
using System.Linq;
using System.Collections;

public class GridManager : MonoSingleton<GridManager>
{
    /// <summary>
     /// 육각형 그리드에서 인접한 버블의 상대적 오프셋 좌표 (Odd-r Offset)
     /// 현재 행 (row)이 짝수인지 홀수인지에 따라 다릅니다.
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

    // 그리드 설정
    public int gridRows = 10;
    public int gridCols = 8;
    public float bubbleRadius = 0.5f; // 실제 버블의 반지름

    // 그리드에 버블을 저장할 2차원 배열 (GameObject는 버블 인스턴스)
    // null은 빈 칸을 의미
    private GameObject[,] grid;

    // 선택 사항: 특정 그리드 위치에 빠르게 접근하기 위한 Dictionary
    private Dictionary<Vector2Int, GameObject> activeBubbles;

    void Awake()
    {
        grid = new GameObject[gridCols, gridRows];
        activeBubbles = new Dictionary<Vector2Int, GameObject>();
    }

    /// <summary>
    /// 버블을 특정 그리드 위치에 추가하고 월드 좌표를 설정합니다.
    /// </summary>
    public void PlaceBubble(GameObject bubble, int col, int row, bool isLaunched = false)
    {
        if (col < 0 || col >= gridCols || row < 0 || row >= gridRows)
        {
            Debug.LogWarning($"GridManager: ({col},{row})는 유효하지 않은 그리드 위치입니다.");
            return;
        }
        if (grid[col, row] != null)
        {
            Debug.LogWarning($"GridManager: ({col},{row}) 위치에 이미 버블이 있습니다.");
            return;
        }

        grid[col, row] = bubble;
        activeBubbles[new Vector2Int(col, row)] = bubble;

        // 버블의 월드 위치를 그리드에 맞게 설정 (버블의 Rigidbody2D는 Kinematic으로 설정)
        bubble.transform.position = GetWorldPosition(col, row);

        // 버블이 그리드에 붙었으므로 Rigidbody2D를 Kinematic으로 변경하고 속도 초기화
        if (bubble.TryGetComponent<Rigidbody2D>(out var rigidbody))
        {
            rigidbody.linearVelocity = Vector2.zero;
            rigidbody.angularVelocity = 0f;
            rigidbody.bodyType = RigidbodyType2D.Kinematic;
            rigidbody.simulated = true; // 시뮬레이션은 계속 활성화
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
    /// 특정 그리드 위치에서 시작하여 같은 색상의 연결된 버블들을 모두 찾습니다. (BFS/DFS)
    /// </summary>
    /// <param name="startGridPos">탐색을 시작할 버블의 그리드 좌표</param>
    /// <returns>연결된 같은 색상 버블 GameObject 리스트</returns>
    public List<GameObject> FindMatchingBubbles(Vector2Int startGridPos)
    {
        GameObject startBubble = GetBubbleAtGrid(startGridPos.x, startGridPos.y);
        if (startBubble == null)
        {
            // Debug.LogWarning($"FindMatchingBubbles: 시작 위치 ({startGridPos.x},{startGridPos.y})에 버블이 없습니다.");
            return new List<GameObject>(); // 빈 리스트 반환
        }

        var startBubbleController = startBubble.GetComponent<Bubble>();
        if (startBubbleController == null || startBubbleController.bubbleType == eBubbleType.NONE)
        {
            // Debug.LogWarning($"FindMatchingBubbles: 시작 버블에 BubbleController가 없거나 색상이 없습니다.");
            return new List<GameObject>();
        }

        var targetColor = startBubbleController.bubbleType;
        List<GameObject> matchingBubbles = new List<GameObject>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>(); // 이미 방문한 그리드 위치 기록

        queue.Enqueue(startGridPos);
        visited.Add(startGridPos);
        matchingBubbles.Add(startBubble);

        while (queue.Count > 0)
        {
            Vector2Int currentGridPos = queue.Dequeue();

            // 현재 버블의 인접한 6방향 버블 탐색
            Vector2Int[] offsets = (currentGridPos.y % 2 == 0) ? evenRowNeighbors : oddRowNeighbors;

            foreach (Vector2Int offset in offsets)
            {
                int neighborCol = currentGridPos.x + offset.x;
                int neighborRow = currentGridPos.y + offset.y;
                Vector2Int neighborGridPos = new Vector2Int(neighborCol, neighborRow);

                // 유효한 그리드 범위 내에 있고, 아직 방문하지 않았는지 확인
                if (neighborCol >= 0 && neighborCol < gridCols &&
                    neighborRow >= 0 && neighborRow < gridRows &&
                    !visited.Contains(neighborGridPos))
                {
                    GameObject neighborBubble = GetBubbleAtGrid(neighborCol, neighborRow);
                    if (neighborBubble != null)
                    {
                        var neighborController = neighborBubble.GetComponent<Bubble>();
                        // 이웃 버블이 존재하고, BubbleController가 있으며, 색상이 같으면
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
    /// 발견된 버블들을 터트리고 그리드에서 제거합니다.
    /// </summary>
    /// <param name="bubblesToPop">터트릴 버블 GameObject 리스트</param>
    public void PopBubbles(List<GameObject> bubblesToPop)
    {
        if (bubblesToPop == null || bubblesToPop.Count == 0) return;

        // 중복 제거 (혹시 같은 버블이 여러 번 리스트에 들어갈 경우 대비)
        bubblesToPop = bubblesToPop.Distinct().ToList();

        foreach (GameObject bubble in bubblesToPop)
        {
            // 버블 오브젝트의 위치를 그리드 좌표로 변환
            Vector2Int gridPos = GetGridPosition(bubble.transform.position);

            // 그리드에서 버블 제거
            RemoveBubble(gridPos.x, gridPos.y);

            // TODO: 버블 터지는 시각 효과/사운드 재생 (예: particle system, audio source)
            // Debug.Log($"버블 터짐: {bubble.name} at {gridPos}");

            // 버블을 오브젝트 풀로 반환
            BubbleManager.Instance.ReleaseBubble(bubble);
        }

        // TODO: 버블이 터진 후, 공중에 떠 있는 (지지되지 않는) 버블 찾기 로직 호출
        FindFloatingBubbles();
    }

    /// <summary>
    /// 터진 버블로 인해 지지점을 잃고 공중에 떠 있는 버블들을 찾아 떨어뜨립니다.
    /// </summary>
    public void FindFloatingBubbles()
    {
        // 최상단 줄 (gridRows - 1)부터 시작하여 모든 버블을 순회합니다.
        // 연결된 버블 그룹을 찾고, 그 그룹이 상단 벽(가장 높은 줄)에 닿아있는지 확인합니다.
        // 닿아있지 않다면 떨어뜨립니다.

        HashSet<GameObject> visited = new HashSet<GameObject>();
        List<GameObject> floatingBubbles = new List<GameObject>();

        // 그리드 전체를 순회하면서 모든 활성 버블을 확인
        for (int r = gridRows - 1; r >= 0; r--) // 위에서 아래로
        {
            for (int c = 0; c < gridCols; c++)
            {
                GameObject currentBubble = GetBubbleAtGrid(c, r);
                if (currentBubble != null && !visited.Contains(currentBubble))
                {
                    // 이 버블이 속한 연결된 그룹을 찾습니다 (색상 무관)
                    List<GameObject> connectedGroup = FindConnectedGroup(new Vector2Int(c, r), visited);

                    // 이 그룹이 공중에 떠 있는지 확인
                    bool isFloating = true;
                    foreach (GameObject bubbleInGroup in connectedGroup)
                    {
                        Vector2Int groupBubblePos = GetGridPosition(bubbleInGroup.transform.position);
                        if (groupBubblePos.y >= gridRows - 1) // 가장 높은 줄에 닿아있는 버블이 그룹 내에 있다면
                        {
                            isFloating = false; // 이 그룹은 공중에 떠 있지 않음
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

        // 찾은 모든 떠 있는 버블들을 떨어뜨립니다.
        foreach (GameObject floatingBubble in floatingBubbles)
        {
            Vector2Int gridPos = GetGridPosition(floatingBubble.transform.position);
            RemoveBubble(gridPos.x, gridPos.y); // 그리드에서 제거

            Rigidbody2D rb = floatingBubble.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic; // 물리 시뮬레이션 다시 활성화
                rb.simulated = true;
                rb.gravityScale = 1.0f; // 중력 적용하여 떨어뜨림
                // TODO: 떨어지는 애니메이션/사운드 등 추가
                // N초 후 풀로 반환하는 코루틴 시작
                StartCoroutine(DelayedReturnToPool(floatingBubble, 2f));
            }
            else
            {
                BubbleManager.Instance.ReleaseBubble(floatingBubble);
            }
        }
    }

    /// <summary>
    /// 특정 그리드 위치에서 시작하여 모든 연결된 버블 그룹을 찾습니다. (색상 무관)
    /// FindFloatingBubbles를 위한 헬퍼 함수.
    /// </summary>
    private List<GameObject> FindConnectedGroup(Vector2Int startGridPos, HashSet<GameObject> visitedBubblesTracker)
    {
        List<GameObject> connectedGroup = new List<GameObject>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        GameObject startBubble = GetBubbleAtGrid(startGridPos.x, startGridPos.y);
        if (startBubble == null || visitedBubblesTracker.Contains(startBubble)) return connectedGroup;

        queue.Enqueue(startGridPos);
        visitedBubblesTracker.Add(startBubble); // 전체 탐색에서 방문 처리
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
    /// 특정 그리드 위치에서 버블을 제거합니다.
    /// </summary>
    public void RemoveBubble(int col, int row)
    {
        if (col < 0 || col >= gridCols || row < 0 || row >= gridRows) return;

        if (grid[col, row] != null)
        {
            GameObject removedBubble = grid[col, row];
            grid[col, row] = null;
            activeBubbles.Remove(new Vector2Int(col, row));
            // TODO: 제거된 버블을 풀로 반환하거나 파괴하는 로직
            BubbleManager.Instance.ReleaseBubble(removedBubble);
        }
    }

    /// <summary>
    /// 특정 그리드 위치에 버블이 있는지 확인합니다.
    /// </summary>
    public GameObject GetBubbleAtGrid(int col, int row)
    {
        if (col < 0 || col >= gridCols || row < 0 || row >= gridRows) return null;
        return grid[col, row];
    }

    /// <summary>
    /// 그리드 좌표에 해당하는 월드 위치를 반환합니다.
    /// </summary>
    public Vector2 GetWorldPosition(int col, int row)
    {
        float x = col * bubbleRadius * 2;
        float y = row * bubbleRadius * Mathf.Sqrt(3); // 겹치는 부분 고려

        // 홀수 행은 X축으로 반 칸 이동
        if (row % 2 == 1)
        {
            x += bubbleRadius;
        }
        return new Vector2(x, y);
    }

    /// <summary>
    /// 월드 위치에서 가장 가까운 그리드 셀 좌표를 찾습니다.
    /// 이 부분은 육각형 그리드 좌표계 변환에서 가장 까다로운 부분입니다.
    /// </summary>
    public Vector2Int GetGridPosition(Vector2 worldPosition)
    {
        // RedBlobGames Hex Grid 공식 활용 (Offset Coordinates)
        // https://www.redblobgames.com/grids/hexagons/

        // 대략적인 그리드 x, y 계산 (반올림 전)
        float roughCol = worldPosition.x / (bubbleRadius * 2);
        float roughRow = worldPosition.y / (bubbleRadius * Mathf.Sqrt(3));

        // 짝수 또는 홀수 행에 따른 보정
        // 이 부분은 정확한 육각형 좌표 변환 로직이 들어가야 합니다.
        // 예를 들어, axial/cube coordinate로 변환 후 반올림, 다시 offset으로 변환하는 방식이 더 견고합니다.
        // 여기서는 개념만 제시하고 실제 구현은 더 복잡할 수 있습니다.

        // 임시 반환값 (실제 로직 필요)
        int col = Mathf.RoundToInt(roughCol);
        int row = Mathf.RoundToInt(roughRow);

        // 홀수 행 보정 (다시 역으로 적용)
        if (row % 2 == 1)
        {
            col = Mathf.RoundToInt((worldPosition.x - bubbleRadius) / (bubbleRadius * 2));
        }

        return new Vector2Int(col, row);
    }

    // 초기 그리드 버블 설정 (테스트용)
    void Start()
    {
        // 상단에 미리 버블 채우기
        // (GridManager.Instance.PlaceBubble 호출 시 ObjectPoolManager 필요)
        // ObjectPoolManager가 싱글톤이라면 다음과 같이 사용
        if (BubbleManager.Instance != null)
        {
            for (int r = 0; r < 4; r++) // 예시로 4줄만 채움
            {
                for (int c = 0; c < gridCols; c++)
                {
                    int convertC = c + offsetX;
                    int convertR = r + offsetY;

                    // 홀수 행일 때 마지막 컬럼은 비워두는 경우가 많음 (그리드 모양 맞추기)
                    if (convertR % 2 == 1 && convertC == gridCols - 1) continue;

                    GameObject newBubble = BubbleManager.Instance.GetBubble();
                    if (newBubble != null)
                    {
                        // 버블의 타입을 설정한다.
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