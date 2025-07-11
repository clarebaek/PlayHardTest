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
     /// 육각형 그리드에서 인접한 버블의 상대적 오프셋 좌표 (Odd-r Offset)
     /// 현재 행 (row)이 짝수인지 홀수인지에 따라 다릅니다.
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

    // 그리드 설정
    public int gridRows = 10;
    public int gridCols = 8;
    public float bubbleRadius = 0.5f; // 실제 버블의 반지름

    public float MIN_AXIS_X;
    public float MAX_AXIS_X;
    public float MIN_AXIS_Y;
    public float MAX_AXIS_Y;

    // 그리드에 버블을 저장할 2차원 배열 (GameObject는 버블 인스턴스)
    // null은 빈 칸을 의미
    private GameObject[,] grid;

    // 선택 사항: 특정 그리드 위치에 빠르게 접근하기 위한 Dictionary
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
        // 오브젝트가 활성화될 때마다 새로운 CancellationTokenSource 생성
        // 기존 작업이 아직 실행 중일 경우를 대비하여 새로운 소스를 만듭니다.
        _cancellationTokenSource = new CancellationTokenSource();
    }

    void OnDisable()
    {
        // 오브젝트가 비활성화되거나 파괴될 때
        // 현재 실행 중인 모든 비동기 작업을 취소합니다.
        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel(); // 취소 요청
            _cancellationTokenSource.Dispose(); // CancellationTokenSource 리소스 해제
            _cancellationTokenSource = null;
        }
    }

    /// <summary>
    /// 버블을 특정 그리드 위치에 추가하고 월드 좌표를 설정합니다.
    /// </summary>
    public void PlaceBubble(GameObject bubble, int col, int row, bool isLaunched = false)
    {
        if (col < 0 || col >= gridCols || row < 0 || row >= gridRows)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"GridManager: ({col},{row})는 유효하지 않은 그리드 위치입니다.");
#endif
            return;
        }
        if (grid[col, row] != null)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"GridManager: ({col},{row}) 위치에 이미 버블이 있습니다.");
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

        // 버블의 월드 위치를 그리드에 맞게 설정
        bubble.transform.position = GetWorldPosition(col, row);

        //발사된 버블이라면
        //그리드에 놓여지면서 폭발검사진행
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
    /// 특정 그리드 위치에서 시작하여 같은 색상의 연결된 버블들을 모두 찾습니다. (BFS/DFS)
    /// </summary>
    /// <param name="startGridPos">탐색을 시작할 버블의 그리드 좌표</param>
    /// <returns>연결된 같은 색상 버블 GameObject 리스트</returns>
    private List<GameObject> _FindMatchingBubbles(Vector2Int startGridPos)
    {
        GameObject startBubble = GetBubbleAtGrid(startGridPos.x, startGridPos.y);
        if (startBubble == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"FindMatchingBubbles: 시작 위치 ({startGridPos.x},{startGridPos.y})에 버블이 없습니다.");
#endif
            return new List<GameObject>();
        }

        var startBubbleController = startBubble.GetComponent<Bubble>();
        if (startBubbleController == null || startBubbleController.bubbleType == eBubbleType.NONE)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"FindMatchingBubbles: 시작 버블에 Bubble이 없거나 색상이 없습니다.");
#endif
            return new List<GameObject>();
        }

        var targetColor = startBubbleController.bubbleColor;
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
    /// FindMatchBubble을 바탕으로 주변 n칸만큼 탐색하도록 수정한 함수
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
            Debug.LogWarning($"FindBombRange: 시작 위치 ({startGridPos.x},{startGridPos.y})에 버블이 없습니다.");
#endif
            return new List<GameObject>(); // 빈 리스트 반환
        }

        var startBubbleController = startBubble.GetComponent<Bubble>();
        if (startBubbleController == null || startBubbleController.bubbleType == eBubbleType.NONE)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"FindBombRange: 시작 버블에 Bubble이 없거나 색상이 없습니다.");
