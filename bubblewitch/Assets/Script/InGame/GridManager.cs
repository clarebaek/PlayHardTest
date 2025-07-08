using UnityEngine;
using System.Collections.Generic;
using Utility.Singleton;

public class GridManager : MonoSingleton<GridManager>
{
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
    public void PlaceBubble(GameObject bubble, int col, int row)
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
        float y = row * bubbleRadius * 2;// Mathf.Sqrt(3) * 0.75f; // 겹치는 부분 고려

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
        float roughRow = worldPosition.y / (bubbleRadius * 2);//Mathf.Sqrt(3) * 0.75f);

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
                    // 홀수 행일 때 마지막 컬럼은 비워두는 경우가 많음 (그리드 모양 맞추기)
                    if (r % 2 == 1 && c == gridCols - 1) continue;

                    GameObject newBubble = BubbleManager.Instance.GetBubble();
                    if (newBubble != null)
                    {
                        // 버블의 타입을 설정한다.
                        if (newBubble.TryGetComponent<Bubble>(out var bubbleScript))
                        {
                            bubbleScript.SetType((eBubbleType)Random.Range((int)eBubbleType.NORMAL_START, (int)eBubbleType.NORMAL_END));
                        }
                        PlaceBubble(newBubble, c + offsetX, r + offsetY);
                    }
                }
            }
        }
    }
}