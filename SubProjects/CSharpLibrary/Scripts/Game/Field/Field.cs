using System.Collections.Generic;

public class Field : MonoScript
{
    // =========================================================
    // パラメーター
    // =========================================================

    [SerializeField] public int rows = 10;
    [SerializeField] public int columns = 10;
    [SerializeField] public float cellSize = 4.0f;
    [SerializeField] public float cellHeight = 1.0f;


    // =========================================================
    // アクセサー
    // =========================================================

    public int Rows => rows;
    public int Columns => columns;
    public float CellSize => cellSize;

    // =========================================================
    // 内部状態
    // =========================================================

    private List<Entity> cells_ = new List<Entity>();

    // =========================================================
    // ライフサイクル
    // =========================================================

    public override void Initialize()
    {
        SpawnGrid();
    }

    // =========================================================
    // グリッド生成
    // =========================================================

    private void SpawnGrid()
    {
        Vector3 origin = transform.position;
        float halfWidth = (columns - 1) * 0.5f * cellSize;
        float halfDepth = (rows - 1) * 0.5f * cellSize;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                Entity cell = ecsGroup.CreateEntity("FieldCell");
                cell.transform.position = new Vector3(
                    origin.x + c * cellSize - halfWidth,
                    origin.y,
                    origin.z + r * cellSize - halfDepth
                );
                cell.transform.scale = new Vector3(cellSize, cellHeight, cellSize);
                cells_.Add(cell);
            }
        }

        Debug.Log("Field: " + rows + "x" + columns + " グリッドを生成しました（計" + (rows * columns) + "マス）");
    }

    // =========================================================
    // 座標変換ユーティリティ
    // =========================================================

    // グリッド座標（row, col）→ ワールド座標
    public Vector3 GridToWorld(int row, int col)
    {
        Vector3 origin = transform.position;
        return new Vector3(
            origin.x + col * cellSize - (columns - 1) * 0.5f * cellSize,
            origin.y,
            origin.z + row * cellSize - (rows - 1) * 0.5f * cellSize
        );
    }

    // ワールド座標 → グリッド座標
    public bool WorldToGrid(Vector3 worldPos, out int row, out int col)
    {
        Vector3 origin = transform.position;
        float halfWidth = (columns - 1) * 0.5f * cellSize;
        float halfDepth = (rows - 1) * 0.5f * cellSize;

        col = (int)System.Math.Round((worldPos.x - origin.x + halfWidth) / cellSize);
        row = (int)System.Math.Round((worldPos.z - origin.z + halfDepth) / cellSize);

        return col >= 0 && col < columns && row >= 0 && row < rows;
    }
}