#endif
            return new List<GameObject>();
        }

        List<GameObject> matchingBubbles = new List<GameObject>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>(); // 이미 방문한 그리드 위치 기록

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
    /// 발견된 버블들을 터트리고 그리드에서 제거합니다.
    /// </summary>
    /// <param name="bubblesToPop">터트릴 버블 GameObject 리스트</param>
    public void PopBubbles(List<GameObject> bubblesToPop)
    {
        if (bubblesToPop == null || bubblesToPop.Count == 0) return;

        foreach (GameObject bubble in bubblesToPop)
        {
            // 버블 오브젝트의 위치를 그리드 좌표로 변환
            Vector2Int gridPos = GetGridPosition(bubble.transform.position);

            // 그리드에서 버블 제거
            RemoveBubble(gridPos.x, gridPos.y);

            if (bubble.TryGetComponent<Bubble>(out var bubbleScript))
            {
                if(bubbleScript.bubbleType == eBubbleType.FAIRY)
                {
                    //보스에게 데미지
                    StageManager.Instance.DamageBoss(-5);
                }
            }

#if UNITY_EDITOR
            Debug.Log($"버블 터짐: {bubble.name} at {gridPos}");
#endif
        }

        // 공중에 뜬 버블 탐색
        _FindFloatingBubbles();

        // 버블 생성
        _RunBubbleRefill();
    }

    /// <summary>
    /// 터진 버블로 인해 지지점을 잃고 공중에 떠 있는 버블들을 찾아 떨어뜨립니다.
    /// </summary>
    private void _FindFloatingBubbles()
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
                    List<GameObject> connectedGroup = _FindConnectedGroup(new Vector2Int(c, r), visited);

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
            RemoveBubble(gridPos.x, gridPos.y, true, true); // 그리드에서 제거
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
    /// 특정 그리드 위치에서 시작하여 모든 연결된 버블 그룹을 찾습니다. (색상 무관)
    /// FindFloatingBubbles를 위한 헬퍼 함수.
    /// </summary>
    private List<GameObject> _FindConnectedGroup(Vector2Int startGridPos, HashSet<GameObject> visitedBubblesTracker)
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


    /// <summary>
    /// 특정 그리드 위치에서 버블을 제거합니다.
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
                // 맵생성 경로상에서 제거 위함
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
    /// 경로탐색시 그리드 주변에 버블이 있는지 찾기 위함
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public bool GetNearBubbleByPosition(Vector2 pos)
    {
        // 그리드의 원점을 기준으로 6섹션으로 나눈다음
        //#TODO :: GridPosition + WorldPosition 조합을 많이써서 공통화시키기
        var gridAxis = GetGridPosition(pos);
        var gridPos = GetWorldPosition(gridAxis.x, gridAxis.y);

        // 현재의 pos가 어떤 섹션에 있는지 확인한다.
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

        // 각 섹션별로 인접한 그리드에 버블이 있는지 체크한다.
        int startIndex = section - 1 < 0 ? 5 : section - 1;
        int endIndex = (section + 1) % 6;
        int neighborIndex = startIndex;
        for(int index = 0; index < 3; index++)
        {
            // 짝수냐 홀수냐에 따라서 영역이 달라짐
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
    /// 점이 2D 삼각형 내부에 있는지 확인합니다 (무게중심 좌표 방법).
    /// </summary>
    /// <param name="p">확인할 점의 위치</param>
    /// <param name="a">삼각형의 첫 번째 꼭짓점</param>
    /// <param name="b">삼각형의 두 번째 꼭짓점</param>
    /// <param name="c">삼각형의 세 번째 꼭짓점</param>
    /// <returns>점이 삼각형 내부에 있으면 true, 외부에 있으면 false</returns>
    private bool _IsPointInTriangleBarycentric(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        // 꼭짓점 A를 기준으로 벡터를 만듭니다.
        Vector2 v0 = c - a; // AC 벡터
        Vector2 v1 = b - a; // AB 벡터
        Vector2 v2 = p - a; // AP 벡터

        // 각 벡터의 내적을 계산합니다.
        // Dot(v, v) = v.magnitude^2
        float dot00 = Vector2.Dot(v0, v0); // AC . AC
        float dot01 = Vector2.Dot(v0, v1); // AC . AB
        float dot02 = Vector2.Dot(v0, v2); // AC . AP
        float dot11 = Vector2.Dot(v1, v1); // AB . AB
        float dot12 = Vector2.Dot(v1, v2); // AB . AP

        // 무게중심 좌표를 계산하기 위한 분모
        float invDenom = 1 / (dot00 * dot11 - dot01 * dot01);

        // 베타 (beta) 및 감마 (gamma) 무게중심 좌표 계산
        // 알파 (alpha)는 1 - beta - gamma
        float u = (dot11 * dot02 - dot01 * dot12) * invDenom; // u = gamma
        float v = (dot00 * dot12 - dot01 * dot02) * invDenom; // v = beta

        // 점이 삼각형 안에 있으려면 모든 무게중심 좌표가 0과 1 사이에 있어야 합니다.
        // u >= 0 && v >= 0 은 u, v가 양수여야 함을 의미합니다.
        // u + v < 1 은 나머지 좌표 (alpha)가 양수여야 함을 의미합니다.
        // 경계선에 있는 점도 포함하려면 >= 와 <= 를 사용합니다.
        return (u >= 0) && (v >= 0) && (u + v < 1);
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
    /// 가로벽에 있는지 확인합니다.
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public bool GetHorizontalWallAtPosition(Vector2 pos)
    {
        if (pos.x < MIN_AXIS_X || pos.x > MAX_AXIS_X) return true;
        return false;
    }

    /// <summary>
    /// 세로벽에 있는지 확인합니다.
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public bool GetVerticalWallAtPosition(Vector2 pos)
    {
        if (pos.y < MIN_AXIS_Y || pos.y > MAX_AXIS_Y) return true;
        return false;
    }

    /// <summary>
    /// 그리드 좌표에 해당하는 월드 위치를 반환합니다.
    /// </summary>
    public Vector2 GetWorldPosition(int col, int row)
    {
        float y = row * bubbleRadius * 1.5f;
        float x = row % 2 == 0 ?
            col * bubbleRadius * Mathf.Sqrt(3) :
            col * bubbleRadius * Mathf.Sqrt(3) - bubbleRadius * 0.5f * Mathf.Sqrt(3); // 겹치는 부분 고려

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

        float roughRow = worldPosition.y / (bubbleRadius * 1.5f);
        int row = Mathf.RoundToInt(roughRow);

        float roughCol = row % 2 == 0 ?
            worldPosition.x / (bubbleRadius * Mathf.Sqrt(3)) :
            worldPosition.x / (bubbleRadius * Mathf.Sqrt(3)) + 0.5f;
        int col = Mathf.RoundToInt(roughCol);

        return new Vector2Int(col, row);
    }

    // 초기 그리드 버블 설정 (테스트용)
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

    private async Task _InitBubble() // async UniTask로 변경하여 await 가능하게 함
    {
        if (_bubbleMakerList == null || _bubbleMakerList.Count == 0)
        {
#if UNITY_EDITOR
            Debug.LogWarning("BubbleSpawnManager: BubbleMaker 리스트가 비어 있습니다.");
#endif
            return;
        }

        if (_cancellationTokenSource != null)
        {
            // 취소 토큰 가져오기
            CancellationToken token = _cancellationTokenSource.Token;

            // 버블 생성 전 취소 요청이 있었는지 확인
            if (token.IsCancellationRequested)
            {
#if UNITY_EDITOR
                Debug.Log("MakeBubble: 취소 요청 감지, 버블 생성 중단.");
#endif
                return;
            }
        }

        foreach (var bubbleMaker in _bubbleMakerList)
        {
            await _InitBubbleByBubbleMaker(bubbleMaker);
        }

#if UNITY_EDITOR
        Debug.Log("모든 버블 메이커의 초기 버블 생성이 완료되었습니다.");
#endif
    }


    private async Task _InitBubbleByBubbleMaker(BubbleMaker bubbleMaker)
    {
        for (int i = 0; i < bubbleMaker.BubblePathCount; i++)
        {
            bubbleMaker.MakeBubble();
            // 각 버블 생성 후 일정 시간 대기
            await Task.Delay(TimeSpan.FromSeconds(0.1f));
        }
    }

    /// <summary>
    /// 버블 리필
    /// </summary>
    async void _RunBubbleRefill()
    {
        StageManager.Instance.ProgressGridProcess();
        await _RefillBubble();
        StageManager.Instance.CompleteGridProcess();
    }

    /// <summary>
    /// 버블 리필 로직
    /// </summary>
    /// <returns></returns>
    private async Task _RefillBubble()
    {
        if (_bubbleMakerList == null || _bubbleMakerList.Count == 0)
        {
#if UNITY_EDITOR
            Debug.LogWarning("BubbleSpawnManager: BubbleMaker 리스트가 비어 있습니다.");
#endif
            return;
        }

        if (_cancellationTokenSource != null)
        {
            // 취소 토큰 가져오기
            CancellationToken token = _cancellationTokenSource.Token;

            // 버블 생성 전 취소 요청이 있었는지 확인
            if (token.IsCancellationRequested)
            {
#if UNITY_EDITOR
                Debug.Log("MakeBubble: 취소 요청 감지, 버블 생성 중단.");
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
    /// 유니티 에디터의 씬 뷰에 그리드 격자를 그립니다.
    /// </summary>
    void OnDrawGizmos()
    {
        // 기즈모를 그릴 색상 설정
        Gizmos.color = Color.cyan; // 하늘색으로 그리드 라인 표시

        // 그리드의 각 셀을 순회하며 육각형 테두리 그리기
        for (int r = 0; r < gridRows; r++)
        {
            for (int c = 0; c < gridCols; c++)
            {
                // 해당 그리드 셀의 월드 중심 위치를 계산
                Vector2 cellCenter = GetWorldPosition(c, r);

                // Gizmos.DrawWireSphere를 사용하여 버블이 놓일 위치를 원으로 표시
                // 육각형 그리드이므로, 각 셀의 중심에 버블이 놓인다고 가정하고 원을 그립니다.
                Gizmos.DrawWireSphere(cellCenter, bubbleRadius); // 버블의 반지름 크기로 원을 그림

                // (선택 사항) 그리드 좌표 텍스트 표시 - Gizmos.DrawSphere로 작은 점을 찍고, Handles.Label 사용
                // Handles는 UnityEditor 네임스페이스에 속하므로, 에디터 스크립트에서만 사용 가능합니다.
                // 일반 스크립트에서는 컴파일 오류가 발생합니다.
                // 만약 에디터 확장 스크립트로 별도 구현한다면 유용합니다.
                // using UnityEditor;
                // Handles.Label(cellCenter + Vector2.up * bubbleRadius * 0.5f, $"({c},{r})");

                // (선택 사항) 육각형 셀의 테두리를 직접 그리기 (더 정확한 시각화)
                // 육각형의 6개 꼭짓점 계산
                Vector3[] hexCorners = new Vector3[6];
                for (int i = 0; i < 6; i++)
                {
                    float angle_deg = 30 + 60 * i;
                    float angle_rad = Mathf.PI / 180 * angle_deg;
                    hexCorners[i] = cellCenter + new Vector2(bubbleRadius * Mathf.Cos(angle_rad), bubbleRadius * Mathf.Sin(angle_rad));
                }

                // 6개 선분 그리기
                for (int i = 0; i < 6; i++)
                {
                    Gizmos.DrawLine(hexCorners[i], hexCorners[(i + 1) % 6]);
                }
            }
        }

        // 현재 버블 발사 지점 표시 (선택 사항)
        // BubbleLauncher에서 launchPoint를 참조하고 있다면 여기서도 그릴 수 있습니다.
        // 또는 BubbleLauncher.cs의 OnDrawGizmos에서 그려도 됩니다.
        // if (GetComponent<BubbleLauncher>() != null && GetComponent<BubbleLauncher>().launchPoint != null)
        // {
        //     Gizmos.color = Color.red;
        //     Gizmos.DrawSphere(GetComponent<BubbleLauncher>().launchPoint.position, 0.2f);
        // }
    }
#endif
}