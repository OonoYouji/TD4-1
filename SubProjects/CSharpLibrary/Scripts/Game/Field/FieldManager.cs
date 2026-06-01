using System.Collections.Generic;

public class FieldManager : MonoScript
{
    // =========================================================
    // パラメーター
    // =========================================================

    [SerializeField] public int rows = 10;
    [SerializeField] public int columns = 10;
    [SerializeField] public float cellSize = 2.0f;
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
    private Entity[,] cellGrid_;
    private FieldCellType[,] cellTypes_;

    // =========================================================
    // ライフサイクル
    // =========================================================

    public override void Initialize()
    {

        // グリッドとタイプの初期化
        cellGrid_ = new Entity[rows, columns];
        cellTypes_ = new FieldCellType[rows, columns];

        // タイプ割り当て
        AssignCellTypes();
        // グリッド生成
        SpawnGrid();
    }

    // =========================================================
    // タイプ割り当て
    // =========================================================

    private void AssignCellTypes()
    {

        // ランダムにタイプを割り当てる
        var rng = new System.Random();
        var types = (FieldCellType[])System.Enum.GetValues(typeof(FieldCellType));

        for (int r = 0; r < rows; r++)
            for (int c = 0; c < columns; c++)
                cellTypes_[r, c] = types[rng.Next(types.Length)];
    }

    // =========================================================
    // グリッド生成
    // =========================================================

    private void SpawnGrid()
    {
        Vector3 origin = transform.position;
        float halfWidth = (columns - 1) * cellSize;
        float halfDepth = (rows - 1) * cellSize;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                FieldCellType type = cellTypes_[r, c];

                Entity cell = ecsGroup.CreateEntity("FieldCell");
                cell.parent = entity;
                cell.transform.position = new Vector3(
                    origin.x + c * cellSize - halfWidth,
                    origin.y,
                    origin.z + r * cellSize - halfDepth
                );

                cell.transform.scale = new Vector3(cellSize * 0.5f, cellHeight, cellSize * 0.5f);

                // タイプに応じてメッシュを切り替え
                var mesh = cell.GetComponent<MeshRenderer>();
                if (mesh != null)
                    mesh.meshPath = GetMeshPath(type);

                var cellScript = cell.GetScript<FieldCell>();
                if (cellScript != null)
                {
                    cellScript.cellType = type;
                    cellScript.gridRow = r;
                    cellScript.gridCol = c;
                }

                cells_.Add(cell);
                cellGrid_[r, c] = cell;
            }
        }
    }

    private string GetMeshPath(FieldCellType type)
    {
        switch (type)
        {
            case FieldCellType.TypeA: return "./Assets/Models/Block/ruins_block1.obj";
            case FieldCellType.TypeB: return "./Assets/Models/Block/ruins_block2.obj";
            case FieldCellType.TypeC: return "./Assets/Models/Block/ruins_block3.obj";
            case FieldCellType.TypeD: return "./Assets/Models/Block/ruins_block4.obj";
            default: return "./Assets/Models/Block/ruins_block1.obj";
        }
    }

    // =========================================================
    // 座標変換ユーティリティ
    // =========================================================

    public Vector3 GridToWorld(int row, int col)
    {
        Vector3 origin = transform.position;
        return new Vector3(
            origin.x + col * cellSize - (columns - 1) * cellSize,
            origin.y,
            origin.z + row * cellSize - (rows - 1) * cellSize
        );
    }

    public bool WorldToGrid(Vector3 worldPos, out int row, out int col)
    {

        // 座標の取得
        Vector3 origin = transform.position;
        // 半分のブロックサイズを計算
        float halfWidth = (columns - 1) * cellSize;
        float halfDepth = (rows - 1) * cellSize;

        // グリッド座標の計算
        col = (int)System.Math.Round((worldPos.x - origin.x + halfWidth) / cellSize);
        row = (int)System.Math.Round((worldPos.z - origin.z + halfDepth) / cellSize);

        // グリッド内かどうかのチェック
        return col >= 0 && col < columns && row >= 0 && row < rows;
    }

    public Entity GetCellAt(int row, int col)
    {

        // 範囲外チェック
        if (row < 0 || row >= rows || col < 0 || col >= columns) {
            return null;
        }

        // セルエンティティを返す
        return cellGrid_[row, col];
    }

    public FieldCellType GetCellType(int row, int col)
    {
        // 引数の範囲チェック
        if (row < 0 || row >= rows || col < 0 || col >= columns) { 
            return FieldCellType.TypeA; 
        }

        // セルタイプを返す
        return cellTypes_[row, col];
    }

    public void TriggerCellAt(Vector3 worldPos)
    {
        int row, col;

        // 変換した結果が範囲外だったら何もしない
        if (!WorldToGrid(worldPos, out row, out col))
        {
            return;
        }

        // セルエンティティを取得
        Entity cellEntity = GetCellAt(row, col);
        if (cellEntity == null) {
            return;
        }

        // セルスクリプトを取得して落下開始
        cellEntity.GetScript<FieldFall>()?.StartFalling();
    }
}
