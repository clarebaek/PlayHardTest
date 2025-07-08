using UnityEngine;
using System.Collections.Generic;
using Utility.Singleton;

public class GridManager : MonoSingleton<GridManager>
{
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
    public void PlaceBubble(GameObject bubble, int col, int row)
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
        float y = row * bubbleRadius * 2;// Mathf.Sqrt(3) * 0.75f; // ��ġ�� �κ� ���

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
        float roughRow = worldPosition.y / (bubbleRadius * 2);//Mathf.Sqrt(3) * 0.75f);

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
                    // Ȧ�� ���� �� ������ �÷��� ����δ� ��찡 ���� (�׸��� ��� ���߱�)
                    if (r % 2 == 1 && c == gridCols - 1) continue;

                    GameObject newBubble = BubbleManager.Instance.GetBubble();
                    if (newBubble != null)
                    {
                        // ������ Ÿ���� �����Ѵ�.
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